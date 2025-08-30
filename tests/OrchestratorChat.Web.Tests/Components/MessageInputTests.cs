using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using OrchestratorChat.Web.Components;

namespace OrchestratorChat.Web.Tests.Components;

public class MessageInputTests : TestContext
{
    public MessageInputTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void MessageInput_Should_Render_Input_Field()
    {
        // Act
        var component = RenderComponent<MessageInput>();

        // Assert
        Assert.NotNull(component.Find("input"));
        Assert.Contains("Type your message...", component.Markup);
    }

    [Fact]
    public void MessageInput_Should_Enable_Send_Button_When_Message_Present()
    {
        // Act
        var component = RenderComponent<MessageInput>();
        var input = component.Find("input");
        var sendButton = component.Find("button:contains('Send')");

        // Assert - initially disabled
        Assert.True(sendButton.HasAttribute("disabled"));

        // Act - add text to input
        input.Change("Hello world");

        // Assert - now enabled
        Assert.False(sendButton.HasAttribute("disabled"));
    }

    [Fact]
    public void MessageInput_Should_Show_Attachment_Button_When_OnAttach_Present()
    {
        // Arrange
        var attachCallback = () => Task.CompletedTask;

        // Act
        var component = RenderComponent<MessageInput>(parameters =>
            parameters.Add(p => p.OnAttach, attachCallback));

        // Assert
        var attachButton = component.Find("button[aria-label*='attach'], .mud-icon-button");
        Assert.NotNull(attachButton);
    }

    [Fact]
    public void MessageInput_Should_Trigger_OnSendMessage_Callback()
    {
        // Arrange
        var callbackTriggered = false;
        var receivedMessage = string.Empty;

        // Act
        var component = RenderComponent<MessageInput>(parameters =>
            parameters.Add(p => p.OnSendMessage, (string msg) =>
            {
                callbackTriggered = true;
                receivedMessage = msg;
                return Task.CompletedTask;
            }));

        var input = component.Find("input");
        var sendButton = component.Find("button:contains('Send')");

        // Simulate user typing
        input.Change("Test message");
        
        // Click send button
        sendButton.Click();

        // Assert
        Assert.True(callbackTriggered);
        Assert.Equal("Test message", receivedMessage);
    }
}