namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Event arguments for agent output events
/// </summary>
public class AgentOutputEventArgs : EventArgs
{
    /// <summary>
    /// ID of the agent that produced the output
    /// </summary>
    public string AgentId { get; set; }
    
    /// <summary>
    /// The output content
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// Type of output (stdout, stderr, etc.)
    /// </summary>
    public string OutputType { get; set; }
    
    /// <summary>
    /// Timestamp when the output was received
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata about the output
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}