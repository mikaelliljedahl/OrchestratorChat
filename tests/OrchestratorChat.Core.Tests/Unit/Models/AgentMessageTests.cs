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
        Assert.NotEqual(Guid.Empty.ToString(), message.Id);
        Assert.True(Math.Abs((DateTime.UtcNow - message.Timestamp).TotalSeconds) < 1);
        Assert.Empty(message.Attachments);
        Assert.Empty(message.Metadata);
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
        Assert.Equal(messageId, message.Id);
        Assert.Equal(content, message.Content);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(agentId, message.AgentId);
        Assert.Equal(sessionId, message.SessionId);
        Assert.Equal(timestamp, message.Timestamp);
        Assert.True(message.Metadata.ContainsKey("testKey"));
        Assert.Equal("testValue", message.Metadata["testKey"]);
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

    [Fact]
    public void DefaultUserMessage_Should_Have_Correct_Properties()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var content = "Test user message";

        // Act
        var message = TestDataBuilder.DefaultUserMessage(sessionId, content);

        // Assert
        Assert.Equal(content, message.Content);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(sessionId, message.SessionId);
        Assert.Equal("user", message.AgentId);
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
        Assert.Equal(content, message.Content);
        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal(sessionId, message.SessionId);
        Assert.Equal(agentId, message.AgentId);
    }
}