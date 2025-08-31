namespace OrchestratorChat.Core.Handoffs;

/// <summary>
/// Context information for a handoff request
/// </summary>
public class HandoffContext
{
    /// <summary>
    /// Description of current progress on the step
    /// </summary>
    public string CurrentProgress { get; set; } = string.Empty;
    
    /// <summary>
    /// List of files that have been modified during the step
    /// </summary>
    public List<string> FilesModified { get; set; } = new();
    
    /// <summary>
    /// Additional notes or context for the receiving agent
    /// </summary>
    public string Notes { get; set; } = string.Empty;
    
    /// <summary>
    /// Dependencies or requirements for completing the step
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Any additional metadata or context data
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}