namespace OrchestratorChat.Core.Teams;

/// <summary>
/// Represents a team of agents collaborating on a session
/// </summary>
public class Team
{
    /// <summary>
    /// Unique identifier for the team
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Session ID this team is associated with
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// List of team members with their roles
    /// </summary>
    public List<TeamMember> Members { get; set; } = new();
    
    /// <summary>
    /// Team policies as JSON string (for now)
    /// </summary>
    public string PoliciesJson { get; set; } = string.Empty;
    
    /// <summary>
    /// When the team was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the team was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}