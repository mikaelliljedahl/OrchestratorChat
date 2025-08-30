namespace OrchestratorChat.Agents.Saturn;

public class SaturnConfiguration
{
    public List<string> SupportedModels { get; set; } = new()
    {
        "claude-opus-4-1-20250805",
        "claude-opus-4",
        "claude-sonnet-4-20250514",
        "claude-3.7-sonnet",
        "claude-3.5-haiku",
        "anthropic/claude-3.5-sonnet",
        "openai/gpt-4o",
        "google/gemini-pro-1.5",
        "meta-llama/llama-3.1-70b-instruct",
        "deepseek/deepseek-chat"
    };

    public string DefaultProvider { get; set; } = "OpenRouter";
    public Dictionary<string, string> ProviderSettings { get; set; } = new();
    public bool EnableMultiAgent { get; set; } = true;
    public int MaxSubAgents { get; set; } = 5;
    
    // Properties for test compatibility
    public bool EnableToolExecution { get; set; } = true;
    public int HealthCheckIntervalMs { get; set; } = 30000;
}