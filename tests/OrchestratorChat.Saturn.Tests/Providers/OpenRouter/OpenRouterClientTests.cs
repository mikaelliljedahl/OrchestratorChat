using System.Net;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.OpenRouter;

/// <summary>
/// Tests for OpenRouterClient functionality
/// Based on Track 2 test implementation plan for provider testing
/// </summary>
public class OpenRouterClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly OpenRouterClient _client;
    private readonly ILogger<OpenRouterClient> _logger;

    public OpenRouterClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHandler);
        
        _logger = new LoggerFactory().CreateLogger<OpenRouterClient>();
        
        var options = new OpenRouterOptions
        {
            ApiKey = TestConstants.TestApiKey,
            BaseUrl = TestConstants.TestOpenRouterBaseUrl,
            DefaultModel = TestConstants.ValidOpenRouterModel
        };
        
        _client = new OpenRouterClient(options, _logger);
        
        // Use reflection to replace the HttpClientAdapter's HttpClient
        var adapterField = typeof(OpenRouterClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (adapterField?.GetValue(_client) is HttpClientAdapter adapter)
        {
            var httpClientField = typeof(HttpClientAdapter)
                .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(adapter, httpClient);
        }
    }

    [Fact]
    public async Task CreateChatCompletionAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Hello" }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _client.ChatAsync(messages, TestConstants.ValidOpenRouterModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("chatcmpl-123", result.Id);
        Assert.NotEmpty(result.Choices);
        Assert.Equal("Hello from OpenRouter", result.Choices.First().Message?.Content);
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Post, "/chat/completions"));
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithStreaming_StreamsCorrectly()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Stream test" }
        };

        var streamChunks = new[]
        {
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": ""Hello""}}]}",
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": "" World""}}]}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<string>();
        await foreach (var chunk in _client.ChatStreamAsync(messages, TestConstants.ValidOpenRouterModel))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                result.Add(content);
            }
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[0]);
        Assert.Equal(" World", result[1]);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_InvalidApiKey_ReturnsError()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Test" }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.Unauthorized, "Invalid API key");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.ChatAsync(messages, TestConstants.ValidOpenRouterModel));
        
        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_ModelNotAvailable_ReturnsError()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Test" }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.BadRequest, "Model not available");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.ChatAsync(messages, "invalid-model"));
        
        Assert.Contains("Model not available", exception.Message);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelList()
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
                }
            ]
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var models = await _client.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        
        var claudeModel = models.FirstOrDefault(m => m.Id == "anthropic/claude-3.5-sonnet");
        Assert.NotNull(claudeModel);
        Assert.Equal("Claude 3.5 Sonnet", claudeModel.Name);
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, "/models"));
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WithTools_SendsCorrectly()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Use a tool" }
        };

        var tools = new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Type = "function",
                Function = new ToolFunction
                {
                    Name = "read_file",
                    Description = "Read file contents",
                    Parameters = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["file_path"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "Path to file"
                            }
                        },
                        ["required"] = new[] { "file_path" }
                    }
                }
            }
        };

        var responseWithTool = @"{
            ""id"": ""chatcmpl-123"",
            ""object"": ""chat.completion"",
            ""created"": 1677652288,
            ""model"": ""anthropic/claude-3.5-sonnet"",
            ""choices"": [{
                ""index"": 0,
                ""message"": {
                    ""role"": ""assistant"",
                    ""content"": null,
                    ""tool_calls"": [{
                        ""id"": ""call_123"",
                        ""type"": ""function"",
                        ""function"": {
                            ""name"": ""read_file"",
                            ""arguments"": ""{\""file_path\"": \""/test.txt\""}""
                        }
                    }]
                },
                ""finish_reason"": ""tool_calls""
            }],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 15,
                ""total_tokens"": 25
            }
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, responseWithTool);

        // Act
        var result = await _client.ChatAsync(messages, TestConstants.ValidOpenRouterModel, tools: tools);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Choices);
        Assert.NotNull(result.Choices.First().Message?.ToolCalls);
        Assert.NotEmpty(result.Choices.First().Message.ToolCalls);
        
        var toolCall = result.Choices.First().Message.ToolCalls.First();
        Assert.Equal("read_file", toolCall.Function?.Name);
        
        // Verify tools were sent in request
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "tools"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/chat/completions", "read_file"));
    }

    [Fact]
    public async Task CompleteAsync_SimplePrompt_ReturnsText()
    {
        // Arrange
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidOpenRouterResponse);

        // Act
        var result = await _client.CompleteAsync("Hello world", TestConstants.ValidOpenRouterModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello from OpenRouter", result);
    }

    [Fact]
    public async Task CompleteStreamAsync_SimplePrompt_StreamsText()
    {
        // Arrange
        var streamChunks = new[]
        {
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": ""Streaming""}}]}",
            @"{""id"": ""chatcmpl-123"", ""choices"": [{""delta"": {""content"": "" response""}}]}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<string>();
        await foreach (var chunk in _client.CompleteStreamAsync("Hello", TestConstants.ValidOpenRouterModel))
        {
            result.Add(chunk);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Streaming", result[0]);
        Assert.Equal(" response", result[1]);
    }

    [Fact]
    public async Task GetModelAsync_SpecificModel_ReturnsModelInfo()
    {
        // Arrange
        var modelResponse = @"{
            ""id"": ""anthropic/claude-3.5-sonnet"",
            ""name"": ""Claude 3.5 Sonnet"",
            ""description"": ""Anthropic's flagship model"",
            ""pricing"": {
                ""prompt"": ""0.000003"",
                ""completion"": ""0.000015""
            },
            ""context_length"": 200000
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, modelResponse);

        // Act
        var model = await _client.GetModelAsync("anthropic/claude-3.5-sonnet");

        // Assert
        Assert.NotNull(model);
        Assert.Equal("anthropic/claude-3.5-sonnet", model.Id);
        Assert.Equal("Claude 3.5 Sonnet", model.Name);
    }

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, @"{""data"":[]}");

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        Assert.True(result);
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Get, "/models"));
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        _mockHandler.EnqueueErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetClientInfo_ReturnsConfigurationInfo()
    {
        // Act
        var info = _client.GetClientInfo();

        // Assert
        Assert.NotNull(info);
        Assert.Equal(TestConstants.TestOpenRouterBaseUrl, info.BaseUrl);
        Assert.Equal(TestConstants.ValidOpenRouterModel, info.DefaultModel);
        Assert.True(info.EnableStreaming);
        Assert.True(info.EnableTools);
    }

    [Fact]
    public async Task ChatAsync_EmptyMessages_ThrowsArgumentException()
    {
        // Arrange
        var emptyMessages = new List<Message>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _client.ChatAsync(emptyMessages));
    }

    [Fact]
    public async Task CompleteAsync_EmptyPrompt_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _client.CompleteAsync(string.Empty));
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => _client.CompleteAsync(null));
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Act
        _client.Dispose();

        // Assert - should not throw
        Assert.Throws<ObjectDisposedException>(() => _client.GetClientInfo());
    }

    [Fact]
    public async Task CreateChatCompletionAsync_NetworkTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Timeout test" }
        };

        _mockHandler.EnqueueTimeout(TestConstants.ShortTimeoutMs);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _client.ChatAsync(messages, TestConstants.ValidOpenRouterModel));
    }

    public void Dispose()
    {
        _client?.Dispose();
        _mockHandler?.Dispose();
    }
}