namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Request for orchestrating multiple agents to achieve a goal
/// </summary>
public class OrchestrationRequest
{
    /// <summary>
    /// The goal or task to accomplish through orchestration
    /// </summary>
    public string Goal { get; set; }
    
    /// <summary>
    /// List of agent IDs available for orchestration
    /// </summary>
    public List<string> AvailableAgentIds { get; set; } = new();
    
    /// <summary>
    /// Strategy to use for orchestration
    /// </summary>
    public OrchestrationStrategy Strategy { get; set; }
    
    /// <summary>
    /// Constraints and limitations for the orchestration
    /// </summary>
    public Dictionary<string, object> Constraints { get; set; } = new();
    
    /// <summary>
    /// Maximum number of steps allowed in the orchestration
    /// </summary>
    public int MaxSteps { get; set; } = 10;
    
    /// <summary>
    /// Maximum time allowed for the entire orchestration
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
}