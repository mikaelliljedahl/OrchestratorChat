using OrchestratorChat.Core.Decisions;

namespace OrchestratorChat.Data.Models;

public class DecisionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string ProposedBy { get; set; } = string.Empty;
    public string Proposal { get; set; } = string.Empty;
    public DecisionStatus Status { get; set; } = DecisionStatus.Proposed;
    public string? Rationale { get; set; }
    public string? Consequences { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CommittedAt { get; set; }
    
    // JSON serialized data
    public string OptionsJson { get; set; } = string.Empty; // List<string>
    public string VotesJson { get; set; } = string.Empty; // List<DecisionVote>
    
    // Navigation properties
    public virtual SessionEntity Session { get; set; } = null!;
}