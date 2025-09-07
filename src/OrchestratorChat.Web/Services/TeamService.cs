using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Teams;
using OrchestratorChat.Data.Repositories;

namespace OrchestratorChat.Web.Services;

public class TeamService : ITeamService
{
    private readonly IAgentService _agentService;
    private readonly ITeamRepository _teamRepository;

    public TeamService(IAgentService agentService, ITeamRepository teamRepository)
    {
        _agentService = agentService;
        _teamRepository = teamRepository;
    }

    public async Task<List<AgentInfo>> GetAvailableAgentsAsync()
    {
        var agents = await _agentService.GetConfiguredAgentsAsync();
        return agents.Where(a => a.Status == AgentStatus.Ready).ToList();
    }

    public async Task<Team> CreateTeamAsync(string sessionId, List<TeamMember> members)
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Members = members,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _teamRepository.AddAsync(team);
        return team;
    }

    public async Task<bool> AssignRoleAsync(Guid teamId, string agentId, string role)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return false;

        var member = team.Members.FirstOrDefault(m => m.AgentId == agentId);
        if (member == null) return false;

        member.Role = role;
        team.UpdatedAt = DateTime.UtcNow;

        await _teamRepository.UpdateAsync(team);
        return true;
    }

    public async Task<Team?> GetTeamBySessionAsync(string sessionId)
    {
        return await _teamRepository.GetBySessionIdAsync(sessionId);
    }

    public async Task<bool> UpdateTeamAsync(Team team)
    {
        team.UpdatedAt = DateTime.UtcNow;
        await _teamRepository.UpdateAsync(team);
        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid teamId, string agentId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null) return false;

        var member = team.Members.FirstOrDefault(m => m.AgentId == agentId);
        if (member == null) return false;

        team.Members.Remove(member);
        team.UpdatedAt = DateTime.UtcNow;

        await _teamRepository.UpdateAsync(team);
        return true;
    }
}