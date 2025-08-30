using FluentAssertions;
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
        session.Id.Should().NotBeEmpty();
        session.ParticipantAgentIds.Should().BeEmpty();
        session.Messages.Should().BeEmpty();
        session.Context.Should().BeEmpty();
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
        session.Id.Should().Be(sessionId);
        session.Name.Should().Be("Test Session");
        session.Type.Should().Be(SessionType.MultiAgent);
        session.Status.Should().Be(SessionStatus.Active);
        session.ParticipantAgentIds.Should().Contain(agentId);
        session.Context.Should().ContainKey("testKey");
        session.Context["testKey"].Should().Be("testValue");
    }

    [Fact]
    public void AgentMessage_Should_Initialize_With_Default_Values()
    {
        // Act
        var message = new AgentMessage();

        // Assert
        message.Id.Should().NotBeEmpty();
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.Attachments.Should().BeEmpty();
        message.Metadata.Should().BeEmpty();
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
        userMessage.Content.Should().Be("Hello");
        userMessage.Role.Should().Be(MessageRole.User);
        userMessage.SessionId.Should().Be(sessionId);
        userMessage.AgentId.Should().Be("user");

        assistantMessage.Content.Should().Be("Hi there");
        assistantMessage.Role.Should().Be(MessageRole.Assistant);
        assistantMessage.SessionId.Should().Be(sessionId);
        assistantMessage.AgentId.Should().Be("agent1");
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
        message.Role.Should().Be(role);
    }
}