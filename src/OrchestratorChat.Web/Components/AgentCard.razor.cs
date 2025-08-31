using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Web.Components;

public partial class AgentCard : ComponentBase
{
    [Parameter] public AgentInfo Agent { get; set; } = new();
    [Parameter] public EventCallback OnClick { get; set; }
    
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    
    private Color GetStatusColor()
    {
        return Agent.Status switch
        {
            AgentStatus.Ready => Color.Success,
            AgentStatus.Busy => Color.Warning,
            AgentStatus.Error => Color.Error,
            AgentStatus.Initializing => Color.Info,
            _ => Color.Default
        };
    }
    
    private void OnChatClick()
    {
        Navigation.NavigateTo($"/chat/{Agent.Id}");
    }
}