using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Handoffs;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Implementation of handoff handling functionality
/// </summary>
public class HandoffHandler : IHandoffHandler
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<HandoffHandler> _logger;
    private readonly ConcurrentDictionary<string, Handoff> _handoffs;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _handoffWaiters;

    /// <summary>
    /// Initializes a new instance of the HandoffHandler
    /// </summary>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="logger">Logger for recording handoff activities</param>
    public HandoffHandler(IEventBus eventBus, ILogger<HandoffHandler> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handoffs = new ConcurrentDictionary<string, Handoff>();
        _handoffWaiters = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
    }

    /// <summary>
    /// Initiates a handoff request from one agent to another
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="stepId">ID of the orchestration step</param>
    /// <param name="fromAgentId">ID of the agent initiating the handoff</param>
    /// <param name="toAgentId">ID of the agent to receive the handoff</param>
    /// <param name="reason">Reason for the handoff</param>
    /// <param name="context">Context information for the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created handoff request</returns>
    public async Task<Handoff> InitiateHandoffAsync(
        string sessionId,
        string stepId,
        string fromAgentId,
        string toAgentId,
        string reason,
        HandoffContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
        if (string.IsNullOrEmpty(stepId))
            throw new ArgumentException("Step ID cannot be empty", nameof(stepId));
        if (string.IsNullOrEmpty(fromAgentId))
            throw new ArgumentException("From agent ID cannot be empty", nameof(fromAgentId));
        if (string.IsNullOrEmpty(toAgentId))
            throw new ArgumentException("To agent ID cannot be empty", nameof(toAgentId));
        if (string.IsNullOrEmpty(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var handoffId = Guid.NewGuid().ToString();
        var handoff = new Handoff
        {
            Id = handoffId,
            SessionId = sessionId,
            StepId = stepId,
            FromAgentId = fromAgentId,
            ToAgentId = toAgentId,
            Reason = reason,
            Context = context,
            Status = HandoffStatus.Initiated,
            InitiatedAt = DateTime.UtcNow
        };

        _handoffs.TryAdd(handoffId, handoff);
        
        _logger.LogInformation("Handoff {HandoffId} initiated from {FromAgent} to {ToAgent} for step {StepId}: {Reason}",
            handoffId, fromAgentId, toAgentId, stepId, reason);

        // Publish handoff initiated event
        await _eventBus.PublishAsync(new HandoffInitiatedEvent(handoff));

        return handoff;
    }

    /// <summary>
    /// Acknowledges a handoff request, accepting ownership of the step
    /// </summary>
    /// <param name="handoffId">ID of the handoff to acknowledge</param>
    /// <param name="acknowledgingAgentId">ID of the agent acknowledging the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated handoff</returns>
    public async Task<Handoff> AcknowledgeHandoffAsync(
        string handoffId,
        string acknowledgingAgentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handoffId))
            throw new ArgumentException("Handoff ID cannot be empty", nameof(handoffId));
        if (string.IsNullOrEmpty(acknowledgingAgentId))
            throw new ArgumentException("Acknowledging agent ID cannot be empty", nameof(acknowledgingAgentId));

        if (!_handoffs.TryGetValue(handoffId, out var handoff))
            throw new InvalidOperationException($"Handoff {handoffId} not found");

        if (handoff.Status != HandoffStatus.Initiated)
            throw new InvalidOperationException($"Handoff {handoffId} is not in Initiated status");

        if (handoff.ToAgentId != acknowledgingAgentId)
            throw new InvalidOperationException($"Agent {acknowledgingAgentId} is not the target agent for handoff {handoffId}");

        handoff.Status = HandoffStatus.Acknowledged;
        handoff.AcknowledgedAt = DateTime.UtcNow;

        _logger.LogInformation("Handoff {HandoffId} acknowledged by agent {AgentId}",
            handoffId, acknowledgingAgentId);

        // Publish handoff acknowledged event
        await _eventBus.PublishAsync(new HandoffAcknowledgedEvent(handoff));

        // Complete any waiting tasks
        if (_handoffWaiters.TryRemove(handoffId, out var waiter))
        {
            waiter.SetResult(true);
        }

        return handoff;
    }

    /// <summary>
    /// Rejects a handoff request, declining ownership of the step
    /// </summary>
    /// <param name="handoffId">ID of the handoff to reject</param>
    /// <param name="rejectingAgentId">ID of the agent rejecting the handoff</param>
    /// <param name="reason">Reason for rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated handoff</returns>
    public async Task<Handoff> RejectHandoffAsync(
        string handoffId,
        string rejectingAgentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handoffId))
            throw new ArgumentException("Handoff ID cannot be empty", nameof(handoffId));
        if (string.IsNullOrEmpty(rejectingAgentId))
            throw new ArgumentException("Rejecting agent ID cannot be empty", nameof(rejectingAgentId));
        if (string.IsNullOrEmpty(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        if (!_handoffs.TryGetValue(handoffId, out var handoff))
            throw new InvalidOperationException($"Handoff {handoffId} not found");

        if (handoff.Status != HandoffStatus.Initiated)
            throw new InvalidOperationException($"Handoff {handoffId} is not in Initiated status");

        if (handoff.ToAgentId != rejectingAgentId)
            throw new InvalidOperationException($"Agent {rejectingAgentId} is not the target agent for handoff {handoffId}");

        handoff.Status = HandoffStatus.Rejected;
        handoff.Context.Notes = $"Rejected: {reason}";

        _logger.LogInformation("Handoff {HandoffId} rejected by agent {AgentId}: {Reason}",
            handoffId, rejectingAgentId, reason);

        // Publish handoff rejected event
        await _eventBus.PublishAsync(new HandoffRejectedEvent(handoff, reason));

        // Complete any waiting tasks
        if (_handoffWaiters.TryRemove(handoffId, out var waiter))
        {
            waiter.SetResult(false);
        }

        return handoff;
    }

    /// <summary>
    /// Gets a handoff by its ID
    /// </summary>
    /// <param name="handoffId">ID of the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The handoff if found, null otherwise</returns>
    public Task<Handoff?> GetHandoffAsync(
        string handoffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handoffId))
            return Task.FromResult<Handoff?>(null);

        _handoffs.TryGetValue(handoffId, out var handoff);
        return Task.FromResult(handoff);
    }

    /// <summary>
    /// Gets all handoffs for a specific step
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="stepId">ID of the step</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of handoffs for the step</returns>
    public Task<IReadOnlyList<Handoff>> GetStepHandoffsAsync(
        string sessionId,
        string stepId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(stepId))
            return Task.FromResult<IReadOnlyList<Handoff>>(Array.Empty<Handoff>());

        var stepHandoffs = _handoffs.Values
            .Where(h => h.SessionId == sessionId && h.StepId == stepId)
            .OrderBy(h => h.InitiatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Handoff>>(stepHandoffs);
    }

    /// <summary>
    /// Gets all pending handoffs for an agent
    /// </summary>
    /// <param name="agentId">ID of the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending handoffs for the agent</returns>
    public Task<IReadOnlyList<Handoff>> GetPendingHandoffsAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(agentId))
            return Task.FromResult<IReadOnlyList<Handoff>>(Array.Empty<Handoff>());

        var pendingHandoffs = _handoffs.Values
            .Where(h => h.ToAgentId == agentId && h.Status == HandoffStatus.Initiated)
            .OrderBy(h => h.InitiatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Handoff>>(pendingHandoffs);
    }

    /// <summary>
    /// Waits for handoff acknowledgment or timeout
    /// </summary>
    /// <param name="handoffId">ID of the handoff to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if acknowledged, false if rejected or timed out</returns>
    public async Task<bool> WaitForHandoffResponseAsync(
        string handoffId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(handoffId))
            return false;

        if (!_handoffs.TryGetValue(handoffId, out var handoff))
            return false;

        // If handoff is already completed, return immediately
        if (handoff.Status == HandoffStatus.Acknowledged)
            return true;
        if (handoff.Status == HandoffStatus.Rejected || handoff.Status == HandoffStatus.Timeout)
            return false;

        // Create a task completion source for this handoff
        var waiter = new TaskCompletionSource<bool>();
        if (!_handoffWaiters.TryAdd(handoffId, waiter))
        {
            // Another waiter already exists, wait on it instead
            if (_handoffWaiters.TryGetValue(handoffId, out var existingWaiter))
            {
                waiter = existingWaiter;
            }
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
            var completedTask = await Task.WhenAny(waiter.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout occurred
                _logger.LogWarning("Handoff {HandoffId} timed out after {Timeout}",
                    handoffId, timeout);

                // Update handoff status
                if (_handoffs.TryGetValue(handoffId, out var timeoutHandoff))
                {
                    timeoutHandoff.Status = HandoffStatus.Timeout;
                }

                // Complete the waiter with false result
                _handoffWaiters.TryRemove(handoffId, out _);
                if (!waiter.Task.IsCompleted)
                {
                    waiter.SetResult(false);
                }

                return false;
            }

            return await waiter.Task;
        }
        catch (OperationCanceledException)
        {
            // Cancellation occurred
            _logger.LogDebug("Handoff {HandoffId} wait was cancelled", handoffId);
            
            _handoffWaiters.TryRemove(handoffId, out _);
            if (!waiter.Task.IsCompleted)
            {
                waiter.SetCanceled();
            }
            
            throw;
        }
    }
}