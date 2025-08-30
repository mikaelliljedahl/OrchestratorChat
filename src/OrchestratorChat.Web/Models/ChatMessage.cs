using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Web.Models;

public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public string? AgentId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<Attachment>? Attachments { get; set; }
    public TokenUsage? TokenUsage { get; set; }
    public bool IsStreaming { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// ID of the sender (for tracking message origin)
    /// </summary>
    public string? SenderId { get; set; }
}