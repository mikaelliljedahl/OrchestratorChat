namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Defines the type of agent response
/// </summary>
public enum ResponseType
{
    /// <summary>
    /// Plain text response
    /// </summary>
    Text,
    
    /// <summary>
    /// Tool call response
    /// </summary>
    ToolCall,
    
    /// <summary>
    /// Error response
    /// </summary>
    Error,
    
    /// <summary>
    /// Status update response
    /// </summary>
    Status,
    
    /// <summary>
    /// Thinking/reasoning response
    /// </summary>
    Thinking
}