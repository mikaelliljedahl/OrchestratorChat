namespace OrchestratorChat.Data.Models;

public class TeamEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SessionId { get; set; } = string.Empty;
    public string PoliciesJson { get; set; } = string.Empty; // JSON serialized policies
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual SessionEntity Session { get; set; } = null!;
    public virtual ICollection<TeamMemberEntity> Members { get; set; } = new List<TeamMemberEntity>();
}