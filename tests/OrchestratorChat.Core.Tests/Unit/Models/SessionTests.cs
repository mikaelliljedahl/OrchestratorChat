using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using OrchestratorChat.Core.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Core.Tests.Unit.Models;

/// <summary>
/// Unit tests for the Session model
/// </summary>
public class SessionTests
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
        Assert.Equal(SessionStatus.Active, session.Status); // Default to Active
    }

    [Fact]
    public void Session_Should_Accept_Configuration_Via_Builder()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var agentId = "test-agent";
        var message = TestDataBuilder.DefaultUserMessage(sessionId);

        // Act
        var session = TestDataBuilder.Session()
            .WithId(sessionId)
            .WithName("Test Session")
            .WithType(SessionType.MultiAgent)
            .WithStatus(SessionStatus.Active)
            .WithParticipantAgent(agentId)
            .WithMessage(message)
            .WithContextValue("testKey", "testValue")
            .Build();

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal("Test Session", session.Name);
        Assert.Equal(SessionType.MultiAgent, session.Type);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Contains(agentId, session.ParticipantAgentIds);
        Assert.Contains(message, session.Messages);
        Assert.True(session.Context.ContainsKey("testKey"));
        Assert.Equal("testValue", session.Context["testKey"]);
    }

    [Fact]
    public void Session_Should_Support_Multiple_Participants()
    {
        // Arrange
        var agentIds = new[] { "agent1", "agent2", "agent3" };

        // Act
        var session = TestDataBuilder.Session()
            .WithParticipantAgents(agentIds)
            .Build();

        // Assert
        Assert.Equal(3, session.ParticipantAgentIds.Count);
        Assert.All(agentIds, agentId => Assert.Contains(agentId, session.ParticipantAgentIds));
    }

    [Fact]
    public void Session_Should_Support_Multiple_Messages()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var message1 = TestDataBuilder.DefaultUserMessage(sessionId, "First message");
        var message2 = TestDataBuilder.DefaultAssistantMessage(sessionId, "agent1", "Response message");

        // Act
        var session = TestDataBuilder.Session()
            .WithId(sessionId)
            .WithMessages(message1, message2)
            .Build();

        // Assert
        Assert.Equal(2, session.Messages.Count);
        Assert.Contains(message1, session.Messages);
        Assert.Contains(message2, session.Messages);
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
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Failed)]
    public void Session_Should_Support_All_Session_Statuses(SessionStatus status)
    {
        // Act
        var session = TestDataBuilder.Session()
            .WithStatus(status)
            .Build();

        // Assert
        Assert.Equal(status, session.Status);
    }
}