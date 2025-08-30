using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Data.Models;

public class MessageEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string ParentMessageId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
    
    // Navigation properties
    public virtual SessionEntity Session { get; set; } = null!;
    public virtual AgentEntity Agent { get; set; } = null!;
    public virtual ICollection<AttachmentEntity> Attachments { get; set; } = new List<AttachmentEntity>();
    public virtual ICollection<ToolCallEntity> ToolCalls { get; set; } = new List<ToolCallEntity>();
    
    // JSON serialized data
    public string MetadataJson { get; set; } = string.Empty;
    public string TokenUsageJson { get; set; } = string.Empty; // TokenUsage object
}