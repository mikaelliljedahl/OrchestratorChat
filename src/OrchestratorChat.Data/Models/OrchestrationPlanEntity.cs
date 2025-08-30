using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Data.Models;

public class OrchestrationPlanEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public OrchestrationStrategy Strategy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    
    // Navigation properties
    public virtual SessionEntity Session { get; set; } = null!;
    public virtual ICollection<OrchestrationStepEntity> Steps { get; set; } = new List<OrchestrationStepEntity>();
    
    // JSON serialized data
    public string SharedContextJson { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
}