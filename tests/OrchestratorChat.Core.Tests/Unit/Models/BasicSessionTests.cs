using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using Xunit;

namespace OrchestratorChat.Core.Tests.Unit.Models;

/// <summary>
/// Simple unit tests for the Session model without complex dependencies
/// </summary>
public class BasicSessionTests
{
    [Fact]
    public void Session_Should_Initialize_With_Default_Values()
    {
        // Act
        var session = new Session();

        // Assert
        Assert.NotEqual(Guid.Empty.ToString(), session.Id);
        Assert.Empty(session.ParticipantAgentIds);
        Assert.Empty(session.Messages);
        Assert.Empty(session.Context);
    }

    [Fact]
    public void Session_Should_Accept_Basic_Configuration()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var agentId = "test-agent";

        // Act
        var session = TestDataBuilder.Session()
            .WithId(sessionId)
            .WithName("Test Session")
            .WithType(SessionType.MultiAgent)
            .WithStatus(SessionStatus.Active)
            .WithParticipantAgent(agentId)
            .WithContextValue("testKey", "testValue")
            .Build();

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal("Test Session", session.Name);
        Assert.Equal(SessionType.MultiAgent, session.Type);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Contains(agentId, session.ParticipantAgentIds);
        Assert.True(session.Context.ContainsKey("testKey"));
        Assert.Equal("testValue", session.Context["testKey"]);
    }

    [Fact]
    public void AgentMessage_Should_Initialize_With_Default_Values()
    {
        // Act
        var message = new AgentMessage();

        // Assert
        Assert.NotEqual(Guid.Empty.ToString(), message.Id);
        Assert.True(Math.Abs((DateTime.UtcNow - message.Timestamp).TotalSeconds) < 1);
        Assert.Empty(message.Attachments);
        Assert.Empty(message.Metadata);
    }

    [Fact]
    public void TestDataBuilder_Should_Create_Valid_Messages()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var userMessage = TestDataBuilder.DefaultUserMessage(sessionId, "Hello");
        var assistantMessage = TestDataBuilder.DefaultAssistantMessage(sessionId, "agent1", "Hi there");

        // Assert
        Assert.Equal("Hello", userMessage.Content);
        Assert.Equal(MessageRole.User, userMessage.Role);
        Assert.Equal(sessionId, userMessage.SessionId);
        Assert.Equal("user", userMessage.AgentId);

        Assert.Equal("Hi there", assistantMessage.Content);
        Assert.Equal(MessageRole.Assistant, assistantMessage.Role);
        Assert.Equal(sessionId, assistantMessage.SessionId);
        Assert.Equal("agent1", assistantMessage.AgentId);
    }

    [Theory]
    [InlineData(SessionType.SingleAgent)]
    [InlineData(SessionType.MultiAgent)]
    [InlineData(SessionType.Orchestrated)]
    public void Session_Should_Support_All_Session_Types(SessionType sessionType)
    {
        // Act
        var session = TestDataBuilder.Session()
            .WithType(sessionType)
            .Build();

        // Assert
        Assert.Equal(sessionType, session.Type);
    }

    [Theory]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    [InlineData(MessageRole.System)]
    public void AgentMessage_Should_Support_All_Message_Roles(MessageRole role)
    {
        // Act
        var message = TestDataBuilder.Message()
            .WithRole(role)
            .Build();

        // Assert
        Assert.Equal(role, message.Role);
    }
}