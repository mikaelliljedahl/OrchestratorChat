namespace OrchestratorChat.Data.Models;

public class SessionStatistics
{
    public string SessionId { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int AgentCount { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalTokensUsed { get; set; }
    public int ToolCallCount { get; set; }
}