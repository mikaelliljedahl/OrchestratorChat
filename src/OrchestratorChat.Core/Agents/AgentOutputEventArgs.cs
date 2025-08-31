namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Event arguments for agent output events
/// </summary>
public class AgentOutputEventArgs : EventArgs
{
    /// <summary>
    /// ID of the agent that produced the output
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// The output content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Legacy property name for test compatibility
    /// </summary>
    public string Output { get => Content; set => Content = value; }
    
    /// <summary>
    /// Type of output (stdout, stderr, etc.)
    /// </summary>
    public string OutputType { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the output was received
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata about the output
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}