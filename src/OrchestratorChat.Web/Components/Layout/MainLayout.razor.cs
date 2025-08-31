using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ISessionManager SessionManager { get; set; } = null!;
    [Inject] private IAgentService AgentService { get; set; } = null!;
    
    private bool _drawerOpen = true;
    private MudTheme _theme = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Darken2,
            Secondary = Colors.Green.Accent4,
            AppbarBackground = Colors.Blue.Darken3,
        }
    };
    
    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
    
    void OpenSettings()
    {
        Navigation.NavigateTo("/settings");
    }
    
    private async Task HandleNewChatClick()
    {
        try
        {
            var defaultAgent = await AgentService.GetDefaultAgentAsync();
            if (defaultAgent != null)
            {
                // Navigate directly to chat with default agent
                Navigation.NavigateTo($"/chat/{defaultAgent.Id}");
            }
            else
            {
                // No default agent - navigate to dashboard which will show first-run wizard
                Navigation.NavigateTo("/dashboard");
            }
        }
        catch (Exception)
        {
            // Fallback to dashboard on any error
            Navigation.NavigateTo("/dashboard");
        }
    }
}