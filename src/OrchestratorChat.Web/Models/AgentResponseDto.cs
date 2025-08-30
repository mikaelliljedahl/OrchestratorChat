using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Web.Models;

public class AgentResponseDto
{
    public string AgentId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public AgentResponse Response { get; set; } = new();
    public DateTime Timestamp { get; set; }
}