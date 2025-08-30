namespace OrchestratorChat.Data.Models;

public class SessionSnapshotEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }
    
    // Navigation property
    public virtual SessionEntity Session { get; set; } = null!;
    
    // JSON serialized data
    public string SessionStateJson { get; set; } = string.Empty; // Full session state
    public string AgentStatesJson { get; set; } = string.Empty; // Dictionary<string, AgentState>
}