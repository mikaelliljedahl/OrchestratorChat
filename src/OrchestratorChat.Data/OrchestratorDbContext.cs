using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Data.Models;
using OrchestratorChat.Data.Entities;

namespace OrchestratorChat.Data;

public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<SessionEntity> Sessions { get; set; } = null!;
    public DbSet<MessageEntity> Messages { get; set; } = null!;
    public DbSet<AgentEntity> Agents { get; set; } = null!;
    public DbSet<AgentConfigurationEntity> AgentConfigurations { get; set; } = null!;
    public DbSet<SessionAgentEntity> SessionAgents { get; set; } = null!;
    public DbSet<AttachmentEntity> Attachments { get; set; } = null!;
    public DbSet<ToolCallEntity> ToolCalls { get; set; } = null!;
    public DbSet<SessionSnapshotEntity> SessionSnapshots { get; set; } = null!;
    public DbSet<OrchestrationPlanEntity> OrchestrationPlans { get; set; } = null!;
    public DbSet<OrchestrationStepEntity> OrchestrationSteps { get; set; } = null!;
    public DbSet<TeamEntity> Teams { get; set; } = null!;
    public DbSet<TeamMemberEntity> TeamMembers { get; set; } = null!;
    public DbSet<Plan> Plans { get; set; } = null!;
    public DbSet<PlanStep> PlanSteps { get; set; } = null!;
    
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
            entity.HasIndex(e => e.IsDefault);
            
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
        
        // Team configuration
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Members)
                .WithOne(m => m.Team)
                .HasForeignKey(m => m.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // TeamMember configuration (Many-to-many: Team-Agent)
        modelBuilder.Entity<TeamMemberEntity>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.AgentId });
            
            entity.HasOne(e => e.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TeamId);
            
            entity.HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId);
        });
        
        // Plan configuration
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CommittedAt);
            
            entity.HasMany(e => e.Steps)
                .WithOne(s => s.Plan)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PlanStep configuration
        modelBuilder.Entity<PlanStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlanId, e.StepOrder });
            entity.HasIndex(e => e.Owner);
        });
    }
}