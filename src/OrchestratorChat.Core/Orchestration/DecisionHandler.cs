using OrchestratorChat.Core.Decisions;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Exceptions;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Handles decisions in the orchestration system
/// </summary>
public class DecisionHandler : IDecisionHandler
{
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, Decision> _decisions;
    
    public DecisionHandler(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _decisions = new Dictionary<string, Decision>();
    }
    
    /// <summary>
    /// Propose a decision for team consideration
    /// </summary>
    public async Task<Decision> ProposeAsync(
        string sessionId,
        string proposedBy,
        string proposal,
        IEnumerable<string> options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
        
        if (string.IsNullOrWhiteSpace(proposedBy))
            throw new ArgumentException("Proposed by cannot be empty", nameof(proposedBy));
        
        if (string.IsNullOrWhiteSpace(proposal))
            throw new ArgumentException("Proposal cannot be empty", nameof(proposal));
        
        var optionsList = options?.ToList() ?? new List<string>();
        if (!optionsList.Any())
            throw new ArgumentException("At least one option must be provided", nameof(options));
        
        var decision = new Decision
        {
            SessionId = sessionId,
            ProposedBy = proposedBy,
            Proposal = proposal,
            Status = DecisionStatus.Proposed,
            CreatedAt = DateTime.UtcNow
        };
        
        foreach (var option in optionsList)
        {
            decision.AddOption(option);
        }
        
        _decisions[decision.Id] = decision;
        
        var decisionEvent = new DecisionProposedEvent
        {
            DecisionId = decision.Id,
            SessionId = sessionId,
            ProposedBy = proposedBy,
            Proposal = proposal,
            Options = optionsList
        };
        
        await _eventBus.PublishAsync(decisionEvent);
        
        return decision;
    }
    
    /// <summary>
    /// Start the deliberation phase for a decision
    /// </summary>
    public Task<Decision> DeliberateAsync(
        string decisionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decisionId))
            throw new ArgumentException("Decision ID cannot be empty", nameof(decisionId));
        
        if (!_decisions.TryGetValue(decisionId, out var decision))
            throw new OrchestratorException($"Decision with ID '{decisionId}' not found");
        
        if (decision.Status != DecisionStatus.Proposed)
            throw new OrchestratorException($"Decision must be in Proposed status to start deliberation. Current status: {decision.Status}");
        
        decision.Status = DecisionStatus.Deliberating;
        
        return Task.FromResult(decision);
    }
    
    /// <summary>
    /// Cast a vote on a decision
    /// </summary>
    public async Task<Decision> VoteAsync(
        string decisionId,
        string agentId,
        string option,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decisionId))
            throw new ArgumentException("Decision ID cannot be empty", nameof(decisionId));
        
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
        
        if (string.IsNullOrWhiteSpace(option))
            throw new ArgumentException("Option cannot be empty", nameof(option));
        
        if (!_decisions.TryGetValue(decisionId, out var decision))
            throw new OrchestratorException($"Decision with ID '{decisionId}' not found");
        
        if (decision.Status != DecisionStatus.Deliberating && decision.Status != DecisionStatus.Voting)
        {
            // Auto-transition to voting if we're still in deliberating
            if (decision.Status == DecisionStatus.Deliberating)
            {
                decision.Status = DecisionStatus.Voting;
            }
            else
            {
                throw new OrchestratorException($"Decision must be in Deliberating or Voting status to cast votes. Current status: {decision.Status}");
            }
        }
        
        if (!decision.Options.Contains(option))
            throw new ArgumentException($"Option '{option}' is not valid for this decision", nameof(option));
        
        var vote = new DecisionVote
        {
            AgentId = agentId,
            Option = option,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
        
        decision.AddVote(vote);
        
        var voteEvent = new DecisionVoteReceivedEvent
        {
            DecisionId = decisionId,
            AgentId = agentId,
            Option = option,
            Reason = reason
        };
        
        await _eventBus.PublishAsync(voteEvent);
        
        return decision;
    }
    
    /// <summary>
    /// Commit a decision based on the votes
    /// </summary>
    public async Task<Decision> CommitAsync(
        string decisionId,
        string rationale,
        string? consequences = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decisionId))
            throw new ArgumentException("Decision ID cannot be empty", nameof(decisionId));
        
        if (string.IsNullOrWhiteSpace(rationale))
            throw new ArgumentException("Rationale cannot be empty", nameof(rationale));
        
        if (!_decisions.TryGetValue(decisionId, out var decision))
            throw new OrchestratorException($"Decision with ID '{decisionId}' not found");
        
        if (decision.Status != DecisionStatus.Voting)
            throw new OrchestratorException($"Decision must be in Voting status to commit. Current status: {decision.Status}");
        
        if (!decision.Votes.Any())
            throw new OrchestratorException("Decision must have at least one vote to be committed");
        
        decision.Status = DecisionStatus.Committed;
        decision.Rationale = rationale;
        decision.Consequences = consequences;
        decision.CommittedAt = DateTime.UtcNow;
        
        var commitEvent = new DecisionCommittedEvent
        {
            DecisionId = decisionId,
            SessionId = decision.SessionId,
            Rationale = rationale,
            Consequences = consequences,
            Votes = decision.Votes.ToList()
        };
        
        await _eventBus.PublishAsync(commitEvent);
        
        return decision;
    }
    
    /// <summary>
    /// Get a decision by its ID
    /// </summary>
    public Task<Decision?> GetDecisionAsync(
        string decisionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decisionId))
            return Task.FromResult<Decision?>(null);
        
        _decisions.TryGetValue(decisionId, out var decision);
        return Task.FromResult(decision);
    }
    
    /// <summary>
    /// Get all decisions for a session
    /// </summary>
    public Task<IReadOnlyList<Decision>> GetSessionDecisionsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult<IReadOnlyList<Decision>>(Array.Empty<Decision>());
        
        var sessionDecisions = _decisions.Values
            .Where(d => d.SessionId == sessionId)
            .OrderBy(d => d.CreatedAt)
            .ToList()
            .AsReadOnly();
        
        return Task.FromResult<IReadOnlyList<Decision>>(sessionDecisions);
    }
}