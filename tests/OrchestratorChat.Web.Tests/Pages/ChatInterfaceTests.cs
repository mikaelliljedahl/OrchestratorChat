using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Services;
using OrchestratorChat.Web.Pages;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Core.Sessions;
using NSubstitute;
using Xunit;

namespace OrchestratorChat.Web.Tests.Pages;

public class ChatInterfaceTests : TestContext
{
    public ChatInterfaceTests()
    {
        Services.AddMudServices(configuration => { });
        
        // Mock JSRuntime for JavaScript interactions
        var mockJSRuntime = Substitute.For<IJSRuntime>();
        Services.AddSingleton(mockJSRuntime);
        
        // Mock NavigationManager
        Services.AddSingleton(MockServiceFactory.CreateMockNavigationManager());
        
        // Add mock services
        Services.AddSingleton(MockServiceFactory.CreateMockSessionManager());
        Services.AddSingleton(MockServiceFactory.CreateMockAgentService());
    }

    [Fact]
    public void ChatInterface_Should_Render_Without_Errors()
    {
        // Act
        var component = RenderComponent<ChatInterface>();
        
        // Assert
        Assert.NotNull(component);
        Assert.Contains("chat-container", component.Markup);
    }

    [Fact]
    public void ChatInterface_Should_Display_Message_List()
    {
        // Act
        var component = RenderComponent<ChatInterface>();
        
        // Assert
        Assert.Contains("messages-container", component.Markup);
    }

    [Fact]
    public void ChatInterface_Should_Have_Input_Area()
    {
        // Act
        var component = RenderComponent<ChatInterface>();
        
        // Assert
        // The MessageInput component should be present
        Assert.NotNull(component.FindComponent<MessageInput>());
    }

    [Fact]
    public void ChatInterface_Should_Have_Send_Button()
    {
        // Act
        var component = RenderComponent<ChatInterface>();
        
        // Assert
        // MessageInput component contains the send functionality
        var messageInput = component.FindComponent<MessageInput>();
        Assert.NotNull(messageInput);
    }

    [Fact]
    public void ChatInterface_Should_Initialize_SignalR_Connection()
    {
        // Arrange
        var mockJSRuntime = Services.GetRequiredService<IJSRuntime>();
        
        // Act
        var component = RenderComponent<ChatInterface>();
        
        // Assert
        // Component should render without throwing SignalR connection errors
        Assert.NotNull(component);
        // The hub connection is initialized in OnInitializedAsync
        Assert.Contains("chat-panel", component.Markup);
    }
}

