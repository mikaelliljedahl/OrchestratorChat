namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Defines the type of a message in the system
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Plain text message
    /// </summary>
    Text,
    
    /// <summary>
    /// Message from a user
    /// </summary>
    UserMessage,
    
    /// <summary>
    /// Response message from an agent
    /// </summary>
    AgentResponse,
    
    /// <summary>
    /// Tool execution request message
    /// </summary>
    ToolRequest,
    
    /// <summary>
    /// Tool execution result message
    /// </summary>
    ToolResult,
    
    /// <summary>
    /// System message
    /// </summary>
    SystemMessage
}