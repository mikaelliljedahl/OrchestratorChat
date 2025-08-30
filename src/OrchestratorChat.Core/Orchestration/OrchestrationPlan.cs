namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Defines a plan for orchestrating multiple agents
/// </summary>
public class OrchestrationPlan
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Ordered list of steps in the orchestration plan
    /// </summary>
    public List<OrchestrationStep> Steps { get; set; } = new();
    
    /// <summary>
    /// Context data shared across all steps in the plan
    /// </summary>
    public Dictionary<string, object> SharedContext { get; set; } = new();
    
    /// <summary>
    /// List of agent IDs required for executing this plan
    /// </summary>
    public List<string> RequiredAgents { get; set; } = new();
    
    /// <summary>
    /// Description of the goal this plan aims to achieve
    /// </summary>
    public string Goal { get; set; } = string.Empty;
    
    /// <summary>
    /// Strategy for executing this plan
    /// </summary>
    public OrchestrationStrategy Strategy { get; set; }
    
    /// <summary>
    /// Name of the plan
    /// </summary>
    public string Name { get; set; } = string.Empty;
}