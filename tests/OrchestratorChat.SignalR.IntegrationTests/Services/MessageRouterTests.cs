using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Clients;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.SignalR.Hubs;
using OrchestratorChat.SignalR.Services;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using Xunit;

namespace OrchestratorChat.SignalR.IntegrationTests.Services
{
    /// <summary>
    /// Tests for MessageRouter service
    /// </summary>
    public class MessageRouterTests
    {
        private readonly Mock<IHubContext<AgentHub, IAgentClient>> _mockAgentHubContext;
        private readonly Mock<IHubContext<OrchestratorHub, IOrchestratorClient>> _mockOrchestratorHubContext;
        private readonly Mock<ILogger<MessageRouter>> _mockLogger;
        private readonly MessageRouter _messageRouter;

        public MessageRouterTests()
        {
            _mockAgentHubContext = new Mock<IHubContext<AgentHub, IAgentClient>>();
            _mockOrchestratorHubContext = new Mock<IHubContext<OrchestratorHub, IOrchestratorClient>>();
            _mockLogger = new Mock<ILogger<MessageRouter>>();

            _messageRouter = new MessageRouter(
                _mockAgentHubContext.Object,
                _mockOrchestratorHubContext.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task RouteAgentMessageAsync_ShouldSendToAgentGroupAndSessionGroup()
        {
            // Arrange
            var message = TestDataBuilder.CreateTestAgentMessage("test-agent", "Hello World");
            var sessionId = "test-session";

            var mockAgentGroupClients = new Mock<IAgentClient>();
            var mockSessionGroupClients = new Mock<IAgentClient>();
            var mockAgentClients = new Mock<IHubCallerClients<IAgentClient>>();

            _mockAgentHubContext.SetupGet(x => x.Clients).Returns(mockAgentClients.Object);
            mockAgentClients.Setup(x => x.Group("agent-test-agent")).Returns(mockAgentGroupClients.Object);
            mockAgentClients.Setup(x => x.Group("session-test-session")).Returns(mockSessionGroupClients.Object);

            // Act
            await _messageRouter.RouteAgentMessageAsync(sessionId, message);

            // Assert
            mockAgentGroupClients.Verify(
                x => x.ReceiveAgentResponse(It.Is<AgentResponseDto>(dto =>
                    dto.AgentId == "test-agent" &&
                    dto.SessionId == sessionId &&
                    dto.Response.Content == "Hello World")),
                Times.Once);

            mockSessionGroupClients.Verify(
                x => x.ReceiveAgentResponse(It.Is<AgentResponseDto>(dto =>
                    dto.AgentId == "test-agent" &&
                    dto.SessionId == sessionId &&
                    dto.Response.Content == "Hello World")),
                Times.Once);
        }

        [Fact]
        public async Task RouteOrchestrationUpdateAsync_ShouldSendToSessionGroup()
        {
            // Arrange
            var progress = TestDataBuilder.CreateTestOrchestrationProgress();
            var sessionId = "test-session";

            var mockSessionGroupClients = new Mock<IOrchestratorClient>();
            var mockOrchestratorClients = new Mock<IHubCallerClients<IOrchestratorClient>>();

            _mockOrchestratorHubContext.SetupGet(x => x.Clients).Returns(mockOrchestratorClients.Object);
            mockOrchestratorClients.Setup(x => x.Group("session-test-session")).Returns(mockSessionGroupClients.Object);

            // Act
            await _messageRouter.RouteOrchestrationUpdateAsync(sessionId, progress);

            // Assert
            mockSessionGroupClients.Verify(
                x => x.OrchestrationProgress(progress),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastToSessionAsync_WithAgentMethod_ShouldUseAgentHub()
        {
            // Arrange
            var sessionId = "test-session";
            var method = "AgentCustomMethod";
            var data = new { message = "test data" };

            var mockClientProxy = new Mock<IClientProxy>();
            var mockAgentClients = new Mock<IHubCallerClients<IAgentClient>>();

            _mockAgentHubContext.SetupGet(x => x.Clients).Returns(mockAgentClients.Object);
            mockAgentClients.Setup(x => x.Group("session-test-session")).Returns(mockClientProxy.Object);

            // Act
            await _messageRouter.BroadcastToSessionAsync(sessionId, method, data);

            // Assert
            mockClientProxy.Verify(
                x => x.SendAsync(method, data, default),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastToSessionAsync_WithOrchestrationMethod_ShouldUseOrchestratorHub()
        {
            // Arrange
            var sessionId = "test-session";
            var method = "OrchestrationCustomMethod";
            var data = new { message = "test data" };

            var mockClientProxy = new Mock<IClientProxy>();
            var mockOrchestratorClients = new Mock<IHubCallerClients<IOrchestratorClient>>();

            _mockOrchestratorHubContext.SetupGet(x => x.Clients).Returns(mockOrchestratorClients.Object);
            mockOrchestratorClients.Setup(x => x.Group("session-test-session")).Returns(mockClientProxy.Object);

            // Act
            await _messageRouter.BroadcastToSessionAsync(sessionId, method, data);

            // Assert
            mockClientProxy.Verify(
                x => x.SendAsync(method, data, default),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastToSessionAsync_WithGenericMethod_ShouldUseOrchestratorHub()
        {
            // Arrange
            var sessionId = "test-session";
            var method = "CustomGenericMethod";
            var data = new { message = "test data" };

            var mockClientProxy = new Mock<IClientProxy>();
            var mockOrchestratorClients = new Mock<IHubCallerClients<IOrchestratorClient>>();

            _mockOrchestratorHubContext.SetupGet(x => x.Clients).Returns(mockOrchestratorClients.Object);
            mockOrchestratorClients.Setup(x => x.Group("session-test-session")).Returns(mockClientProxy.Object);

            // Act
            await _messageRouter.BroadcastToSessionAsync(sessionId, method, data);

            // Assert
            mockClientProxy.Verify(
                x => x.SendAsync(method, data, default),
                Times.Once);
        }

        [Fact]
        public async Task RouteToolExecutionUpdateAsync_ShouldSendToAgentGroupAndSessionGroup()
        {
            // Arrange
            var sessionId = "test-session";
            var agentId = "test-agent";
            var update = TestDataBuilder.CreateTestToolExecutionUpdate();

            var mockAgentGroupClients = new Mock<IAgentClient>();
            var mockSessionGroupClients = new Mock<IAgentClient>();
            var mockAgentClients = new Mock<IHubCallerClients<IAgentClient>>();

            _mockAgentHubContext.SetupGet(x => x.Clients).Returns(mockAgentClients.Object);
            mockAgentClients.Setup(x => x.Group("agent-test-agent")).Returns(mockAgentGroupClients.Object);
            mockAgentClients.Setup(x => x.Group("session-test-session")).Returns(mockSessionGroupClients.Object);

            // Act
            await _messageRouter.RouteToolExecutionUpdateAsync(sessionId, agentId, update);

            // Assert
            mockAgentGroupClients.Verify(
                x => x.ToolExecutionUpdate(update),
                Times.Once);

            mockSessionGroupClients.Verify(
                x => x.ToolExecutionUpdate(update),
                Times.Once);
        }

        [Fact]
        public async Task RouteAgentMessageAsync_WithException_ShouldLogError()
        {
            // Arrange
            var message = TestDataBuilder.CreateTestAgentMessage("test-agent", "Hello World");
            var sessionId = "test-session";

            var mockAgentClients = new Mock<IHubCallerClients<IAgentClient>>();
            _mockAgentHubContext.SetupGet(x => x.Clients).Returns(mockAgentClients.Object);
            mockAgentClients.Setup(x => x.Group(It.IsAny<string>())).Throws(new Exception("Test exception"));

            // Act
            await _messageRouter.RouteAgentMessageAsync(sessionId, message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to route agent message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RouteOrchestrationUpdateAsync_WithException_ShouldLogError()
        {
            // Arrange
            var progress = TestDataBuilder.CreateTestOrchestrationProgress();
            var sessionId = "test-session";

            var mockOrchestratorClients = new Mock<IHubCallerClients<IOrchestratorClient>>();
            _mockOrchestratorHubContext.SetupGet(x => x.Clients).Returns(mockOrchestratorClients.Object);
            mockOrchestratorClients.Setup(x => x.Group(It.IsAny<string>())).Throws(new Exception("Test exception"));

            // Act
            await _messageRouter.RouteOrchestrationUpdateAsync(sessionId, progress);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to route orchestration progress")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastToSessionAsync_WithException_ShouldLogError()
        {
            // Arrange
            var sessionId = "test-session";
            var method = "TestMethod";
            var data = new { };

            var mockOrchestratorClients = new Mock<IHubCallerClients<IOrchestratorClient>>();
            _mockOrchestratorHubContext.SetupGet(x => x.Clients).Returns(mockOrchestratorClients.Object);
            mockOrchestratorClients.Setup(x => x.Group(It.IsAny<string>())).Throws(new Exception("Test exception"));

            // Act
            await _messageRouter.BroadcastToSessionAsync(sessionId, method, data);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to broadcast")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RouteToolExecutionUpdateAsync_WithException_ShouldLogError()
        {
            // Arrange
            var sessionId = "test-session";
            var agentId = "test-agent";
            var update = TestDataBuilder.CreateTestToolExecutionUpdate();

            var mockAgentClients = new Mock<IHubCallerClients<IAgentClient>>();
            _mockAgentHubContext.SetupGet(x => x.Clients).Returns(mockAgentClients.Object);
            mockAgentClients.Setup(x => x.Group(It.IsAny<string>())).Throws(new Exception("Test exception"));

            // Act
            await _messageRouter.RouteToolExecutionUpdateAsync(sessionId, agentId, update);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to route tool execution update")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}