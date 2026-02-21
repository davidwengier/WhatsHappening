using System.Text.Json;
using Microsoft.JSInterop;

namespace WhatsHappening.Services;

public sealed class FirebaseAuthService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<FirebaseAuthService>? _dotNetRef;

    public event Action<FirebaseUser?>? AuthStateChanged;

    public FirebaseAuthService(IJSRuntime js) => _js = js;

    public async Task<bool> HasConfigAsync()
        => await _js.InvokeAsync<bool>("firebaseInterop.hasConfig");

    public async Task SetConfigAsync(FirebaseConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        await _js.InvokeVoidAsync("firebaseInterop.setConfig", json);
    }

    public async Task ClearConfigAsync()
        => await _js.InvokeVoidAsync("firebaseInterop.clearConfig");

    public async Task<FirebaseConfig?> GetConfigAsync()
        => await _js.InvokeAsync<FirebaseConfig?>("firebaseInterop.getConfig");

    public async Task<bool> InitializeAsync()
        => await _js.InvokeAsync<bool>("firebaseInterop.initialize");

    public async Task<FirebaseUser?> SignInWithGitHubAsync()
        => await _js.InvokeAsync<FirebaseUser?>("firebaseInterop.signInWithGitHub");

    public async Task SignOutAsync()
        => await _js.InvokeVoidAsync("firebaseInterop.signOut");

    public async Task<FirebaseUser?> GetCurrentUserAsync()
        => await _js.InvokeAsync<FirebaseUser?>("firebaseInterop.getCurrentUser");

    public async Task<FirebaseUser?> WaitForAuthStateAsync()
        => await _js.InvokeAsync<FirebaseUser?>("firebaseInterop.waitForAuthState");

    public async Task<string?> GetGitHubTokenAsync()
        => await _js.InvokeAsync<string?>("firebaseInterop.getGitHubToken");

    public async Task ListenForAuthStateChangesAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("firebaseInterop.onAuthStateChanged", _dotNetRef);
    }

    [JSInvokable]
    public void OnAuthStateChanged(FirebaseUser? user)
        => AuthStateChanged?.Invoke(user);

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
