using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Timeline;
using OrchestratorChat.Web.Services;
using System.Collections.Concurrent;

namespace OrchestratorChat.Web.Components;

public partial class SessionTimeline : ComponentBase, IDisposable
{
    [Parameter] public string? SessionId { get; set; }
    [Parameter] public bool AutoRefreshEnabled { get; set; } = true;
    [Parameter] public int PageSize { get; set; } = 25;

    [Inject] private ITimelineService TimelineService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<ITimelineEvent> TimelineEvents { get; set; } = new();
    private Dictionary<TimelineEventType, int> EventCounts { get; set; } = new();
    private IReadOnlyCollection<object> SelectedEventTypes { get; set; } = new List<object>();
    private HashSet<string> ExpandedEventIds { get; set; } = new();
    
    private bool IsLoading { get; set; } = true;
    private bool IsLoadingMore { get; set; } = false;
    private bool HasMoreEvents { get; set; } = false;
    private int CurrentOffset { get; set; } = 0;
    
    private IDisposable? _timelineSubscription;
    private readonly ConcurrentQueue<ITimelineEvent> _pendingEvents = new();
    private Timer? _updateTimer;

    protected override async Task OnInitializedAsync()
    {
        // Initialize all event types as selected by default
        SelectedEventTypes = new List<object>
        {
            TimelineEventType.Message,
            TimelineEventType.Decision,
            TimelineEventType.Handoff,
            TimelineEventType.Approval,
            TimelineEventType.StepExecution,
            TimelineEventType.ToolExecution
        };

        await LoadTimelineDataAsync();
        
        if (AutoRefreshEnabled && !string.IsNullOrEmpty(SessionId))
        {
            await SubscribeToUpdatesAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(SessionId))
        {
            await LoadTimelineDataAsync();
            
            if (AutoRefreshEnabled)
            {
                await SubscribeToUpdatesAsync();
            }
            else
            {
                UnsubscribeFromUpdates();
            }
        }
    }

    private async Task LoadTimelineDataAsync()
    {
        if (string.IsNullOrEmpty(SessionId))
            return;

        try
        {
            IsLoading = true;
            CurrentOffset = 0;
            
            // Load event counts
            EventCounts = await TimelineService.GetEventCountsAsync(SessionId);
            
            // Load initial events
            var eventTypes = SelectedEventTypes.Cast<TimelineEventType>();
            var events = await TimelineService.GetSessionEventsAsync(
                SessionId, 
                eventTypes, 
                pageSize: PageSize, 
                offset: CurrentOffset);
            
            TimelineEvents = events;
            CurrentOffset = events.Count;
            HasMoreEvents = events.Count == PageSize;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading timeline: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreEventsAsync()
    {
        if (string.IsNullOrEmpty(SessionId) || IsLoadingMore)
            return;

        try
        {
            IsLoadingMore = true;
            
            var eventTypes = SelectedEventTypes.Cast<TimelineEventType>();
            var moreEvents = await TimelineService.GetSessionEventsAsync(
                SessionId, 
                eventTypes, 
                pageSize: PageSize, 
                offset: CurrentOffset);
            
            TimelineEvents.AddRange(moreEvents);
            CurrentOffset += moreEvents.Count;
            HasMoreEvents = moreEvents.Count == PageSize;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading more events: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoadingMore = false;
            StateHasChanged();
        }
    }

    private async Task SubscribeToUpdatesAsync()
    {
        if (string.IsNullOrEmpty(SessionId) || _timelineSubscription != null)
            return;

        try
        {
            _timelineSubscription = await TimelineService.SubscribeToUpdatesAsync(
                SessionId, 
                OnTimelineEventReceived);
            
            // Start timer to process pending events periodically
            _updateTimer = new Timer(ProcessPendingEvents, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error subscribing to timeline updates: {ex.Message}", Severity.Warning);
        }
    }

    private void UnsubscribeFromUpdates()
    {
        _timelineSubscription?.Dispose();
        _timelineSubscription = null;
        
        _updateTimer?.Dispose();
        _updateTimer = null;
    }

    private void OnTimelineEventReceived(ITimelineEvent timelineEvent)
    {
        // Queue event for processing to avoid too many UI updates
        _pendingEvents.Enqueue(timelineEvent);
    }

    private void ProcessPendingEvents(object? state)
    {
        var hasNewEvents = false;
        var eventTypesToShow = SelectedEventTypes.Cast<TimelineEventType>().ToHashSet();

        while (_pendingEvents.TryDequeue(out var pendingEvent))
        {
            // Only add events that match the current filter
            if (eventTypesToShow.Contains(pendingEvent.EventType))
            {
                // Insert in chronological order
                var insertIndex = TimelineEvents.FindIndex(e => e.Timestamp > pendingEvent.Timestamp);
                if (insertIndex >= 0)
                {
                    TimelineEvents.Insert(insertIndex, pendingEvent);
                }
                else
                {
                    TimelineEvents.Add(pendingEvent);
                }
                
                hasNewEvents = true;
            }
            
            // Update event counts
            if (EventCounts.ContainsKey(pendingEvent.EventType))
            {
                EventCounts[pendingEvent.EventType]++;
            }
            else
            {
                EventCounts[pendingEvent.EventType] = 1;
            }
        }

        if (hasNewEvents)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleEventExpansion(string eventId)
    {
        if (ExpandedEventIds.Contains(eventId))
        {
            ExpandedEventIds.Remove(eventId);
        }
        else
        {
            ExpandedEventIds.Add(eventId);
        }
        
        StateHasChanged();
    }

    private static string GetEventIcon(TimelineEventType eventType)
    {
        return eventType switch
        {
            TimelineEventType.Message => Icons.Material.Filled.Message,
            TimelineEventType.Decision => Icons.Material.Filled.HowToVote,
            TimelineEventType.Handoff => Icons.Material.Filled.SwapHoriz,
            TimelineEventType.Approval => Icons.Material.Filled.CheckCircle,
            TimelineEventType.StepExecution => Icons.Material.Filled.PlayCircle,
            TimelineEventType.ToolExecution => Icons.Material.Filled.Build,
            _ => Icons.Material.Filled.Timeline
        };
    }

    private static Color GetEventColor(TimelineEventType eventType)
    {
        return eventType switch
        {
            TimelineEventType.Message => Color.Primary,
            TimelineEventType.Decision => Color.Secondary,
            TimelineEventType.Handoff => Color.Tertiary,
            TimelineEventType.Approval => Color.Success,
            TimelineEventType.StepExecution => Color.Warning,
            TimelineEventType.ToolExecution => Color.Info,
            _ => Color.Default
        };
    }

    private static string GetTruncatedDescription(string description)
    {
        const int maxLength = 150;
        if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
        {
            return description ?? string.Empty;
        }
        
        return description.Substring(0, maxLength) + "...";
    }

    public void Dispose()
    {
        UnsubscribeFromUpdates();
    }
}