using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Web.Tests.Components;

public class AgentCardTests : TestContext
{
    public AgentCardTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void AgentCard_Should_Display_Agent_Name_And_Status()
    {
        // Arrange
        var agent = TestDataFactory.CreateAgent("Claude");
        agent.Status = AgentStatus.Ready;

        // Act
        var component = RenderComponent<AgentCard>(parameters => 
            parameters.Add(p => p.Agent, agent));

        // Assert
        Assert.Contains("Claude", component.Markup);
        Assert.Contains("Ready", component.Markup);
    }

    [Fact]
    public void AgentCard_Should_Show_Capabilities()
    {
        // Arrange
        var agent = TestDataFactory.CreateAgent("Saturn");
        agent.Capabilities = new AgentCapabilities
        {
            AvailableTools = new List<string> { "FileRead", "WebSearch", "CodeGen" }
        };

        // Act
        var component = RenderComponent<AgentCard>(parameters => 
            parameters.Add(p => p.Agent, agent));

        // Assert
        Assert.Contains("Tools: 3", component.Markup);
    }

    [Fact]
    public void AgentCard_Should_Have_Correct_Status_Indicator_Color()
    {
        // Arrange
        var readyAgent = TestDataFactory.CreateAgent("ReadyAgent");
        readyAgent.Status = AgentStatus.Ready;
        
        var busyAgent = TestDataFactory.CreateAgent("BusyAgent");
        busyAgent.Status = AgentStatus.Busy;

        // Act
        var readyComponent = RenderComponent<AgentCard>(parameters => 
            parameters.Add(p => p.Agent, readyAgent));
        var busyComponent = RenderComponent<AgentCard>(parameters => 
            parameters.Add(p => p.Agent, busyAgent));

        // Assert
        Assert.Contains("mud-success", readyComponent.Markup);
        Assert.Contains("mud-warning", busyComponent.Markup);
    }
}