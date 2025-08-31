using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.IntegrationTests.Fixtures;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Hubs
{
    /// <summary>
    /// Integration tests for OrchestratorHub
    /// </summary>
    public class OrchestratorHubTests : IClassFixture<SignalRTestFixture>, IAsyncDisposable
    {
        private readonly SignalRTestFixture _fixture;
        private SignalRTestClient? _client;

        public OrchestratorHubTests(SignalRTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OnConnectedAsync_ShouldNotifyClientWithConnectionInfo()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            var connectionReceived = false;
            ConnectionInfo? receivedInfo = null;

            _client.On<ConnectionInfo>("Connected", info =>
            {
                connectionReceived = true;
                receivedInfo = info;
            });

            // Act - Connection already established in CreateOrchestratorClientAsync
            await Task.Delay(100); // Wait for connection message

            // Assert
            Assert.True(connectionReceived);
            Assert.NotNull(receivedInfo);
            Assert.False(string.IsNullOrEmpty(receivedInfo!.ConnectionId));
            Assert.True(Math.Abs((receivedInfo.ConnectedAt - DateTime.UtcNow).TotalSeconds) < 30);
        }

        [Fact]
        public async Task CreateSession_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var testSession = TestDataBuilder.CreateTestSession();
            var sessionCreated = false;
            Session? receivedSession = null;

            _fixture.MockSessionManager
                .Setup(x => x.CreateSessionAsync(It.IsAny<Core.Sessions.CreateSessionRequest>()))
                .ReturnsAsync(testSession);

            _client.On<Session>("SessionCreated", session =>
            {
                sessionCreated = true;
                receivedSession = session;
            });

            var request = new CreateSessionRequest
            {
                Name = "Test Session",
                Type = SessionType.MultiAgent,
                AgentIds = new List<string> { "agent-1", "agent-2" },
                WorkingDirectory = "/test"
            };

            // Act
            var response = await _client.InvokeAsync<SessionCreatedResponse>("CreateSession", request);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(testSession.Id, response.SessionId);
            Assert.NotNull(response.Session);
            
            // Wait for the group message
            await Task.Delay(100);
            Assert.True(sessionCreated);
            Assert.NotNull(receivedSession);
            Assert.Equal(testSession.Id, receivedSession!.Id);
        }

        [Fact]
        public async Task CreateSession_WithException_ShouldReturnErrorResponse()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            _fixture.MockSessionManager
                .Setup(x => x.CreateSessionAsync(It.IsAny<Core.Sessions.CreateSessionRequest>()))
                .ThrowsAsync(new Exception("Test exception"));

            var request = new CreateSessionRequest
            {
                Name = "Failing Session",
                Type = SessionType.MultiAgent
            };

            // Act
            var response = await _client.InvokeAsync<SessionCreatedResponse>("CreateSession", request);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("Test exception", response.Error);
        }

        [Fact]
        public async Task SendOrchestrationMessage_WithValidRequest_ShouldCreateAndExecutePlan()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var testPlan = TestDataBuilder.CreateTestOrchestrationPlan();
            var testResult = TestDataBuilder.CreateTestOrchestrationResult();
            
            var planCreated = false;
            var orchestrationCompleted = false;
            OrchestrationPlan? receivedPlan = null;
            OrchestrationResult? receivedResult = null;

            _fixture.MockOrchestrator
                .Setup(x => x.CreatePlanAsync(It.IsAny<OrchestrationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testPlan);

            _fixture.MockOrchestrator
                .Setup(x => x.ExecutePlanAsync(It.IsAny<OrchestrationPlan>(), It.IsAny<IProgress<OrchestrationProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            _client.On<OrchestrationPlan>("OrchestrationPlanCreated", plan =>
            {
                planCreated = true;
                receivedPlan = plan;
            });

            _client.On<OrchestrationResult>("OrchestrationCompleted", result =>
            {
                orchestrationCompleted = true;
                receivedResult = result;
            });

            var request = new OrchestrationMessageRequest
            {
                SessionId = "test-session",
                Message = "Test orchestration goal",
                AgentIds = new List<string> { "agent-1" },
                Strategy = OrchestrationStrategy.Sequential
            };

            // Act
            await _client.InvokeAsync("SendOrchestrationMessage", request);

            // Assert
            await Task.Delay(500); // Wait for async execution

            Assert.True(planCreated);
            Assert.NotNull(receivedPlan);
            Assert.Equal(testPlan.Id, receivedPlan!.Id);

            Assert.True(orchestrationCompleted);
            Assert.NotNull(receivedResult);
            Assert.Equal(testResult.Success, receivedResult!.Success);
        }

        [Fact]
        public async Task JoinSession_WithExistingSession_ShouldAddToGroupAndNotifyClient()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var testSession = TestDataBuilder.CreateTestSession();
            var sessionJoined = false;
            Session? receivedSession = null;

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("test-session"))
                .ReturnsAsync(testSession);

            _client.On<Session>("SessionJoined", session =>
            {
                sessionJoined = true;
                receivedSession = session;
            });

            // Act
            await _client.InvokeAsync("JoinSession", "test-session");

            // Assert
            await Task.Delay(100);
            Assert.True(sessionJoined);
            Assert.NotNull(receivedSession);
            Assert.Equal(testSession.Id, receivedSession!.Id);
        }

        [Fact]
        public async Task JoinSession_WithNonExistentSession_ShouldReturnError()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var errorReceived = false;
            ErrorResponse? receivedError = null;

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("nonexistent-session"))
                .ReturnsAsync((Session?)null);

            _client.On<ErrorResponse>("ReceiveError", error =>
            {
                errorReceived = true;
                receivedError = error;
            });

            // Act
            await _client.InvokeAsync("JoinSession", "nonexistent-session");

            // Assert
            await Task.Delay(100);
            Assert.True(errorReceived);
            Assert.NotNull(receivedError);
            Assert.Contains("Session nonexistent-session not found", receivedError!.Error);
        }

        [Fact]
        public async Task LeaveSession_ShouldRemoveFromGroup()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var testSession = TestDataBuilder.CreateTestSession();
            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("test-session"))
                .ReturnsAsync(testSession);

            // First join the session
            await _client.InvokeAsync("JoinSession", "test-session");
            await Task.Delay(100);

            // Act
            await _client.InvokeAsync("LeaveSession", "test-session");

            // Assert - No exception should be thrown
            // This test primarily ensures the method executes without error
            await Task.Delay(100);
        }

        [Fact]
        public async Task SendOrchestrationMessage_WithException_ShouldReturnError()
        {
            // Arrange
            _client = await _fixture.CreateOrchestratorClientAsync();
            
            var errorReceived = false;
            ErrorResponse? receivedError = null;

            _fixture.MockOrchestrator
                .Setup(x => x.CreatePlanAsync(It.IsAny<OrchestrationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test orchestration exception"));

            _client.On<ErrorResponse>("ReceiveError", error =>
            {
                errorReceived = true;
                receivedError = error;
            });

            var request = new OrchestrationMessageRequest
            {
                SessionId = "test-session",
                Message = "Failing orchestration",
                AgentIds = new List<string> { "agent-1" }
            };

            // Act
            await _client.InvokeAsync("SendOrchestrationMessage", request);

            // Assert
            await Task.Delay(100);
            Assert.True(errorReceived);
            Assert.NotNull(receivedError);
            Assert.Equal("Test orchestration exception", receivedError!.Error);
        }

        public async ValueTask DisposeAsync()
        {
            if (_client != null)
            {
                await _client.DisposeAsync();
            }
            _fixture.ResetMocks();
        }
    }
}