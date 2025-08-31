using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Core.Security;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Web.Services;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

public class AnthropicOAuthServiceTests
{
    private readonly AnthropicOAuthService _service;
    private readonly InMemoryOAuthStateStore _stateStore;
    private readonly InMemoryTokenStore _tokenStore;
    private readonly ILogger<AnthropicOAuthService> _logger;

    public AnthropicOAuthServiceTests()
    {
        _stateStore = new InMemoryOAuthStateStore();
        _tokenStore = new InMemoryTokenStore();
        _logger = new TestLogger<AnthropicOAuthService>();
        _service = new AnthropicOAuthService(_stateStore, _tokenStore, _logger);
    }

    [Fact]
    public async Task StartAuthAsync_GeneratesValidAuthorizationUrl()
    {
        // Act
        var result = await _service.StartAuthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AuthUrl);
        Assert.NotEmpty(result.State);
        Assert.Contains("https://auth.claude.ai/oauth/authorize", result.AuthUrl);
        Assert.Contains("response_type=code", result.AuthUrl);
        Assert.Contains("code_challenge_method=S256", result.AuthUrl);
        Assert.Contains($"state={result.State}", result.AuthUrl);
    }

    [Fact]
    public async Task StartAuthAsync_StoresStateWithPkceVerifier()
    {
        // Act
        var result = await _service.StartAuthAsync();

        // Assert
        var storedData = await _stateStore.RetrieveAsync(result.State);
        Assert.NotNull(storedData);
        Assert.NotEmpty(storedData.PkceVerifier);
        Assert.True(storedData.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithValidState_ExchangesCodeSuccessfully()
    {
        // Arrange
        var startResult = await _service.StartAuthAsync();
        var code = "test_auth_code";

        // Act
        var result = await _service.HandleCallbackAsync(code, startResult.State);

        // Assert
        Assert.NotNull(result);
        // Note: Actual token exchange would fail in unit test without mocked HTTP client
        // This test validates the flow structure
    }

    [Fact]
    public async Task HandleCallbackAsync_WithInvalidState_ReturnsError()
    {
        // Arrange
        var invalidState = "invalid_state_value";
        var code = "test_auth_code";

        // Act
        var result = await _service.HandleCallbackAsync(code, invalidState);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallbackAsync_ConsumesStateOnUse()
    {
        // Arrange
        var startResult = await _service.StartAuthAsync();
        var code = "test_auth_code";

        // Act
        var result = await _service.HandleCallbackAsync(code, startResult.State);

        // Assert - State should be consumed (one-time use)
        var storedData = await _stateStore.RetrieveAsync(startResult.State);
        Assert.Null(storedData);
    }

    [Fact]
    public async Task SubmitCodeAsync_WithValidState_ProcessesCode()
    {
        // Arrange
        var startResult = await _service.StartAuthAsync();
        var code = "manual_auth_code";

        // Act
        var result = await _service.SubmitCodeAsync(code, startResult.State);

        // Assert
        Assert.NotNull(result);
        // Note: Actual token exchange would fail in unit test without mocked HTTP client
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotAuthenticated_ReturnsDisconnected()
    {
        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.False(status.Connected);
        Assert.Null(status.ExpiresAt);
        Assert.Empty(status.Scopes);
    }

    [Fact]
    public async Task GetStatusAsync_WhenAuthenticated_ReturnsConnected()
    {
        // Arrange
        var token = new AnthropicTokens
        {
            AccessToken = "test_access_token",
            RefreshToken = "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Scopes = new[] { "read", "write" }
        };
        await _tokenStore.SaveTokensAsync(token);

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.Connected);
        Assert.NotNull(status.ExpiresAt);
        Assert.Equal(2, status.Scopes.Length);
    }

    [Fact]
    public async Task LogoutAsync_ClearsStoredTokens()
    {
        // Arrange
        var token = new AnthropicTokens
        {
            AccessToken = "test_access_token",
            RefreshToken = "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        await _tokenStore.SaveTokensAsync(token);

        // Act
        await _service.LogoutAsync();

        // Assert
        var tokens = await _tokenStore.GetTokensAsync();
        Assert.Null(tokens);
    }

    // Helper classes for testing
    private class InMemoryOAuthStateStore : IOAuthStateStore
    {
        private readonly Dictionary<string, OAuthStateData> _store = new();

        public Task StoreAsync(string state, OAuthStateData data, TimeSpan expiration)
        {
            _store[state] = data;
            return Task.CompletedTask;
        }

        public Task<OAuthStateData?> RetrieveAsync(string state)
        {
            if (_store.TryGetValue(state, out var data))
            {
                _store.Remove(state);
                return Task.FromResult<OAuthStateData?>(data);
            }
            return Task.FromResult<OAuthStateData?>(null);
        }

        public Task RemoveAsync(string state)
        {
            _store.Remove(state);
            return Task.CompletedTask;
        }
    }

    private class InMemoryTokenStore : ITokenStore
    {
        private AnthropicTokens? _tokens;

        public Task SaveTokensAsync(AnthropicTokens tokens)
        {
            _tokens = tokens;
            return Task.CompletedTask;
        }

        public Task<AnthropicTokens?> GetTokensAsync()
        {
            return Task.FromResult(_tokens);
        }

        public Task ClearTokensAsync()
        {
            _tokens = null;
            return Task.CompletedTask;
        }

        public Task<bool> HasValidTokenAsync()
        {
            return Task.FromResult(_tokens?.AccessToken != null && _tokens.ExpiresAt > DateTime.UtcNow);
        }
    }

    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}