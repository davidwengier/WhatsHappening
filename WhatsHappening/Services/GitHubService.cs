using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WhatsHappening.Services;

public sealed partial class GitHubService
{
    private readonly HttpClient _http;
    private readonly FirebaseAuthService _auth;

    private static readonly Regex GitHubUrlPattern = GitHubUrlRegex();

    public GitHubService(HttpClient http, FirebaseAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public static bool TryParseGitHubUrl(string input, out string owner, out string repo, out int number, out string type)
    {
        owner = repo = type = string.Empty;
        number = 0;

        var match = GitHubUrlPattern.Match(input.Trim());
        if (!match.Success) return false;

        owner = match.Groups["owner"].Value;
        repo = match.Groups["repo"].Value;
        number = int.Parse(match.Groups["number"].Value);
        type = match.Groups["type"].Value switch
        {
            "issues" => "issue",
            "pull" => "pull",
            _ => "issue"
        };
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

    [GeneratedRegex(@"https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/(?<type>issues|pull)/(?<number>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubUrlRegex();
}
