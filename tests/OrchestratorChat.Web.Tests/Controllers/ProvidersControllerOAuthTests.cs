using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Web.Controllers;
using Xunit;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Web.Tests.Controllers;

/// <summary>
/// Integration tests for OAuth roundtrip flow in ProvidersController
/// </summary>
public class ProvidersControllerOAuthTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDirectory;

    public ProvidersControllerOAuthTests(WebApplicationFactory<Program> factory)
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ProvidersControllerOAuthTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace with in-memory implementations for testing
                services.AddSingleton<IMemoryCache, MemoryCache>();
                services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task OAuthRoundtrip_StartToCallbackToStatus_ReturnsConnected()
    {
        // Step 1: Start OAuth flow
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();

        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        
        var authUrl = startResult.GetProperty("authUrl").GetString();
        var state = startResult.GetProperty("state").GetString();
        
        Assert.NotNull(authUrl);
        Assert.NotNull(state);
        Assert.Contains("claude.ai/oauth/authorize", authUrl);
        Assert.Contains($"state={state}", authUrl);

        // Step 2: Simulate OAuth callback with valid code
        var simulatedCode = "test_authorization_code_123";
        var callbackResponse = await _client.GetAsync($"/oauth/anthropic/callback?code={simulatedCode}&state={state}");
        
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackContent = await callbackResponse.Content.ReadAsStringAsync();
        Assert.Contains("oauth-success", callbackContent);
        Assert.Contains("Authentication successful", callbackContent);

        // Step 3: Check status to verify tokens are stored
        var statusResponse = await _client.GetAsync("/api/providers/anthropic/status");
        statusResponse.EnsureSuccessStatusCode();

        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        var statusResult = JsonSerializer.Deserialize<JsonElement>(statusContent);
        
        var connected = statusResult.GetProperty("connected").GetBoolean();
        Assert.True(connected);

        var expiresAt = statusResult.GetProperty("expiresAt").GetString();
        Assert.NotNull(expiresAt);
        Assert.NotEmpty(expiresAt);
    }

    [Fact]
    public async Task StartOAuth_GeneratesPKCEAndState()
    {
        // Act
        var response = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var authUrl = result.GetProperty("authUrl").GetString();
        var state = result.GetProperty("state").GetString();

        Assert.NotNull(authUrl);
        Assert.NotNull(state);
        Assert.Contains("code_challenge=", authUrl);
        Assert.Contains("code_challenge_method=S256", authUrl);
        Assert.Contains("client_id=9d1c250a-e61b-44d9-88ed-5944d1962f5e", authUrl);
        Assert.Contains("response_type=code", authUrl);
        Assert.Contains("scope=org%3acreate_api_key+user%3aprofile+user%3ainference", authUrl);
    }

    [Fact]
    public async Task OAuthCallback_WithInvalidState_ReturnsError()
    {
        // Arrange - Use invalid state that wasn't stored
        var invalidState = "invalid_state_not_in_cache";
        var code = "test_code";

        // Act
        var response = await _client.GetAsync($"/oauth/anthropic/callback?code={code}&state={invalidState}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content);
        Assert.Contains("Invalid or expired state parameter", content);
    }

    [Fact]
    public async Task OAuthCallback_WithMissingParameters_ReturnsError()
    {
        // Act - Missing both code and state
        var response = await _client.GetAsync("/oauth/anthropic/callback");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content);
        Assert.Contains("Missing required parameters", content);
    }

    [Fact]
    public async Task OAuthCallback_WithOAuthError_ReturnsError()
    {
        // Act - Simulate OAuth provider returning an error
        var response = await _client.GetAsync("/oauth/anthropic/callback?error=access_denied&error_description=User%20denied%20access");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content);
        Assert.Contains("access_denied", content);
    }

    [Fact]
    public async Task AnthropicStatus_WithNoTokens_ReturnsNotConnected()
    {
        // Act
        var response = await _client.GetAsync("/api/providers/anthropic/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var connected = result.GetProperty("connected").GetBoolean();
        var expiresAt = result.GetProperty("expiresAt");
        var scopes = result.GetProperty("scopes").EnumerateArray().ToList();

        Assert.False(connected);
        Assert.Equal(JsonValueKind.Null, expiresAt.ValueKind);
        Assert.Empty(scopes);
    }

    [Fact]
    public async Task AnthropicStatus_WithValidTokens_ReturnsConnected()
    {
        // Arrange - Store valid tokens
        var tokenStore = CreateTestTokenStore();
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await tokenStore.SaveTokensAsync(validTokens);

        // Act
        var response = await _client.GetAsync("/api/providers/anthropic/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var connected = result.GetProperty("connected").GetBoolean();
        var scopes = result.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).ToArray();

        Assert.True(connected);
        Assert.Contains("user:profile", scopes);
        Assert.Contains("user:inference", scopes);

        // Cleanup
        await tokenStore.ClearTokensAsync();
        tokenStore.Dispose();
    }

    [Fact]
    public async Task LogoutAnthropicOAuth_ClearsTokens()
    {
        // Arrange - Store tokens first
        var tokenStore = CreateTestTokenStore();
        var tokens = new StoredTokens
        {
            AccessToken = "token_to_clear",
            RefreshToken = "refresh_to_clear",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await tokenStore.SaveTokensAsync(tokens);

        // Verify tokens are stored
        var statusBeforeResponse = await _client.GetAsync("/api/providers/anthropic/status");
        var statusBeforeContent = await statusBeforeResponse.Content.ReadAsStringAsync();
        var statusBefore = JsonSerializer.Deserialize<JsonElement>(statusBeforeContent);
        Assert.True(statusBefore.GetProperty("connected").GetBoolean());

        // Act - Logout
        var logoutResponse = await _client.PostAsync("/api/providers/anthropic/logout", new StringContent(""));

        // Assert
        logoutResponse.EnsureSuccessStatusCode();
        var logoutContent = await logoutResponse.Content.ReadAsStringAsync();
        var logoutResult = JsonSerializer.Deserialize<JsonElement>(logoutContent);
        Assert.True(logoutResult.GetProperty("success").GetBoolean());

        // Verify tokens are cleared
        var statusAfterResponse = await _client.GetAsync("/api/providers/anthropic/status");
        var statusAfterContent = await statusAfterResponse.Content.ReadAsStringAsync();
        var statusAfter = JsonSerializer.Deserialize<JsonElement>(statusAfterContent);
        Assert.False(statusAfter.GetProperty("connected").GetBoolean());

        tokenStore.Dispose();
    }

    [Fact]
    public async Task ProvidersStatus_IncludesAnthropicOAuthStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/providers/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Should have all provider statuses including AnthropicOAuth
        Assert.True(result.TryGetProperty("anthropicOAuth", out var anthropicOAuth));
        
        // Should be Missing since no tokens are stored in this test
        var oauthStatus = anthropicOAuth.GetString();
        Assert.Equal("Missing", oauthStatus);
    }

    private TestTokenStore CreateTestTokenStore()
    {
        var uniqueTestDir = Path.Combine(_testDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueTestDir);
        return new TestTokenStore(uniqueTestDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
        _client.Dispose();
    }
}

/// <summary>
/// Test-specific TokenStore for integration tests
/// </summary>
public class TestTokenStore : TokenStore
{
    private readonly string _testDirectory;

    public TestTokenStore(string testDirectory)
    {
        _testDirectory = testDirectory;
        
        var tokenPathField = typeof(TokenStore).GetField("_tokenPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var keyPathField = typeof(TokenStore).GetField("_keyPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var saltPathField = typeof(TokenStore).GetField("_saltPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        tokenPathField?.SetValue(this, Path.Combine(_testDirectory, "integration.tokens"));
        keyPathField?.SetValue(this, Path.Combine(_testDirectory, ".integrationkeystore"));
        saltPathField?.SetValue(this, Path.Combine(_testDirectory, ".integrationsalt"));
    }

    public void Dispose()
    {
        try
        {
            var files = new[]
            {
                Path.Combine(_testDirectory, "integration.tokens"),
                Path.Combine(_testDirectory, ".integrationkeystore"),
                Path.Combine(_testDirectory, ".integrationsalt")
            };

            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

/// <summary>
/// Test HTTP client factory that returns mock clients for token exchange
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new TestHttpMessageHandler();
        return new HttpClient(handler);
    }
}

/// <summary>
/// Mock HTTP handler that simulates successful token exchange
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Simulate successful token exchange
        if (request.RequestUri?.ToString().Contains("oauth/token") == true)
        {
            var tokenResponse = new
            {
                access_token = "test_access_token_from_exchange",
                refresh_token = "test_refresh_token_from_exchange",
                token_type = "Bearer",
                expires_in = 3600,
                scope = "user:profile user:inference"
            };

            var json = JsonSerializer.Serialize(tokenResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        // Default response for other requests
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}