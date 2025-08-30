using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OrchestratorChat.Web.Tests.TestHelpers;

public class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _user;

    public TestAuthenticationStateProvider(bool isAuthenticated = true)
    {
        if (isAuthenticated)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            _user = new ClaimsPrincipal(identity);
        }
        else
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }
}