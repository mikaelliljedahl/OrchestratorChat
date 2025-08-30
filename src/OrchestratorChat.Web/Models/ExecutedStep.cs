namespace OrchestratorChat.Web.Models;

public class ExecutedStep
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string Output { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AgentId { get; set; }
}