namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Represents a single step in an orchestration plan
/// </summary>
public class OrchestrationStep
{
    /// <summary>
    /// Order of execution for this step
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Name of the step
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent that should execute this step
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the task to be performed in this step
    /// </summary>
    public string Task { get; set; } = string.Empty;
    
    /// <summary>
    /// List of step orders that this step depends on
    /// </summary>
    public List<string> DependsOn { get; set; } = new();
    
    /// <summary>
    /// Input data for this step
    /// </summary>
    public Dictionary<string, object> Input { get; set; } = new();
    
    /// <summary>
    /// Timeout for this individual step
    /// </summary>
    public TimeSpan Timeout { get; set; }
    
    /// <summary>
    /// Whether this step can be run in parallel with other steps
    /// </summary>
    public bool CanRunInParallel { get; set; }
    
    /// <summary>
    /// Description of what this step does
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent assigned to execute this step
    /// </summary>
    public string AssignedAgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Expected duration for this step
    /// </summary>
    public TimeSpan ExpectedDuration { get; set; }
}