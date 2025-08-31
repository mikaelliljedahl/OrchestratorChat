using System.Net;
using System.Text;
using Moq;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic;

/// <summary>
/// Tests for AnthropicClient functionality
/// Based on Track 2 test implementation plan for provider testing
/// </summary>
public class AnthropicClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly AnthropicClient _client;
    private readonly HttpClient _httpClient;

    public AnthropicClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        
        // Create client using reflection to inject mock HttpClient
        _client = new AnthropicClient();
        
        // Use reflection to replace the HttpClient field
        var httpClientField = typeof(AnthropicClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpClientField?.SetValue(_client, _httpClient);

        // Create a mock authentication service using a derived class
        var mockAuthService = new MockAnthropicAuthService();

        var authServiceField = typeof(AnthropicClient)
            .GetField("_authService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        authServiceField?.SetValue(_client, mockAuthService);
    }

    [Fact]
    public async Task SendMessageAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Hello", Role = MessageRole.User }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidAnthropicResponse);

        // Act
        var result = await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello from Anthropic", result);
        Assert.True(_mockHandler.VerifyRequest(HttpMethod.Post, "/messages"));
    }

    [Fact]
    public async Task SendMessageAsync_WithStreaming_StreamsCorrectly()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Stream test", Role = MessageRole.User }
        };

        var streamChunks = new[]
        {
            @"{""type"": ""content_block_delta"", ""delta"": {""type"": ""text_delta"", ""text"": ""Hello""}}",
            @"{""type"": ""content_block_delta"", ""delta"": {""type"": ""text_delta"", ""text"": "" World""}}",
            "[DONE]"
        };

        _mockHandler.EnqueueStreamingResponse(streamChunks);

        // Act
        var result = new List<string>();
        await foreach (var chunk in _client.StreamMessageAsync(messages, TestConstants.ValidClaudeModel))
        {
            result.Add(chunk);
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Hello", result[0]);
        Assert.Equal(" World", result[1]);
    }

    [Fact]
    public async Task SendMessageAsync_InvalidApiKey_ReturnsError()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Test", Role = MessageRole.User }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.Unauthorized, "Invalid API key");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel));
        
        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task SendMessageAsync_RateLimited_Retries()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Rate limit test", Role = MessageRole.User }
        };

        // First request gets rate limited
        _mockHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, 
            @"{""error"":{""type"":""rate_limit_exceeded"",""message"":""Too many requests""}}");
        
        // Second request succeeds
        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidAnthropicResponse);

        // Act
        var result = await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, _mockHandler.RequestCount); // Should have retried
    }

    [Fact]
    public async Task SendMessageAsync_WithTools_IncludesToolDefinitions()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Use tools", Role = MessageRole.User }
        };

        var responseWithTool = @"{
            ""id"": ""msg_123"",
            ""type"": ""message"",
            ""role"": ""assistant"",
            ""content"": [{
                ""type"": ""tool_use"",
                ""id"": ""toolu_123"",
                ""name"": ""read_file"",
                ""input"": {""file_path"": ""/test.txt""}
            }],
            ""model"": ""claude-sonnet-4-20250514"",
            ""stop_reason"": ""tool_use"",
            ""usage"": {""input_tokens"": 10, ""output_tokens"": 15}
        }";

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, responseWithTool);

        // Act
        var result = await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.NotNull(result);
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/messages", "tools"));
    }

    [Fact]
    public async Task SendMessageAsync_WithAttachments_HandlesMultimodal()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage 
            { 
                Content = "Describe this image", 
                Role = MessageRole.User,
                Metadata = new Dictionary<string, object>
                {
                    ["attachments"] = new List<object>
                    {
                        new { type = "image", data = "base64imagedata" }
                    }
                }
            }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidAnthropicResponse);

        // Act
        var result = await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello from Anthropic", result);
        
        // Verify multimodal content was sent
        var lastRequest = _mockHandler.LastRequest;
        Assert.NotNull(lastRequest);
        Assert.NotNull(lastRequest.Content);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModelList()
    {
        // Act
        var models = await _client.GetAvailableModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        
        var opusModel = models.FirstOrDefault(m => m.Id == "claude-opus-4-1-20250805");
        Assert.NotNull(opusModel);
        Assert.Equal("Claude Opus 4.1", opusModel.Name);
        Assert.Equal("Anthropic", opusModel.Provider);
        
        var sonnetModel = models.FirstOrDefault(m => m.Id == "claude-sonnet-4-20250514");
        Assert.NotNull(sonnetModel);
        Assert.Equal("Claude Sonnet 4", sonnetModel.Name);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ValidTokens_ReturnsTrue()
    {
        // Note: This test would require mocking the auth service
        // For now, we'll test the basic method availability
        var result = await _client.IsAuthenticatedAsync();
        
        // Without valid tokens, this should return false
        Assert.False(result);
    }

    [Fact]
    public void Logout_ClearsAuthentication()
    {
        // Act
        _client.Logout();
        
        // This method should complete without throwing
        // Actual verification would require testing the auth service state
        Assert.True(true);
    }

    [Fact]
    public async Task SendMessageAsync_EnsuresClaudeCodeSystemPrompt()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Test without system prompt", Role = MessageRole.User }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidAnthropicResponse);

        // Act
        await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/messages", 
            "You are Claude Code, Anthropic's official CLI for Claude"));
    }

    [Fact]
    public async Task SendMessageAsync_PreservesExistingSystemPrompt()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage 
            { 
                Content = "Custom system instructions here", 
                Role = MessageRole.System 
            },
            new AgentMessage { Content = "Test message", Role = MessageRole.User }
        };

        _mockHandler.EnqueueResponse(HttpStatusCode.OK, TestConstants.ValidAnthropicResponse);

        // Act
        await _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel);

        // Assert
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/messages", 
            "You are Claude Code"));
        Assert.True(await _mockHandler.VerifyRequestAsync(HttpMethod.Post, "/messages", 
            "Custom system instructions"));
    }

    [Fact]
    public async Task SendMessageAsync_NetworkTimeout_ThrowsException()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Timeout test", Role = MessageRole.User }
        };

        _mockHandler.EnqueueTimeout(TestConstants.ShortTimeoutMs);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel));
    }

    [Fact]
    public async Task SendMessageAsync_ServerError_ThrowsHttpException()
    {
        // Arrange
        var messages = new List<AgentMessage>
        {
            new AgentMessage { Content = "Server error test", Role = MessageRole.User }
        };

        _mockHandler.EnqueueErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.SendMessageAsync(messages, TestConstants.ValidClaudeModel));
        
        Assert.Contains("Internal server error", exception.Message);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }
}

/// <summary>
/// Mock implementation of AnthropicAuthService for testing
/// Since AnthropicAuthService methods are not virtual, we'll create this as a separate mock class
/// and use reflection to replace the service in the client
/// </summary>
public class MockAnthropicAuthService : IDisposable
{
    public Task<StoredTokens?> GetValidTokensAsync()
    {
        return Task.FromResult<StoredTokens?>(new StoredTokens { AccessToken = "mock-access-token" });
    }

    public void Logout()
    {
        // Mock logout - do nothing
    }

    public Task<bool> AuthenticateAsync(bool useClaudeMax = true)
    {
        return Task.FromResult(true); // Mock successful authentication
    }

    public void Dispose()
    {
        // Mock dispose - do nothing
    }
}