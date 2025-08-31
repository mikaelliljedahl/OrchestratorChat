using Microsoft.AspNetCore.Components;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Teams;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Components;

public partial class TeamSetup : ComponentBase
{
    [Parameter] public Team Team { get; set; } = new();
    [Parameter] public EventCallback<Team> OnTeamSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IAgentService AgentService { get; set; } = null!;

    private Team _team = new();
    private List<AgentInfo> _availableAgents = new();
    private AgentInfo? _selectedAgent;
    private string _selectedRole = "contributor";

    protected override async Task OnInitializedAsync()
    {
        _team = Team ?? new Team();
        
        // Load available agents from AgentService
        _availableAgents = await AgentService.GetConfiguredAgentsAsync();
        
        if (_availableAgents.Any())
        {
            _selectedAgent = _availableAgents.First();
        }
    }

    private void AddTeamMember()
    {
        if (_selectedAgent == null || string.IsNullOrWhiteSpace(_selectedRole))
            return;

        // Check if agent already exists in team
        if (_team.Members.Any(m => m.AgentId.Equals(_selectedAgent.Id, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"Agent {_selectedAgent.Name} is already in the team");
            return;
        }

        var member = new TeamMember
        {
            TeamId = _team.Id,
            AgentId = _selectedAgent.Id, // Use real agent ID
            AgentType = _selectedAgent.Type,
            AgentName = _selectedAgent.Name,
            Role = _selectedRole,
            JoinedAt = DateTime.UtcNow
        };

        _team.Members.Add(member);
        Console.WriteLine($"Added {member.AgentName} to team. Team now has {_team.Members.Count} members");
        
        // Reset form - find an agent that's not already in the team
        var availableForSelection = _availableAgents
            .Where(a => !_team.Members.Any(m => m.AgentId.Equals(a.Id, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();
            
        _selectedAgent = availableForSelection;
        _selectedRole = "contributor";
        
        StateHasChanged();
    }

    private void RemoveTeamMember(TeamMember member)
    {
        _team.Members.Remove(member);
        StateHasChanged();
    }

    private async Task SaveTeam()
    {
        if (_team.Members.Any())
        {
            _team.UpdatedAt = DateTime.UtcNow;
            await OnTeamSaved.InvokeAsync(_team);
        }
    }
}