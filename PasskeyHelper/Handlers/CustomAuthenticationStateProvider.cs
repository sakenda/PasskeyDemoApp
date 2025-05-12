using Microsoft.AspNetCore.Components.Authorization;
using PasskeyHelper.Data;
using System.Security.Claims;

namespace PasskeyHelper.Handlers;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticationState? _cachedState;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(_cachedState ?? new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    internal void MarkUserAsAuthenticated(ApplicationUser user)
    {
        // Aus AD Rollen/Autorisierungen lesen
        string[] roles = [ "Admin", "User" ]; // TODO: Replace with actual role retrieval logic

        var claims = new List<Claim>()
        {
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id ?? ""),
        };

        foreach (var role in roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Custom");
        var principal = new ClaimsPrincipal(identity);
        _cachedState = new AuthenticationState(principal);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    internal void MarkUserAsLoggedOut()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _cachedState = new AuthenticationState(principal);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

}
