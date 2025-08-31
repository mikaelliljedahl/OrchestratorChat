using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Providers.OpenRouter;
// using OrchestratorChat.Saturn.Providers.OpenAI; // OpenAIProvider not yet implemented
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
    /// <param name="providerType">The type of provider to create ("anthropic", "openrouter", or "openai")</param>
    /// <param name="config">Provider configuration containing API keys and settings</param>
    /// <returns>Initialized ILLMProvider instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    public async Task<ILLMProvider> CreateProviderAsync(string providerType, ProviderConfiguration config)
    {
        ILLMProvider provider = providerType.ToLower() switch
        {
            "anthropic" => new AnthropicProvider(config),
            "openrouter" => new OpenRouterProvider(config),
            // "openai" => new OpenAIProvider(config), // OpenAIProvider not yet implemented
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };

        await provider.InitializeAsync();
        return provider;
    }

    /// <summary>
    /// Creates an LLM client based on provider settings
    /// </summary>
    /// <param name="settings">Strongly-typed provider settings</param>
    /// <returns>ILLMClient instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
    public ILLMClient CreateClient(ProviderSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        return settings switch
        {
            AnthropicProviderSettings anthropicSettings => CreateAnthropicClient(anthropicSettings),
            OpenRouterProviderSettings openRouterSettings => CreateOpenRouterClient(openRouterSettings),
            // OpenAIProviderSettings openAISettings => CreateOpenAIClient(openAISettings), // OpenAI client not yet implemented
            _ => throw new NotSupportedException($"Provider type '{settings.ProviderType}' is not supported")
        };
    }

    /// <summary>
    /// Creates an LLM client based on provider type and settings (legacy method for backward compatibility)
    /// </summary>
    /// <param name="providerType">The type of provider ("anthropic", "openrouter", or "openai")</param>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>ILLMClient instance</returns>
    /// <exception cref="NotSupportedException">Thrown when provider type is not supported</exception>
    [Obsolete("Use CreateClient(ProviderSettings) instead for strongly-typed settings")]
    public ILLMClient CreateClient(string providerType, Dictionary<string, object>? settings = null)
    {
        return providerType.ToLower() switch
        {
            "anthropic" => CreateAnthropicClient(settings),
            "openrouter" => CreateOpenRouterClient(settings ?? new Dictionary<string, object>()),
            // "openai" => CreateOpenAIClient(settings ?? new Dictionary<string, object>()), // OpenAI client not yet implemented
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
    }

    /// <summary>
    /// Creates an Anthropic client with the specified settings
    /// </summary>
    /// <param name="settings">Anthropic provider settings</param>
    /// <returns>AnthropicClient instance</returns>
    public AnthropicClient CreateAnthropicClient(AnthropicProviderSettings settings)
    {
        // AnthropicClient handles OAuth token and API key internally
        return new AnthropicClient();
    }

    /// <summary>
    /// Creates an Anthropic client with the specified settings (legacy method)
    /// </summary>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>AnthropicClient instance</returns>
    [Obsolete("Use CreateAnthropicClient(AnthropicProviderSettings) instead")]
    public AnthropicClient CreateAnthropicClient(Dictionary<string, object>? settings = null)
    {
        return new AnthropicClient();
    }

    /// <summary>
    /// Creates an OpenRouter client with the specified settings
    /// </summary>
    /// <param name="settings">OpenRouter provider settings</param>
    /// <returns>OpenRouterClient instance</returns>
    public OpenRouterClient CreateOpenRouterClient(OpenRouterProviderSettings settings)
    {
        var apiKey = !string.IsNullOrEmpty(settings.ApiKey) 
            ? settings.ApiKey 
            : Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;

        var options = new OpenRouterOptions
        {
            ApiKey = apiKey
        };

        if (!string.IsNullOrEmpty(settings.SiteUrl))
            options.SiteUrl = settings.SiteUrl;
        
        if (!string.IsNullOrEmpty(settings.SiteName))
            options.SiteName = settings.SiteName;

        var logger = _loggerFactory.CreateLogger<OpenRouterClient>();
        return new OpenRouterClient(options, logger, _loggerFactory);
    }

    /// <summary>
    /// Creates an OpenRouter client with the specified settings (legacy method)
    /// </summary>
    /// <param name="settings">Provider settings containing API keys and configuration</param>
    /// <returns>OpenRouterClient instance</returns>
    [Obsolete("Use CreateOpenRouterClient(OpenRouterProviderSettings) instead")]
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

    // OpenAI client creation is not yet implemented
    // /// <summary>
    // /// Creates an OpenAI client with the specified settings
    // /// </summary>
    // public OpenAIClient CreateOpenAIClient(Dictionary<string, object> settings)
    // {
    //     var apiKey = settings.GetValueOrDefault("ApiKey")?.ToString() ??
    //                  Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
    //                  string.Empty;

    //     var options = new OpenAIOptions
    //     {
    //         ApiKey = apiKey
    //     };

    //     var logger = _loggerFactory.CreateLogger<OpenAIClient>();
    //     return new OpenAIClient(options, logger, _loggerFactory);
    // }
}
