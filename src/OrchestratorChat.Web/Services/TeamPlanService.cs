using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Plans;
using OrchestratorChat.Core.Teams;
using OrchestratorChat.Data;
using OrchestratorChat.Data.Entities;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for managing teams and plans
/// </summary>
public class TeamPlanService
{
    private readonly OrchestratorDbContext _context;

    public TeamPlanService(OrchestratorDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Save a team for the specified session
    /// </summary>
    public async Task<Team> SaveTeamAsync(string sessionId, Team team)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (team == null)
            throw new ArgumentNullException(nameof(team));

        // Check if team already exists for this session
        var existingEntity = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.SessionId == sessionId);

        if (existingEntity != null)
        {
            // Update existing team
            existingEntity.PoliciesJson = team.PoliciesJson;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            
            // Clear existing members
            _context.TeamMembers.RemoveRange(existingEntity.Members);
            
            // Add updated members
            foreach (var member in team.Members)
            {
                existingEntity.Members.Add(new TeamMemberEntity
                {
                    TeamId = existingEntity.Id,
                    AgentId = member.AgentId,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt
                });
            }
        }
        else
        {
            // Create new team
            existingEntity = new TeamEntity
            {
                SessionId = sessionId,
                PoliciesJson = team.PoliciesJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            foreach (var member in team.Members)
            {
                existingEntity.Members.Add(new TeamMemberEntity
                {
                    TeamId = existingEntity.Id,
                    AgentId = member.AgentId,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt
                });
            }
            
            _context.Teams.Add(existingEntity);
        }

        await _context.SaveChangesAsync();
        return MapTeamToCore(existingEntity);
    }

    /// <summary>
    /// Save a plan for the specified session
    /// </summary>
    public async Task<Core.Plans.Plan> SavePlanAsync(string sessionId, Core.Plans.Plan plan)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        // Check if plan already exists for this session
        var existingEntity = await _context.Plans
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.SessionId == sessionId);

        if (existingEntity != null)
        {
            // Update existing plan
            existingEntity.Name = plan.Name;
            existingEntity.Goal = plan.Goal;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            if (plan.CommittedAt.HasValue)
                existingEntity.CommittedAt = plan.CommittedAt;
            
            // Clear existing steps
            _context.PlanSteps.RemoveRange(existingEntity.Steps);
            
            // Add updated steps
            foreach (var step in plan.Steps)
            {
                existingEntity.Steps.Add(new Data.Entities.PlanStep
                {
                    PlanId = existingEntity.Id,
                    StepOrder = step.StepOrder,
                    Title = step.Title,
                    Owner = step.Owner,
                    Description = step.Description,
                    AcceptanceCriteria = step.AcceptanceCriteria
                });
            }
        }
        else
        {
            // Create new plan
            existingEntity = new Data.Entities.Plan
            {
                SessionId = sessionId,
                Name = plan.Name,
                Goal = plan.Goal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = plan.UpdatedAt,
                CommittedAt = plan.CommittedAt
            };
            
            foreach (var step in plan.Steps)
            {
                existingEntity.Steps.Add(new Data.Entities.PlanStep
                {
                    PlanId = existingEntity.Id,
                    StepOrder = step.StepOrder,
                    Title = step.Title,
                    Owner = step.Owner,
                    Description = step.Description,
                    AcceptanceCriteria = step.AcceptanceCriteria
                });
            }
            
            _context.Plans.Add(existingEntity);
        }

        await _context.SaveChangesAsync();
        return MapPlanToCore(existingEntity);
    }

    /// <summary>
    /// Get the team for the specified session
    /// </summary>
    public async Task<Team?> GetTeamAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        var entity = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.SessionId == sessionId);

        return entity != null ? MapTeamToCore(entity) : null;
    }

    /// <summary>
    /// Get the plan for the specified session
    /// </summary>
    public async Task<Core.Plans.Plan?> GetPlanAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return null;

        var entity = await _context.Plans
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.SessionId == sessionId);

        return entity != null ? MapPlanToCore(entity) : null;
    }

    /// <summary>
    /// Map TeamEntity to Core Team model
    /// </summary>
    private static Team MapTeamToCore(TeamEntity entity)
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
                AgentId = m.AgentId,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    /// <summary>
    /// Map Plan entity to Core Plan model
    /// </summary>
    private static Core.Plans.Plan MapPlanToCore(Data.Entities.Plan entity)
    {
        return new Core.Plans.Plan
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            Name = entity.Name,
            Goal = entity.Goal,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CommittedAt = entity.CommittedAt,
            Steps = entity.Steps.OrderBy(s => s.StepOrder).Select(s => new Core.Plans.PlanStep
            {
                Id = s.Id,
                PlanId = s.PlanId,
                StepOrder = s.StepOrder,
                Title = s.Title,
                Owner = s.Owner,
                Description = s.Description,
                AcceptanceCriteria = s.AcceptanceCriteria
            }).ToList()
        };
    }
}