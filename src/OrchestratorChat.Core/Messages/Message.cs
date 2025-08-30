namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Represents a message in the orchestration system
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// The role of the message sender (user, assistant, system)
    /// </summary>
    public MessageRole Role { get; init; } = MessageRole.User;
    
    /// <summary>
    /// Type of the message
    /// </summary>
    public MessageType Type { get; init; } = MessageType.Text;
    
    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID of the session this message belongs to
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// ID of the agent associated with this message
    /// </summary>
    public string? AgentId { get; init; }
    
    /// <summary>
    /// Additional metadata associated with the message
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// File attachments included with the message
    /// </summary>
    public List<Attachment> Attachments { get; init; } = new();
    
    /// <summary>
    /// ID of the parent message if this is a reply
    /// </summary>
    public string? ParentMessageId { get; init; }
    
    /// <summary>
    /// ID of the sender (for tracking message origin)
    /// </summary>
    public string? SenderId { get; init; }
}