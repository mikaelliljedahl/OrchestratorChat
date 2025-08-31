using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Components;

public partial class AgentSidebar : ComponentBase
{
    [Parameter] public List<AgentInfo> Agents { get; set; } = new();
    [Parameter] public AgentInfo? SelectedAgent { get; set; }
    [Parameter] public EventCallback<AgentInfo> OnAgentSelected { get; set; }
    [Parameter] public EventCallback OnCreateAgent { get; set; }
    
    [Inject] private IAgentService AgentService { get; set; } = null!;
    
    private AgentInfo? _defaultAgent;
    
    protected override async Task OnParametersSetAsync()
    {
        // Load default agent info when agents list changes
        if (Agents.Any())
        {
            try
            {
                _defaultAgent = await AgentService.GetDefaultAgentAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading default agent: {ex.Message}");
                _defaultAgent = null;
            }
        }
    }
    
    private async Task SelectAgent(AgentInfo agent)
    {
        if (OnAgentSelected.HasDelegate)
        {
            await OnAgentSelected.InvokeAsync(agent);
        }
    }
    
    private async Task ShowCreateAgentDialog()
    {
        Console.WriteLine("AgentSidebar.ShowCreateAgentDialog: Button clicked");
        if (OnCreateAgent.HasDelegate)
        {
            Console.WriteLine("AgentSidebar.ShowCreateAgentDialog: Invoking OnCreateAgent callback");
            await OnCreateAgent.InvokeAsync();
        }
        else
        {
            Console.WriteLine("AgentSidebar.ShowCreateAgentDialog: OnCreateAgent.HasDelegate is false!");
        }
    }
    
    private async Task SetAsDefault(AgentInfo agent, MouseEventArgs e)
    {
        // Note: StopPropagation is not available on MouseEventArgs in Blazor Server
        // Event propagation is handled differently in Blazor Server
        
        try
        {
            await AgentService.SetDefaultAgentAsync(agent.Id);
            _defaultAgent = agent;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting default agent: {ex.Message}");
        }
    }
    
    private Color GetAgentStatusColor(AgentInfo agent)
    {
        return agent.Status switch
        {
            AgentStatus.Ready => Color.Success,
            AgentStatus.Busy => Color.Warning,
            AgentStatus.Error => Color.Error,
            AgentStatus.Initializing => Color.Info,
            _ => Color.Default
        };
    }
    
    private bool IsDefaultAgent(AgentInfo agent)
    {
        return _defaultAgent?.Id == agent.Id;
    }
}