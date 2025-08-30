using FluentAssertions;
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
        session.Id.Should().NotBeEmpty();
        session.ParticipantAgentIds.Should().BeEmpty();
        session.Messages.Should().BeEmpty();
        session.Context.Should().BeEmpty();
        session.Status.Should().Be(SessionStatus.Active); // Default to Active
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
        session.Id.Should().Be(sessionId);
        session.Name.Should().Be("Test Session");
        session.Type.Should().Be(SessionType.MultiAgent);
        session.Status.Should().Be(SessionStatus.Active);
        session.ParticipantAgentIds.Should().Contain(agentId);
        session.Messages.Should().Contain(message);
        session.Context.Should().ContainKey("testKey");
        session.Context["testKey"].Should().Be("testValue");
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
        session.ParticipantAgentIds.Should().HaveCount(3);
        session.ParticipantAgentIds.Should().Contain(agentIds);
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
        session.Messages.Should().HaveCount(2);
        session.Messages.Should().Contain(message1);
        session.Messages.Should().Contain(message2);
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
        session.Type.Should().Be(sessionType);
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
        session.Status.Should().Be(status);
    }
}