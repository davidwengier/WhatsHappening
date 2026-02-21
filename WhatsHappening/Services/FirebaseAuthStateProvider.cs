using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WhatsHappening.Services;

public sealed class FirebaseAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly FirebaseAuthService _authService;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public FirebaseAuthStateProvider(FirebaseAuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += HandleAuthStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public async Task InitializeAsync()
    {
        // Wait for Firebase to resolve persisted auth state before reporting
        var user = await _authService.WaitForAuthStateAsync();
        UpdateUser(user);
        await _authService.ListenForAuthStateChangesAsync();
    }

    private void HandleAuthStateChanged(FirebaseUser? user)
    {
        UpdateUser(user);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void UpdateUser(FirebaseUser? user)
    {
        if (user is not null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Uid),
                new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? user.Uid),
            };

            if (user.Email is not null)
                claims.Add(new(ClaimTypes.Email, user.Email));

            if (user.PhotoURL is not null)
                claims.Add(new("picture", user.PhotoURL));

            _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));
        }
        else
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public void Dispose()
        => _authService.AuthStateChanged -= HandleAuthStateChanged;
}
