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
            gitHubUrl = item.GitHubUrl,
            gitHubOwner = item.GitHubOwner,
            gitHubRepo = item.GitHubRepo,
            gitHubNumber = item.GitHubNumber,
            gitHubType = item.GitHubType,
            gitHubState = item.GitHubState,
            gitHubBody = item.GitHubBody,
            gitHubLabels = item.GitHubLabels?.Select(l => new { name = l.Name, color = l.Color }).ToArray()
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
}
