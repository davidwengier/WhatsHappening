using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace WhatsHappening.Services;

public sealed partial class AzureDevOpsService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly FirestoreService _firestore;
    private string? _cachedPat;

    private static readonly Regex AzDoWorkItemUrl = AzDoWorkItemRegex();
    private static readonly Regex AzDoWorkItemUrlLegacy = AzDoWorkItemLegacyRegex();
    private static readonly Regex AzDoPrUrl = AzDoPrRegex();
    private static readonly Regex AzDoPrUrlLegacy = AzDoPrLegacyRegex();

    public AzureDevOpsService(HttpClient http, IJSRuntime js, FirestoreService firestore)
    {
        _http = http;
        _js = js;
        _firestore = firestore;
    }

    public static bool TryParseUrl(string input, out string org, out string project, out int id, out string type, out string? repo)
    {
        org = project = type = string.Empty;
        repo = null;
        id = 0;

        var trimmed = input.Trim();

        // Work item URLs
        var match = AzDoWorkItemUrl.Match(trimmed);
        if (match.Success)
        {
            org = match.Groups["org"].Value;
            project = match.Groups["project"].Value;
            id = int.Parse(match.Groups["id"].Value);
            type = "workitem";
            return true;
        }

        match = AzDoWorkItemUrlLegacy.Match(trimmed);
        if (match.Success)
        {
            org = match.Groups["org"].Value;
            project = match.Groups["project"].Value;
            id = int.Parse(match.Groups["id"].Value);
            type = "workitem";
            return true;
        }

        // PR URLs
        match = AzDoPrUrl.Match(trimmed);
        if (match.Success)
        {
            org = match.Groups["org"].Value;
            project = match.Groups["project"].Value;
            id = int.Parse(match.Groups["id"].Value);
            repo = match.Groups["repo"].Value;
            type = "pullrequest";
            return true;
        }

        match = AzDoPrUrlLegacy.Match(trimmed);
        if (match.Success)
        {
            org = match.Groups["org"].Value;
            project = match.Groups["project"].Value;
            id = int.Parse(match.Groups["id"].Value);
            repo = match.Groups["repo"].Value;
            type = "pullrequest";
            return true;
        }

        return false;
    }

    public async Task<string?> GetPatAsync()
    {
        if (_cachedPat is not null) return _cachedPat;
        try
        {
            _cachedPat = await _firestore.GetSettingAsync("azdo_pat");
        }
        catch
        {
            // Firestore not available, fall back to sessionStorage
            _cachedPat = await _js.InvokeAsync<string?>("sessionStorage.getItem", "azdo_pat");
        }
        return _cachedPat;
    }

    public async Task SetPatAsync(string pat)
    {
        _cachedPat = pat;
        try
        {
            await _firestore.SetSettingAsync("azdo_pat", pat);
        }
        catch
        {
            // Fallback to sessionStorage if Firestore unavailable
            await _js.InvokeVoidAsync("sessionStorage.setItem", "azdo_pat", pat);
        }
    }

    public async Task<bool> HasPatAsync()
        => !string.IsNullOrEmpty(await GetPatAsync());

    public async Task<(string Title, string State, string Type, string? Body, string? AssignedTo)?> FetchWorkItemAsync(
        string org, string project, int id)
    {
        var pat = await GetPatAsync();
        if (pat is null) return null;

        var endpoint = $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{id}?api-version=7.1&$expand=relations";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuth(request, pat);

        using var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new AzDoAuthException();
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var fields = doc.RootElement.GetProperty("fields");

        var title = fields.GetProperty("System.Title").GetString() ?? "Untitled";
        var state = fields.GetProperty("System.State").GetString() ?? "unknown";
        var typeName = fields.GetProperty("System.WorkItemType").GetString() ?? "Task";
        var body = fields.TryGetProperty("System.Description", out var descEl) ? descEl.GetString() : null;

        // Strip HTML tags from description
        if (body is not null)
        {
            body = HtmlTagRegex().Replace(body, "").Trim();
            if (body.Length > 500) body = body[..500] + "…";
        }

        string? assignedTo = null;
        if (fields.TryGetProperty("System.AssignedTo", out var assignEl)
            && assignEl.ValueKind == JsonValueKind.Object
            && assignEl.TryGetProperty("displayName", out var nameEl))
        {
            assignedTo = nameEl.GetString();
        }

        return (title, state, typeName, body, assignedTo);
    }

    public async Task<(string Title, string State, string? Repo)?> FetchPullRequestAsync(
        string org, string project, int id, string repo)
    {
        var pat = await GetPatAsync();
        if (pat is null) return null;

        var endpoint = $"https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/pullrequests/{id}?api-version=7.1";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuth(request, pat);

        using var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new AzDoAuthException();
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        var title = root.GetProperty("title").GetString() ?? "Untitled";
        var status = root.GetProperty("status").GetString() ?? "unknown";
        // AzDo PR statuses: "active", "completed", "abandoned"
        var repoName = root.TryGetProperty("repository", out var repoEl)
            && repoEl.TryGetProperty("name", out var rn)
            ? rn.GetString() : repo;

        return (title, status, repoName);
    }

    /// <summary>Parse linked PRs from work item relations.</summary>
    public async Task<(int Id, string Repo, string State)?> FetchLinkedPrFromWorkItemAsync(
        string org, string project, int workItemId)
    {
        var pat = await GetPatAsync();
        if (pat is null) return null;

        // Re-fetch with relations
        var endpoint = $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{workItemId}?api-version=7.1&$expand=relations";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuth(request, pat);

        using var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new AzDoAuthException();
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        if (!root.TryGetProperty("relations", out var relations) || relations.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var rel in relations.EnumerateArray())
        {
            var relType = rel.TryGetProperty("rel", out var rt) ? rt.GetString() : null;
            if (relType != "ArtifactLink") continue;

            var url = rel.TryGetProperty("url", out var u) ? u.GetString() : null;
            if (url is null || !url.Contains("PullRequestId", StringComparison.OrdinalIgnoreCase)) continue;

            // URL format: vstfs:///Git/PullRequestId/{projectId}%2F{repoId}%2F{prId}
            var segments = url.Split('/');
            var lastSegment = Uri.UnescapeDataString(segments[^1]);
            var parts = lastSegment.Split('%');
            if (parts.Length < 3) continue;

            if (int.TryParse(parts[^1], out var prId))
            {
                // Fetch PR details to get repo name and status
                // We need to search across repos, use the project-level PR endpoint
                var prDetails = await FetchPrByIdAsync(org, project, prId, pat);
                if (prDetails is not null)
                    return (prId, prDetails.Value.Repo, prDetails.Value.State);
            }
        }

        return null;
    }

    private async Task<(string Repo, string State)?> FetchPrByIdAsync(string org, string project, int prId, string pat)
    {
        var endpoint = $"https://dev.azure.com/{org}/{project}/_apis/git/pullrequests/{prId}?api-version=7.1";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuth(request, pat);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;

        var status = root.GetProperty("status").GetString() ?? "unknown";
        var repoName = root.TryGetProperty("repository", out var repoEl)
            && repoEl.TryGetProperty("name", out var rn)
            ? rn.GetString() ?? "unknown" : "unknown";

        return (repoName, status);
    }

    private static void AddAuth(HttpRequestMessage request, string pat)
    {
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    [GeneratedRegex(@"https?://dev\.azure\.com/(?<org>[^/]+)/(?<project>[^/]+)/_workitems/edit/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AzDoWorkItemRegex();

    [GeneratedRegex(@"https?://(?<org>[^.]+)\.visualstudio\.com/(?<project>[^/]+)/_workitems/edit/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AzDoWorkItemLegacyRegex();

    [GeneratedRegex(@"https?://dev\.azure\.com/(?<org>[^/]+)/(?<project>[^/]+)/_git/(?<repo>[^/]+)/pullrequest/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AzDoPrRegex();

    [GeneratedRegex(@"https?://(?<org>[^.]+)\.visualstudio\.com/(?<project>[^/]+)/_git/(?<repo>[^/]+)/pullrequest/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AzDoPrLegacyRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}

public class AzDoAuthException : Exception
{
    public AzDoAuthException() : base("Azure DevOps authentication failed. Your PAT may have expired.") { }
}
