using Microsoft.AspNetCore.Components;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Web.Models;

namespace OrchestratorChat.Web.Components;

public partial class MessageBubble : ComponentBase
{
    [Parameter] public ChatMessage Message { get; set; } = new();
    [CascadingParameter] public Session? CurrentSession { get; set; }
    
    private string GetMessageClass()
    {
        return Message.Role == MessageRole.User ? "user-message" : "agent-message";
    }
    
    private string GetSenderName()
    {
        if (Message.Role == MessageRole.User)
            return "You";
            
        var agentId = CurrentSession?.ParticipantAgentIds?.FirstOrDefault(id => id == Message.AgentId);
        return agentId ?? "Agent";
    }
}