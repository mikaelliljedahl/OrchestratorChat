using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OrchestratorChat.Saturn.Providers.Anthropic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace OrchestratorChat.Web.Tests.Security;

/// <summary>
/// Security tests for PKCE and state parameter enforcement in OAuth flow
/// </summary>
public class PKCESecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PKCESecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task OAuthCallback_WithMismatchedState_RejectsRequest()
    {
        // Arrange - Start OAuth to get a valid state
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();
        
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var validState = startResult.GetProperty("state").GetString();
        
        // Use a different state than what was generated
        var mismatchedState = "mismatched_state_value_should_be_rejected";
        var code = "test_authorization_code";

        // Act
        var response = await _client.GetAsync($"/oauth/anthropic/callback?code={code}&state={mismatchedState}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("oauth-error", content);
        Assert.Contains("Invalid or expired state parameter", content);
        Assert.DoesNotContain("oauth-success", content);
    }

    [Fact]
    public async Task OAuthCallback_WithExpiredState_RejectsRequest()
    {
        // This test simulates state expiry by manually manipulating cache
        // In a real scenario, we'd wait for the 10-minute timeout
        
        // Arrange - Start OAuth to populate cache
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();
        
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var state = startResult.GetProperty("state").GetString();

        // Simulate cache expiry by clearing the memory cache
        var scope = _factory.Services.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        memoryCache.Remove($"oauth_state_{state}");
        memoryCache.Remove($"pkce_{state}");

        var code = "test_authorization_code";

        // Act
        var response = await _client.GetAsync($"/oauth/anthropic/callback?code={code}&state={state}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("oauth-error", content);
        Assert.Contains("Invalid or expired state parameter", content);
    }

    [Fact]
    public async Task OAuthCallback_WithMissingPKCEVerifier_RejectsRequest()
    {
        // Arrange - Start OAuth to get valid state
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();
        
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var state = startResult.GetProperty("state").GetString();

        // Clear only the PKCE verifier from cache (but leave state)
        var scope = _factory.Services.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        memoryCache.Remove($"pkce_{state}");

        var code = "test_authorization_code";

        // Act
        var response = await _client.GetAsync($"/oauth/anthropic/callback?code={code}&state={state}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("oauth-error", content);
        Assert.Contains("PKCE verifier not found or expired", content);
    }

    [Fact]
    public async Task StartOAuth_GeneratesUnpredictableState()
    {
        // Act - Start multiple OAuth flows
        var states = new HashSet<string>();
        
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var state = result.GetProperty("state").GetString();
            
            Assert.NotNull(state);
            states.Add(state!);
        }

        // Assert - All states should be unique (entropy check)
        Assert.Equal(10, states.Count);
        
        // Verify state format (base64url without padding)
        Assert.All(states, state =>
        {
            Assert.DoesNotContain('=', state); // No padding
            Assert.DoesNotContain('+', state); // No + (should be -)
            Assert.DoesNotContain('/', state); // No / (should be _)
            Assert.True(state.Length >= 32); // Sufficient entropy
        });
    }

    [Fact]
    public async Task StartOAuth_GeneratesValidPKCEChallenge()
    {
        // Act
        var response = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        var authUrl = result.GetProperty("authUrl").GetString();

        // Assert
        Assert.NotNull(authUrl);
        Assert.Contains("code_challenge=", authUrl);
        Assert.Contains("code_challenge_method=S256", authUrl);
        
        // Extract the code challenge from the URL
        var challengeStart = authUrl.IndexOf("code_challenge=") + "code_challenge=".Length;
        var challengeEnd = authUrl.IndexOf('&', challengeStart);
        if (challengeEnd == -1) challengeEnd = authUrl.Length;
        
        var codeChallenge = authUrl.Substring(challengeStart, challengeEnd - challengeStart);
        
        // Verify the challenge is base64url encoded and has appropriate length
        Assert.True(codeChallenge.Length >= 43); // Base64url encoded SHA256 should be 43 chars minimum
        Assert.DoesNotContain('=', codeChallenge); // No padding
        Assert.DoesNotContain('+', codeChallenge); // No +
        Assert.DoesNotContain('/', codeChallenge); // No /
    }

    [Fact]
    public async Task OAuthCallback_WithNullOrEmptyCode_RejectsRequest()
    {
        // Test with null code
        var response1 = await _client.GetAsync("/oauth/anthropic/callback?state=test_state");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var content1 = await response1.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content1);
        Assert.Contains("Missing required parameters", content1);

        // Test with empty code
        var response2 = await _client.GetAsync("/oauth/anthropic/callback?code=&state=test_state");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var content2 = await response2.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content2);
        Assert.Contains("Missing required parameters", content2);
    }

    [Fact]
    public async Task OAuthCallback_WithNullOrEmptyState_RejectsRequest()
    {
        // Test with null state
        var response1 = await _client.GetAsync("/oauth/anthropic/callback?code=test_code");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var content1 = await response1.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content1);
        Assert.Contains("Missing required parameters", content1);

        // Test with empty state
        var response2 = await _client.GetAsync("/oauth/anthropic/callback?code=test_code&state=");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var content2 = await response2.Content.ReadAsStringAsync();
        Assert.Contains("oauth-error", content2);
        Assert.Contains("Missing required parameters", content2);
    }

    [Fact]
    public async Task OAuthStart_WithConcurrentRequests_GeneratesUniqueStates()
    {
        // Act - Send concurrent OAuth start requests
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            _client.PostAsync("/api/providers/anthropic/start", new StringContent(""))
        ).ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All responses should be successful
        Assert.All(responses, response => response.EnsureSuccessStatusCode());

        // Extract all states
        var states = new List<string>();
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var state = result.GetProperty("state").GetString();
            Assert.NotNull(state);
            states.Add(state!);
        }

        // All states should be unique
        var uniqueStates = states.Distinct().ToList();
        Assert.Equal(20, uniqueStates.Count);
    }

    [Fact]
    public async Task StateParameter_IsCsrfProtected()
    {
        // This test verifies that state provides CSRF protection by ensuring
        // each OAuth flow gets its own unique, unpredictable state
        
        // Arrange - Start two separate OAuth flows
        var response1 = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        var response2 = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1);
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2);
        
        var state1 = result1.GetProperty("state").GetString();
        var state2 = result2.GetProperty("state").GetString();

        // Assert - States should be different (CSRF protection)
        Assert.NotEqual(state1, state2);
        
        // Attempt to use state1 with a callback from flow2 should fail
        var crossCallbackResponse = await _client.GetAsync($"/oauth/anthropic/callback?code=test_code&state={state1}");
        var crossCallbackContent = await crossCallbackResponse.Content.ReadAsStringAsync();
        
        // Should reject the cross-flow state usage
        Assert.Contains("oauth-error", crossCallbackContent);
        Assert.DoesNotContain("oauth-success", crossCallbackContent);
    }

    [Fact]
    public async Task PKCEGenerator_ProducesValidChallengeVerifierPair()
    {
        // This test verifies the PKCE implementation matches the specification
        
        // Arrange
        var pkcePair1 = PKCEGenerator.Generate();
        var pkcePair2 = PKCEGenerator.Generate();

        // Assert - Pairs should be unique
        Assert.NotEqual(pkcePair1.Verifier, pkcePair2.Verifier);
        Assert.NotEqual(pkcePair1.Challenge, pkcePair2.Challenge);

        // Verify verifier format
        Assert.True(pkcePair1.Verifier.Length >= 43); // Minimum length per RFC 7636
        Assert.True(pkcePair1.Verifier.Length <= 128); // Maximum length per RFC 7636
        Assert.Matches(@"^[A-Za-z0-9\-._~]+$", pkcePair1.Verifier); // Valid characters only

        // Verify challenge format
        Assert.True(pkcePair1.Challenge.Length == 43); // SHA256 base64url is always 43 chars
        Assert.DoesNotContain('=', pkcePair1.Challenge); // No padding
        Assert.DoesNotContain('+', pkcePair1.Challenge); // No +
        Assert.DoesNotContain('/', pkcePair1.Challenge); // No /
    }

    [Fact]
    public async Task OAuthCallback_WithReplayedState_RejectsSecondAttempt()
    {
        // Arrange - Start OAuth and complete it once
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();
        
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var state = startResult.GetProperty("state").GetString();

        // First callback attempt
        var firstCallbackResponse = await _client.GetAsync($"/oauth/anthropic/callback?code=test_code&state={state}");
        firstCallbackResponse.EnsureSuccessStatusCode();

        // Act - Try to replay the same state
        var replayResponse = await _client.GetAsync($"/oauth/anthropic/callback?code=another_code&state={state}");

        // Assert - Second attempt should fail
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replayContent = await replayResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("oauth-error", replayContent);
        Assert.Contains("Invalid or expired state parameter", replayContent);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}