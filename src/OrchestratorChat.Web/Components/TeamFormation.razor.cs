using Microsoft.AspNetCore.Components;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Teams;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Components;

public partial class TeamFormation : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public string SessionId { get; set; } = string.Empty;
    [Parameter] public EventCallback<Team> OnTeamCreated { get; set; }

    [Inject] private ITeamService TeamService { get; set; } = null!;

    private bool _isLoading = false;
    private bool _isSaving = false;
    private int _selectedTeamSize = 3;
    private List<AgentInfo> _availableAgents = new();
    private List<TeamMember> _teamMembers = new();
    private List<string> _validationErrors = new();

    private readonly Dictionary<string, string> _availableRoles = new()
    {
        { "Orchestrator", "Coordinates team activities and manages workflow" },
        { "Coder", "Implements code solutions and technical tasks" },
        { "Reviewer", "Reviews code and provides quality assurance" },
        { "Researcher", "Gathers information and analyzes requirements" },
        { "Planner", "Creates project plans and architectural decisions" }
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadAvailableAgentsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && !_availableAgents.Any())
        {
            await LoadAvailableAgentsAsync();
        }
    }

    private async Task LoadAvailableAgentsAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _availableAgents = await TeamService.GetAvailableAgentsAsync();
        }
        catch (Exception ex)
        {
            // Handle error - could emit an event or set an error state
            _validationErrors.Add($"Failed to load agents: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void AddToTeam(AgentInfo agent)
    {
        if (_teamMembers.Count >= _selectedTeamSize)
            return;

        var member = new TeamMember
        {
            TeamId = Guid.Empty, // Will be set when team is created
            AgentId = agent.Id,
            AgentType = agent.Type,
            AgentName = agent.Name,
            Role = string.Empty, // User will select role
            JoinedAt = DateTime.UtcNow
        };

        _teamMembers.Add(member);
        ValidateTeam();
        StateHasChanged();
    }

    private void RemoveFromTeam(string agentId)
    {
        var member = _teamMembers.FirstOrDefault(m => m.AgentId == agentId);
        if (member != null)
        {
            _teamMembers.Remove(member);
            ValidateTeam();
            StateHasChanged();
        }
    }

    private void ValidateTeam()
    {
        _validationErrors.Clear();

        if (_teamMembers.Count < 3)
        {
            _validationErrors.Add("Team must have at least 3 members");
        }

        if (_teamMembers.Count > _selectedTeamSize)
        {
            _validationErrors.Add($"Team has too many members (max: {_selectedTeamSize})");
        }

        // Check for required roles
        var assignedRoles = _teamMembers.Where(m => !string.IsNullOrEmpty(m.Role)).Select(m => m.Role).ToHashSet();
        
        if (!assignedRoles.Contains("Orchestrator"))
        {
            _validationErrors.Add("Team must have an Orchestrator");
        }

        var membersWithoutRoles = _teamMembers.Where(m => string.IsNullOrEmpty(m.Role)).ToList();
        if (membersWithoutRoles.Any())
        {
            _validationErrors.Add($"{membersWithoutRoles.Count} member(s) need role assignment");
        }

        // Check for duplicate roles (except Coder which can have multiple)
        var roleGroups = assignedRoles.Where(r => r != "Coder").GroupBy(r => r);
        foreach (var roleGroup in roleGroups)
        {
            var membersWithRole = _teamMembers.Count(m => m.Role == roleGroup.Key);
            if (membersWithRole > 1)
            {
                _validationErrors.Add($"Only one {roleGroup.Key} is allowed per team");
            }
        }
    }

    private bool IsTeamValid()
    {
        ValidateTeam();
        return !_validationErrors.Any();
    }

    private async Task CloseDialog(bool saveTeam)
    {
        if (saveTeam && IsTeamValid())
        {
            await SaveTeamAsync();
        }

        IsVisible = false;
        if (IsVisibleChanged.HasDelegate)
        {
            await IsVisibleChanged.InvokeAsync(false);
        }
    }

    private async Task SaveTeamAsync()
    {
        if (string.IsNullOrEmpty(SessionId))
        {
            _validationErrors.Add("Session ID is required to create a team");
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            var team = await TeamService.CreateTeamAsync(SessionId, _teamMembers);
            
            if (OnTeamCreated.HasDelegate)
            {
                await OnTeamCreated.InvokeAsync(team);
            }

            // Reset form state
            _teamMembers.Clear();
            _selectedTeamSize = 3;
            _validationErrors.Clear();
        }
        catch (Exception ex)
        {
            _validationErrors.Add($"Failed to create team: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
}