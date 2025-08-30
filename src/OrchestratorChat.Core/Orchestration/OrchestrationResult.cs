namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Result of executing an entire orchestration plan
/// </summary>
public class OrchestrationResult
{
    /// <summary>
    /// Whether the orchestration completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Results from each step in the orchestration
    /// </summary>
    public List<StepResult> StepResults { get; set; } = new();
    
    /// <summary>
    /// Final output from the orchestration
    /// </summary>
    public string FinalOutput { get; set; }
    
    /// <summary>
    /// Total time taken for the entire orchestration
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
    
    /// <summary>
    /// Final context state after orchestration completion
    /// </summary>
    public Dictionary<string, object> FinalContext { get; set; } = new();
}