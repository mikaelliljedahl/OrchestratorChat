namespace OrchestratorChat.Data.Entities;

public class PlanStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlanId { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty; // AgentId
    public string Description { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    
    // Navigation property
    public virtual Plan Plan { get; set; } = null!;
}