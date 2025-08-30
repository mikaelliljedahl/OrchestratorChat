using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data.Models;

namespace OrchestratorChat.Data;

public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<SessionEntity> Sessions { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<AgentEntity> Agents { get; set; }
    public DbSet<AgentConfigurationEntity> AgentConfigurations { get; set; }
    public DbSet<SessionAgentEntity> SessionAgents { get; set; }
    public DbSet<AttachmentEntity> Attachments { get; set; }
    public DbSet<ToolCallEntity> ToolCalls { get; set; }
    public DbSet<SessionSnapshotEntity> SessionSnapshots { get; set; }
    public DbSet<OrchestrationPlanEntity> OrchestrationPlans { get; set; }
    public DbSet<OrchestrationStepEntity> OrchestrationSteps { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Session configuration
        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProjectId);
            
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Snapshots)
                .WithOne(s => s.Session)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Message configuration
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.AgentId);
            
            entity.HasMany(e => e.Attachments)
                .WithOne(a => a.Message)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.ToolCalls)
                .WithOne(t => t.Message)
                .HasForeignKey(t => t.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Agent configuration
        modelBuilder.Entity<AgentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            
            entity.HasOne(e => e.Configuration)
                .WithOne(c => c.Agent)
                .HasForeignKey<AgentConfigurationEntity>(c => c.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Many-to-many: Session-Agent
        modelBuilder.Entity<SessionAgentEntity>(entity =>
        {
            entity.HasKey(e => new { e.SessionId, e.AgentId });
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionAgents)
                .HasForeignKey(e => e.SessionId);
            
            entity.HasOne(e => e.Agent)
                .WithMany(a => a.SessionAgents)
                .HasForeignKey(e => e.AgentId);
        });
        
        // Orchestration configuration
        modelBuilder.Entity<OrchestrationPlanEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasMany(e => e.Steps)
                .WithOne(s => s.Plan)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<OrchestrationStepEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlanId, e.Order });
            entity.HasIndex(e => e.Status);
        });
    }
}