namespace WhatsHappening;

public sealed class TodoItem
{
    public string? Id { get; set; }
    public string Title { get; set; }
    public bool IsComplete { get; set; }
    public int Order { get; set; }
    public string? GroupId { get; set; }
    public long? CompletedAt { get; set; }

    // GitHub issue/PR link
    public string? GitHubUrl { get; set; }
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public int? GitHubNumber { get; set; }
    public string? GitHubType { get; set; } // "issue" or "pull"
    public string? GitHubState { get; set; }
    public string? GitHubBody { get; set; }
    public List<GitHubLabel>? GitHubLabels { get; set; }

    public bool IsGitHubLinked => GitHubUrl is not null;

    public TodoItem() => Title = string.Empty;

    public TodoItem(string title) => Title = title;
}

public sealed class GitHubLabel
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "636c76"; // hex without #
}