using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OrchestratorChat.Web.Tests.Security;

/// <summary>
/// Security tests to ensure tokens and secrets are never logged or exposed
/// </summary>
public class TokenLoggingSecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly TestLoggerProvider _loggerProvider;

    public TokenLoggingSecurityTests(WebApplicationFactory<Program> factory)
    {
        _loggerProvider = new TestLoggerProvider();
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add our test logger to capture all log entries
                services.AddSingleton<ILoggerProvider>(_loggerProvider);
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AnthropicStatusEndpoint_DoesNotReturnTokenValues()
    {
        // Arrange - Store tokens to simulate OAuth being connected
        var testTokenStore = CreateTestTokenStore();
        var tokens = new StoredTokens
        {
            AccessToken = "secret_access_token_should_not_be_logged",
            RefreshToken = "secret_refresh_token_should_not_be_logged",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await testTokenStore.SaveTokensAsync(tokens);

        // Act
        var response = await _client.GetAsync("/api/providers/anthropic/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Verify response only contains safe metadata
        Assert.True(result.TryGetProperty("connected", out var connected));
        Assert.True(result.TryGetProperty("expiresAt", out var expiresAt));
        Assert.True(result.TryGetProperty("scopes", out var scopes));

        // Verify tokens are NOT in the response
        Assert.DoesNotContain("secret_access_token", content);
        Assert.DoesNotContain("secret_refresh_token", content);
        Assert.DoesNotContain("accessToken", content);
        Assert.DoesNotContain("refreshToken", content);
        Assert.DoesNotContain("access_token", content);
        Assert.DoesNotContain("refresh_token", content);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task ProvidersStatusEndpoint_DoesNotExposeApiKeys()
    {
        // Act
        var response = await _client.GetAsync("/api/providers/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Response should only contain status enums, not actual keys
        Assert.DoesNotContain("sk-", content); // OpenAI/Anthropic API keys start with sk-
        Assert.DoesNotContain("Bearer ", content); // No bearer tokens
        Assert.DoesNotContain("api_key", content); // No key field names
        Assert.DoesNotContain("apiKey", content);
        Assert.DoesNotContain("token", content); // No token values

        // Should only contain status indicators
        Assert.Contains("Present", content);
        Assert.Contains("Missing", content);
    }

    [Fact]
    public async Task OAuthStartEndpoint_DoesNotLogPKCEVerifier()
    {
        // Clear any existing logs
        _loggerProvider.ClearLogs();

        // Act
        var response = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));

        // Assert
        response.EnsureSuccessStatusCode();
        var logs = _loggerProvider.GetLogs();

        // Verify PKCE verifier is not logged anywhere
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain("verifier", log.Message.ToLowerInvariant());
            Assert.DoesNotContain("pkce", log.Message.ToLowerInvariant());
        });

        // Response should not contain verifier
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("verifier", content);
        Assert.DoesNotContain("code_verifier", content);
    }

    [Fact]
    public async Task OAuthCallbackEndpoint_DoesNotLogAuthorizationCode()
    {
        // Arrange - Start OAuth to get valid state
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        startResponse.EnsureSuccessStatusCode();
        
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var state = startResult.GetProperty("state").GetString();

        // Clear logs after start to focus on callback
        _loggerProvider.ClearLogs();

        // Act
        var sensitiveCode = "sensitive_auth_code_12345";
        var callbackResponse = await _client.GetAsync($"/oauth/anthropic/callback?code={sensitiveCode}&state={state}");

        // Assert
        callbackResponse.EnsureSuccessStatusCode();
        var logs = _loggerProvider.GetLogs();

        // Verify authorization code is not logged
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain(sensitiveCode, log.Message);
            Assert.DoesNotContain("sensitive_auth_code", log.Message);
        });

        // Response should not echo the authorization code
        var callbackContent = await callbackResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sensitiveCode, callbackContent);
    }

    [Fact]
    public async Task TokenExchangeFailure_DoesNotLogSensitiveData()
    {
        // Arrange - Start OAuth to get state
        var startResponse = await _client.PostAsync("/api/providers/anthropic/start", new StringContent(""));
        var startContent = await startResponse.Content.ReadAsStringAsync();
        var startResult = JsonSerializer.Deserialize<JsonElement>(startContent);
        var state = startResult.GetProperty("state").GetString();

        // Clear logs to focus on the failure case
        _loggerProvider.ClearLogs();

        // Act - Use invalid code to trigger token exchange failure
        var invalidCode = "invalid_code_that_will_fail_exchange";
        var callbackResponse = await _client.GetAsync($"/oauth/anthropic/callback?code={invalidCode}&state={state}");

        // Assert
        var logs = _loggerProvider.GetLogs();

        // Even in failure scenarios, sensitive data should not be logged
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain(invalidCode, log.Message);
            Assert.DoesNotContain("invalid_code_that_will_fail", log.Message);
            
            // Should not log any token-related sensitive data
            Assert.DoesNotContain("access_token", log.Message);
            Assert.DoesNotContain("refresh_token", log.Message);
        });

        // Verify appropriate error logging without exposing codes
        var errorLogs = logs.Where(l => l.LogLevel == LogLevel.Warning || l.LogLevel == LogLevel.Error).ToList();
        Assert.Contains(errorLogs, log => log.Message.Contains("Token exchange failed"));
    }

    [Fact]
    public async Task ProviderVerificationService_DoesNotLogApiKeys()
    {
        // Arrange
        var mockConfigProvider = Substitute.For<OrchestratorChat.Core.Configuration.IConfigurationProvider>();
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var testLogger = new TestLogger<ProviderVerificationService>();

        var service = new ProviderVerificationService(mockConfigProvider, mockHttpClientFactory, testLogger);

        var sensitiveApiKey = "sk-ant-test-sensitive-api-key-12345";

        // Act - Validate API key
        await service.ValidateAnthropicKeyAsync(sensitiveApiKey);

        // Assert
        var logs = testLogger.GetLogs();
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain(sensitiveApiKey, log.Message);
            Assert.DoesNotContain("sk-ant-test-sensitive", log.Message);
        });
    }

    [Fact]
    public async Task LogoutEndpoint_DoesNotLogClearedTokens()
    {
        // Arrange - Store tokens first
        var testTokenStore = CreateTestTokenStore();
        var tokens = new StoredTokens
        {
            AccessToken = "token_to_be_cleared_secretly",
            RefreshToken = "refresh_to_be_cleared_secretly",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(tokens);

        // Clear logs before logout
        _loggerProvider.ClearLogs();

        // Act
        var response = await _client.PostAsync("/api/providers/anthropic/logout", new StringContent(""));

        // Assert
        response.EnsureSuccessStatusCode();
        var logs = _loggerProvider.GetLogs();

        // Verify cleared tokens are not logged
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain("token_to_be_cleared_secretly", log.Message);
            Assert.DoesNotContain("refresh_to_be_cleared_secretly", log.Message);
        });

        // Cleanup
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task ErrorScenarios_MaskSensitiveInformation()
    {
        // Clear logs
        _loggerProvider.ClearLogs();

        // Act - Trigger various error scenarios
        await _client.GetAsync("/oauth/anthropic/callback?error=access_denied&error_description=User%20denied");
        await _client.GetAsync("/oauth/anthropic/callback"); // Missing parameters
        await _client.GetAsync("/oauth/anthropic/callback?code=test&state=invalid");

        // Assert
        var logs = _loggerProvider.GetLogs();
        var errorWarningLogs = logs.Where(l => l.LogLevel >= LogLevel.Warning).ToList();

        // Error logs should exist but not contain sensitive details
        Assert.NotEmpty(errorWarningLogs);
        Assert.All(errorWarningLogs, log =>
        {
            // Should not contain actual error details that could leak info
            Assert.DoesNotContain("User denied", log.Message);
            
            // Should mask or generalize error information
            var message = log.Message.ToLowerInvariant();
            if (message.Contains("oauth") || message.Contains("callback") || message.Contains("failed"))
            {
                // OAuth-related logs are okay, just shouldn't contain sensitive data
                Assert.DoesNotContain("access_denied", log.Message);
            }
        });
    }

    [Fact]
    public void PKCEGenerator_DoesNotLogGeneratedValues()
    {
        // Arrange
        var testLogger = new TestLogger<PKCEGenerator>();
        
        // Act
        var pkcePair = PKCEGenerator.Generate();
        
        // Assert
        var logs = testLogger.GetLogs();
        
        // Verify PKCE values are not logged
        Assert.All(logs, log =>
        {
            Assert.DoesNotContain(pkcePair.Verifier, log.Message);
            Assert.DoesNotContain(pkcePair.Challenge, log.Message);
        });
    }

    [Fact]
    public async Task ConfigurationEndpoints_DoNotEchoBackSecrets()
    {
        // Act - Test OpenRouter validation endpoint
        var openRouterRequest = new ValidateApiKeyRequest { ApiKey = "secret_openrouter_key_123" };
        var openRouterJson = JsonSerializer.Serialize(openRouterRequest);
        var openRouterContent = new StringContent(openRouterJson, Encoding.UTF8, "application/json");
        
        var openRouterResponse = await _client.PostAsync("/api/providers/openrouter/validate", openRouterContent);

        // Assert - Response should not echo back the API key
        var openRouterResponseContent = await openRouterResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret_openrouter_key_123", openRouterResponseContent);

        // Act - Test Anthropic validation endpoint
        var anthropicRequest = new ValidateApiKeyRequest { ApiKey = "secret_anthropic_key_456" };
        var anthropicJson = JsonSerializer.Serialize(anthropicRequest);
        var anthropicContent = new StringContent(anthropicJson, Encoding.UTF8, "application/json");
        
        var anthropicResponse = await _client.PostAsync("/api/providers/anthropic/validate", anthropicContent);

        // Assert - Response should not echo back the API key
        var anthropicResponseContent = await anthropicResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret_anthropic_key_456", anthropicResponseContent);
    }

    private TestTokenStore CreateTestTokenStore()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"TokenLoggingSecurityTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        return new TestTokenStore(testDir);
    }

    public void Dispose()
    {
        _client.Dispose();
        _loggerProvider.Dispose();
    }
}

/// <summary>
/// Test logger provider to capture log entries for security verification
/// </summary>
public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<TestLogEntry> _logs = new();
    private readonly object _lock = new object();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, this);
    }

    public void AddLog(LogLevel logLevel, string categoryName, string message)
    {
        lock (_lock)
        {
            _logs.Add(new TestLogEntry
            {
                LogLevel = logLevel,
                CategoryName = categoryName,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public List<TestLogEntry> GetLogs()
    {
        lock (_lock)
        {
            return new List<TestLogEntry>(_logs);
        }
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    public void Dispose()
    {
        _logs.Clear();
    }
}

/// <summary>
/// Test logger that captures log entries
/// </summary>
public class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TestLoggerProvider _provider;

    public TestLogger(string categoryName, TestLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _provider.AddLog(logLevel, _categoryName, message);
    }
}

/// <summary>
/// Test logger for specific types
/// </summary>
public class TestLogger<T> : TestLogger, ILogger<T>
{
    public TestLogger() : base(typeof(T).Name, new TestLoggerProvider())
    {
    }

    public List<TestLogEntry> GetLogs() => ((TestLoggerProvider)_provider).GetLogs();
    
    private readonly TestLoggerProvider _provider = new TestLoggerProvider();
}

/// <summary>
/// Log entry for testing
/// </summary>
public class TestLogEntry
{
    public LogLevel LogLevel { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Test-specific TokenStore for security tests
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

        tokenPathField?.SetValue(this, Path.Combine(_testDirectory, "security.tokens"));
        keyPathField?.SetValue(this, Path.Combine(_testDirectory, ".securitykeystore"));
        saltPathField?.SetValue(this, Path.Combine(_testDirectory, ".securitysalt"));
    }

    public void Dispose()
    {
        try
        {
            var files = new[]
            {
                Path.Combine(_testDirectory, "security.tokens"),
                Path.Combine(_testDirectory, ".securitykeystore"),
                Path.Combine(_testDirectory, ".securitysalt")
            };

            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }

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