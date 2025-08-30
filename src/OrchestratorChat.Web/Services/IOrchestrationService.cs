using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Services;

public interface IOrchestrationService
{
    Task<OrchestrationPlan> CreatePlanAsync(OrchestrationRequest request);
    Task<OrchestrationResult> ExecutePlanAsync(OrchestrationPlan plan, IProgress<OrchestratorChat.Web.Models.OrchestrationProgress>? progress = null);
    Task<List<OrchestrationPlan>> GetRecentPlansAsync(int count = 10);
    Task<OrchestrationResult?> GetExecutionResultAsync(string planId);
}