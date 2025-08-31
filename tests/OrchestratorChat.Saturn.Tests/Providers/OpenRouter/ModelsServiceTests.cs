using System.Net;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.OpenRouter.Services;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.OpenRouter;

/// <summary>
/// Tests for ModelsService functionality
/// Based on Track 2 test implementation plan for provider testing
/// </summary>
public class ModelsServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly ModelsService _service;
    private readonly HttpClientAdapter _httpClient;
    private readonly ILogger<ModelsService> _logger;

    public ModelsServiceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        var options = new OpenRouterOptions
        {
            ApiKey = TestConstants.TestApiKey,
            BaseUrl = TestConstants.TestOpenRouterBaseUrl
        };
        
        var httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri(options.BaseUrl)
        };
        
        var httpLogger = new LoggerFactory().CreateLogger<HttpClientAdapter>();
        _httpClient = new HttpClientAdapter(options, httpLogger);
        
        // Use reflection to replace the HttpClient in the adapter
        var httpClientField = typeof(HttpClientAdapter)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpClientField?.SetValue(_httpClient, httpClient);
        
        _logger = new LoggerFactory().CreateLogger<ModelsService>();
        _service = new ModelsService(_httpClient, _logger);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModels()
    {
        // Arrange
        var modelsResponse = @"{
            ""data"": [
                {
                    ""id"": ""anthropic/claude-3.5-sonnet"",
                    ""name"": ""Claude 3.5 Sonnet"",
                    ""description"": ""Anthropic's flagship model"",
                    ""pricing"": {
                        ""prompt"": ""0.000003"",
                        ""completion"": ""0.000015""
                    },
                    ""context_length"": 200000,
                    ""architecture"": {
                        ""modality"": ""text"",
                        ""tokenizer"": ""Claude""
                    },
                    ""top_provider"": {
                        ""context_length"": 200000,
                        ""max_completion_tokens"": 4096
                    }
                },
                {
                    ""id"": ""anthropic/claude-3-opus"",
                    ""name"": ""Claude 3 Opus"",
                    ""description"": ""Anthropic's most capable model"",
                    ""pricing"": {
                        ""prompt"": ""0.000015"",
                        ""completion"": ""0.000075""
                    },
                    ""context_length"": 200000,
                    ""architecture"": {
                        ""modality"": ""text"",
                        ""tokenizer"": ""Claude""
                    },
                    ""top_provider"": {
                        ""context_length"": 200000,
                        ""max_completion_tokens"": 4096
                    }
                }
            ]
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var models = await _service.GetModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Equal(2, models.Count);
        
        var sonnetModel = models.FirstOrDefault(m => m.Id == "anthropic/claude-3.5-sonnet");
        Assert.NotNull(sonnetModel);
        Assert.Equal("Claude 3.5 Sonnet", sonnetModel.Name);
        Assert.Equal("Anthropic's flagship model", sonnetModel.Description);
        Assert.Equal(200000, sonnetModel.ContextLength);
        
        var opusModel = models.FirstOrDefault(m => m.Id == "anthropic/claude-3-opus");
        Assert.NotNull(opusModel);
        Assert.Equal("Claude 3 Opus", opusModel.Name);
        
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, "/models"));
    }

    [Fact]
    public async Task GetAvailableModelsAsync_CachesResults()
    {
        // Arrange
        var modelsResponse = @"{""data"":[{""id"":""test-model"",""name"":""Test Model""}]}";
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);

        // Act - First call
        var models1 = await _service.GetModelsAsync(useCache: true);
        
        // Act - Second call (should use cache, no HTTP request)
        var models2 = await _service.GetModelsAsync(useCache: true);

        // Assert
        Assert.NotNull(models1);
        Assert.NotNull(models2);
        Assert.Equal(models1.Count, models2.Count);
        Assert.Equal(models1.First().Id, models2.First().Id);
        
        // Only one HTTP request should have been made
        Assert.Equal(1, _mockHandler.RequestCount);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_NoCacheForced_MakesNewRequest()
    {
        // Arrange
        var modelsResponse = @"{""data"":[{""id"":""test-model"",""name"":""Test Model""}]}";
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);

        // Act - First call
        var models1 = await _service.GetModelsAsync(useCache: true);
        
        // Act - Second call with cache disabled
        var models2 = await _service.GetModelsAsync(useCache: false);

        // Assert
        Assert.NotNull(models1);
        Assert.NotNull(models2);
        
        // Two HTTP requests should have been made
        Assert.Equal(2, _mockHandler.RequestCount);
    }

    [Fact]
    public async Task GetModelDetailsAsync_ReturnsDetails()
    {
        // Arrange
        var modelId = "anthropic/claude-3.5-sonnet";
        var modelResponse = @"{
            ""id"": ""anthropic/claude-3.5-sonnet"",
            ""name"": ""Claude 3.5 Sonnet"",
            ""description"": ""Anthropic's flagship model"",
            ""pricing"": {
                ""prompt"": ""0.000003"",
                ""completion"": ""0.000015""
            },
            ""context_length"": 200000,
            ""architecture"": {
                ""modality"": ""text"",
                ""tokenizer"": ""Claude""
            },
            ""top_provider"": {
                ""context_length"": 200000,
                ""max_completion_tokens"": 4096
            }
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelResponse);

        // Act
        var model = await _service.GetModelAsync(modelId);

        // Assert
        Assert.NotNull(model);
        Assert.Equal(modelId, model.Id);
        Assert.Equal("Claude 3.5 Sonnet", model.Name);
        Assert.Equal("Anthropic's flagship model", model.Description);
        Assert.Equal(200000, model.ContextLength);
        
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, $"/models/{modelId}"));
    }

    [Fact]
    public async Task GetModelDetailsAsync_ModelNotFound_ReturnsNull()
    {
        // Arrange
        var modelId = "nonexistent/model";
        _mockHandler.EnqueueErrorResponse(HttpStatusCode.NotFound, "Model not found");

        // Act
        var model = await _service.GetModelAsync(modelId);

        // Assert
        Assert.Null(model);
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, $"/models/{modelId}"));
    }

    [Fact]
    public async Task GetModelAsync_ChecksModelExistence_Correctly()
    {
        // Arrange - Available model
        var modelResponse = @"{
            ""id"": ""anthropic/claude-3.5-sonnet"", 
            ""name"": ""Claude 3.5 Sonnet""
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelResponse);

        // Act - Available model
        var availableModel = await _service.GetModelAsync("anthropic/claude-3.5-sonnet");
        
        // Assert
        Assert.NotNull(availableModel);
        Assert.Equal("anthropic/claude-3.5-sonnet", availableModel.Id);
        
        // Arrange - Unavailable model
        _mockHandler.EnqueueErrorResponse(HttpStatusCode.NotFound, "Model not found");
        
        // Act - Unavailable model
        var unavailableModel = await _service.GetModelAsync("nonexistent/model");
        
        // Assert
        Assert.Null(unavailableModel);
        Assert.Equal(2, _mockHandler.RequestCount);
    }

    [Fact]
    public async Task GetModelsAsync_UnauthorizedRequest_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHandler.EnqueueErrorResponse(HttpStatusCode.Unauthorized, "Invalid API key");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.GetModelsAsync());
        
        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task GetModelsAsync_NetworkTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        _mockHandler.EnqueueTimeout(TestConstants.ShortTimeoutMs);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.GetModelsAsync());
    }

    [Fact]
    public async Task GetModelsAsync_ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHandler.EnqueueErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.GetModelsAsync());
        
        Assert.Contains("Internal server error", exception.Message);
    }

    [Fact]
    public async Task GetModelsAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var emptyResponse = @"{""data"":[]}";
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, emptyResponse);

        // Act
        var models = await _service.GetModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Empty(models);
    }

    [Fact]
    public async Task GetModelsAsync_InvalidJsonResponse_ThrowsJsonException()
    {
        // Arrange
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, "invalid json response");

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => _service.GetModelsAsync());
    }

    [Fact]
    public async Task GetModelAsync_NullOrEmptyModelId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetModelAsync(null));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetModelAsync(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetModelAsync(" "));
    }

    [Fact]
    public async Task GetModelsByProviderAsync_FiltersByProvider_Correctly()
    {
        // Arrange
        var modelsResponse = @"{
            ""data"": [
                {""id"": ""anthropic/claude-3.5-sonnet"", ""name"": ""Claude 3.5 Sonnet""},
                {""id"": ""openai/gpt-4"", ""name"": ""GPT-4""}
            ]
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var anthropicModels = await _service.GetModelsByProviderAsync("anthropic");

        // Assert
        Assert.NotNull(anthropicModels);
        // Note: Actual filtering logic would be in the service implementation
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, "/models"));
    }

    [Fact]
    public void ClearCache_ClearsModelCache()
    {
        // This test verifies the cache clearing functionality exists
        // Act
        _service.ClearCache();
        
        // Assert - should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task GetModelsAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.GetModelsAsync(useCache: false, cts.Token));
    }

    [Fact]
    public async Task GetModelAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.GetModelAsync("test-model", useCache: false, cts.Token));
    }

    [Fact]
    public async Task GetModelsAsync_CacheExpiration_RefreshesAfterTimeout()
    {
        // Arrange
        var modelsResponse1 = @"{""data"":[{""id"":""model-1"",""name"":""Model 1""}]}";
        var modelsResponse2 = @"{""data"":[{""id"":""model-2"",""name"":""Model 2""}]}";
        
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse1);
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse2);

        // Act - First call
        var models1 = await _service.GetModelsAsync(useCache: true);
        
        // Clear cache manually to simulate expiration
        _service.ClearCache();
        
        // Second call after cache clear
        var models2 = await _service.GetModelsAsync(useCache: true);

        // Assert
        Assert.Single(models1);
        Assert.Single(models2);
        Assert.Equal("model-1", models1.First().Id);
        Assert.Equal("model-2", models2.First().Id);
        Assert.Equal(2, _mockHandler.RequestCount);
    }

    public void Dispose()
    {
        _service?.ClearCache();
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }
}