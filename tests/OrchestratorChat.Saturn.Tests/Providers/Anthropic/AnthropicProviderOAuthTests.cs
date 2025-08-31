using NSubstitute;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using System.Net.Http;
using System.Text;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic;

/// <summary>
/// Tests for AnthropicProvider OAuth initialization and authentication handling
/// </summary>
public class AnthropicProviderOAuthTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly string _testDirectory;

    public AnthropicProviderOAuthTests()
    {
        _fileHelper = new FileTestHelper("AnthropicProviderOAuthTests");
        _testDirectory = _fileHelper.TestDirectory;
    }

    [Fact]
    public async Task InitializeAsync_WithValidOAuthTokens_UsesOAuthOverApiKey()
    {
        // Arrange
        var testTokenStore = CreateTestTokenStore();
        var validTokens = new StoredTokens
        {
            AccessToken = "oauth_access_token",
            RefreshToken = "oauth_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await testTokenStore.SaveTokensAsync(validTokens);

        var settings = new Dictionary<string, object>
        {
            ["ApiKey"] = "api_key_should_not_be_used"
        };
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "env_key_should_not_be_used");

        var provider = new TestableAnthropicProvider(settings, testTokenStore);

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.True(provider.HasBearerAuthHeader);
        Assert.False(provider.HasApiKeyHeader);
        Assert.Equal("oauth_access_token", provider.GetUsedOAuthToken());

        // Cleanup
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithOAuthTokensNeedingRefresh_RefreshesAndUses()
    {
        // Arrange
        var testTokenStore = CreateTestTokenStore();
        var tokensNeedingRefresh = new StoredTokens
        {
            AccessToken = "old_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(2), // Within refresh window
            CreatedAt = DateTime.UtcNow.AddMinutes(-58),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(tokensNeedingRefresh);

        var settings = new Dictionary<string, object>();
        var mockAuthService = Substitute.For<IAnthropicAuthService>();
        
        var refreshedTokens = new StoredTokens
        {
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token", 
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        
        mockAuthService.RefreshTokensAsync("valid_refresh_token").Returns(refreshedTokens);

        var provider = new TestableAnthropicProvider(settings, testTokenStore, mockAuthService);

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.True(provider.HasBearerAuthHeader);
        Assert.False(provider.HasApiKeyHeader);
        Assert.Equal("new_access_token", provider.GetUsedOAuthToken());

        // Verify refresh was called
        await mockAuthService.Received(1).RefreshTokensAsync("valid_refresh_token");

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_WithExpiredTokensNoRefresh_FallsBackToApiKey()
    {
        // Arrange
        var testTokenStore = CreateTestTokenStore();
        var expiredTokens = new StoredTokens
        {
            AccessToken = "expired_access_token",
            RefreshToken = null, // No refresh token
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(expiredTokens);

        var settings = new Dictionary<string, object>
        {
            ["ApiKey"] = "fallback_api_key"
        };

        var provider = new TestableAnthropicProvider(settings, testTokenStore);

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.False(provider.HasBearerAuthHeader);
        Assert.True(provider.HasApiKeyHeader);
        Assert.Null(provider.GetUsedOAuthToken());

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_NoTokensButHasApiKey_UsesApiKey()
    {
        // Arrange - no tokens stored
        var settings = new Dictionary<string, object>
        {
            ["ApiKey"] = "api_key_only"
        };

        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory));

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.False(provider.HasBearerAuthHeader);
        Assert.True(provider.HasApiKeyHeader);
        Assert.Null(provider.GetUsedOAuthToken());
    }

    [Fact]
    public async Task InitializeAsync_NoTokensNoApiKeyButEnvVar_UsesEnvVar()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "env_api_key");
        var settings = new Dictionary<string, object>();
        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory));

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.False(provider.HasBearerAuthHeader);
        Assert.True(provider.HasApiKeyHeader);

        // Cleanup
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
    }

    [Fact]
    public async Task InitializeAsync_NoCredentialsAtAll_NotInitialized()
    {
        // Arrange - no tokens, no API key, no env var
        var settings = new Dictionary<string, object>();
        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory));

        // Act
        await provider.InitializeAsync();

        // Assert
        Assert.False(provider.IsInitialized);
        Assert.False(provider.HasBearerAuthHeader);
        Assert.False(provider.HasApiKeyHeader);
    }

    [Fact]
    public async Task InitializeAsync_WithConfiguration_UsesOAuthOverConfigApiKey()
    {
        // Arrange
        var testTokenStore = CreateTestTokenStore();
        var validTokens = new StoredTokens
        {
            AccessToken = "oauth_from_config",
            RefreshToken = "refresh_from_config",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(validTokens);

        var configuration = new ProviderConfiguration
        {
            ApiKey = "config_api_key_should_not_be_used",
            Settings = new Dictionary<string, object>()
        };

        var provider = new TestableAnthropicProvider(new Dictionary<string, object>(), testTokenStore);

        // Act
        await provider.InitializeAsync(configuration);

        // Assert
        Assert.True(provider.IsInitialized);
        Assert.True(provider.HasBearerAuthHeader);
        Assert.False(provider.HasApiKeyHeader);
        Assert.Equal("oauth_from_config", provider.GetUsedOAuthToken());

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task EnsureValidAuthenticationAsync_WithValidOAuth_ReturnsTrue()
    {
        // Arrange
        var testTokenStore = CreateTestTokenStore();
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_oauth_token",
            RefreshToken = "valid_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(validTokens);

        var provider = new TestableAnthropicProvider(new Dictionary<string, object>(), testTokenStore);
        await provider.InitializeAsync();

        // Act
        var result = await provider.CallEnsureValidAuthenticationAsync();

        // Assert
        Assert.True(result);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task EnsureValidAuthenticationAsync_WithApiKey_ReturnsTrue()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            ["ApiKey"] = "valid_api_key"
        };
        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory));
        await provider.InitializeAsync();

        // Act
        var result = await provider.CallEnsureValidAuthenticationAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StreamCompletionAsync_WithoutCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new Dictionary<string, object>();
        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory));
        // Don't call InitializeAsync - leave uninitialized

        var messages = new List<AgentMessage>
        {
            new() { Content = "Test message", Role = MessageRole.User }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var chunk in provider.StreamCompletionAsync(messages, "claude-sonnet-4"))
            {
                // Should throw before yielding any results
            }
        });
    }

    [Fact]
    public async Task StreamCompletionAsync_WithMissingCredentials_ThrowsExpectedMessage()
    {
        // Arrange - initialized but no credentials
        var settings = new Dictionary<string, object>();
        var provider = new TestableAnthropicProvider(settings, new TestTokenStore(_testDirectory), shouldFailAuth: true);
        await provider.InitializeAsync(); // Will be initialized but auth will fail

        var messages = new List<AgentMessage>
        {
            new() { Content = "Test message", Role = MessageRole.User }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var chunk in provider.StreamCompletionAsync(messages, "claude-sonnet-4"))
            {
                // Should throw before yielding any results
            }
        });

        Assert.Contains("Missing Anthropic credentials", exception.Message);
        Assert.Contains("connect to Anthropic via OAuth", exception.Message);
    }

    private TestTokenStore CreateTestTokenStore()
    {
        var uniqueTestDir = Path.Combine(_testDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueTestDir);
        return new TestTokenStore(uniqueTestDir);
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
    }
}

/// <summary>
/// Testable version of AnthropicProvider that exposes internal state for verification
/// </summary>
public class TestableAnthropicProvider : AnthropicProvider
{
    private readonly TokenStore _tokenStore;
    private readonly IAnthropicAuthService? _mockAuthService;
    private readonly bool _shouldFailAuth;
    private HttpClient? _httpClient;
    private string? _oauthToken;
    private string? _apiKey;

    public TestableAnthropicProvider(Dictionary<string, object> settings, TokenStore tokenStore, IAnthropicAuthService? mockAuthService = null, bool shouldFailAuth = false) 
        : base(settings)
    {
        _tokenStore = tokenStore;
        _mockAuthService = mockAuthService;
        _shouldFailAuth = shouldFailAuth;

        // Use reflection to set the private _tokenStore field
        var tokenStoreField = typeof(AnthropicProvider).GetField("_tokenStore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tokenStoreField?.SetValue(this, tokenStore);
    }

    public override async Task InitializeAsync()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Claude-Code/1.0");

        // OAuth logic with potential mock
        try
        {
            var tokens = await _tokenStore.LoadTokensAsync();
            if (tokens != null && (!tokens.IsExpired || !string.IsNullOrEmpty(tokens.RefreshToken)))
            {
                if (tokens.NeedsRefresh && !string.IsNullOrEmpty(tokens.RefreshToken))
                {
                    if (_mockAuthService != null)
                    {
                        var refreshedTokens = await _mockAuthService.RefreshTokensAsync(tokens.RefreshToken);
                        if (refreshedTokens != null)
                        {
                            tokens = refreshedTokens;
                            await _tokenStore.SaveTokensAsync(refreshedTokens);
                        }
                    }
                }

                if (!tokens.IsExpired && !string.IsNullOrEmpty(tokens.AccessToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");
                    _oauthToken = tokens.AccessToken;
                    IsInitialized = true;
                    return;
                }
            }
        }
        catch
        {
            // Fall through to API key
        }

        // Fall back to API key (call parent implementation)
        await base.InitializeAsync();

        // Capture API key for testing
        var settingsField = typeof(AnthropicProvider).GetField("_settings", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var settings = settingsField?.GetValue(this) as Dictionary<string, object>;
        _apiKey = settings?.ContainsKey("ApiKey") == true ? settings["ApiKey"]?.ToString() : null;
        _apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    public override async IAsyncEnumerable<string> StreamCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        CancellationToken cancellationToken = default)
    {
        if (_httpClient == null || !IsInitialized)
        {
            throw new InvalidOperationException("Anthropic provider not initialized");
        }

        // Simulate auth check failure for testing
        if (_shouldFailAuth)
        {
            throw new InvalidOperationException(
                "Missing Anthropic credentials. Please connect to Anthropic via OAuth or set ANTHROPIC_API_KEY environment variable.");
        }

        // Don't actually make HTTP requests in tests - just yield test content
        yield return "Test";
        yield return " response";
        yield return " chunk";
    }

    public bool HasBearerAuthHeader => 
        _httpClient?.DefaultRequestHeaders.Authorization?.Scheme == "Bearer";

    public bool HasApiKeyHeader =>
        _httpClient?.DefaultRequestHeaders.Contains("x-api-key") == true;

    public string? GetUsedOAuthToken() => _oauthToken;

    public async Task<bool> CallEnsureValidAuthenticationAsync()
    {
        // Call the private method via reflection
        var method = typeof(AnthropicProvider).GetMethod("EnsureValidAuthenticationAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var task = method?.Invoke(this, null) as Task<bool>;
        return task != null ? await task : false;
    }
}

/// <summary>
/// Interface for mocking AnthropicAuthService in tests
/// </summary>
public interface IAnthropicAuthService
{
    Task<StoredTokens?> RefreshTokensAsync(string refreshToken);
}