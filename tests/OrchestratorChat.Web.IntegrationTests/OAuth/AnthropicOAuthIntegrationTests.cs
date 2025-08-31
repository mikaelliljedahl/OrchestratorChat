using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Web.Services;
using Xunit;

namespace OrchestratorChat.Web.IntegrationTests.OAuth;

public class AnthropicOAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AnthropicOAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real OAuth service with mock for testing
                services.AddSingleton<IAnthropicOAuthService, MockAnthropicOAuthService>();
                services.AddSingleton<IOAuthStateStore, InMemoryOAuthStateStore>();
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task OAuthFlow_StartAuth_ReturnsAuthorizationUrl()
    {
        // Act
        var response = await _client.PostAsync("/api/providers/anthropic/start", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuthStartResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.AuthUrl);
        Assert.NotEmpty(result.State);
        Assert.Contains("https://auth.claude.ai/oauth/authorize", result.AuthUrl);
    }

    [Fact]
    public async Task OAuthFlow_Callback_WithValidState_Succeeds()
    {
        // Arrange - Start OAuth flow first
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", null);
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<OAuthStartResponse>(startContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act - Simulate callback
        var callbackUrl = $"/oauth/anthropic/callback?code=test_code&state={startResult.State}";
        var response = await _client.GetAsync(callbackUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("oauth-success", content);
    }

    [Fact]
    public async Task OAuthFlow_Callback_WithInvalidState_Fails()
    {
        // Act - Callback with invalid state
        var callbackUrl = "/oauth/anthropic/callback?code=test_code&state=invalid_state";
        var response = await _client.GetAsync(callbackUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Still returns OK with error page
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content);
    }

    [Fact]
    public async Task OAuthFlow_SubmitCode_WithValidState_Succeeds()
    {
        // Arrange - Start OAuth flow first
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", null);
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<OAuthStartResponse>(startContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act - Submit code
        var submitRequest = new
        {
            code = "manual_test_code",
            state = startResult.State
        };
        var submitContent = new StringContent(
            JsonSerializer.Serialize(submitRequest),
            Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/providers/anthropic/submit-code", submitContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SubmitCodeResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        Assert.True(result.Success);
    }

    [Fact]
    public async Task OAuthFlow_Status_WhenAuthenticated_ReturnsConnected()
    {
        // Arrange - Complete OAuth flow
        await CompleteOAuthFlow();

        // Act
        var response = await _client.GetAsync("/api/providers/anthropic/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<OAuthStatusResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(status);
        Assert.True(status.Connected);
        Assert.NotEmpty(status.Scopes);
    }

    [Fact]
    public async Task OAuthFlow_Logout_ClearsAuthentication()
    {
        // Arrange - Complete OAuth flow
        await CompleteOAuthFlow();

        // Act - Logout
        var logoutResponse = await _client.PostAsync("/api/providers/anthropic/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Assert - Check status after logout
        var statusResponse = await _client.GetAsync("/api/providers/anthropic/status");
        var content = await statusResponse.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<OAuthStatusResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.False(status.Connected);
    }

    [Fact]
    public async Task OAuthFlow_StateConsumption_PreventsReplay()
    {
        // Arrange - Start OAuth flow
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", null);
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<OAuthStartResponse>(startContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Act - Use state once
        var firstCallbackUrl = $"/oauth/anthropic/callback?code=test_code&state={startResult.State}";
        var firstResponse = await _client.GetAsync(firstCallbackUrl);
        Assert.Contains("oauth-success", await firstResponse.Content.ReadAsStringAsync());

        // Act - Try to reuse same state
        var secondCallbackUrl = $"/oauth/anthropic/callback?code=another_code&state={startResult.State}";
        var secondResponse = await _client.GetAsync(secondCallbackUrl);

        // Assert - Second use should fail
        var content = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content);
    }

    private async Task CompleteOAuthFlow()
    {
        // Start flow
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", null);
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<OAuthStartResponse>(startContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Complete callback
        var callbackUrl = $"/oauth/anthropic/callback?code=test_code&state={startResult.State}";
        await _client.GetAsync(callbackUrl);
    }

    // Response DTOs for deserialization
    private class OAuthStartResponse
    {
        public string AuthUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    private class SubmitCodeResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    private class OAuthStatusResponse
    {
        public bool Connected { get; set; }
        public string? ExpiresAt { get; set; }
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }
}

// Mock implementation for testing
public class MockAnthropicOAuthService : IAnthropicOAuthService
{
    private readonly IOAuthStateStore _stateStore;
    private bool _isAuthenticated = false;

    public MockAnthropicOAuthService(IOAuthStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task<OAuthStartResult> StartAuthAsync()
    {
        var state = Guid.NewGuid().ToString();
        var verifier = Guid.NewGuid().ToString();
        
        await _stateStore.StoreAsync(state, new OAuthStateData
        {
            PkceVerifier = verifier,
            CreatedAt = DateTime.UtcNow
        }, TimeSpan.FromMinutes(10));

        return new OAuthStartResult
        {
            AuthUrl = $"https://auth.claude.ai/oauth/authorize?state={state}&response_type=code&code_challenge_method=S256",
            State = state
        };
    }

    public async Task<OAuthCallbackResult> HandleCallbackAsync(string code, string state)
    {
        var stateData = await _stateStore.RetrieveAsync(state);
        if (stateData == null)
        {
            return new OAuthCallbackResult
            {
                Success = false,
                ErrorMessage = "Invalid state"
            };
        }

        _isAuthenticated = true;
        return new OAuthCallbackResult
        {
            Success = true,
            SuccessMessage = "Authentication successful"
        };
    }

    public async Task<OAuthCallbackResult> SubmitCodeAsync(string code, string state)
    {
        return await HandleCallbackAsync(code, state);
    }

    public Task<OAuthStatus> GetStatusAsync()
    {
        var status = new OAuthStatus
        {
            Connected = _isAuthenticated,
            ExpiresAt = _isAuthenticated ? DateTime.UtcNow.AddHours(1).ToString("O") : null,
            Scopes = _isAuthenticated ? new[] { "read", "write" } : Array.Empty<string>()
        };
        return Task.FromResult(status);
    }

    public Task LogoutAsync()
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }
}

// In-memory implementation for testing
public class InMemoryOAuthStateStore : IOAuthStateStore
{
    private readonly Dictionary<string, (OAuthStateData Data, DateTime ExpiresAt)> _store = new();

    public Task StoreAsync(string state, OAuthStateData data, TimeSpan expiration)
    {
        _store[state] = (data, DateTime.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task<OAuthStateData?> RetrieveAsync(string state)
    {
        if (_store.TryGetValue(state, out var entry))
        {
            _store.Remove(state); // One-time use
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult<OAuthStateData?>(entry.Data);
            }
        }
        return Task.FromResult<OAuthStateData?>(null);
    }

    public Task RemoveAsync(string state)
    {
        _store.Remove(state);
        return Task.CompletedTask;
    }
}