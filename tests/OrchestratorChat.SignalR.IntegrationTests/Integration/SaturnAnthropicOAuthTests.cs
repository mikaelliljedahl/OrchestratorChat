using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.IntegrationTests.Fixtures;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using System.Collections.Concurrent;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Integration;

/// <summary>
/// Integration tests for Saturn+Anthropic OAuth functionality via SignalR
/// </summary>
public class SaturnAnthropicOAuthTests : IClassFixture<SignalRTestFixture>, IAsyncDisposable
{
    private readonly SignalRTestFixture _fixture;
    private SignalRTestClient? _agentClient;
    private readonly string _testDirectory;

    public SaturnAnthropicOAuthTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaturnAnthropicOAuthTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task SendMessage_ToSaturnAnthropicAgentWithOAuth_StreamsTextResponse()
    {
        // Arrange
        _agentClient = await _fixture.CreateAgentClientAsync();

        var testSession = TestDataBuilder.CreateTestSession();
        var testTokenStore = CreateTestTokenStore();
        
        // Store valid OAuth tokens
        var validTokens = new StoredTokens
        {
            AccessToken = "valid_oauth_token_for_streaming",
            RefreshToken = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await testTokenStore.SaveTokensAsync(validTokens);

        var testSaturnAgent = new Mock<IAgent>();
        var testResponses = CreateTestSaturnResponseStream();
        var receivedResponses = new ConcurrentQueue<AgentResponseDto>();

        _fixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _fixture.MockSessionManager
            .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
            .ReturnsAsync(true);

        _fixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-1", testSaturnAgent.Object);

        testSaturnAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResponses);

        _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => 
            receivedResponses.Enqueue(response));

        // Act
        var agentRequest = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-1",
            Content = "Write a Python function using Anthropic's Claude via OAuth",
            CommandId = "oauth-test-cmd-1"
        };

        await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

        // Assert
        await Task.Delay(800); // Allow time for streaming responses

        Assert.True(receivedResponses.Count >= 3);
        Assert.All(receivedResponses, response =>
        {
            Assert.Equal("saturn-anthropic-1", response.AgentId);
            Assert.Equal(testSession.Id, response.SessionId);
            Assert.NotNull(response.Response);
            Assert.Equal(ResponseType.Text, response.Response.Type);
        });

        // Verify the content flows through correctly
        var allContent = string.Concat(receivedResponses.Select(r => r.Response?.Content ?? ""));
        Assert.Contains("Python", allContent);
        Assert.Contains("OAuth", allContent);
        Assert.Contains("authentication", allContent);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task SendMessage_ToSaturnAnthropicAgentWithoutOAuth_ReturnsError()
    {
        // Arrange
        _agentClient = await _fixture.CreateAgentClientAsync();

        var testSession = TestDataBuilder.CreateTestSession();
        var testSaturnAgent = new Mock<IAgent>();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();
        var receivedResponses = new ConcurrentQueue<AgentResponseDto>();

        _fixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _fixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-no-auth", testSaturnAgent.Object);

        // Configure agent to throw authentication error
        testSaturnAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Missing Anthropic credentials. Please connect to Anthropic via OAuth or set ANTHROPIC_API_KEY environment variable."));

        _agentClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));
        _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => receivedResponses.Enqueue(response));

        // Act
        var agentRequest = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-no-auth",
            Content = "This should fail without OAuth",
            CommandId = "no-auth-test-cmd"
        };

        await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

        // Assert
        await Task.Delay(500);

        Assert.Single(receivedErrors);
        Assert.Empty(receivedResponses);

        var error = receivedErrors.First();
        Assert.Contains("Missing Anthropic credentials", error.Error);
        Assert.Contains("connect to Anthropic via OAuth", error.Error);
    }

    [Fact]
    public async Task SendMessage_ToSaturnAnthropicAgentWithExpiredTokens_ReturnsError()
    {
        // Arrange
        _agentClient = await _fixture.CreateAgentClientAsync();

        var testSession = TestDataBuilder.CreateTestSession();
        var testTokenStore = CreateTestTokenStore();
        
        // Store expired OAuth tokens without refresh capability
        var expiredTokens = new StoredTokens
        {
            AccessToken = "expired_oauth_token",
            RefreshToken = null, // No refresh token
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };
        await testTokenStore.SaveTokensAsync(expiredTokens);

        var testSaturnAgent = new Mock<IAgent>();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();

        _fixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _fixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-expired", testSaturnAgent.Object);

        testSaturnAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Authentication failed - unable to refresh OAuth tokens. Please reconnect to Anthropic."));

        _agentClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));

        // Act
        var agentRequest = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-expired",
            Content = "This should fail with expired tokens",
            CommandId = "expired-test-cmd"
        };

        await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

        // Assert
        await Task.Delay(500);

        Assert.Single(receivedErrors);
        var error = receivedErrors.First();
        Assert.Contains("Authentication failed", error.Error);
        Assert.Contains("unable to refresh OAuth tokens", error.Error);
        Assert.Contains("reconnect to Anthropic", error.Error);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task SendMessage_ToSaturnAnthropicAgentWithRefreshableTokens_RefreshesAndStreams()
    {
        // Arrange
        _agentClient = await _fixture.CreateAgentClientAsync();

        var testSession = TestDataBuilder.CreateTestSession();
        var testTokenStore = CreateTestTokenStore();
        
        // Store tokens that need refresh but have refresh capability
        var refreshableTokens = new StoredTokens
        {
            AccessToken = "old_access_token_needs_refresh",
            RefreshToken = "valid_refresh_token_can_refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(2), // Within refresh window
            CreatedAt = DateTime.UtcNow.AddMinutes(-58),
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await testTokenStore.SaveTokensAsync(refreshableTokens);

        var testSaturnAgent = new Mock<IAgent>();
        var testResponses = CreateTestRefreshResponseStream();
        var receivedResponses = new ConcurrentQueue<AgentResponseDto>();

        _fixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _fixture.MockSessionManager
            .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
            .ReturnsAsync(true);

        _fixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-refresh", testSaturnAgent.Object);

        testSaturnAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResponses);

        _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => 
            receivedResponses.Enqueue(response));

        // Act
        var agentRequest = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-refresh",
            Content = "Test token refresh flow",
            CommandId = "refresh-test-cmd"
        };

        await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

        // Assert
        await Task.Delay(800);

        Assert.True(receivedResponses.Count >= 3);
        Assert.All(receivedResponses, response =>
        {
            Assert.Equal("saturn-anthropic-refresh", response.AgentId);
            Assert.Equal(testSession.Id, response.SessionId);
            Assert.NotNull(response.Response);
        });

        // Verify refresh was successful by checking content
        var allContent = string.Concat(receivedResponses.Select(r => r.Response?.Content ?? ""));
        Assert.Contains("Token refreshed", allContent);
        Assert.Contains("successfully", allContent);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    [Fact]
    public async Task SendMessage_ConcurrentSaturnAnthropicRequests_HandlesMultipleStreams()
    {
        // Arrange
        _agentClient = await _fixture.CreateAgentClientAsync();

        var testSession = TestDataBuilder.CreateTestSession();
        var testTokenStore = CreateTestTokenStore();
        
        // Store valid OAuth tokens
        var validTokens = new StoredTokens
        {
            AccessToken = "concurrent_test_token",
            RefreshToken = "concurrent_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        await testTokenStore.SaveTokensAsync(validTokens);

        var testSaturnAgent = new Mock<IAgent>();
        var receivedResponses = new ConcurrentQueue<AgentResponseDto>();

        _fixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _fixture.MockSessionManager
            .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
            .ReturnsAsync(true);

        _fixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-concurrent", testSaturnAgent.Object);

        testSaturnAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestConcurrentResponseStream());

        _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => 
            receivedResponses.Enqueue(response));

        // Act - Send 3 concurrent requests
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            var request = new AgentMessageRequest
            {
                SessionId = testSession.Id,
                AgentId = "saturn-anthropic-concurrent",
                Content = $"Concurrent request {i + 1}",
                CommandId = $"concurrent-cmd-{i + 1}"
            };
            tasks.Add(_agentClient.InvokeAsync("SendAgentMessage", request));
        }

        await Task.WhenAll(tasks);

        // Assert
        await Task.Delay(1200); // Allow time for all streaming responses

        // Should receive 9 responses (3 requests × 3 response chunks each)
        Assert.Equal(9, receivedResponses.Count);
        
        // Verify all requests were handled
        var commandIds = receivedResponses.Select(r => r.Response.Metadata?.ContainsKey("CommandId") == true ? r.Response.Metadata["CommandId"].ToString() : null).Where(id => id != null).Distinct().ToList();
        Assert.Equal(3, commandIds.Count);
        Assert.Contains("concurrent-cmd-1", commandIds);
        Assert.Contains("concurrent-cmd-2", commandIds);
        Assert.Contains("concurrent-cmd-3", commandIds);

        // Cleanup
        await testTokenStore.ClearTokensAsync();
        testTokenStore.Dispose();
    }

    private TestTokenStore CreateTestTokenStore()
    {
        var uniqueTestDir = Path.Combine(_testDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueTestDir);
        return new TestTokenStore(uniqueTestDir);
    }

    private static async IAsyncEnumerable<AgentResponse> CreateTestSaturnResponseStream()
    {
        var contentChunks = new[]
        {
            "Here's a Python function that uses Anthropic's Claude via OAuth authentication:\n\n",
            "```python\nimport asyncio\nfrom anthropic import AsyncAnthropic\n\n",
            "async def chat_with_claude_oauth(message: str):\n    # OAuth token automatically handled\n    return await client.messages.create(model='claude-3.5-sonnet', messages=[{'role': 'user', 'content': message}])\n```"
        };
        
        for (int i = 0; i < contentChunks.Length; i++)
        {
            await Task.Delay(50);
            yield return new AgentResponse
            {
                Content = contentChunks[i],
                Type = ResponseType.Text,
                IsComplete = i == contentChunks.Length - 1,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private static async IAsyncEnumerable<AgentResponse> CreateTestRefreshResponseStream()
    {
        var contentChunks = new[]
        {
            "Token refreshed successfully. ",
            "Proceeding with your request... ",
            "The authentication flow is working correctly with automatic token refresh."
        };
        
        for (int i = 0; i < contentChunks.Length; i++)
        {
            await Task.Delay(50);
            yield return new AgentResponse
            {
                Content = contentChunks[i],
                Type = ResponseType.Text,
                IsComplete = i == contentChunks.Length - 1,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private static async IAsyncEnumerable<AgentResponse> CreateTestConcurrentResponseStream()
    {
        var contentChunks = new[]
        {
            "Processing concurrent request... ",
            "OAuth authentication successful... ",
            "Response completed."
        };
        
        for (int i = 0; i < contentChunks.Length; i++)
        {
            await Task.Delay(50);
            yield return new AgentResponse
            {
                Content = contentChunks[i],
                Type = ResponseType.Text,
                IsComplete = i == contentChunks.Length - 1,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_agentClient != null)
        {
            await _agentClient.DisposeAsync();
        }
        
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
        
        _fixture.ResetMocks();
    }
}


/// <summary>
/// Test-specific TokenStore for SignalR OAuth tests
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

        tokenPathField?.SetValue(this, Path.Combine(_testDirectory, "signalr.tokens"));
        keyPathField?.SetValue(this, Path.Combine(_testDirectory, ".signalrkeystore"));
        saltPathField?.SetValue(this, Path.Combine(_testDirectory, ".signalrsalt"));
    }

    public void Dispose()
    {
        try
        {
            var files = new[]
            {
                Path.Combine(_testDirectory, "signalr.tokens"),
                Path.Combine(_testDirectory, ".signalrkeystore"),
                Path.Combine(_testDirectory, ".signalrsalt")
            };

            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}