using System.Text.Json;
using Microsoft.JSInterop;

namespace WhatsHappening.Services;

public sealed class FirestoreService
{
    private readonly IJSRuntime _js;

    public FirestoreService(IJSRuntime js) => _js = js;

    public async Task<List<TodoItem>> GetTodosAsync()
    {
        var items = await _js.InvokeAsync<List<TodoItem>>("firebaseInterop.getTodos");
        return items ?? [];
    }

    public async Task<string> AddTodoAsync(TodoItem item)
    {
        var data = new
        {
            title = item.Title,
            isComplete = item.IsComplete,
            order = item.Order,
            groupId = item.GroupId,
            gitHubUrl = item.GitHubUrl,
            gitHubOwner = item.GitHubOwner,
            gitHubRepo = item.GitHubRepo,
            gitHubNumber = item.GitHubNumber,
            gitHubType = item.GitHubType,
            gitHubState = item.GitHubState,
            gitHubBody = item.GitHubBody,
            gitHubLabels = item.GitHubLabels?.Select(l => new { name = l.Name, color = l.Color }).ToArray(),
            linkedPrNumber = item.LinkedPrNumber,
            linkedPrRepo = item.LinkedPrRepo,
            linkedPrState = item.LinkedPrState
        };
        return await _js.InvokeAsync<string>("firebaseInterop.addTodo", data);
    }

    public async Task UpdateTodoAsync(string docId, object data)
        => await _js.InvokeVoidAsync("firebaseInterop.updateTodo", docId, data);

    public async Task DeleteTodoAsync(string docId)
        => await _js.InvokeVoidAsync("firebaseInterop.deleteTodo", docId);

    public async Task ReorderTodosAsync(IEnumerable<(string Id, int Order)> updates)
    {
        var data = updates.Select(u => new { id = u.Id, order = u.Order }).ToArray();
        await _js.InvokeVoidAsync("firebaseInterop.reorderTodos", data);
    }

    // Group operations

    public async Task<List<TodoGroup>> GetGroupsAsync()
    {
        var items = await _js.InvokeAsync<List<TodoGroup>>("firebaseInterop.getGroups");
        return items ?? [];
    }

    public async Task<string> AddGroupAsync(TodoGroup group)
    {
        var data = new { name = group.Name, order = group.Order };
        return await _js.InvokeAsync<string>("firebaseInterop.addGroup", data);
    }

    public async Task UpdateGroupAsync(string docId, object data)
        => await _js.InvokeVoidAsync("firebaseInterop.updateGroup", docId, data);

    public async Task DeleteGroupAsync(string docId)
        => await _js.InvokeVoidAsync("firebaseInterop.deleteGroup", docId);

    // Settings
    public async Task<string?> GetSettingAsync(string key)
        => await _js.InvokeAsync<string?>("firebaseInterop.getSetting", key);

    public async Task SetSettingAsync(string key, string value)
        => await _js.InvokeVoidAsync("firebaseInterop.setSetting", key, value);
}
