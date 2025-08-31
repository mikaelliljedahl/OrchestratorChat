using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Components;

public partial class SessionHistory : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<Session> OnSessionSelected { get; set; }
    
    [Inject] private ISessionManager SessionManager { get; set; } = null!;
    
    private List<SessionSummary> _sessions = new();
    private List<SessionSummary> _filteredSessions = new();
    private string _searchQuery = string.Empty;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadSessions();
    }
    
    protected override void OnParametersSet()
    {
        FilterSessions();
    }
    
    private async Task LoadSessions()
    {
        try
        {
            var sessions = await SessionManager.GetRecentSessionsAsync(50);
            _sessions = sessions.Select(s => new SessionSummary
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                CreatedAt = s.CreatedAt,
                LastActivity = s.LastActivity,
                MessageCount = s.Messages.Count,
                LastMessage = s.Messages.LastOrDefault()?.Content?.Substring(0, Math.Min(50, s.Messages.LastOrDefault()?.Content?.Length ?? 0)),
                ParticipantNames = s.ParticipantAgents?.Select(a => a.Name).ToList() ?? new(),
                Status = s.Status
            }).ToList();
            
            FilterSessions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sessions: {ex.Message}");
        }
    }
    
    private void FilterSessions()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _filteredSessions = _sessions.ToList();
        }
        else
        {
            _filteredSessions = _sessions.Where(s => 
                s.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                s.LastMessage?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                s.ParticipantNames.Any(p => p.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }
    
    private async Task LoadSession(SessionSummary sessionSummary)
    {
        try
        {
            var session = await SessionManager.GetSessionAsync(sessionSummary.Id);
            if (session != null && OnSessionSelected.HasDelegate)
            {
                await OnSessionSelected.InvokeAsync(session);
            }
            await Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading session: {ex.Message}");
        }
    }
    
    private async Task Close()
    {
        IsOpen = false;
        if (IsOpenChanged.HasDelegate)
        {
            await IsOpenChanged.InvokeAsync(false);
        }
    }
    
    private Color GetStatusColor(SessionStatus status)
    {
        return status switch
        {
            SessionStatus.Active => Color.Success,
            SessionStatus.Paused => Color.Warning,
            SessionStatus.Completed => Color.Info,
            SessionStatus.Cancelled => Color.Error,
            _ => Color.Default
        };
    }
}