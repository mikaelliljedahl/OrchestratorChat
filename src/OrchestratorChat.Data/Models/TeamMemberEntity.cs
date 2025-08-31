namespace OrchestratorChat.Data.Models;

public class TeamMemberEntity
{
    public Guid TeamId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g., "lead", "contributor", "reviewer"
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual TeamEntity Team { get; set; } = null!;
    public virtual AgentEntity Agent { get; set; } = null!;
}