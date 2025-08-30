using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.Web.Models;

public class SessionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessage { get; set; }
    public List<string> ParticipantNames { get; set; } = new();
    public SessionStatus Status { get; set; }
}