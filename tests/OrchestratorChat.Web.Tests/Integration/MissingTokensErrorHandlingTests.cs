using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.IntegrationTests.Fixtures;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using System.Collections.Concurrent;
using Xunit;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Web.Tests.Integration;

/// <summary>
/// Integration tests for missing tokens error handling across different scenarios
/// </summary>
public class MissingTokensErrorHandlingTests : IClassFixture<SignalRTestFixture>, IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly SignalRTestFixture _signalRFixture;
    private readonly WebApplicationFactory<Program> _webFactory;
    private readonly HttpClient _httpClient;
    private SignalRTestClient? _signalRClient;

    public MissingTokensErrorHandlingTests(SignalRTestFixture signalRFixture, WebApplicationFactory<Program> webFactory)
    {
        _signalRFixture = signalRFixture;
        _webFactory = webFactory;
        _httpClient = _webFactory.CreateClient();
    }

    [Fact]
    public async Task AnthropicStatus_WithNoTokensStored_ReturnsFriendlyNotConnectedStatus()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/providers/anthropic/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var connected = result.GetProperty("connected").GetBoolean();
        var expiresAt = result.GetProperty("expiresAt");
        var scopes = result.GetProperty("scopes").EnumerateArray().ToList();

        Assert.False(connected);
        Assert.Equal(JsonValueKind.Null, expiresAt.ValueKind);
        Assert.Empty(scopes);
    }

    [Fact]
    public async Task ProviderStatus_WithNoAnthropicCredentials_ShowsMissingStatus()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/providers/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("anthropicOAuth", out var anthropicOAuth));
        Assert.Equal("Missing", anthropicOAuth.GetString());
    }

    [Fact]
    public async Task SendMessageToSaturnAnthropic_WithoutCredentials_ReturnsUserFriendlyError()
    {
        // Arrange
        _signalRClient = await _signalRFixture.CreateAgentClientAsync();
        
        var testSession = TestDataBuilder.CreateTestSession();
        var testAgent = new Mock<IAgent>();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();

        _signalRFixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _signalRFixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-no-creds", testAgent.Object);

        // Configure agent to throw the specific missing credentials error
        testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Missing Anthropic credentials. Please connect to Anthropic via OAuth or set ANTHROPIC_API_KEY environment variable."));

        _signalRClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));

        // Act
        var request = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-no-creds",
            Content = "This should fail gracefully",
            CommandId = "no-creds-test"
        };

        await _signalRClient.InvokeAsync("SendAgentMessage", request);

        // Assert
        await Task.Delay(500);

        Assert.Single(receivedErrors);
        var error = receivedErrors.First();
        
        Assert.Contains("Missing Anthropic credentials", error.Error);
        Assert.Contains("connect to Anthropic via OAuth", error.Error);
        Assert.Contains("ANTHROPIC_API_KEY environment variable", error.Error);
        
        // Verify it's a user-friendly message, not a technical stack trace
        Assert.DoesNotContain("System.InvalidOperationException", error.Error);
        Assert.DoesNotContain("at ", error.Error);
        Assert.DoesNotContain("StackTrace", error.Error);
    }

    [Fact]
    public async Task SendMessageToSaturnAnthropic_WithExpiredTokensNoRefresh_ReturnsReconnectGuidance()
    {
        // Arrange
        _signalRClient = await _signalRFixture.CreateAgentClientAsync();
        
        var testSession = TestDataBuilder.CreateTestSession();
        var testAgent = new Mock<IAgent>();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();

        _signalRFixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _signalRFixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-anthropic-expired", testAgent.Object);

        // Configure agent to throw the token refresh failure error
        testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Authentication failed - unable to refresh OAuth tokens. Please reconnect to Anthropic."));

        _signalRClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));

        // Act
        var request = new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-anthropic-expired",
            Content = "This should fail with expired token guidance",
            CommandId = "expired-token-test"
        };

        await _signalRClient.InvokeAsync("SendAgentMessage", request);

        // Assert
        await Task.Delay(500);

        Assert.Single(receivedErrors);
        var error = receivedErrors.First();
        
        Assert.Contains("Authentication failed", error.Error);
        Assert.Contains("unable to refresh OAuth tokens", error.Error);
        Assert.Contains("reconnect to Anthropic", error.Error);
        
        // Verify it provides actionable guidance
        Assert.DoesNotContain("System.Exception", error.Error);
        Assert.DoesNotContain("NullReferenceException", error.Error);
    }

    [Fact]
    public async Task MultipleAgentsWithMissingCredentials_EachReturnsAppropriateError()
    {
        // Arrange
        _signalRClient = await _signalRFixture.CreateAgentClientAsync();
        
        var testSession = TestDataBuilder.CreateTestSession();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();

        _signalRFixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        // Setup multiple agents with different credential issues
        var agent1 = new Mock<IAgent>();
        var agent2 = new Mock<IAgent>();
        
        _signalRFixture.MockAgentFactory.SetupCreateAgentAsync("saturn-no-oauth", agent1.Object);
        _signalRFixture.MockAgentFactory.SetupCreateAgentAsync("saturn-expired-oauth", agent2.Object);

        agent1.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Missing Anthropic credentials. Please connect to Anthropic via OAuth or set ANTHROPIC_API_KEY environment variable."));

        agent2.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Authentication failed - unable to refresh OAuth tokens. Please reconnect to Anthropic."));

        _signalRClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));

        // Act - Send messages to both agents
        await _signalRClient.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-no-oauth",
            Content = "Test message 1",
            CommandId = "test-1"
        });

        await _signalRClient.InvokeAsync("SendAgentMessage", new AgentMessageRequest
        {
            SessionId = testSession.Id,
            AgentId = "saturn-expired-oauth",
            Content = "Test message 2", 
            CommandId = "test-2"
        });

        // Assert
        await Task.Delay(800);

        Assert.Equal(2, receivedErrors.Count);
        var errors = receivedErrors.ToList();

        // Verify we got both types of errors
        Assert.Contains(errors, e => e.Error.Contains("Missing Anthropic credentials"));
        Assert.Contains(errors, e => e.Error.Contains("unable to refresh OAuth tokens"));
        
        // Both should be user-friendly
        Assert.All(errors, error =>
        {
            Assert.DoesNotContain("System.", error.Error);
            Assert.DoesNotContain("Exception", error.Error);
            Assert.True(error.Error.Contains("Anthropic"));
        });
    }

    [Fact]
    public async Task StartOAuth_WithMissingState_HandlesCookieIssuesGracefully()
    {
        // This tests the edge case where PKCE state is lost (e.g., due to server restart)
        
        // Act - Try callback without starting OAuth first (no state in cache)
        var response = await _httpClient.GetAsync("/oauth/anthropic/callback?code=test_code&state=nonexistent_state");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("oauth-error", content);
        Assert.Contains("Invalid or expired state parameter", content);
        Assert.Contains("window.opener.postMessage", content); // Should still try to communicate with opener
    }

    [Fact]
    public async Task LogoutFromAnthropic_WithNoTokensStored_HandlesGracefully()
    {
        // Act - Try to logout when no tokens are stored
        var response = await _httpClient.PostAsync("/api/providers/anthropic/logout", new StringContent(""));

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Should succeed even if no tokens were stored
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Contains("Logged out successfully", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ProviderVerification_WithCorruptedTokenFile_HandlesGracefully()
    {
        // This test would require injecting a corrupted token file, which is handled in the unit tests
        // Here we just verify the endpoint doesn't crash with missing tokens
        
        // Act
        var response = await _httpClient.GetAsync("/api/providers/anthropic/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Should handle gracefully even with token store issues
        Assert.True(result.TryGetProperty("connected", out var connected));
        Assert.False(connected.GetBoolean());
    }

    [Fact]
    public async Task ConcurrentRequestsWithMissingCredentials_AllReturnConsistentErrors()
    {
        // Arrange
        _signalRClient = await _signalRFixture.CreateAgentClientAsync();
        
        var testSession = TestDataBuilder.CreateTestSession();
        var testAgent = new Mock<IAgent>();
        var receivedErrors = new ConcurrentQueue<ErrorResponse>();

        _signalRFixture.MockSessionManager
            .Setup(x => x.GetSessionAsync(testSession.Id))
            .ReturnsAsync(testSession);

        _signalRFixture.MockAgentFactory
            .SetupCreateAgentAsync("saturn-concurrent-no-creds", testAgent.Object);

        testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Missing Anthropic credentials. Please connect to Anthropic via OAuth or set ANTHROPIC_API_KEY environment variable."));

        _signalRClient.On<ErrorResponse>("ReceiveError", error => receivedErrors.Enqueue(error));

        // Act - Send multiple concurrent requests
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var request = new AgentMessageRequest
            {
                SessionId = testSession.Id,
                AgentId = "saturn-concurrent-no-creds",
                Content = $"Concurrent request {i + 1}",
                CommandId = $"concurrent-no-creds-{i + 1}"
            };
            tasks.Add(_signalRClient.InvokeAsync("SendAgentMessage", request));
        }

        await Task.WhenAll(tasks);

        // Assert
        await Task.Delay(1000);

        Assert.Equal(5, receivedErrors.Count);
        Assert.All(receivedErrors, error =>
        {
            Assert.Contains("Missing Anthropic credentials", error.Error);
            Assert.Contains("connect to Anthropic via OAuth", error.Error);
            
            // Verify consistent error format
            Assert.DoesNotContain("System.InvalidOperationException", error.Error);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_signalRClient != null)
        {
            await _signalRClient.DisposeAsync();
        }
        _httpClient.Dispose();
        _signalRFixture.ResetMocks();
    }
}