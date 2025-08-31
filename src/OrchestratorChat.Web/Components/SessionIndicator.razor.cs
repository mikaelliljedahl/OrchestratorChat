using Microsoft.AspNetCore.Components;
using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.Web.Components;

public partial class SessionIndicator : ComponentBase, IDisposable
{
    [Inject] private ISessionManager SessionManager { get; set; } = null!;
    
    private Session? _currentSession;
    private int _participantCount = 0;
    private Timer? _updateTimer;

    protected override async Task OnInitializedAsync()
    {
        await UpdateSession();
        _updateTimer = new Timer(async _ => await InvokeAsync(UpdateSession), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task UpdateSession()
    {
        try
        {
            _currentSession = await SessionManager.GetCurrentSessionAsync();
            _participantCount = _currentSession?.ParticipantAgentIds?.Count ?? 0;
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Handle errors silently for the indicator
        }
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}