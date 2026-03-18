using System.Net.Http.Headers;
using System.Text.Json;

namespace WhatsHappening.Services;

public sealed class GitHubService
{
    private readonly HttpClient _http;
    private readonly FirebaseAuthService _auth;

    public GitHubService(HttpClient http, FirebaseAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public static bool TryParseGitHubUrl(string input, out string url, out string owner, out string repo, out int number, out string type)
    {
        url = owner = repo = type = string.Empty;
        number = 0;

        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 4)
            return false;

        owner = segments[0];
        repo = segments[1];
        var typePath = segments[2].ToLowerInvariant();
        if (!int.TryParse(segments[3], out number))
            return false;

        type = typePath switch
        {
            "issues" => "issue",
            "pull" => "pull",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(type))
            return false;

        url = $"https://github.com/{owner}/{repo}/{typePath}/{number}";
        return true;
    }

    public async Task<(string Title, string State, string? Body, List<GitHubLabel> Labels)?> FetchIssuePrDetailsAsync(string owner, string repo, int number, string type)
    {
        var endpoint = type == "pull"
            ? $"https://api.github.com/repos/{owner}/{repo}/pulls/{number}"
            : $"https://api.github.com/repos/{owner}/{repo}/issues/{number}";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("WhatsHappening/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        // Add token if available (needed for private repos, helps with rate limits)
        var token = await _auth.GetGitHubTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = doc.RootElement;
        var title = root.GetProperty("title").GetString() ?? "Untitled";
        var state = root.GetProperty("state").GetString() ?? "unknown";
        // GitHub API returns "closed" for both closed and merged PRs
        if (type == "pull" && state == "closed" && root.TryGetProperty("merged", out var mergedEl) && mergedEl.GetBoolean())
            state = "merged";
        var body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        if (body is not null && body.Length > 500)
            body = body[..500] + "…";

        var labels = new List<GitHubLabel>();
        if (root.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var lbl in labelsEl.EnumerateArray())
            {
                var name = lbl.TryGetProperty("name", out var n) ? n.GetString() : null;
                var color = lbl.TryGetProperty("color", out var c) ? c.GetString() : "636c76";
                if (name is not null)
                    labels.Add(new GitHubLabel { Name = name, Color = color ?? "636c76" });
            }
        }

        return (title, state, body, labels);
    }

    public async Task<(int Number, string Repo, string State)?> FetchLinkedPrAsync(string owner, string repo, int issueNumber)
    {
        var endpoint = $"https://api.github.com/repos/{owner}/{repo}/issues/{issueNumber}/timeline";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("WhatsHappening/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        var token = await _auth.GetGitHubTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        // Walk timeline events looking for cross-referenced PRs
        var candidates = new List<(int Number, string Repo, string State)>();
        foreach (var ev in doc.RootElement.EnumerateArray())
        {
            if (ev.TryGetProperty("event", out var eventType) && eventType.GetString() == "cross-referenced"
                && ev.TryGetProperty("source", out var source)
                && source.TryGetProperty("issue", out var issue)
                && issue.TryGetProperty("pull_request", out _))
            {
                var prNumber = issue.GetProperty("number").GetInt32();
                var prState = issue.GetProperty("state").GetString() ?? "unknown";
                if (prState == "closed"
                    && issue.TryGetProperty("pull_request", out var prEl)
                    && prEl.TryGetProperty("merged_at", out var mergedAt)
                    && mergedAt.ValueKind != JsonValueKind.Null)
                {
                    prState = "merged";
                }
                var prRepoName = issue.TryGetProperty("repository", out var repoEl)
                    ? repoEl.GetProperty("full_name").GetString() ?? $"{owner}/{repo}"
                    : $"{owner}/{repo}";
                candidates.Add((prNumber, prRepoName, prState));
            }
        }

        if (candidates.Count == 0) return null;

        // Prefer open PRs over merged/closed
        return candidates.FirstOrDefault(c => c.State == "open") is var open && open != default
            ? open
            : candidates[^1];
    }
}
