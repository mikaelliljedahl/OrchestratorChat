using OrchestratorChat.Core.Handoffs;

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
    
    /// <summary>
    /// Tools that are allowed to be used during this step
    /// </summary>
    public List<string> AllowedTools { get; set; } = new();
    
    /// <summary>
    /// Whether this step requires tool approval before execution
    /// </summary>
    public bool RequireToolApproval { get; set; } = false;
    
    /// <summary>
    /// Additional execution context data for the agent
    /// </summary>
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    
    /// <summary>
    /// Priority level for this step (higher values = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Maximum number of retry attempts if the step fails
    /// </summary>
    public int MaxRetries { get; set; } = 0;
    
    /// <summary>
    /// ID of the agent currently owning this step (may differ from AssignedAgentId after handoffs)
    /// </summary>
    public string CurrentOwner { get; set; } = string.Empty;
    
    /// <summary>
    /// History of all handoffs for this step
    /// </summary>
    public List<IHandoff> HandoffHistory { get; set; } = new();
}