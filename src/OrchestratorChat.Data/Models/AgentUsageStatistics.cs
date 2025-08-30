namespace OrchestratorChat.Data.Models;

public class AgentUsageStatistics
{
    public string AgentId { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int SessionCount { get; set; }
    public long TotalTokensUsed { get; set; }
    public int ToolCallCount { get; set; }
    public TimeSpan? Period { get; set; }
}