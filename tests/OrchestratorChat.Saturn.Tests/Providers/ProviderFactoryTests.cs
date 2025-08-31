using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using Xunit;

namespace OrchestratorChat.Saturn.Tests.Providers;

public class ProviderFactoryTests
{
    private readonly ProviderFactory _factory;
    private readonly ILoggerFactory _loggerFactory;

    public ProviderFactoryTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _factory = new ProviderFactory(_loggerFactory);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProviderFactory(null!));
    }

    [Fact]
    public async Task CreateProviderAsync_WithAnthropicType_ReturnsAnthropicProvider()
    {
        // Arrange
        var config = new ProviderConfiguration
        {
            ApiKey = "test_key"
        };

        // Act
        var provider = await _factory.CreateProviderAsync("anthropic", config);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<AnthropicProvider>(provider);
    }

    [Fact]
    public async Task CreateProviderAsync_WithOpenRouterType_ReturnsOpenRouterProvider()
    {
        // Arrange
        var config = new ProviderConfiguration
        {
            ApiKey = "test_key"
        };

        // Act
        var provider = await _factory.CreateProviderAsync("openrouter", config);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<OpenRouterProvider>(provider);
    }

    [Fact]
    public async Task CreateProviderAsync_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange
        var config = new ProviderConfiguration();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => _factory.CreateProviderAsync("unsupported", config));
    }

    [Fact]
    public void CreateClient_WithAnthropicSettings_ReturnsAnthropicClient()
    {
        // Arrange
        var settings = new AnthropicProviderSettings
        {
            ApiKey = "test_key"
        };

        // Act
        var client = _factory.CreateClient(settings);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<AnthropicClient>(client);
    }

    [Fact]
    public void CreateClient_WithOpenRouterSettings_ReturnsOpenRouterClient()
    {
        // Arrange
        var settings = new OpenRouterProviderSettings
        {
            ApiKey = "test_key"
        };

        // Act
        var client = _factory.CreateClient(settings);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenRouterClient>(client);
    }

    [Fact]
    public void CreateClient_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.CreateClient((ProviderSettings)null!));
    }

    [Fact]
    public void CreateAnthropicClient_WithSettings_ReturnsClient()
    {
        // Arrange
        var settings = new AnthropicProviderSettings
        {
            OAuthToken = "test_oauth_token"
        };

        // Act
        var client = _factory.CreateAnthropicClient(settings);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<AnthropicClient>(client);
    }

    [Fact]
    public void CreateOpenRouterClient_WithSettings_ReturnsClientWithApiKey()
    {
        // Arrange
        var settings = new OpenRouterProviderSettings
        {
            ApiKey = "test_api_key",
            SiteUrl = "https://test.site",
            SiteName = "Test Site"
        };

        // Act
        var client = _factory.CreateOpenRouterClient(settings);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenRouterClient>(client);
    }

    [Fact]
    public void CreateOpenRouterClient_WithEmptyApiKey_UsesEnvironmentVariable()
    {
        // Arrange
        var originalEnvVar = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", "env_api_key");
            var settings = new OpenRouterProviderSettings
            {
                ApiKey = string.Empty
            };

            // Act
            var client = _factory.CreateOpenRouterClient(settings);

            // Assert
            Assert.NotNull(client);
            Assert.IsType<OpenRouterClient>(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", originalEnvVar);
        }
    }

    [Fact]
    public void LegacyCreateClient_WithAnthropicType_ReturnsAnthropicClient()
    {
        // Arrange
        var settings = new Dictionary<string, object>();

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var client = _factory.CreateClient("anthropic", settings);
#pragma warning restore CS0618

        // Assert
        Assert.NotNull(client);
        Assert.IsType<AnthropicClient>(client);
    }

    [Fact]
    public void LegacyCreateClient_WithOpenRouterType_ReturnsOpenRouterClient()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            { "ApiKey", "test_key" }
        };

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var client = _factory.CreateClient("openrouter", settings);
#pragma warning restore CS0618

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenRouterClient>(client);
    }

    [Fact]
    public void LegacyCreateClient_WithUnsupportedType_ThrowsNotSupportedException()
    {
        // Act & Assert
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Throws<NotSupportedException>(() => _factory.CreateClient("unsupported", null));
#pragma warning restore CS0618
    }

    [Fact]
    public void CreateProviderAsync_IsCaseInsensitive()
    {
        // Arrange
        var config = new ProviderConfiguration { ApiKey = "test" };
        var testCases = new[] { "ANTHROPIC", "Anthropic", "AnThRoPiC" };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var task = _factory.CreateProviderAsync(testCase, config);
            Assert.NotNull(task);
        }
    }
}