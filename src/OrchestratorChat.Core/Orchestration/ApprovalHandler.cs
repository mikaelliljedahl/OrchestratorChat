using OrchestratorChat.Core.Approvals;
using OrchestratorChat.Core.Events;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Implementation of approval handler for orchestration system
/// </summary>
public class ApprovalHandler : IApprovalHandler
{
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, Approval> _approvals = new();
    private readonly Dictionary<string, TaskCompletionSource<bool>> _approvalTasks = new();
    private readonly object _lock = new object();

    /// <summary>
    /// Creates a new instance of ApprovalHandler
    /// </summary>
    /// <param name="eventBus">Event bus for publishing approval events</param>
    public ApprovalHandler(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <inheritdoc />
    public Task<Approval> RequestApprovalAsync(
        string sessionId,
        string stepId,
        string requestedBy,
        ApprovalType requestType,
        string details,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Step ID cannot be null or empty", nameof(stepId));
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentException("Requested by cannot be null or empty", nameof(requestedBy));
        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("Details cannot be null or empty", nameof(details));

        var approval = new Approval
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            StepId = stepId,
            RequestedBy = requestedBy,
            RequestType = requestType,
            Details = details,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _approvals[approval.Id] = approval;
            _approvalTasks[approval.Id] = new TaskCompletionSource<bool>();
        }

        // Schedule timeout
        _ = Task.Delay(timeout, cancellationToken).ContinueWith(async _ =>
        {
            await HandleTimeoutAsync(approval.Id);
        }, cancellationToken);

        // Publish approval requested event
        _ = _eventBus.PublishAsync(new ApprovalRequestedEvent
        {
            ApprovalId = approval.Id,
            SessionId = sessionId,
            StepId = stepId,
            RequestedBy = requestedBy,
            RequestType = requestType,
            Details = details,
            RequestedAt = approval.RequestedAt
        });

        return Task.FromResult(approval);
    }

    /// <inheritdoc />
    public Task<Approval> ApproveAsync(
        string approvalId,
        string approvedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId))
            throw new ArgumentException("Approval ID cannot be null or empty", nameof(approvalId));
        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("Approved by cannot be null or empty", nameof(approvedBy));

        Approval? approval = null;
        lock (_lock)
        {
            if (!_approvals.TryGetValue(approvalId, out approval))
                throw new InvalidOperationException($"Approval with ID {approvalId} not found");

            if (approval.Status != ApprovalStatus.Pending)
                throw new InvalidOperationException($"Approval {approvalId} is not pending");

            approval.Status = ApprovalStatus.Approved;
            approval.ApprovedBy = approvedBy;
            approval.Reason = reason;
            approval.RespondedAt = DateTime.UtcNow;

            if (_approvalTasks.TryGetValue(approvalId, out var tcs))
            {
                tcs.SetResult(true);
                _approvalTasks.Remove(approvalId);
            }
        }

        // Publish approval granted event
        _ = _eventBus.PublishAsync(new ApprovalGrantedEvent
        {
            ApprovalId = approvalId,
            SessionId = approval.SessionId,
            StepId = approval.StepId,
            ApprovedBy = approvedBy,
            Reason = reason,
            ApprovedAt = approval.RespondedAt.Value
        });

        return Task.FromResult(approval);
    }

    /// <inheritdoc />
    public Task<Approval> RejectAsync(
        string approvalId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId))
            throw new ArgumentException("Approval ID cannot be null or empty", nameof(approvalId));
        if (string.IsNullOrWhiteSpace(rejectedBy))
            throw new ArgumentException("Rejected by cannot be null or empty", nameof(rejectedBy));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        Approval? approval = null;
        lock (_lock)
        {
            if (!_approvals.TryGetValue(approvalId, out approval))
                throw new InvalidOperationException($"Approval with ID {approvalId} not found");

            if (approval.Status != ApprovalStatus.Pending)
                throw new InvalidOperationException($"Approval {approvalId} is not pending");

            approval.Status = ApprovalStatus.Rejected;
            approval.RejectedBy = rejectedBy;
            approval.Reason = reason;
            approval.RespondedAt = DateTime.UtcNow;

            if (_approvalTasks.TryGetValue(approvalId, out var tcs))
            {
                tcs.SetResult(false);
                _approvalTasks.Remove(approvalId);
            }
        }

        // Publish approval rejected event
        _ = _eventBus.PublishAsync(new ApprovalRejectedEvent
        {
            ApprovalId = approvalId,
            SessionId = approval.SessionId,
            StepId = approval.StepId,
            RejectedBy = rejectedBy,
            Reason = reason,
            RejectedAt = approval.RespondedAt.Value
        });

        return Task.FromResult(approval);
    }

    /// <inheritdoc />
    public Task<Approval?> GetApprovalAsync(string approvalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId))
            throw new ArgumentException("Approval ID cannot be null or empty", nameof(approvalId));

        lock (_lock)
        {
            _approvals.TryGetValue(approvalId, out var approval);
            return Task.FromResult(approval);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Approval>> GetPendingApprovalsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        lock (_lock)
        {
            var pendingApprovals = _approvals.Values
                .Where(a => a.SessionId == sessionId && a.Status == ApprovalStatus.Pending)
                .ToList();
            return Task.FromResult<IReadOnlyList<Approval>>(pendingApprovals);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Approval>> GetSessionApprovalsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        lock (_lock)
        {
            var sessionApprovals = _approvals.Values
                .Where(a => a.SessionId == sessionId)
                .ToList();
            return Task.FromResult<IReadOnlyList<Approval>>(sessionApprovals);
        }
    }

    /// <inheritdoc />
    public async Task<bool> WaitForApprovalAsync(string approvalId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId))
            throw new ArgumentException("Approval ID cannot be null or empty", nameof(approvalId));

        TaskCompletionSource<bool>? tcs;
        lock (_lock)
        {
            if (!_approvals.TryGetValue(approvalId, out var approval))
                throw new InvalidOperationException($"Approval with ID {approvalId} not found");

            // If already resolved, return immediately
            if (approval.Status != ApprovalStatus.Pending)
                return approval.Status == ApprovalStatus.Approved;

            _approvalTasks.TryGetValue(approvalId, out tcs);
        }

        if (tcs == null)
            return false;

        try
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            return await tcs.Task.WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Handle timeout
            await HandleTimeoutAsync(approvalId);
            return false;
        }
    }

    private async Task HandleTimeoutAsync(string approvalId)
    {
        Approval? approval;
        TaskCompletionSource<bool>? tcs;
        
        lock (_lock)
        {
            if (!_approvals.TryGetValue(approvalId, out approval) || approval.Status != ApprovalStatus.Pending)
                return;

            approval.Status = ApprovalStatus.Timeout;
            approval.RespondedAt = DateTime.UtcNow;

            _approvalTasks.TryGetValue(approvalId, out tcs);
            if (tcs != null)
            {
                _approvalTasks.Remove(approvalId);
            }
        }

        tcs?.SetResult(false);

        // Publish timeout event
        await _eventBus.PublishAsync(new ApprovalRejectedEvent
        {
            ApprovalId = approvalId,
            SessionId = approval.SessionId,
            StepId = approval.StepId,
            RejectedBy = "System",
            Reason = "Approval request timed out",
            RejectedAt = approval.RespondedAt.Value
        });
    }
}

