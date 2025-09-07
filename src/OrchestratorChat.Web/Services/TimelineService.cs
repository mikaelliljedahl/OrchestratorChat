using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Timeline;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Approvals;
using OrchestratorChat.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for managing session timeline events and real-time updates
/// </summary>
public class TimelineService : ITimelineService
{
    private readonly OrchestratorDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly IDecisionHandler _decisionHandler;
    private readonly IHandoffHandler _handoffHandler;
    private readonly IApprovalHandler _approvalHandler;
    private readonly ConcurrentDictionary<string, List<Action<ITimelineEvent>>> _subscriptions = new();

    public TimelineService(
        OrchestratorDbContext context, 
        IEventBus eventBus,
        IDecisionHandler decisionHandler,
        IHandoffHandler handoffHandler,
        IApprovalHandler approvalHandler)
    {
        _context = context;
        _eventBus = eventBus;
        _decisionHandler = decisionHandler;
        _handoffHandler = handoffHandler;
        _approvalHandler = approvalHandler;
        
        // Subscribe to all events that can generate timeline events
        SubscribeToTimelineEvents();
    }

    public async Task<List<ITimelineEvent>> GetSessionEventsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await GetSessionEventsAsync(
            sessionId, 
            eventTypes: null, 
            fromTimestamp: null, 
            toTimestamp: null, 
            pageSize: 1000, 
            offset: 0, 
            cancellationToken);
    }

    public async Task<List<ITimelineEvent>> GetSessionEventsAsync(
        string sessionId,
        IEnumerable<TimelineEventType>? eventTypes = null,
        DateTime? fromTimestamp = null,
        DateTime? toTimestamp = null,
        int pageSize = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var events = new List<ITimelineEvent>();
        
        // Get messages if requested or no filter specified
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.Message))
        {
            var messageEvents = await GetMessageEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(messageEvents);
        }

        // Get step execution events
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.StepExecution))
        {
            var stepEvents = await GetStepExecutionEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(stepEvents);
        }

        // Get tool execution events
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.ToolExecution))
        {
            var toolEvents = await GetToolExecutionEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(toolEvents);
        }

        // Get decision events
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.Decision))
        {
            var decisionEvents = await GetDecisionEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(decisionEvents);
        }

        // Get handoff events
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.Handoff))
        {
            var handoffEvents = await GetHandoffEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(handoffEvents);
        }

        // Get approval events
        if (eventTypes == null || eventTypes.Contains(TimelineEventType.Approval))
        {
            var approvalEvents = await GetApprovalEventsAsync(sessionId, fromTimestamp, toTimestamp, cancellationToken);
            events.AddRange(approvalEvents);
        }

        // Sort all events by timestamp and apply pagination
        var sortedEvents = events
            .OrderBy(e => e.Timestamp)
            .Skip(offset)
            .Take(pageSize)
            .ToList();

        return sortedEvents;
    }

    public Task<IDisposable> SubscribeToUpdatesAsync(
        string sessionId,
        Action<ITimelineEvent> onEventReceived,
        CancellationToken cancellationToken = default)
    {
        _subscriptions.AddOrUpdate(
            sessionId,
            new List<Action<ITimelineEvent>> { onEventReceived },
            (key, existing) => 
            {
                existing.Add(onEventReceived);
                return existing;
            });

        var subscription = new TimelineSubscription(sessionId, onEventReceived, this);
        return Task.FromResult<IDisposable>(subscription);
    }

    public async Task<Dictionary<TimelineEventType, int>> GetEventCountsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<TimelineEventType, int>();

        // Count messages
        var messageCount = await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .CountAsync(cancellationToken);
        counts[TimelineEventType.Message] = messageCount;

        // For other event types, we would query from event store or database
        // For now, return the counts we can compute
        counts[TimelineEventType.Decision] = 0; // TODO: Implement when decision storage is available
        counts[TimelineEventType.Handoff] = 0; // TODO: Implement when handoff storage is available
        counts[TimelineEventType.Approval] = 0; // TODO: Implement when approval storage is available
        counts[TimelineEventType.StepExecution] = 0; // TODO: Implement when step storage is available
        counts[TimelineEventType.ToolExecution] = 0; // TODO: Implement when tool execution storage is available

        return counts;
    }

    public async Task<ITimelineEvent?> GetLatestEventAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var events = await GetSessionEventsAsync(sessionId, pageSize: 1, cancellationToken: cancellationToken);
        return events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
    }

    private async Task<List<ITimelineEvent>> GetMessageEventsAsync(
        string sessionId, 
        DateTime? fromTimestamp, 
        DateTime? toTimestamp, 
        CancellationToken cancellationToken)
    {
        var query = _context.Messages.Where(m => m.SessionId == sessionId);

        if (fromTimestamp.HasValue)
            query = query.Where(m => m.Timestamp >= fromTimestamp.Value);

        if (toTimestamp.HasValue)
            query = query.Where(m => m.Timestamp <= toTimestamp.Value);

        var messageEntities = await query.ToListAsync(cancellationToken);
        
        return messageEntities.Select(entity => new MessageTimelineEvent(MapToCore(entity))).Cast<ITimelineEvent>().ToList();
    }

    private Task<List<ITimelineEvent>> GetStepExecutionEventsAsync(
        string sessionId, 
        DateTime? fromTimestamp, 
        DateTime? toTimestamp, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement when step execution storage is available
        return Task.FromResult(new List<ITimelineEvent>());
    }

    private Task<List<ITimelineEvent>> GetToolExecutionEventsAsync(
        string sessionId, 
        DateTime? fromTimestamp, 
        DateTime? toTimestamp, 
        CancellationToken cancellationToken)
    {
        // TODO: Implement when tool execution storage is available
        return Task.FromResult(new List<ITimelineEvent>());
    }

    private async Task<List<ITimelineEvent>> GetDecisionEventsAsync(
        string sessionId,
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken)
    {
        var decisions = await _decisionHandler.GetSessionDecisionsAsync(sessionId, cancellationToken);
        var events = new List<ITimelineEvent>();

        foreach (var decision in decisions)
        {
            // Filter by timestamp if specified
            if (fromTimestamp.HasValue && decision.CreatedAt < fromTimestamp.Value)
                continue;
            if (toTimestamp.HasValue && decision.CreatedAt > toTimestamp.Value)
                continue;

            // Create DecisionProposedEvent and timeline event for proposal
            var proposedEvent = new DecisionProposedEvent
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = decision.CreatedAt,
                SessionId = decision.SessionId,
                DecisionId = decision.Id,
                ProposedBy = decision.ProposedBy,
                Proposal = decision.Proposal,
                Options = decision.Options.ToList()
            };
            events.Add(new DecisionTimelineEvent(proposedEvent));
            
            if (decision.CommittedAt.HasValue)
            {
                // Create DecisionCommittedEvent and timeline event for commitment
                var committedEvent = new DecisionCommittedEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = decision.CommittedAt.Value,
                    SessionId = decision.SessionId,
                    DecisionId = decision.Id,
                    Rationale = decision.Rationale ?? "Decision committed",
                    Consequences = decision.Consequences,
                    Votes = decision.Votes?.ToList() ?? new List<Core.Decisions.DecisionVote>()
                };
                events.Add(new DecisionTimelineEvent(committedEvent));
            }
        }

        return events;
    }

    private Task<List<ITimelineEvent>> GetHandoffEventsAsync(
        string sessionId,
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken)
    {
        // Since HandoffHandler doesn't have GetSessionHandoffsAsync, we can't get handoffs by session
        // The current interface only supports GetStepHandoffsAsync which requires stepId
        // For now, return empty list until a proper GetSessionHandoffsAsync method is added to HandoffHandler
        // This maintains the existing pattern where unimplemented methods return empty lists
        return Task.FromResult(new List<ITimelineEvent>());
    }

    private async Task<List<ITimelineEvent>> GetApprovalEventsAsync(
        string sessionId,
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken)
    {
        var approvals = await _approvalHandler.GetSessionApprovalsAsync(sessionId, cancellationToken);
        var events = new List<ITimelineEvent>();

        foreach (var approval in approvals)
        {
            // Filter by timestamp if specified
            if (fromTimestamp.HasValue && approval.RequestedAt < fromTimestamp.Value)
                continue;
            if (toTimestamp.HasValue && approval.RequestedAt > toTimestamp.Value)
                continue;

            // Create ApprovalRequestedEvent and timeline event for request
            var requestedEvent = new ApprovalRequestedEvent
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = approval.RequestedAt,
                SessionId = approval.SessionId,
                StepId = approval.StepId,
                ApprovalId = approval.Id,
                RequestedBy = approval.RequestedBy,
                RequestType = approval.RequestType,
                Details = approval.Details,
                RequestedAt = approval.RequestedAt
            };
            events.Add(new ApprovalTimelineEvent(requestedEvent));
            
            if (approval.Status == ApprovalStatus.Approved && approval.RespondedAt.HasValue)
            {
                // Create ApprovalGrantedEvent and timeline event for approval
                var grantedEvent = new ApprovalGrantedEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = approval.RespondedAt.Value,
                    SessionId = approval.SessionId,
                    StepId = approval.StepId,
                    ApprovalId = approval.Id,
                    ApprovedBy = approval.ApprovedBy ?? "Unknown",
                    Reason = approval.Reason,
                    ApprovedAt = approval.RespondedAt.Value
                };
                events.Add(new ApprovalTimelineEvent(grantedEvent));
            }
            else if (approval.Status == ApprovalStatus.Rejected && approval.RespondedAt.HasValue)
            {
                // Create ApprovalRejectedEvent and timeline event for rejection
                var rejectedEvent = new ApprovalRejectedEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = approval.RespondedAt.Value,
                    SessionId = approval.SessionId,
                    StepId = approval.StepId,
                    ApprovalId = approval.Id,
                    RejectedBy = approval.RejectedBy ?? "Unknown",
                    Reason = approval.Reason ?? "Approval rejected",
                    RejectedAt = approval.RespondedAt.Value
                };
                events.Add(new ApprovalTimelineEvent(rejectedEvent));
            }
        }

        return events;
    }

    private void SubscribeToTimelineEvents()
    {
        // Subscribe to decision events
        _eventBus.Subscribe<DecisionProposedEvent>(HandleDecisionProposed);
        _eventBus.Subscribe<DecisionCommittedEvent>(HandleDecisionCommitted);
        
        // Subscribe to approval events
        _eventBus.Subscribe<ApprovalRequestedEvent>(HandleApprovalRequested);
        _eventBus.Subscribe<ApprovalGrantedEvent>(HandleApprovalGranted);
        _eventBus.Subscribe<ApprovalRejectedEvent>(HandleApprovalRejected);
        
        // Subscribe to handoff events
        _eventBus.Subscribe<HandoffInitiatedEvent>(HandleHandoffInitiated);
        _eventBus.Subscribe<HandoffAcknowledgedEvent>(HandleHandoffAcknowledged);
        _eventBus.Subscribe<HandoffRejectedEvent>(HandleHandoffRejected);
        
        // Subscribe to step execution events
        _eventBus.Subscribe<StepCompletedEvent>(HandleStepCompleted);
        
        // Subscribe to tool execution events
        _eventBus.Subscribe<ToolExecutedEvent>(HandleToolExecuted);
        
        // Subscribe to message events
        _eventBus.Subscribe<MessageAddedEvent>(HandleMessageAdded);
    }

    private void HandleDecisionProposed(DecisionProposedEvent eventData)
    {
        var timelineEvent = new DecisionTimelineEvent(eventData);
        NotifySubscribers(eventData.SessionId, timelineEvent);
    }

    private void HandleDecisionCommitted(DecisionCommittedEvent eventData)
    {
        var timelineEvent = new DecisionTimelineEvent(eventData);
        NotifySubscribers(eventData.SessionId, timelineEvent);
    }

    private void HandleApprovalRequested(ApprovalRequestedEvent eventData)
    {
        var timelineEvent = new ApprovalTimelineEvent(eventData);
        NotifySubscribers(eventData.SessionId, timelineEvent);
    }

    private void HandleApprovalGranted(ApprovalGrantedEvent eventData)
    {
        var timelineEvent = new ApprovalTimelineEvent(eventData);
        NotifySubscribers(eventData.SessionId, timelineEvent);
    }

    private void HandleApprovalRejected(ApprovalRejectedEvent eventData)
    {
        var timelineEvent = new ApprovalTimelineEvent(eventData);
        NotifySubscribers(eventData.SessionId, timelineEvent);
    }

    private void HandleHandoffInitiated(HandoffInitiatedEvent eventData)
    {
        var timelineEvent = new HandoffTimelineEvent(eventData);
        NotifySubscribers(eventData.Handoff.SessionId, timelineEvent);
    }

    private void HandleHandoffAcknowledged(HandoffAcknowledgedEvent eventData)
    {
        var timelineEvent = new HandoffTimelineEvent(eventData);
        NotifySubscribers(eventData.Handoff.SessionId, timelineEvent);
    }

    private void HandleHandoffRejected(HandoffRejectedEvent eventData)
    {
        var timelineEvent = new HandoffTimelineEvent(eventData);
        NotifySubscribers(eventData.Handoff.SessionId, timelineEvent);
    }

    private void HandleStepCompleted(StepCompletedEvent eventData)
    {
        var timelineEvent = new StepExecutionTimelineEvent(eventData);
        // StepCompletedEvent doesn't have SessionId, try to get it from context or skip
        // NotifySubscribers("unknown", timelineEvent);
    }

    private void HandleToolExecuted(ToolExecutedEvent eventData)
    {
        var timelineEvent = new ToolExecutionTimelineEvent(eventData);
        NotifySubscribers(eventData.Call.SessionId ?? "unknown", timelineEvent);
    }

    private void HandleMessageAdded(MessageAddedEvent eventData)
    {
        var timelineEvent = new MessageTimelineEvent(TimelineServiceExtensions.ConvertAgentMessageToMessage(eventData.Message));
        NotifySubscribers(eventData.Message.SessionId ?? "unknown", timelineEvent);
    }

    private void NotifySubscribers(string sessionId, ITimelineEvent timelineEvent)
    {
        if (_subscriptions.TryGetValue(sessionId, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    subscriber(timelineEvent);
                }
                catch
                {
                    // Log error but don't fail other subscribers
                }
            }
        }
    }

    internal void RemoveSubscription(string sessionId, Action<ITimelineEvent> callback)
    {
        if (_subscriptions.TryGetValue(sessionId, out var subscribers))
        {
            subscribers.Remove(callback);
            if (subscribers.Count == 0)
            {
                _subscriptions.TryRemove(sessionId, out _);
            }
        }
    }

    private static Message MapToCore(Data.Models.MessageEntity entity)
    {
        return new Message
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            AgentId = entity.AgentId,
            Content = entity.Content,
            Role = entity.Role,
            Type = MessageType.Text, // Default type
            Timestamp = entity.Timestamp,
            Attachments = new List<Core.Messages.Attachment>() // TODO: Map from AttachmentEntities
        };
    }
}

/// <summary>
/// Represents a timeline subscription that can be disposed to unsubscribe
/// </summary>
internal class TimelineSubscription : IDisposable
{
    private readonly string _sessionId;
    private readonly Action<ITimelineEvent> _callback;
    private readonly TimelineService _service;
    private bool _disposed = false;

    public TimelineSubscription(string sessionId, Action<ITimelineEvent> callback, TimelineService service)
    {
        _sessionId = sessionId;
        _callback = callback;
        _service = service;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _service.RemoveSubscription(_sessionId, _callback);
            _disposed = true;
        }
    }
}

public static class TimelineServiceExtensions
{
    public static Message ConvertAgentMessageToMessage(Core.Messages.AgentMessage agentMessage)
    {
        return new Message
        {
            Id = agentMessage.Id,
            SessionId = agentMessage.SessionId,
            AgentId = agentMessage.SenderId,
            Content = agentMessage.Content,
            Role = agentMessage.Role,
            Type = MessageType.Text, // Default type as AgentMessage may not have Type
            Timestamp = agentMessage.Timestamp,
            Attachments = agentMessage.Attachments?.ToList() ?? new List<Core.Messages.Attachment>()
        };
    }
}