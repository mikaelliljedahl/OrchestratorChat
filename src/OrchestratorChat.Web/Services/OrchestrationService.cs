using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

public class OrchestrationService : IOrchestrationService
{
    private readonly IOrchestrator _orchestrator;
    private readonly Dictionary<string, OrchestrationResult> _results = new();
    private readonly List<OrchestrationPlan> _recentPlans = new();

    public OrchestrationService(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<OrchestrationPlan> CreatePlanAsync(OrchestrationRequest request)
    {
        var plan = await _orchestrator.CreatePlanAsync(request);
        _recentPlans.Insert(0, plan);
        
        // Keep only last 50 plans
        if (_recentPlans.Count > 50)
        {
            _recentPlans.RemoveRange(50, _recentPlans.Count - 50);
        }
        
        return plan;
    }

    public async Task<OrchestrationResult> ExecutePlanAsync(
        OrchestrationPlan plan, 
        IProgress<OrchestratorChat.Web.Models.OrchestrationProgress>? progress = null)
    {
        var coreProgress = progress != null 
            ? new Progress<Core.Orchestration.OrchestrationProgress>(p => 
                progress.Report(MapProgress(p))) 
            : null;
            
        var result = await _orchestrator.ExecutePlanAsync(plan, coreProgress);
        _results[plan.Id] = result;
        
        return result;
    }

    public async Task<List<OrchestrationPlan>> GetRecentPlansAsync(int count = 10)
    {
        return _recentPlans.Take(count).ToList();
    }

    public async Task<OrchestrationResult?> GetExecutionResultAsync(string planId)
    {
        _results.TryGetValue(planId, out var result);
        return result;
    }

    private OrchestratorChat.Web.Models.OrchestrationProgress MapProgress(Core.Orchestration.OrchestrationProgress coreProgress)
    {
        return new OrchestratorChat.Web.Models.OrchestrationProgress
        {
            CurrentStep = coreProgress?.CurrentTask ?? string.Empty,
            CompletedSteps = coreProgress?.CurrentStep ?? 0,
            TotalSteps = coreProgress?.TotalSteps ?? 0,
            Status = "Running",
            ErrorMessage = null,
            Data = new Dictionary<string, object>()
        };
    }
}