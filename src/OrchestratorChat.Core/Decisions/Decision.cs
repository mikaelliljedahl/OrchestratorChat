namespace OrchestratorChat.Core.Decisions;

/// <summary>
/// Represents a decision in the orchestration system
/// </summary>
public class Decision : IDecision
{
    /// <summary>
    /// Unique identifier for the decision
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// ID of the session this decision belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent that proposed this decision
    /// </summary>
    public string ProposedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// The proposal text describing what needs to be decided
    /// </summary>
    public string Proposal { get; set; } = string.Empty;
    
    /// <summary>
    /// Available options for this decision (backing field)
    /// </summary>
    private readonly List<string> _options = new();
    
    /// <summary>
    /// Available options for this decision
    /// </summary>
    public IReadOnlyList<string> Options => _options.AsReadOnly();
    
    /// <summary>
    /// Votes collected from team members (backing field)
    /// </summary>
    private readonly List<DecisionVote> _votes = new();
    
    /// <summary>
    /// Votes collected from team members
    /// </summary>
    public IReadOnlyList<DecisionVote> Votes => _votes.AsReadOnly();
    
    /// <summary>
    /// Current status of the decision
    /// </summary>
    public DecisionStatus Status { get; set; } = DecisionStatus.Proposed;
    
    /// <summary>
    /// Rationale for the final decision (populated when committed)
    /// </summary>
    public string? Rationale { get; set; }
    
    /// <summary>
    /// Consequences or impact of the decision (populated when committed)
    /// </summary>
    public string? Consequences { get; set; }
    
    /// <summary>
    /// When the decision was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the decision was committed (null if not yet committed)
    /// </summary>
    public DateTime? CommittedAt { get; set; }
    
    /// <summary>
    /// Add an option to this decision
    /// </summary>
    /// <param name="option">The option to add</param>
    public void AddOption(string option)
    {
        if (!string.IsNullOrWhiteSpace(option) && !_options.Contains(option))
        {
            _options.Add(option);
        }
    }
    
    /// <summary>
    /// Add a vote to this decision
    /// </summary>
    /// <param name="vote">The vote to add</param>
    public void AddVote(DecisionVote vote)
    {
        if (vote != null)
        {
            // Remove any existing vote from the same agent
            _votes.RemoveAll(v => v.AgentId == vote.AgentId);
            _votes.Add(vote);
        }
    }
    
    /// <summary>
    /// Clear all options from this decision
    /// </summary>
    public void ClearOptions()
    {
        _options.Clear();
    }
    
    /// <summary>
    /// Clear all votes from this decision
    /// </summary>
    public void ClearVotes()
    {
        _votes.Clear();
    }
}