using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.IntegrationTests.Fixtures;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using System.Collections.Concurrent;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Integration
{
    /// <summary>
    /// End-to-end integration tests for complete message flows
    /// </summary>
    public class EndToEndMessageFlowTests : IClassFixture<SignalRTestFixture>, IAsyncDisposable
    {
        private readonly SignalRTestFixture _fixture;
        private SignalRTestClient? _orchestratorClient;
        private SignalRTestClient? _agentClient;

        public EndToEndMessageFlowTests(SignalRTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CompleteOrchestrationFlow_ShouldExecuteSuccessfully()
        {
            // Arrange
            _orchestratorClient = await _fixture.CreateOrchestratorClientAsync();
            _agentClient = await _fixture.CreateAgentClientAsync();

            var testSession = TestDataBuilder.CreateTestSession();
            var testPlan = TestDataBuilder.CreateTestOrchestrationPlan();
            var testResult = TestDataBuilder.CreateTestOrchestrationResult();

            var sessionEvents = new ConcurrentQueue<string>();
            var orchestrationEvents = new ConcurrentQueue<string>();

            // Setup mocks
            _fixture.MockSessionManager
                .Setup(x => x.CreateSessionAsync(It.IsAny<Core.Sessions.CreateSessionRequest>()))
                .ReturnsAsync(testSession);

            _fixture.MockOrchestrator
                .Setup(x => x.CreatePlanAsync(It.IsAny<OrchestrationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testPlan);

            _fixture.MockOrchestrator
                .Setup(x => x.ExecutePlanAsync(It.IsAny<OrchestrationPlan>(), It.IsAny<IProgress<OrchestrationProgress>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            // Setup event handlers
            _orchestratorClient.On<ConnectionInfo>("Connected", info => sessionEvents.Enqueue("Connected"));
            _orchestratorClient.On<Session>("SessionCreated", session => sessionEvents.Enqueue("SessionCreated"));
            _orchestratorClient.On<OrchestrationPlan>("OrchestrationPlanCreated", plan => orchestrationEvents.Enqueue("PlanCreated"));
            _orchestratorClient.On<OrchestrationProgress>("OrchestrationProgress", progress => orchestrationEvents.Enqueue("Progress"));
            _orchestratorClient.On<OrchestrationResult>("OrchestrationCompleted", result => orchestrationEvents.Enqueue("Completed"));

            // Act & Assert - Step 1: Create Session
            var createSessionRequest = new CreateSessionRequest
            {
                Name = "E2E Test Session",
                Type = SessionType.MultiAgent,
                AgentIds = new List<string> { "agent-1", "agent-2" },
                WorkingDirectory = "/test"
            };

            var sessionResponse = await _orchestratorClient.InvokeAsync<SessionCreatedResponse>("CreateSession", createSessionRequest);
            
            Assert.NotNull(sessionResponse);
            Assert.True(sessionResponse.Success);
            Assert.Equal(testSession.Id, sessionResponse.SessionId);

            // Wait for events
            await Task.Delay(200);
            Assert.Contains("Connected", sessionEvents);
            Assert.Contains("SessionCreated", sessionEvents);

            // Act & Assert - Step 2: Send Orchestration Message
            var orchestrationRequest = new OrchestrationMessageRequest
            {
                SessionId = testSession.Id,
                Message = "Create a web application with authentication",
                AgentIds = new List<string> { "agent-1", "agent-2" },
                Strategy = OrchestrationStrategy.Sequential
            };

            await _orchestratorClient.InvokeAsync("SendOrchestrationMessage", orchestrationRequest);

            // Wait for orchestration to complete
            await Task.Delay(1000);

            // Assert orchestration flow
            Assert.Contains("PlanCreated", orchestrationEvents);
            Assert.Contains("Completed", orchestrationEvents);
            
            // Verify mocks were called correctly
            _fixture.MockOrchestrator.Verify(x => x.CreatePlanAsync(It.IsAny<OrchestrationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _fixture.MockOrchestrator.Verify(x => x.ExecutePlanAsync(It.IsAny<OrchestrationPlan>(), It.IsAny<IProgress<OrchestrationProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AgentMessageFlow_ShouldStreamResponsesCorrectly()
        {
            // Arrange
            _agentClient = await _fixture.CreateAgentClientAsync();

            var testSession = TestDataBuilder.CreateTestSession();
            var testAgent = new Mock<IAgent>();
            var testResponses = TestDataBuilder.CreateTestAgentResponseStream();

            var receivedResponses = new ConcurrentQueue<AgentResponseDto>();

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync(testSession.Id))
                .ReturnsAsync(testSession);

            _fixture.MockSessionManager
                .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
                .ReturnsAsync(true);

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("agent-1", testAgent.Object);

            testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponses);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentResponse
                {
                    Content = "Final response",
                    Type = ResponseType.Text,
                    IsComplete = true
                });

            _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => 
                receivedResponses.Enqueue(response));

            // Act
            var agentRequest = new AgentMessageRequest
            {
                SessionId = testSession.Id,
                AgentId = "agent-1",
                Content = "Write a Python function to sort a list",
                CommandId = "cmd-123"
            };

            await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

            // Assert
            await Task.Delay(500);

            Assert.Equal(3, receivedResponses.Count);
            Assert.All(receivedResponses, response =>
            {
                Assert.Equal("agent-1", response.AgentId);
                Assert.Equal(testSession.Id, response.SessionId);
                Assert.NotNull(response.Response);
            });

            // Verify session was updated
            _fixture.MockSessionManager.Verify(
                x => x.UpdateSessionAsync(It.Is<Session>(s => 
                    s.Messages.Any(m => m.Content.Contains("Python function") && m.AgentId == "agent-1"))),
                Times.Once);
        }

        [Fact]
        public async Task SessionJoinAndLeave_ShouldWorkAcrossMultipleClients()
        {
            // Arrange
            var client1 = await _fixture.CreateOrchestratorClientAsync();
            var client2 = await _fixture.CreateOrchestratorClientAsync();

            var testSession = TestDataBuilder.CreateTestSession();
            
            var client1Events = new ConcurrentQueue<string>();
            var client2Events = new ConcurrentQueue<string>();

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync(testSession.Id))
                .ReturnsAsync(testSession);

            client1.On<Session>("SessionJoined", session => client1Events.Enqueue("SessionJoined"));
            client2.On<Session>("SessionJoined", session => client2Events.Enqueue("SessionJoined"));

            // Act - Both clients join the session
            await client1.InvokeAsync("JoinSession", testSession.Id);
            await client2.InvokeAsync("JoinSession", testSession.Id);

            await Task.Delay(200);

            // Assert - Both clients should receive join confirmation
            Assert.Contains("SessionJoined", client1Events);
            Assert.Contains("SessionJoined", client2Events);

            // Act - Client 1 leaves
            await client1.InvokeAsync("LeaveSession", testSession.Id);

            // Assert - No exceptions should occur
            await Task.Delay(100);

            // Cleanup
            await client1.DisposeAsync();
            await client2.DisposeAsync();
        }

        [Fact]
        public async Task ToolExecution_ShouldReturnCorrectResults()
        {
            // Arrange
            _agentClient = await _fixture.CreateAgentClientAsync();

            var testAgent = new Mock<IAgent>();
            var testResult = TestDataBuilder.CreateTestToolResult();

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("agent-1", testAgent.Object);

            testAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            // Act
            var toolRequest = new ToolExecutionRequest
            {
                AgentId = "agent-1",
                SessionId = "test-session",
                ToolName = "file_read",
                Parameters = new Dictionary<string, object> 
                { 
                    ["path"] = "/test/file.txt" 
                }
            };

            var response = await _agentClient.InvokeAsync<ToolExecutionResponse>("ExecuteTool", toolRequest);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(testResult.Output, response.Output);
            Assert.Equal(testResult.ExecutionTime, response.ExecutionTime);

            // Verify tool was called with correct parameters
            testAgent.Verify(
                x => x.ExecuteToolAsync(It.Is<ToolCall>(call =>
                    call.ToolName == "file_read" &&
                    call.Parameters.ContainsKey("path") &&
                    call.Parameters["path"].ToString() == "/test/file.txt"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ErrorHandling_ShouldPropagateErrorsCorrectly()
        {
            // Arrange
            _orchestratorClient = await _fixture.CreateOrchestratorClientAsync();
            _agentClient = await _fixture.CreateAgentClientAsync();

            var orchestratorErrors = new ConcurrentQueue<ErrorResponse>();
            var agentErrors = new ConcurrentQueue<ErrorResponse>();

            _fixture.MockOrchestrator
                .Setup(x => x.CreatePlanAsync(It.IsAny<OrchestrationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Orchestration failed"));

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Session not found"));

            _orchestratorClient.On<ErrorResponse>("ReceiveError", error => orchestratorErrors.Enqueue(error));
            _agentClient.On<ErrorResponse>("ReceiveError", error => agentErrors.Enqueue(error));

            // Act - Orchestration error
            var orchestrationRequest = new OrchestrationMessageRequest
            {
                SessionId = "test-session",
                Message = "This will fail",
                AgentIds = new List<string> { "agent-1" }
            };

            await _orchestratorClient.InvokeAsync("SendOrchestrationMessage", orchestrationRequest);

            // Act - Agent error
            var agentRequest = new AgentMessageRequest
            {
                SessionId = "nonexistent-session",
                AgentId = "agent-1",
                Content = "This will also fail"
            };

            await _agentClient.InvokeAsync("SendAgentMessage", agentRequest);

            // Assert
            await Task.Delay(300);

            Assert.Single(orchestratorErrors);
            Assert.Equal("Orchestration failed", orchestratorErrors.First().Error);

            Assert.Single(agentErrors);
            Assert.Equal("Session not found", agentErrors.First().Error);
        }

        [Fact]
        public async Task MultipleSimultaneousOperations_ShouldHandleConcurrency()
        {
            // Arrange
            _orchestratorClient = await _fixture.CreateOrchestratorClientAsync();
            _agentClient = await _fixture.CreateAgentClientAsync();

            var testSession = TestDataBuilder.CreateTestSession();
            var testAgent = new Mock<IAgent>();
            var testResponses = TestDataBuilder.CreateTestAgentResponseStream();

            var allResponses = new ConcurrentQueue<AgentResponseDto>();

            _fixture.MockSessionManager
                .Setup(x => x.CreateSessionAsync(It.IsAny<Core.Sessions.CreateSessionRequest>()))
                .ReturnsAsync(testSession);

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync(It.IsAny<string>()))
                .ReturnsAsync(testSession);

            _fixture.MockSessionManager
                .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
                .ReturnsAsync(true);

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("agent-1", testAgent.Object);

            testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponses);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentResponse
                {
                    Content = "Final response",
                    Type = ResponseType.Text,
                    IsComplete = true
                });

            _agentClient.On<AgentResponseDto>("ReceiveAgentResponse", response => 
                allResponses.Enqueue(response));

            // Act - Send multiple messages simultaneously
            var tasks = new List<Task>();
            
            for (int i = 0; i < 5; i++)
            {
                var request = new AgentMessageRequest
                {
                    SessionId = testSession.Id,
                    AgentId = "agent-1",
                    Content = $"Concurrent message {i}",
                    CommandId = $"cmd-{i}"
                };

                tasks.Add(_agentClient.InvokeAsync("SendAgentMessage", request));
            }

            await Task.WhenAll(tasks);

            // Assert
            await Task.Delay(1000);

            // Should receive 3 responses per message (5 messages × 3 responses)
            Assert.Equal(15, allResponses.Count);
            
            // Verify all agents were created and called
            testAgent.Verify(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        }

        public async ValueTask DisposeAsync()
        {
            if (_orchestratorClient != null)
            {
                await _orchestratorClient.DisposeAsync();
            }
            if (_agentClient != null)
            {
                await _agentClient.DisposeAsync();
            }
            _fixture.ResetMocks();
        }
    }
}