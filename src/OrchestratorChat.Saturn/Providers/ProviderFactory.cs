using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Providers;

/// <summary>
/// Factory for creating LLM provider instances
/// </summary>
public static class ProviderFactory
{
    /// <summary>
    /// Creates and initializes an LLM provider based on the specified type and configuration
    /// </summary>
    /// <param name="providerType">The type of provider to create ("anthropic" or "openrouter")</param>
    /// <param name="config">Provider configuration containing API keys and settings</param>
    /// <returns>Initialized ILLMProvider instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    public static async Task<ILLMProvider> CreateProviderAsync(string providerType, ProviderConfiguration config)
    {
        ILLMProvider provider = providerType.ToLower() switch
        {
            "anthropic" => new AnthropicProvider(config),
            "openrouter" => new OpenRouterProvider(config),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
        
        await provider.InitializeAsync();
        return provider;
    }
}