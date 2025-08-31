using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Teams;

/// <summary>
/// Represents a member of a team with their assigned role
/// </summary>
public class TeamMember
{
    /// <summary>
    /// ID of the team this member belongs to
    /// </summary>
    public Guid TeamId { get; set; }
    
    /// <summary>
    /// ID of the agent assigned to this role
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of agent
    /// </summary>
    public AgentType AgentType { get; set; }
    
    /// <summary>
    /// Name of the agent
    /// </summary>
    public string AgentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role of this member in the team (e.g., "lead", "contributor", "reviewer")
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// When this member joined the team
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}