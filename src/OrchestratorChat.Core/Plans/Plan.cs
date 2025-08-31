namespace OrchestratorChat.Core.Plans;

/// <summary>
/// Represents a plan with steps for team collaboration
/// </summary>
public class Plan
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Session ID this plan is associated with
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the plan
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Goal or objective of the plan
    /// </summary>
    public string Goal { get; set; } = string.Empty;
    
    /// <summary>
    /// List of steps in the plan
    /// </summary>
    public List<PlanStep> Steps { get; set; } = new();
    
    /// <summary>
    /// When the plan was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the plan was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// When the plan was committed (finalized)
    /// </summary>
    public DateTime? CommittedAt { get; set; }
}