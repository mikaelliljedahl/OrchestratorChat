namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Result of executing a single orchestration step
/// </summary>
public class StepResult
{
    /// <summary>
    /// Order of the step that was executed
    /// </summary>
    public int StepOrder { get; set; }
    
    /// <summary>
    /// Name of the step that was executed
    /// </summary>
    public string StepName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent that executed the step
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the step completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Output produced by the step
    /// </summary>
    public string Output { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message if the step failed
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// Time taken to execute the step
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// Duration taken to execute the step (alias for ExecutionTime)
    /// </summary>
    public TimeSpan Duration
    {
        get => ExecutionTime;
        set => ExecutionTime = value;
    }
    
    /// <summary>
    /// Start time when the step execution began
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Additional data produced by the step for use in subsequent steps
    /// </summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
}