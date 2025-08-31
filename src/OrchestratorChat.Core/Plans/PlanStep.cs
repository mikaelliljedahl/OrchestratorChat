namespace OrchestratorChat.Core.Plans;

/// <summary>
/// Represents a step within a plan
/// </summary>
public class PlanStep
{
    /// <summary>
    /// Unique identifier for the step
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// ID of the plan this step belongs to
    /// </summary>
    public string PlanId { get; set; } = string.Empty;
    
    /// <summary>
    /// Order/sequence of this step in the plan
    /// </summary>
    public int StepOrder { get; set; }
    
    /// <summary>
    /// Title of the step
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent ID responsible for this step
    /// </summary>
    public string Owner { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the step
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Acceptance criteria for completing this step
    /// </summary>
    public string AcceptanceCriteria { get; set; } = string.Empty;
}