using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Represents a response from an agent
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// ID of the message this response relates to
    /// </summary>
    public string MessageId { get; set; } = string.Empty;
    
    /// <summary>
    /// Content of the response
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of response
    /// </summary>
    public ResponseType Type { get; set; }
    
    /// <summary>
    /// Whether this response is complete or if more content is expected
    /// </summary>
    public bool IsComplete { get; set; }
    
    /// <summary>
    /// Tool calls requested in this response
    /// </summary>
    public List<ToolCall> ToolCalls { get; set; } = new();
    
    /// <summary>
    /// Additional metadata associated with the response
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Token usage information for this response
    /// </summary>
    public TokenUsage Usage { get; set; } = new();
    
    /// <summary>
    /// Unique identifier for the response (for test compatibility)
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Role of the agent that generated this response (for test compatibility)
    /// </summary>
    public MessageRole Role { get; set; } = MessageRole.Assistant;
    
    /// <summary>
    /// Timestamp when the response was generated (for test compatibility)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}