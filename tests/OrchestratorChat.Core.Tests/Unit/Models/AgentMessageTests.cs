using FluentAssertions;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tests.Fixtures;
using Xunit;

namespace OrchestratorChat.Core.Tests.Unit.Models;

/// <summary>
/// Unit tests for the AgentMessage model
/// </summary>
public class AgentMessageTests
{
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
    public void AgentMessage_Should_Accept_Configuration_Via_Builder()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var agentId = "test-agent";
        var content = "Test message content";
        var timestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var message = TestDataBuilder.Message()
            .WithId(messageId)
            .WithContent(content)
            .WithRole(MessageRole.User)
            .WithAgentId(agentId)
            .WithSessionId(sessionId)
            .WithTimestamp(timestamp)
            .WithMetadata("testKey", "testValue")
            .Build();

        // Assert
        message.Id.Should().Be(messageId);
        message.Content.Should().Be(content);
        message.Role.Should().Be(MessageRole.User);
        message.AgentId.Should().Be(agentId);
        message.SessionId.Should().Be(sessionId);
        message.Timestamp.Should().Be(timestamp);
        message.Metadata.Should().ContainKey("testKey");
        message.Metadata["testKey"].Should().Be("testValue");
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

    [Fact]
    public void DefaultUserMessage_Should_Have_Correct_Properties()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var content = "Test user message";

        // Act
        var message = TestDataBuilder.DefaultUserMessage(sessionId, content);

        // Assert
        message.Content.Should().Be(content);
        message.Role.Should().Be(MessageRole.User);
        message.SessionId.Should().Be(sessionId);
        message.AgentId.Should().Be("user");
    }

    [Fact]
    public void DefaultAssistantMessage_Should_Have_Correct_Properties()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var agentId = "test-agent";
        var content = "Test assistant response";

        // Act
        var message = TestDataBuilder.DefaultAssistantMessage(sessionId, agentId, content);

        // Assert
        message.Content.Should().Be(content);
        message.Role.Should().Be(MessageRole.Assistant);
        message.SessionId.Should().Be(sessionId);
        message.AgentId.Should().Be(agentId);
    }
}