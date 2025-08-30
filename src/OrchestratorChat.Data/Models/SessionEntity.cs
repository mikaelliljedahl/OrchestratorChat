using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.Data.Models;

public class SessionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public SessionStatus Status { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
    public virtual ICollection<SessionAgentEntity> SessionAgents { get; set; } = new List<SessionAgentEntity>();
    public virtual ICollection<SessionSnapshotEntity> Snapshots { get; set; } = new List<SessionSnapshotEntity>();
    
    // JSON serialized data
    public string ContextJson { get; set; } = string.Empty; // Dictionary<string, object>
    public string MetadataJson { get; set; } = string.Empty; // Additional metadata
}