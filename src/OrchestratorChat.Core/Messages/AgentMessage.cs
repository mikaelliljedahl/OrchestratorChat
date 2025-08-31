namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Represents a message sent to or from an agent
/// </summary>
public class AgentMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public MessageRole Role { get; set; }
    
    /// <summary>
    /// ID of the agent associated with this message
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session this message belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// File attachments included with the message
    /// </summary>
    public List<Attachment> Attachments { get; set; } = new();
    
    /// <summary>
    /// Additional metadata associated with the message
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// ID of the parent message if this is a reply
    /// </summary>
    public string ParentMessageId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the sender (for tracking message origin)
    /// </summary>
    public string? SenderId { get; set; }
}