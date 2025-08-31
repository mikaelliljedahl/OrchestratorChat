using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using Microsoft.Extensions.Logging;

namespace OrchestratorChat.Saturn.Providers;

/// <summary>
/// Factory for creating LLM provider instances
/// </summary>
public class ProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of ProviderFactory
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating loggers</param>
    public ProviderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates and initializes an LLM provider based on the specified type and configuration
    /// </summary>
    /// <param name="providerType">The type of provider to create ("anthropic" or "openrouter")</param>
    /// <param name="config">Provider configuration containing API keys and settings</param>
    /// <returns>Initialized ILLMProvider instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    public async Task<ILLMProvider> CreateProviderAsync(string providerType, ProviderConfiguration config)
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

    /// <summary>
    /// Creates an LLM client based on provider type and settings
    /// </summary>
    /// <param name="providerType">The type of provider ("anthropic" or "openrouter")</param>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>ILLMClient instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    public ILLMClient CreateClient(string providerType, Dictionary<string, object>? settings = null)
    {
        return providerType.ToLower() switch
        {
            "anthropic" => CreateAnthropicClient(settings),
            "openrouter" => CreateOpenRouterClient(settings ?? new Dictionary<string, object>()),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
    }

    /// <summary>
    /// Creates an Anthropic client with the specified settings
    /// </summary>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>AnthropicClient instance</returns>
    public AnthropicClient CreateAnthropicClient(Dictionary<string, object>? settings = null)
    {
        return new AnthropicClient();
    }

    /// <summary>
    /// Creates an OpenRouter client with the specified settings
    /// </summary>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>OpenRouterClient instance</returns>
    public OpenRouterClient CreateOpenRouterClient(Dictionary<string, object> settings)
    {
        var apiKey = settings.GetValueOrDefault("ApiKey")?.ToString() ??
                    Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ??
                    string.Empty;

        var options = new OpenRouterOptions
        {
            ApiKey = apiKey
        };

        var logger = _loggerFactory.CreateLogger<OpenRouterClient>();
        return new OpenRouterClient(options, logger, _loggerFactory);
    }
}