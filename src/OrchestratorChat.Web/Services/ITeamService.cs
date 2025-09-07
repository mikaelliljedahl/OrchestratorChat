using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Teams;

namespace OrchestratorChat.Web.Services;

public interface ITeamService
{
    /// <summary>
    /// Gets all available agents that can be assigned to teams
    /// </summary>
    Task<List<AgentInfo>> GetAvailableAgentsAsync();

    /// <summary>
    /// Creates a new team with the specified members
    /// </summary>
    Task<Team> CreateTeamAsync(string sessionId, List<TeamMember> members);

    /// <summary>
    /// Assigns a role to an agent in the team
    /// </summary>
    Task<bool> AssignRoleAsync(Guid teamId, string agentId, string role);

    /// <summary>
    /// Gets the team for a specific session
    /// </summary>
    Task<Team?> GetTeamBySessionAsync(string sessionId);

    /// <summary>
    /// Updates an existing team
    /// </summary>
    Task<bool> UpdateTeamAsync(Team team);

    /// <summary>
    /// Removes a member from the team
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid teamId, string agentId);
}