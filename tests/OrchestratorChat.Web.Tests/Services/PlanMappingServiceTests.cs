using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Plans;
using OrchestratorChat.Web.Services;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

public class PlanMappingServiceTests
{
    private readonly PlanMappingService _planMappingService;

    public PlanMappingServiceTests()
    {
        _planMappingService = new PlanMappingService();
    }

    [Fact]
    public void MapToCorePlan_WithValidOrchestrationPlan_MapsAllProperties()
    {
        // Arrange
        var orchestrationPlan = new OrchestrationPlan
        {
            Id = "test-id",
            Name = "Test Plan",
            Goal = "Test Goal",
            Steps = new List<OrchestrationStep>
            {
                new OrchestrationStep
                {
                    Order = 1,
                    Name = "Step 1",
                    Description = "First step",
                    AssignedAgentId = "agent-1",
                    Task = "Complete task 1"
                },
                new OrchestrationStep
                {
                    Order = 2,
                    Name = "Step 2",
                    Description = "Second step",
                    AgentId = "agent-2",
                    Task = "Complete task 2"
                }
            }
        };
        var sessionId = "session-123";

        // Act
        var result = _planMappingService.MapToCorePlan(orchestrationPlan, sessionId);

        // Assert
        Assert.Equal(orchestrationPlan.Id, result.Id);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(orchestrationPlan.Name, result.Name);
        Assert.Equal(orchestrationPlan.Goal, result.Goal);
        Assert.Equal(2, result.Steps.Count);
        
        Assert.Equal(1, result.Steps[0].StepOrder);
        Assert.Equal("Step 1", result.Steps[0].Title);
        Assert.Equal("First step", result.Steps[0].Description);
        Assert.Equal("agent-1", result.Steps[0].Owner);
        Assert.Equal("Complete task 1", result.Steps[0].AcceptanceCriteria);

        Assert.Equal(2, result.Steps[1].StepOrder);
        Assert.Equal("Step 2", result.Steps[1].Title);
        Assert.Equal("Second step", result.Steps[1].Description);
        Assert.Equal("agent-2", result.Steps[1].Owner);
        Assert.Equal("Complete task 2", result.Steps[1].AcceptanceCriteria);
    }

    [Fact]
    public void MapToOrchestrationPlan_WithValidCorePlan_MapsAllProperties()
    {
        // Arrange
        var corePlan = new Plan
        {
            Id = "test-id",
            SessionId = "session-123",
            Name = "Test Plan",
            Goal = "Test Goal",
            Steps = new List<PlanStep>
            {
                new PlanStep
                {
                    StepOrder = 1,
                    Title = "Step 1",
                    Description = "First step",
                    Owner = "agent-1",
                    AcceptanceCriteria = "Complete task 1"
                },
                new PlanStep
                {
                    StepOrder = 2,
                    Title = "Step 2", 
                    Description = "Second step",
                    Owner = "agent-2",
                    AcceptanceCriteria = "Complete task 2"
                }
            }
        };

        // Act
        var result = _planMappingService.MapToOrchestrationPlan(corePlan);

        // Assert
        Assert.Equal(corePlan.Id, result.Id);
        Assert.Equal(corePlan.Name, result.Name);
        Assert.Equal(corePlan.Goal, result.Goal);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(2, result.RequiredAgents.Count);
        Assert.Contains("agent-1", result.RequiredAgents);
        Assert.Contains("agent-2", result.RequiredAgents);

        Assert.Equal(1, result.Steps[0].Order);
        Assert.Equal("Step 1", result.Steps[0].Name);
        Assert.Equal("First step", result.Steps[0].Description);
        Assert.Equal("agent-1", result.Steps[0].AgentId);
        Assert.Equal("agent-1", result.Steps[0].AssignedAgentId);
        Assert.Equal("Complete task 1", result.Steps[0].Task);

        Assert.Equal(2, result.Steps[1].Order);
        Assert.Equal("Step 2", result.Steps[1].Name);
        Assert.Equal("Second step", result.Steps[1].Description);
        Assert.Equal("agent-2", result.Steps[1].AgentId);
        Assert.Equal("agent-2", result.Steps[1].AssignedAgentId);
        Assert.Equal("Complete task 2", result.Steps[1].Task);
    }

    [Fact]
    public void MapToCorePlan_WithNullPlan_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _planMappingService.MapToCorePlan(null!, "session-id"));
    }

    [Fact]
    public void MapToOrchestrationPlan_WithNullPlan_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _planMappingService.MapToOrchestrationPlan(null!));
    }

    [Fact]
    public void MapToCorePlan_PrefersAssignedAgentIdOverAgentId()
    {
        // Arrange
        var orchestrationPlan = new OrchestrationPlan
        {
            Id = "test-id",
            Name = "Test Plan",
            Goal = "Test Goal",
            Steps = new List<OrchestrationStep>
            {
                new OrchestrationStep
                {
                    Order = 1,
                    Name = "Step 1",
                    Description = "First step",
                    AgentId = "old-agent-id",
                    AssignedAgentId = "new-agent-id",
                    Task = "Complete task 1"
                }
            }
        };

        // Act
        var result = _planMappingService.MapToCorePlan(orchestrationPlan, "session-123");

        // Assert
        Assert.Equal("new-agent-id", result.Steps[0].Owner);
    }
}