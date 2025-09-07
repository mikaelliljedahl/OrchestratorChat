using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Teams;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly OrchestratorDbContext _context;

    public TeamRepository(OrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.Agent)
            .FirstOrDefaultAsync(t => t.Id == id);

        return entity != null ? MapEntityToModel(entity) : null;
    }

    public async Task<Team?> GetBySessionIdAsync(string sessionId)
    {
        var entity = await _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.Agent)
            .FirstOrDefaultAsync(t => t.SessionId == sessionId);

        return entity != null ? MapEntityToModel(entity) : null;
    }

    public async Task AddAsync(Team team)
    {
        var entity = MapModelToEntity(team);
        await _context.Teams.AddAsync(entity);
        await _context.SaveChangesAsync();
        
        // Update the team ID from the generated entity
        team.Id = entity.Id;
    }

    public async Task UpdateAsync(Team team)
    {
        var existingEntity = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == team.Id);

        if (existingEntity == null) return;

        // Update team properties
        existingEntity.PoliciesJson = team.PoliciesJson;
        existingEntity.UpdatedAt = team.UpdatedAt;

        // Clear existing members and add updated ones
        _context.TeamMembers.RemoveRange(existingEntity.Members);
        
        foreach (var member in team.Members)
        {
            existingEntity.Members.Add(new TeamMemberEntity
            {
                TeamId = team.Id,
                AgentId = member.AgentId,
                Role = member.Role,
                JoinedAt = member.JoinedAt
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid teamId)
    {
        var entity = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (entity != null)
        {
            _context.Teams.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    // Legacy methods to satisfy IRepository<Team> interface
    Task<Team?> IRepository<Team>.GetByIdAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            return GetByIdAsync(guidId);
        }
        return Task.FromResult<Team?>(null);
    }

    public async Task<IEnumerable<Team>> GetAllAsync()
    {
        var entities = await _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.Agent)
            .ToListAsync();

        return entities.Select(MapEntityToModel);
    }

    public Task<IEnumerable<Team>> FindAsync(System.Linq.Expressions.Expression<Func<Team, bool>> predicate)
    {
        // This would need more complex mapping - for now return empty
        return Task.FromResult(Enumerable.Empty<Team>());
    }

    async Task<Team> IRepository<Team>.AddAsync(Team entity)
    {
        await AddAsync(entity);
        return entity;
    }

    public async Task DeleteAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            await DeleteAsync(guidId);
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            return await _context.Teams.AnyAsync(t => t.Id == guidId);
        }
        return false;
    }

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Team, bool>>? predicate = null)
    {
        return await _context.Teams.CountAsync();
    }

    private static Team MapEntityToModel(TeamEntity entity)
    {
        return new Team
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            PoliciesJson = entity.PoliciesJson,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Members = entity.Members.Select(m => new TeamMember
            {
                TeamId = m.TeamId,
                AgentId = m.AgentId,
                AgentType = m.Agent?.Type ?? Core.Agents.AgentType.Custom,
                AgentName = m.Agent?.Name ?? string.Empty,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    private static TeamEntity MapModelToEntity(Team model)
    {
        return new TeamEntity
        {
            Id = model.Id,
            SessionId = model.SessionId,
            PoliciesJson = model.PoliciesJson,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            Members = model.Members.Select(m => new TeamMemberEntity
            {
                TeamId = model.Id,
                AgentId = m.AgentId,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }
}