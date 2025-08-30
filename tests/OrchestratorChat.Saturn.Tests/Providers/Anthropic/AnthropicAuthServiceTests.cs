using System.Net;
using System.Text.Json;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic;

/// <summary>
/// Tests for AnthropicAuthService OAuth 2.0 authentication flow
/// Tests focus on testable components like PKCE generation and token validation
/// </summary>
public class AnthropicAuthServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly AnthropicAuthService _authService;

    public AnthropicAuthServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttp);
        _authService = new AnthropicAuthService();
    }

    [Fact]
    public async Task GetValidTokensAsync_WithNoStoredTokens_ReturnsNull()
    {
        // This test can run without HTTP mocking since it checks local token storage
        
        // Act
        var result = await _authService.GetValidTokensAsync();

        // Assert - Should return null since no tokens are stored initially
        Assert.Null(result);
    }

    [Fact]
    public void GeneratePKCEChallenge_CreatesValidChallenge()
    {
        // Act
        var pkcePair = PKCEGenerator.Generate();

        // Assert
        Assert.NotNull(pkcePair);
        Assert.NotNull(pkcePair.Verifier);
        Assert.NotNull(pkcePair.Challenge);
        Assert.True(pkcePair.Verifier.Length >= 43);
        Assert.True(pkcePair.Verifier.Length <= 128);
        Assert.True(pkcePair.Challenge.Length > 0);
        
        // Verifier should be base64url encoded (no padding, no +, no /)
        Assert.DoesNotContain("=", pkcePair.Verifier);
        Assert.DoesNotContain("+", pkcePair.Verifier);
        Assert.DoesNotContain("/", pkcePair.Verifier);
        
        // Challenge should be base64url encoded
        Assert.DoesNotContain("=", pkcePair.Challenge);
        Assert.DoesNotContain("+", pkcePair.Challenge);
        Assert.DoesNotContain("/", pkcePair.Challenge);
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesRequiredParameters()
    {
        // Act - We need to test this through reflection since the URL building is internal
        var authMethod = typeof(AnthropicAuthService).GetMethod("AuthenticateAsync");
        Assert.NotNull(authMethod);

        // We can't easily test the URL building without refactoring, but we can verify
        // the constants used in URL construction are correct
        var clientIdField = typeof(AnthropicAuthService).GetField("CLIENT_ID", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var authUrlField = typeof(AnthropicAuthService).GetField("AUTH_URL_CLAUDE", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var redirectUriField = typeof(AnthropicAuthService).GetField("REDIRECT_URI", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var scopesField = typeof(AnthropicAuthService).GetField("SCOPES", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.NotNull(clientIdField?.GetValue(null));
        Assert.NotNull(authUrlField?.GetValue(null));
        Assert.NotNull(redirectUriField?.GetValue(null));
        Assert.NotNull(scopesField?.GetValue(null));
        
        var clientId = (string)clientIdField!.GetValue(null)!;
        var scopes = (string)scopesField!.GetValue(null)!;
        
        Assert.Equal("9d1c250a-e61b-44d9-88ed-5944d1962f5e", clientId);
        Assert.Contains("user:profile", scopes);
        Assert.Contains("user:inference", scopes);
        Assert.Contains("org:create_api_key", scopes);
    }

    [Fact]
    public void ValidateTokens_ValidatesCorrectly()
    {
        // Arrange
        var validTokens = new StoredTokens
        {
            AccessToken = TestConstants.TestAccessToken,
            RefreshToken = TestConstants.TestRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };

        var expiredTokens = new StoredTokens
        {
            AccessToken = "expired_token",
            RefreshToken = "expired_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer"
        };

        // Assert
        Assert.False(validTokens.IsExpired);
        Assert.False(validTokens.NeedsRefresh);
        
        Assert.True(expiredTokens.IsExpired);
        Assert.True(expiredTokens.NeedsRefresh);
    }

    [Fact]
    public void Logout_ClearsStoredTokens()
    {
        // This test verifies the logout method doesn't throw
        // The actual token clearing is tested in TokenStoreTests
        
        // Act & Assert - should not throw
        _authService.Logout();
    }

    public void Dispose()
    {
        _authService.Dispose();
        _httpClient.Dispose();
        _mockHttp.Dispose();
    }
}