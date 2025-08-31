using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Providers;

/// <summary>
/// Common interface for LLM client implementations
/// </summary>
public interface ILLMClient : IDisposable
{
    /// <summary>
    /// Sends a message and returns a complete response
    /// </summary>
    Task<string> SendMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a message response as it's generated
    /// </summary>
    IAsyncEnumerable<string> StreamMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available models for this client
    /// </summary>
    Task<List<LLMModelInfo>> GetAvailableModelsAsync();

    /// <summary>
    /// Checks if the client is properly authenticated and ready to use
    /// </summary>
    Task<bool> IsAuthenticatedAsync();
}

/// <summary>
/// Model information for available models from LLM clients
/// </summary>
public class LLMModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}