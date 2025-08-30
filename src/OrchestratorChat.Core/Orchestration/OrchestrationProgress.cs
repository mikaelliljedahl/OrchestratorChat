namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Progress information for an ongoing orchestration
/// </summary>
public class OrchestrationProgress
{
    /// <summary>
    /// Current step being executed (1-based)
    /// </summary>
    public int CurrentStep { get; set; }
    
    /// <summary>
    /// Total number of steps in the orchestration
    /// </summary>
    public int TotalSteps { get; set; }
    
    /// <summary>
    /// ID of the agent currently executing
    /// </summary>
    public string CurrentAgent { get; set; }
    
    /// <summary>
    /// Description of the current task being performed
    /// </summary>
    public string CurrentTask { get; set; }
    
    /// <summary>
    /// Percentage of completion (0-100)
    /// </summary>
    public double PercentComplete { get; set; }
    
    /// <summary>
    /// Time elapsed since orchestration started
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
}