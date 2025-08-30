namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Defines the role of a message sender
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from a user
    /// </summary>
    User,
    
    /// <summary>
    /// Message from an assistant/agent
    /// </summary>
    Assistant,
    
    /// <summary>
    /// System message
    /// </summary>
    System,
    
    /// <summary>
    /// Tool execution result message
    /// </summary>
    Tool
}