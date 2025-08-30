namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Context information provided to tools during execution
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// ID of the agent executing the tool
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// ID of the session the tool execution belongs to
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// Working directory for the execution
    /// </summary>
    string WorkingDirectory { get; }
    
    /// <summary>
    /// Additional context data available to the tool
    /// </summary>
    Dictionary<string, object> ContextData { get; }
    
    /// <summary>
    /// Cancellation token for the execution
    /// </summary>
    CancellationToken CancellationToken { get; }
}