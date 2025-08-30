namespace OrchestratorChat.Data.Models;

public class ToolCallEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = string.Empty; // Dictionary<string, object>
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; }
    
    // Navigation property
    public virtual MessageEntity Message { get; set; } = null!;
}