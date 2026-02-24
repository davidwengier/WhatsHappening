namespace WhatsHappening;

public sealed class TodoGroup
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Expanded { get; set; } = true;

    public TodoGroup() { }

    public TodoGroup(string name) => Name = name;
}
