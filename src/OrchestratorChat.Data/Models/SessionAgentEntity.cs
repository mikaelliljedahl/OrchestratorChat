namespace OrchestratorChat.Data.Models;

public class SessionAgentEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public string Role { get; set; } = string.Empty; // Primary, Secondary, Observer
    
    // Navigation properties
    public virtual SessionEntity Session { get; set; } = null!;
    public virtual AgentEntity Agent { get; set; } = null!;
}