using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using OrchestratorChat.Web.Components;
using OrchestratorChat.Web.Tests.TestHelpers;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Tests.Components;

public class OrchestrationTimelineTests : TestContext
{
    public OrchestrationTimelineTests()
    {
        Services.AddMudServices(configuration => { });
    }

    [Fact]
    public void OrchestrationTimeline_Should_Render_Timeline_Structure()
    {
        // Arrange
        var steps = new List<ExecutedStep>
        {
            TestDataFactory.CreateExecutedStep("Initialize", "success"),
            TestDataFactory.CreateExecutedStep("Process Data", "running")
        };

        // Act
        var component = RenderComponent<OrchestrationTimeline>(parameters => 
            parameters.Add(p => p.Steps, steps));

        // Assert
        Assert.Contains("Execution Timeline", component.Markup);
        Assert.Contains("mud-timeline", component.Markup);
        Assert.Contains("Initialize", component.Markup);
        Assert.Contains("Process Data", component.Markup);
    }

    [Fact]
    public void OrchestrationTimeline_Should_Show_Step_Information()
    {
        // Arrange
        var step = TestDataFactory.CreateExecutedStep("File Analysis", "success");
        step.Output = "Successfully analyzed 5 files";
        step.Duration = TimeSpan.FromSeconds(2.3);
        
        var steps = new List<ExecutedStep> { step };

        // Act
        var component = RenderComponent<OrchestrationTimeline>(parameters => 
            parameters.Add(p => p.Steps, steps));

        // Assert
        Assert.Contains("File Analysis", component.Markup);
        Assert.Contains("Successfully analyzed 5 files", component.Markup);
        Assert.Contains("Duration: 2.3s", component.Markup);
        Assert.Contains("test-agent", component.Markup);
    }

    [Fact]
    public void OrchestrationTimeline_Should_Show_Correct_Progress_Indicators()
    {
        // Arrange
        var successStep = TestDataFactory.CreateExecutedStep("Success Step", "success");
        var failedStep = TestDataFactory.CreateExecutedStep("Failed Step", "failed");
        var runningStep = TestDataFactory.CreateExecutedStep("Running Step", "running");
        
        var steps = new List<ExecutedStep> { successStep, failedStep, runningStep };

        // Act
        var component = RenderComponent<OrchestrationTimeline>(parameters => 
            parameters.Add(p => p.Steps, steps));

        // Assert
        Assert.Contains("mud-success", component.Markup);
        Assert.Contains("mud-error", component.Markup);
        Assert.Contains("mud-primary", component.Markup);
    }
}