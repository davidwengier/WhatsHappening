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

    public async Task<(string Title, string State)?> FetchIssuePrDetailsAsync(string owner, string repo, int number, string type)
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
        return (title, state);
    }

    [GeneratedRegex(@"https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/(?<type>issues|pull)/(?<number>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubUrlRegex();
}
