using FluentAssertions;
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
                .Returns(Task.CompletedTask);

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>()))
                .Returns(testResponses);

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Ready);
            testAgent.SetupGet(x => x.Capabilities).Returns(new List<string> { "chat", "code" });

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

            receivedResponses.Should().HaveCount(3);
            receivedResponses.Should().AllSatisfy(r =>
            {
                r.AgentId.Should().Be("test-agent");
                r.SessionId.Should().Be("test-session");
                r.Response.Should().NotBeNull();
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
            errorReceived.Should().BeTrue();
            receivedError.Should().NotBeNull();
            receivedError!.Error.Should().Be("Session access failed");
            receivedError.AgentId.Should().Be("test-agent");
            receivedError.SessionId.Should().Be("test-session");
        }

        [Fact]
        public async Task ExecuteTool_WithValidRequest_ShouldReturnResult()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();
            var testResult = TestDataBuilder.CreateTestToolResult();

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>()))
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
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Output.Should().Be(testResult.Output);
            response.ExecutionTime.Should().Be(testResult.ExecutionTime);
        }

        [Fact]
        public async Task ExecuteTool_WithException_ShouldReturnErrorResponse()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>()))
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
            response.Should().NotBeNull();
            response.Success.Should().BeFalse();
            response.Error.Should().Be("Tool execution failed");
        }

        [Fact]
        public async Task SubscribeToAgent_ShouldAddToGroupAndSendStatus()
        {
            // Arrange
            _client = await _fixture.CreateAgentClientAsync();
            
            var testAgent = new Mock<IAgent>();
            var statusReceived = false;
            AgentStatusDto? receivedStatus = null;

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Processing);
            testAgent.SetupGet(x => x.Capabilities).Returns(new List<string> { "chat", "code", "tools" });

            _client.On<AgentStatusDto>("AgentStatusUpdate", status =>
            {
                statusReceived = true;
                receivedStatus = status;
            });

            // Act
            await _client.InvokeAsync("SubscribeToAgent", "test-agent");

            // Assert
            await Task.Delay(100);
            statusReceived.Should().BeTrue();
            receivedStatus.Should().NotBeNull();
            receivedStatus!.AgentId.Should().Be("test-agent");
            receivedStatus.Status.Should().Be(AgentStatus.Processing);
            receivedStatus.Capabilities.Should().Contain("chat", "code", "tools");
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

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.SetupGet(x => x.Status).Returns(AgentStatus.Ready);
            testAgent.SetupGet(x => x.Capabilities).Returns(new List<string> { "chat" });

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
                new AgentStatusChangedEventArgs(AgentStatus.Ready, AgentStatus.Processing));

            // Assert
            await Task.Delay(200);
            statusUpdates.Should().HaveCountGreaterThan(1); // Initial status + status change
            statusUpdates.Last().Status.Should().Be(AgentStatus.Processing);
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
                .Returns(Task.CompletedTask);

            _fixture.MockAgentFactory.As<MockAgentFactory>()
                .SetupCreateAgentAsync("test-agent", testAgent.Object);

            testAgent.Setup(x => x.SendMessageAsync(It.IsAny<AgentMessage>()))
                .Callback<AgentMessage>(msg => capturedMessage = msg)
                .Returns(testResponses);

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
            capturedMessage.Should().NotBeNull();
            capturedMessage!.Attachments.Should().HaveCount(1);
            capturedMessage.Attachments.First().MimeType.Should().Be("file");
            capturedMessage.Attachments.First().Url.Should().Be("/path/to/file.txt");
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