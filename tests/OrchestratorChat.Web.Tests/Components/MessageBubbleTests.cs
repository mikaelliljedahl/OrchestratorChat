using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using Xunit;

namespace OrchestratorChat.Web.Tests.Components;

public class MessageBubbleTests : TestContext
{
    public MessageBubbleTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void MessageBubble_Should_Render_Content_Correctly()
    {
        // Arrange
        var message = TestDataFactory.CreateMessage("Test message content");
        
        // Act
        var component = RenderComponent<MessageBubble>(
            parameters => parameters.Add(p => p.Message, message));
        
        // Assert
        Assert.Contains("Test message content", component.Markup);
    }

    [Fact]
    public void MessageBubble_Should_Show_Correct_Sender_Information()
    {
        // Arrange
        var userMessage = TestDataFactory.CreateMessage("User message");
        userMessage.Role = MessageRole.User;
        
        var session = TestDataFactory.CreateSession();
        
        // Act
        var component = RenderComponent<MessageBubble>(
            parameters => parameters
                .Add(p => p.Message, userMessage)
                .AddCascadingValue(session));
        
        // Assert
        Assert.Contains("You", component.Markup);
    }

    [Fact]
    public void MessageBubble_Should_Display_Timestamp()
    {
        // Arrange
        var testTime = new DateTime(2024, 8, 30, 14, 30, 0);
        var message = TestDataFactory.CreateMessage("Test message");
        message.Timestamp = testTime;
        
        // Act
        var component = RenderComponent<MessageBubble>(
            parameters => parameters.Add(p => p.Message, message));
        
        // Assert
        Assert.Contains("14:30", component.Markup);
    }

    [Fact]
    public void MessageBubble_Should_Handle_Null_Content_Gracefully()
    {
        // Arrange
        var message = TestDataFactory.CreateMessage("");
        message.Content = null!;
        
        // Act & Assert - Should not throw exception
        var component = RenderComponent<MessageBubble>(
            parameters => parameters.Add(p => p.Message, message));
            
        Assert.NotNull(component);
    }

    [Fact]
    public void MessageBubble_Should_Render_Attachments_When_Present()
    {
        // Arrange
        var message = TestDataFactory.CreateMessage("Message with attachment");
        message.Attachments = new List<Attachment>
        {
            new Attachment
            {
                Id = "test-attachment-1",
                FileName = "test-file.txt",
                Size = 1024,
                MimeType = "text/plain",
                Content = new byte[0],
                Url = ""
            }
        };
        
        // Act
        var component = RenderComponent<MessageBubble>(
            parameters => parameters.Add(p => p.Message, message));
        
        // Assert
        Assert.Contains("message-attachments", component.Markup);
    }
}