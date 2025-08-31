using Microsoft.Extensions.Logging;
using NSubstitute;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

/// <summary>
/// Unit tests for ProviderVerificationService OAuth functionality
/// </summary>
public class ProviderVerificationOAuthTests : IDisposable
{
    private readonly IProviderVerificationService _providerVerificationService;
    private readonly OrchestratorChat.Core.Configuration.IConfigurationProvider _mockConfigProvider;
    private readonly IHttpClientFactory _mockHttpClientFactory;
    private readonly ILogger<ProviderVerificationService> _mockLogger;
    private readonly ITokenStore _mockTokenStore;
    private readonly string _testDirectory;

    public ProviderVerificationOAuthTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ProviderVerificationOAuthTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        _mockConfigProvider = Substitute.For<OrchestratorChat.Core.Configuration.IConfigurationProvider>();
        _mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        _mockLogger = Substitute.For<ILogger<ProviderVerificationService>>();
        _mockTokenStore = Substitute.For<ITokenStore>();

        _providerVerificationService = new ProviderVerificationService(
            _mockConfigProvider,
            _mockHttpClientFactory,
            _mockLogger,
            _mockTokenStore);
    }

    [Fact]
    public async Task CheckAnthropicOAuthAsync_WithValidTokens_ReturnsPresent()
    {
        // Arrange
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        _mockTokenStore.LoadTokensAsync().Returns(validTokens);

        // Act
        var result = await _providerVerificationService.CheckAnthropicOAuthAsync();

        // Assert
        Assert.Equal(ProviderStatus.Present, result);
    }

    [Fact]
    public async Task CheckAnthropicOAuthAsync_WithExpiredTokensAndRefresh_ReturnsPresent()
    {
        // Arrange
        var expiredTokensWithRefresh = new StoredTokens
        {
            AccessToken = "expired_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        _mockTokenStore.LoadTokensAsync().Returns(expiredTokensWithRefresh);

        // Act
        var result = await _providerVerificationService.CheckAnthropicOAuthAsync();

        // Assert
        Assert.Equal(ProviderStatus.Present, result);
    }

    [Fact]
    public async Task CheckAnthropicOAuthAsync_WithExpiredTokensNoRefresh_ReturnsMissing()
    {
        // Arrange
        var expiredTokensNoRefresh = new StoredTokens
        {
            AccessToken = "expired_access_token",
            RefreshToken = null, // No refresh token
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        _mockTokenStore.LoadTokensAsync().Returns(expiredTokensNoRefresh);

        // Act
        var result = await _providerVerificationService.CheckAnthropicOAuthAsync();

        // Assert
        Assert.Equal(ProviderStatus.Missing, result);
    }

    [Fact]
    public async Task CheckAnthropicOAuthAsync_WithNoTokensStored_ReturnsMissing()
    {
        // Arrange
        _mockTokenStore.LoadTokensAsync().Returns((StoredTokens?)null);

        // Act
        var result = await _providerVerificationService.CheckAnthropicOAuthAsync();

        // Assert
        Assert.Equal(ProviderStatus.Missing, result);
    }

    [Fact]
    public async Task CheckAnthropicOAuthAsync_WithTokenStoreException_ReturnsMissing()
    {
        // Arrange
        _mockTokenStore.LoadTokensAsync().Returns(Task.FromException<StoredTokens?>(new Exception("Token store error")));

        // Act
        var result = await _providerVerificationService.CheckAnthropicOAuthAsync();

        // Assert
        Assert.Equal(ProviderStatus.Missing, result);
    }

    [Fact]
    public async Task CheckAnthropicProviderAsync_WithOAuthTokens_ReturnsPresent()
    {
        // Arrange
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        _mockTokenStore.LoadTokensAsync().Returns(validTokens);

        // Act
        var result = await _providerVerificationService.CheckAnthropicProviderAsync();

        // Assert
        Assert.Equal(ProviderStatus.Present, result);
    }

    [Fact]
    public async Task CheckAnthropicProviderAsync_NoOAuthButHasApiKey_ReturnsPresent()
    {
        // Arrange - no OAuth tokens, but API key exists
        _mockTokenStore.LoadTokensAsync().Returns((StoredTokens?)null);
        _mockConfigProvider.GetAsync<string>("Anthropic:ApiKey").Returns("test_api_key");

        // Act
        var result = await _providerVerificationService.CheckAnthropicProviderAsync();

        // Assert
        Assert.Equal(ProviderStatus.Present, result);
    }

    [Fact]
    public async Task CheckAnthropicProviderAsync_NoOAuthNoApiKey_ReturnsMissing()
    {
        // Arrange - no OAuth tokens, no API key
        _mockTokenStore.LoadTokensAsync().Returns((StoredTokens?)null);
        _mockConfigProvider.GetAsync<string>("Anthropic:ApiKey").Returns((string?)null);

        // Act
        var result = await _providerVerificationService.CheckAnthropicProviderAsync();

        // Assert
        Assert.Equal(ProviderStatus.Missing, result);
    }

    [Fact]
    public async Task GetProviderStatusAsync_IncludesAnthropicOAuthStatus()
    {
        // Arrange
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_access_token",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        _mockTokenStore.LoadTokensAsync().Returns(validTokens);

        // Act
        var result = await _providerVerificationService.GetProviderStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProviderStatus.Present, result.AnthropicOAuth);
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
    }
}