namespace WhatsHappening;

public sealed class TodoItem
{
    public string? Id { get; set; }
    public string Title { get; set; }
    public bool IsComplete { get; set; }
    public int Order { get; set; }
    public long? CompletedAt { get; set; }

    public TodoItem() => Title = string.Empty;

    public TodoItem(string title) => Title = title;
}