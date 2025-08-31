namespace OrchestratorChat.Saturn.Providers.OpenAI;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string DefaultModel { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 120;
}

