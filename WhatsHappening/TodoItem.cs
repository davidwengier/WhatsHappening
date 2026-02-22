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

    // Linked PR (for issues only)
    public int? LinkedPrNumber { get; set; }
    public string? LinkedPrRepo { get; set; }
    public string? LinkedPrState { get; set; }

    // Azure DevOps work item/PR link
    public string? AzDoUrl { get; set; }
    public string? AzDoOrg { get; set; }
    public string? AzDoProject { get; set; }
    public int? AzDoId { get; set; }
    public string? AzDoType { get; set; }    // "Bug", "Task", "User Story", "Feature", "Pull Request", etc.
    public string? AzDoState { get; set; }   // "New", "Active", "Resolved", "Closed", etc.
    public string? AzDoBody { get; set; }
    public string? AzDoAssignedTo { get; set; }
    public string? AzDoRepo { get; set; }    // For PRs: repo name
    public int? AzDoLinkedPrId { get; set; }
    public string? AzDoLinkedPrState { get; set; }
    public string? AzDoLinkedPrRepo { get; set; }

    public bool IsGitHubLinked => GitHubUrl is not null;
    public bool IsAzDoLinked => AzDoUrl is not null;
    public bool IsExternalLinked => IsGitHubLinked || IsAzDoLinked;

    public TodoItem() => Title = string.Empty;

    public TodoItem(string title) => Title = title;
}

public sealed class GitHubLabel
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "636c76"; // hex without #
}