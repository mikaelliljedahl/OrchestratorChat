using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Pages;

public partial class Chat : ComponentBase
{
    [Inject] private IAgentService AgentService { get; set; } = null!;
    [Inject] private ISessionManager SessionManager { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<Chat> Logger { get; set; } = null!;
    
    private List<SessionSummary> _recentSessions = new();
    private List<AgentInfo> _agents = new();
    private AgentInfo? _defaultAgent;
    private bool _loading = true;
    private bool _showCreateAgentDialog = false;
    
    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Chat landing page initializing");
        
        try
        {
            // Load data in parallel for better performance
            var loadSessionsTask = LoadRecentSessions();
            var loadAgentsTask = LoadAgents();
            
            await Task.WhenAll(loadSessionsTask, loadAgentsTask);
            
            // Get default agent
            try
            {
                _defaultAgent = await AgentService.GetDefaultAgentAsync();
                Logger.LogInformation("Default agent: {AgentName}", _defaultAgent?.Name ?? "None");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get default agent");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing chat landing page");
        }
        finally
        {
            _loading = false;
        }
    }
    
    private async Task LoadRecentSessions()
    {
        try
        {
            Logger.LogDebug("Loading recent sessions");
            var recentSessions = await SessionManager.GetRecentSessionsAsync(20);
            
            // Convert to SessionSummary and get the 5 most recent active or paused sessions
            _recentSessions = recentSessions
                .Select(s => new SessionSummary 
                { 
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status,
                    LastActivity = s.LastActivityAt,
                    MessageCount = s.Messages?.Count ?? 0,
                    // ParticipantCount removed from model - using ParticipantNames instead
                    // ParticipantCount = s.ParticipantAgentIds?.Count ?? 0
                })
                .Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused)
                .OrderByDescending(s => s.LastActivity)
                .Take(5)
                .ToList();
                
            Logger.LogInformation("Loaded {Count} recent sessions", _recentSessions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading recent sessions");
            _recentSessions = new List<SessionSummary>();
        }
    }
    
    private async Task LoadAgents()
    {
        try
        {
            Logger.LogDebug("Loading configured agents");
            _agents = await AgentService.GetConfiguredAgentsAsync();
            Logger.LogInformation("Loaded {Count} agents", _agents.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading agents");
            _agents = new List<AgentInfo>();
        }
    }
    
    private void ContinueSession(string sessionId)
    {
        Logger.LogInformation("Continuing session {SessionId}", sessionId);
        Navigation.NavigateTo($"/session/{sessionId}", forceLoad: true);
    }
    
    private void StartChatWithAgent(string agentId)
    {
        Logger.LogInformation("Starting chat with agent {AgentId}", agentId);
        Navigation.NavigateTo($"/chat/{agentId}", forceLoad: true);
    }
    
    private void ViewAllSessions()
    {
        Logger.LogInformation("Navigating to sessions page");
        Navigation.NavigateTo("/sessions");
    }
    
    private void CreateNewAgent()
    {
        Logger.LogInformation("Showing create agent dialog");
        _showCreateAgentDialog = true;
        StateHasChanged();
    }
    
    private void StartOrchestrationSession()
    {
        Logger.LogInformation("Starting new orchestration session");
        Navigation.NavigateTo("/orchestrator");
    }
    
    private void StartTeamSession()
    {
        Logger.LogInformation("Starting new team session");
        // TODO: Implement team session creation
        Navigation.NavigateTo("/orchestrator");
    }
    
    private void QuickChatWithDefault()
    {
        if (_defaultAgent != null)
        {
            Logger.LogInformation("Starting quick chat with default agent {AgentId}", _defaultAgent.Id);
            Navigation.NavigateTo($"/chat/{_defaultAgent.Id}");
        }
    }
    
    private async Task OnCreateAgentDialogVisibilityChanged(bool isVisible)
    {
        _showCreateAgentDialog = isVisible;
        
        if (!isVisible)
        {
            // Reload agents when dialog closes
            await LoadAgents();
            
            // Refresh default agent
            try
            {
                _defaultAgent = await AgentService.GetDefaultAgentAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to refresh default agent");
            }
            
            StateHasChanged();
        }
    }
    
    private async Task OnAgentCreated(AgentConfiguration configuration)
    {
        try
        {
            Logger.LogInformation("Creating new agent: {AgentName}", configuration.Name);
            var newAgent = await AgentService.CreateAgentAsync(configuration.Type, configuration);
            
            // Set as default if no default exists
            if (_defaultAgent == null)
            {
                await AgentService.SetDefaultAgentAsync(newAgent.Id);
                Logger.LogInformation("Set new agent {AgentId} as default", newAgent.Id);
            }
            
            // Navigate to chat with the new agent
            Logger.LogInformation("Navigating to chat with new agent {AgentId}", newAgent.Id);
            Navigation.NavigateTo($"/chat/{newAgent.Id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating agent");
            // TODO: Show error message to user
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
    
    private Color GetAgentColor(AgentType type)
    {
        return type switch
        {
            AgentType.Claude => Color.Primary,
            AgentType.Saturn => Color.Secondary,
            AgentType.Custom => Color.Tertiary,
            _ => Color.Default
        };
    }
    
    private Color GetAgentStatusColor(AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Ready => Color.Success,
            AgentStatus.Busy => Color.Warning,
            AgentStatus.Processing => Color.Warning,
            AgentStatus.Error => Color.Error,
            AgentStatus.Initializing => Color.Info,
            AgentStatus.Shutdown => Color.Default,
            AgentStatus.Uninitialized => Color.Secondary,
            AgentStatus.Unknown => Color.Default,
            _ => Color.Default
        };
    }
    
    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }
}