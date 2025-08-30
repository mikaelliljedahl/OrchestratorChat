using OrchestratorChat.Data.Models;
using OrchestratorChat.Data.Repositories;

namespace OrchestratorChat.Data;

public interface IUnitOfWork : IDisposable
{
    ISessionRepository Sessions { get; }
    IAgentRepository Agents { get; }
    IRepository<MessageEntity> Messages { get; }
    IRepository<AttachmentEntity> Attachments { get; }
    IRepository<ToolCallEntity> ToolCalls { get; }
    IRepository<SessionSnapshotEntity> Snapshots { get; }
    IRepository<OrchestrationPlanEntity> OrchestrationPlans { get; }
    IRepository<OrchestrationStepEntity> OrchestrationSteps { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}