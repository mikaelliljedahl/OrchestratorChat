using Microsoft.EntityFrameworkCore.Storage;
using OrchestratorChat.Data.Models;
using OrchestratorChat.Data.Repositories;

namespace OrchestratorChat.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrchestratorDbContext _context;
    private IDbContextTransaction? _transaction;
    
    public UnitOfWork(OrchestratorDbContext context)
    {
        _context = context;
        Sessions = new SessionRepository(context);
        Agents = new AgentRepository(context);
        Messages = new Repository<MessageEntity>(context);
        Attachments = new Repository<AttachmentEntity>(context);
        ToolCalls = new Repository<ToolCallEntity>(context);
        Snapshots = new Repository<SessionSnapshotEntity>(context);
        OrchestrationPlans = new Repository<OrchestrationPlanEntity>(context);
        OrchestrationSteps = new Repository<OrchestrationStepEntity>(context);
    }
    
    public ISessionRepository Sessions { get; }
    public IAgentRepository Agents { get; }
    public IRepository<MessageEntity> Messages { get; }
    public IRepository<AttachmentEntity> Attachments { get; }
    public IRepository<ToolCallEntity> ToolCalls { get; }
    public IRepository<SessionSnapshotEntity> Snapshots { get; }
    public IRepository<OrchestrationPlanEntity> OrchestrationPlans { get; }
    public IRepository<OrchestrationStepEntity> OrchestrationSteps { get; }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
    }
    
    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}