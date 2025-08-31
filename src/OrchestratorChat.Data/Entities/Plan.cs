namespace OrchestratorChat.Data.Entities;

public class Plan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CommittedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<PlanStep> Steps { get; set; } = new List<PlanStep>();
}