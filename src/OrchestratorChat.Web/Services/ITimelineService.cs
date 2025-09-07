using OrchestratorChat.Core.Timeline;

namespace OrchestratorChat.Web.Services;

/// <summary>
/// Service for managing session timeline events and real-time updates
/// </summary>
public interface ITimelineService
{
    /// <summary>
    /// Gets all timeline events for a specific session in chronological order
    /// </summary>
    /// <param name="sessionId">The session ID to get events for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of timeline events ordered by timestamp</returns>
    Task<List<ITimelineEvent>> GetSessionEventsAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets timeline events for a session with optional filtering and pagination
    /// </summary>
    /// <param name="sessionId">The session ID to get events for</param>
    /// <param name="eventTypes">Optional filter for specific event types</param>
    /// <param name="fromTimestamp">Optional start timestamp filter</param>
    /// <param name="toTimestamp">Optional end timestamp filter</param>
    /// <param name="pageSize">Number of events to return (default: 50)</param>
    /// <param name="offset">Number of events to skip (default: 0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of filtered timeline events</returns>
    Task<List<ITimelineEvent>> GetSessionEventsAsync(
        string sessionId,
        IEnumerable<TimelineEventType>? eventTypes = null,
        DateTime? fromTimestamp = null,
        DateTime? toTimestamp = null,
        int pageSize = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribes to real-time timeline event updates for a session
    /// </summary>
    /// <param name="sessionId">The session ID to subscribe to</param>
    /// <param name="onEventReceived">Callback invoked when new timeline events are received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription handle for managing the subscription</returns>
    Task<IDisposable> SubscribeToUpdatesAsync(
        string sessionId,
        Action<ITimelineEvent> onEventReceived,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of events by type for a session (for displaying badges/stats)
    /// </summary>
    /// <param name="sessionId">The session ID to get counts for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of event type to count</returns>
    Task<Dictionary<TimelineEventType, int>> GetEventCountsAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the most recent timeline event for a session
    /// </summary>
    /// <param name="sessionId">The session ID to get the latest event for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The most recent timeline event, or null if no events exist</returns>
    Task<ITimelineEvent?> GetLatestEventAsync(string sessionId, CancellationToken cancellationToken = default);
}