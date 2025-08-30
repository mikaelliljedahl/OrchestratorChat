using System.Net;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.OpenRouter.Services;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.OpenRouter;

/// <summary>
/// Tests for ChatCompletionsService functionality
/// Based on Track 2 test implementation plan for provider testing
/// </summary>
public class ChatCompletionsServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly ChatCompletionsService _service;
    private readonly HttpClientAdapter _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<ChatCompletionsService> _logger;

    public ChatCompletionsServiceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHandler);
        
        _options = new OpenRouterOptions
        {
            ApiKey = TestConstants.TestApiKey,
            BaseUrl = TestConstants.TestOpenRouterBaseUrl,
            DefaultModel = TestConstants.ValidOpenRouterModel,
            DefaultTemperature = TestConstants.DefaultTemperature,
            DefaultMaxTokens = TestConstants.DefaultMaxTokens
        };
        
        var httpLogger = new LoggerFactory().CreateLogger<HttpClientAdapter>();
        _httpClient = new HttpClientAdapter(_options, httpLogger);
        
        // Use reflection to replace the HttpClient in the adapter
        var httpClientField = typeof(HttpClientAdapter)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpClientField?.SetValue(_httpClient, httpClient);
        
        _logger = new LoggerFactory().CreateLogger<ChatCompletionsService>();
        _service = new ChatCompletionsService(_httpClient, _options, _logger);
    }

    [Fact]
    public async Task CreateAsync_ValidMessages_ReturnsCompletion()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello, how are you?" }
            },
            Temperature = 0.7,
            MaxTokens = 1000
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("chatcmpl-123", result.Id);
        Assert.Equal("anthropic/claude-3.5-sonnet", result.Model);
        Assert.NotEmpty(result.Choices);
        Assert.Equal("Hello from OpenRouter", result.Choices.First().Message?.Content);
        Assert.NotNull(result.Usage);
        Assert.Equal(25, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task CreateAsync_SystemPrompt_IncludedCorrectly()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "system", Content = "You are a helpful assistant." },
                new Message { Role = "user", Content = "Hello" }
            }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify that system message was included in the request
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            "You are a helpful assistant"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            "system"));
    }

    [Fact]
    public async Task CreateStreamAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Tell me a story" }
            },
            Stream = true
        };

        var streamChunks = new[]
        {
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": ""Once""}}]}",
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": "" upon""}}]}",
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": "" a time""}}]}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<StreamChunk>();
        await foreach (var chunk in _service.CreateCompletionStreamAsync(request))
        {
            result.Add(chunk);
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, chunk => Assert.Equal("chatcmpl-123", chunk.Id));
        
        var content = string.Join("", result.Select(c => c.Choices?.FirstOrDefault()?.Delta?.Content ?? ""));
        Assert.Equal("Once upon a time", content);
    }

    [Fact]
    public async Task CreateAsync_MaxTokensRespected()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Generate a long response" }
            },
            MaxTokens = 100
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify that max_tokens was sent in the request
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "max_tokens"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "100"));
    }

    [Fact]
    public async Task CreateAsync_TemperatureApplied()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Be creative" }
            },
            Temperature = 0.9
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify that temperature was sent in the request
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "temperature"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "0.9"));
    }

    [Fact]
    public async Task CreateSimpleCompletionAsync_ValidPrompt_ReturnsCompletion()
    {
        // Arrange
        var prompt = "What is the capital of France?";
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateSimpleCompletionAsync(prompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello from OpenRouter", result.Choices.First().Message?.Content);
        
        // Verify the prompt was converted to a user message
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", prompt));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "user"));
    }

    [Fact]
    public async Task CreateSimpleCompletionStreamAsync_ValidPrompt_StreamsResponse()
    {
        // Arrange
        var prompt = "Write a haiku about programming";
        
        var streamChunks = new[]
        {
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": ""Code flows""}}]}",
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": "" like water""}}]}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<StreamChunk>();
        await foreach (var chunk in _service.CreateSimpleCompletionStreamAsync(prompt))
        {
            result.Add(chunk);
        }

        // Assert
        Assert.Equal(2, result.Count);
        var content = string.Join("", result.Select(c => c.Choices?.FirstOrDefault()?.Delta?.Content ?? ""));
        Assert.Equal("Code flows like water", content);
    }

    [Fact]
    public async Task CreateAsync_WithTools_IncludesToolDefinitions()
    {
        // Arrange
        var tools = new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Type = "function",
                Function = new ToolFunction
                {
                    Name = "get_weather",
                    Description = "Get current weather",
                    Parameters = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["location"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "City name"
                            }
                        }
                    }
                }
            }
        };

        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "What's the weather like?" }
            },
            Tools = tools
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify tools were included in the request
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "tools"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "get_weather"));
    }

    [Fact]
    public async Task CreateAsync_InvalidModel_ThrowsException()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "invalid-model",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Test" }
            }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.BadRequest, "Model not found");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.CreateCompletionAsync(request));
        
        Assert.Contains("Model not found", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_RateLimited_ThrowsException()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Test rate limit" }
            }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.TooManyRequests, "Rate limit exceeded");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.CreateCompletionAsync(request));
        
        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_AppliesDefaultOptions()
    {
        // Arrange - request without explicit temperature/max_tokens
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Test defaults" }
            }
            // No Temperature or MaxTokens set explicitly
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify default values were applied
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            TestConstants.DefaultTemperature.ToString()));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            TestConstants.DefaultMaxTokens.ToString()));
    }

    [Fact]
    public async Task CreateAsync_EnsuresStreamingDisabled()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Non-streaming test" }
            },
            Stream = true // This should be overridden to false
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _service.CreateCompletionAsync(request);

        // Assert
        Assert.NotNull(result);
        
        // Verify stream was set to false
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            @"""stream"":false"));
    }

    [Fact]
    public async Task CreateStreamAsync_EnsuresStreamingEnabled()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = TestConstants.ValidOpenRouterModel,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Streaming test" }
            },
            Stream = false // This should be overridden to true
        };

        var streamChunks = new[]
        {
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": ""Test""}}]}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<StreamChunk>();
        await foreach (var chunk in _service.CreateCompletionStreamAsync(request))
        {
            result.Add(chunk);
        }

        // Assert
        Assert.Single(result);
        
        // Verify stream was set to true
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", 
            @"""stream"":true"));
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateCompletionAsync(null));
    }

    [Fact]
    public async Task CreateStreamAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var chunk in _service.CreateCompletionStreamAsync(null))
            {
                // Should not reach here
            }
        });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }
}