using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Agents.Claude;

public class ClaudeConfiguration
{
    public string ClaudeExecutablePath { get; set; } = "claude";
    public string DefaultModel { get; set; } = "claude-sonnet-4-20250514";
    public bool EnableMcp { get; set; } = true;
    public string? McpConfigPath { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    
    // Legacy property names for test compatibility
    public string ExecutablePath { get => ClaudeExecutablePath; set => ClaudeExecutablePath = value; }
    public int TimeoutSeconds { get; set; } = 30;
    public int HealthCheckIntervalMs { get; set; } = 30000;
    public int ProcessRestartDelayMs { get; set; } = 1000;
}

internal class ClaudeJsonResponse
{
    public string? Type { get; set; }
    public string? Content { get; set; }
    public bool Done { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public Usage? Usage { get; set; }
}

internal class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}