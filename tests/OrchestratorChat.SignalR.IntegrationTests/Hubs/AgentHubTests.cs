using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.SignalR.Contracts.Requests;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.IntegrationTests.Fixtures;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using System.Runtime.CompilerServices;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Hubs
{
    /// <summary>
    /// Integration tests for AgentHub
    /// </summary>
    public class AgentHubTests : IClassFixture<SignalRTestFixture>, IAsyncDisposable
    {
        private readonly SignalRTestFixture _fixture;
        private SignalRTestClient? _client;

        public AgentHubTests(SignalRTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SendAgentMessage_WithValidRequest_ShouldStreamResponses()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testSession = TestDataBuilder.CreateTestSession();
            var testAgent = new Mock<IAgent>();
            var testResponses = TestDataBuilder.CreateTestAgentResponseStream();

            var receivedResponses = new List<AgentResponseDto>();

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("test-session"))
                .ReturnsAsync(testSession);

            _fixture.MockSessionManager
                .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
                .ReturnsAsync(true);

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponses);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentResponse
                {
                    Content = "Final response",
                    Type = ResponseType.Text,
                    IsComplete = true
                });

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Ready);
            testAgent.SetupGet(x => x.Capabilities).Returns(new AgentCapabilities
            {
                SupportsStreaming = true,
                SupportsTools = true,
                SupportedModels = new List<string> { "chat", "code" }
            });

            _client.On<AgentResponseDto>("ReceiveAgentResponse", response =>
            {
                receivedResponses.Add(response);
            });

            var request = new AgentMessageRequest
            {
                SessionId = "test-session",
                AgentId = "test-agent",
                Content = "Hello agent",
                CommandId = "cmd-123"
            };

            // Act
            await _client.InvokeAsync("SendAgentMessage", request);

            // Assert
            await Task.Delay(500); // Wait for streaming to complete

            Assert.Equal(3, receivedResponses.Count);
            Assert.All(receivedResponses, r =>
            {
                Assert.Equal("test-agent", r.AgentId);
                Assert.Equal("test-session", r.SessionId);
                Assert.NotNull(r.Response);
            });

            // Verify session was updated
            _fixture.MockSessionManager.Verify(
                x => x.UpdateSessionAsync(It.Is<Session>(s => 
                    s.Messages.Any(m => m.Content == "Hello agent" && m.AgentId == "test-agent"))),
                Times.Once);
        }

        [Fact]
        public async Task SendAgentMessage_WithException_ShouldReturnError()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var errorReceived = false;
            ErrorResponse? receivedError = null;

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("test-session"))
                .ThrowsAsync(new Exception("Session access failed"));

            _client.On<ErrorResponse>("ReceiveError", error =>
            {
                errorReceived = true;
                receivedError = error;
            });

            var request = new AgentMessageRequest
            {
                SessionId = "test-session",
                AgentId = "test-agent",
                Content = "This will fail"
            };

            // Act
            await _client.InvokeAsync("SendAgentMessage", request);

            // Assert
            await Task.Delay(100);
            Assert.True(errorReceived);
            Assert.NotNull(receivedError);
            Assert.Equal("Session access failed", receivedError!.Error);
            Assert.Equal("test-agent", receivedError.AgentId);
            Assert.Equal("test-session", receivedError.SessionId);
        }

        [Fact]
        public async Task ExecuteTool_WithValidRequest_ShouldReturnResult()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();
            var testResult = TestDataBuilder.CreateTestToolResult();

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResult);

            var request = new ToolExecutionRequest
            {
                AgentId = "test-agent",
                SessionId = "test-session",
                ToolName = "test-tool",
                Parameters = new Dictionary<string, object> { ["param1"] = "value1" }
            };

            // Act
            var response = await _client.InvokeAsync<ToolExecutionResponse>("ExecuteTool", request);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(testResult.Output, response.Output);
            Assert.Equal(testResult.ExecutionTime, response.ExecutionTime);
        }

        [Fact]
        public async Task ExecuteTool_WithException_ShouldReturnErrorResponse()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Tool execution failed"));

            var request = new ToolExecutionRequest
            {
                AgentId = "test-agent",
                SessionId = "test-session",
                ToolName = "failing-tool",
                Parameters = new Dictionary<string, object>()
            };

            // Act
            var response = await _client.InvokeAsync<ToolExecutionResponse>("ExecuteTool", request);

            // Assert
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("Tool execution failed", response.Error);
        }

        [Fact]
        public async Task SubscribeToAgent_ShouldAddToGroupAndSendStatus()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();
            var statusReceived = false;
            AgentStatusDto? receivedStatus = null;

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Processing);
            testAgent.SetupGet(x => x.Capabilities).Returns(new AgentCapabilities
            {
                SupportsStreaming = true,
                SupportsTools = true,
                SupportsFileOperations = true,
                SupportedModels = new List<string> { "chat", "code", "tools" }
            });

            _client.On<AgentStatusDto>("AgentStatusUpdate", status =>
            {
                statusReceived = true;
                receivedStatus = status;
            });

            // Act
            await _client.InvokeAsync("SubscribeToAgent", "test-agent");

            // Assert
            await Task.Delay(100);
            Assert.True(statusReceived);
            Assert.NotNull(receivedStatus);
            Assert.Equal("test-agent", receivedStatus!.AgentId);
            Assert.Equal(AgentStatus.Processing, receivedStatus.Status);
            Assert.NotNull(receivedStatus.Capabilities);
            Assert.Contains("chat", receivedStatus.Capabilities.SupportedModels);
            Assert.Contains("code", receivedStatus.Capabilities.SupportedModels);
            Assert.Contains("tools", receivedStatus.Capabilities.SupportedModels);
        }

        [Fact]
        public async Task UnsubscribeFromAgent_ShouldRemoveFromGroup()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();

            // First subscribe
            await _client.InvokeAsync("SubscribeToAgent", "test-agent");
            await Task.Delay(50);

            // Act
            await _client.InvokeAsync("UnsubscribeFromAgent", "test-agent");

            // Assert - No exception should be thrown
            // This test primarily ensures the method executes without error
            await Task.Delay(50);
        }

        [Fact]
        public async Task AgentStatusChange_ShouldNotifySubscribers()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();
            var statusUpdates = new List<AgentStatusDto>();

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Ready);
            testAgent.SetupGet(x => x.Capabilities).Returns(new AgentCapabilities
            {
                SupportsStreaming = true,
                SupportedModels = new List<string> { "chat" }
            });

            _client.On<AgentStatusDto>("AgentStatusUpdate", status =>
            {
                statusUpdates.Add(status);
            });

            // Subscribe to agent first
            await _client.InvokeAsync("SubscribeToAgent", "test-agent");
            await Task.Delay(100);

            // Act - Simulate status change
            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Processing);
            testAgent.Raise(x => x.StatusChanged += null, 
                testAgent.Object, 
                new AgentStatusChangedEventArgs
                {
                    AgentId = "test-agent",
                    OldStatus = AgentStatus.Ready,
                    NewStatus = AgentStatus.Processing
                });

            // Assert
            await Task.Delay(200);
            Assert.True(statusUpdates.Count > 1); // Initial status + status change
            Assert.Equal(AgentStatus.Processing, statusUpdates.Last().Status);
        }

        [Fact]
        public async Task SendAgentMessage_WithAttachments_ShouldIncludeAttachments()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testSession = TestDataBuilder.CreateTestSession();
            var testAgent = new Mock<IAgent>();
            var testResponses = TestDataBuilder.CreateTestAgentResponseStream();
            var capturedMessage = (AgentMessage?)null;

            _fixture.MockSessionManager
                .Setup(x => x.GetSessionAsync("test-session"))
                .ReturnsAsync(testSession);

            _fixture.MockSessionManager
                .Setup(x => x.UpdateSessionAsync(It.IsAny<Session>()))
                .ReturnsAsync(true);

            _fixture.MockAgentFactory
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.SendMessageStreamAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .Callback<AgentMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .ReturnsAsync(testResponses);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
                .Callback<AgentMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .ReturnsAsync(new AgentResponse
                {
                    Content = "Final response",
                    Type = ResponseType.Text,
                    IsComplete = true
                });

            var attachments = new List<Attachment>
            {
                new Attachment { MimeType = "file", Url = "/path/to/file.txt", FileName = "file.txt" }
            };

            var request = new AgentMessageRequest
            {
                SessionId = "test-session",
                AgentId = "test-agent",
                Content = "Message with attachments",
                Attachments = attachments
            };

            // Act
            await _client.InvokeAsync("SendAgentMessage", request);

            // Assert
            await Task.Delay(200);
            Assert.NotNull(capturedMessage);
            Assert.Single(capturedMessage!.Attachments);
            Assert.Equal("file", capturedMessage.Attachments.First().MimeType);
            Assert.Equal("/path/to/file.txt", capturedMessage.Attachments.First().Url);
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