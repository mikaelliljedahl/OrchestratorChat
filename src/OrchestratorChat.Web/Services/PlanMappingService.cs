using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Plans;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for mapping between Core.Orchestration.OrchestrationPlan and Core.Plans.Plan
/// </summary>
public class PlanMappingService
{
    /// <summary>
    /// Maps from OrchestrationPlan to Core.Plans.Plan
    /// </summary>
    /// <param name="orchestrationPlan">The orchestration plan to map</param>
    /// <param name="sessionId">The session ID to associate with the plan</param>
    /// <returns>The mapped Core.Plans.Plan</returns>
    public Plan MapToCorePlan(OrchestrationPlan orchestrationPlan, string sessionId)
    {
        if (orchestrationPlan == null)
            throw new ArgumentNullException(nameof(orchestrationPlan));

        return new Plan
        {
            Id = string.IsNullOrEmpty(orchestrationPlan.Id) ? Guid.NewGuid().ToString() : orchestrationPlan.Id,
            SessionId = sessionId,
            Name = orchestrationPlan.Name,
            Goal = orchestrationPlan.Goal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Steps = orchestrationPlan.Steps.Select(s => new PlanStep
            {
                StepOrder = s.Order,
                Title = s.Name,
                Description = s.Description,
                Owner = GetOwnerFromStep(s),
                AcceptanceCriteria = s.Task ?? string.Empty
            }).ToList()
        };
    }

    /// <summary>
    /// Maps from Core.Plans.Plan to OrchestrationPlan
    /// </summary>
    /// <param name="corePlan">The core plan to map</param>
    /// <returns>The mapped OrchestrationPlan</returns>
    public OrchestrationPlan MapToOrchestrationPlan(Plan corePlan)
    {
        if (corePlan == null)
            throw new ArgumentNullException(nameof(corePlan));

        return new OrchestrationPlan
        {
            Id = corePlan.Id,
            Name = corePlan.Name,
            Goal = corePlan.Goal,
            Steps = corePlan.Steps.Select(s => new OrchestrationStep
            {
                Order = s.StepOrder,
                Name = s.Title,
                Description = s.Description,
                AgentId = s.Owner,
                AssignedAgentId = s.Owner,
                Task = s.AcceptanceCriteria,
                Timeout = TimeSpan.FromMinutes(30), // Default timeout
                ExpectedDuration = TimeSpan.FromMinutes(15), // Default duration
                CanRunInParallel = false, // Default to sequential
                DependsOn = new List<string>(), // Default no dependencies
                Input = new Dictionary<string, object>() // Default empty input
            }).ToList(),
            RequiredAgents = corePlan.Steps
                .Where(s => !string.IsNullOrEmpty(s.Owner))
                .Select(s => s.Owner)
                .Distinct()
                .ToList(),
            SharedContext = new Dictionary<string, object>(),
            Strategy = OrchestrationStrategy.Sequential // Default strategy
        };
    }

    /// <summary>
    /// Gets the owner from an orchestration step, handling both AgentId and AssignedAgentId
    /// </summary>
    /// <param name="step">The orchestration step</param>
    /// <returns>The owner agent ID</returns>
    private static string GetOwnerFromStep(OrchestrationStep step)
    {
        // Prefer AssignedAgentId over AgentId as it's more specific to assignment
        return !string.IsNullOrEmpty(step.AssignedAgentId) 
            ? step.AssignedAgentId 
            : step.AgentId;
    }
}