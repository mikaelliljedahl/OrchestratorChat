using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Timeline event representing a message in the conversation
/// </summary>
public class MessageTimelineEvent : ITimelineEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Type of timeline event
    /// </summary>
    public TimelineEventType EventType { get; } = TimelineEventType.Message;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event
    /// </summary>
    public string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// The original message that generated this timeline event
    /// </summary>
    public Message Message { get; }
    
    /// <summary>
    /// Initializes a new MessageTimelineEvent
    /// </summary>
    /// <param name="message">The message that generated this event</param>
    public MessageTimelineEvent(Message message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        
        Id = message.Id;
        Timestamp = message.Timestamp;
        SessionId = message.SessionId;
        AgentId = message.AgentId;
        
        Title = GetMessageTitle(message);
        Description = message.Content;
        
        Metadata = new Dictionary<string, object>
        {
            ["MessageType"] = message.Type.ToString(),
            ["Role"] = message.Role.ToString(),
            ["SenderId"] = message.SenderId ?? string.Empty
        };
        
        if (message.Attachments?.Any() == true)
        {
            Metadata["AttachmentCount"] = message.Attachments.Count;
        }
    }
    
    private static string GetMessageTitle(Message message)
    {
        return message.Role switch
        {
            MessageRole.User => "User Message",
            MessageRole.Assistant => $"Message from {message.AgentId}",
            MessageRole.System => "System Message",
            MessageRole.Tool => "Tool Response",
            _ => "Message"
        };
    }
}