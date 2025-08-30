namespace OrchestratorChat.Data.Models;

public class OrchestrationStepEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlanId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    
    // Navigation properties
    public virtual OrchestrationPlanEntity Plan { get; set; } = null!;
    public virtual AgentEntity Agent { get; set; } = null!;
    
    // JSON serialized data
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public string DependsOnJson { get; set; } = string.Empty; // List<string> of step IDs
}

public enum StepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}