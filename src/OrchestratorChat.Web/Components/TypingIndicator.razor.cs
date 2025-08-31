using Microsoft.AspNetCore.Components;

namespace OrchestratorChat.Web.Components;

public partial class TypingIndicator : ComponentBase
{
    [Parameter] public string AgentName { get; set; } = "Agent";
}