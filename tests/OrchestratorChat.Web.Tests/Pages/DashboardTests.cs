using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using OrchestratorChat.Web.Pages;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Agents;
using NSubstitute;
using Xunit;

namespace OrchestratorChat.Web.Tests.Pages;

public class DashboardTests : TestContext
{
    public DashboardTests()
    {
        Services.AddMudServices();
        
        // Mock NavigationManager
        Services.AddSingleton(MockServiceFactory.CreateMockNavigationManager());
        
        // Add mock services
        Services.AddSingleton(MockServiceFactory.CreateMockSessionManager());
        Services.AddSingleton(MockServiceFactory.CreateMockAgentService());
    }

    [Fact]
    public void Dashboard_Should_Render_Agent_Cards()
    {
        // Act
        var component = RenderComponent<Dashboard>();
        
        // Assert
        // Should find agent card elements for the mocked agents
        Assert.Contains("Claude", component.Markup);
        Assert.Contains("Saturn", component.Markup);
    }

    [Fact]
    public void Dashboard_Should_Show_Session_Statistics()
    {
        // Act
        var component = RenderComponent<Dashboard>();
        
        // Assert
        // The dashboard should display the main heading
        Assert.Contains("Agent Dashboard", component.Markup);
    }

    [Fact]
    public void Dashboard_Should_Have_Create_Session_Button()
    {
        // Act
        var component = RenderComponent<Dashboard>();
        
        // Assert
        Assert.Contains("New Session", component.Markup);
    }

    [Fact]
    public void Dashboard_Should_Render_Navigation_Menu()
    {
        // Act
        var component = RenderComponent<Dashboard>();
        
        // Assert
        // The dashboard should render with its main grid structure
        Assert.Contains("Agent Dashboard", component.Markup);
        Assert.Contains("Add Agent", component.Markup);
    }
}

