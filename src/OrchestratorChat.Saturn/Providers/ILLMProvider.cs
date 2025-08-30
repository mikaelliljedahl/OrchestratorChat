using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.Streaming;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.Anthropic.Models;
using System.Text.Json;
using System.Text;
using System.Runtime.CompilerServices;

namespace OrchestratorChat.Saturn.Providers;

/// <summary>
/// Interface for LLM providers
/// </summary>
public interface ILLMProvider
{
    string Id { get; }
    string Name { get; }
    ProviderType Type { get; }
    bool IsInitialized { get; }
    List<string> SupportedModels { get; }

    Task InitializeAsync();
    Task InitializeAsync(ProviderConfiguration configuration);
    IAsyncEnumerable<string> StreamCompletionAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default);
    
    Task<string> GetCompletionAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateAsync();
}

/// <summary>
/// Base implementation for LLM providers
/// </summary>
public abstract class LLMProviderBase : ILLMProvider
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract ProviderType Type { get; }
    public bool IsInitialized { get; protected set; }
    public abstract List<string> SupportedModels { get; }

    public abstract Task InitializeAsync();
    public abstract Task InitializeAsync(ProviderConfiguration configuration);
    public abstract IAsyncEnumerable<string> StreamCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        CancellationToken cancellationToken = default);
    
    public abstract Task<string> GetCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        CancellationToken cancellationToken = default);
    
    public abstract Task<bool> ValidateAsync();
}

/// <summary>
/// OpenRouter provider implementation
/// </summary>
public class OpenRouterProvider : LLMProviderBase
{
    public override string Id => "openrouter";
    public override string Name => "OpenRouter";
    public override ProviderType Type => ProviderType.OpenRouter;
    public override List<string> SupportedModels => new()
    {
        "anthropic/claude-3.5-sonnet",
        "openai/gpt-4o",
        "google/gemini-pro-1.5",
        "meta-llama/llama-3.1-70b-instruct",
        "deepseek/deepseek-chat"
    };

    private string? _apiKey;
    private ProviderConfiguration? _configuration;
    private HttpClient? _httpClient;

    public OpenRouterProvider(ProviderConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Legacy constructor for backward compatibility
    public OpenRouterProvider(string apiKey)
    {
        _apiKey = apiKey;
    }

    public override async Task InitializeAsync()
    {
        // Try to get API key from environment if not provided
        _apiKey ??= Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/OrchestratorChat");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "OrchestratorChat Saturn");
        }
        
        IsInitialized = !string.IsNullOrEmpty(_apiKey);
        await Task.CompletedTask;
    }

    public override async Task InitializeAsync(ProviderConfiguration configuration)
    {
        _configuration = configuration;
        _apiKey = configuration.ApiKey;
        
        // Fallback to environment variable if not in config
        _apiKey ??= Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/OrchestratorChat");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "OrchestratorChat Saturn");
        }
        
        IsInitialized = !string.IsNullOrEmpty(_apiKey);
        await Task.CompletedTask;
    }

    public override async IAsyncEnumerable<string> StreamCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_httpClient == null || !IsInitialized)
        {
            throw new InvalidOperationException("OpenRouter provider not initialized");
        }

        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages.Select(m => new Message
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Stream = true,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var sseEvents = SseParser.ParseStreamAsync(response, cancellationToken);
        var streamChunks = SseParser.ParseJsonDataAsync<StreamChunk>(sseEvents, cancellationToken);

        await foreach (var chunk in streamChunks.WithCancellation(cancellationToken))
        {
            if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
            {
                yield return chunk.Choices.First().Delta.Content;
            }
        }
    }

    public override async Task<string> GetCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement completion
        throw new NotImplementedException("OpenRouter completion not yet implemented");
    }

    public override async Task<bool> ValidateAsync()
    {
        // TODO: Validate API key and connection
        return !string.IsNullOrEmpty(_apiKey);
    }
}

/// <summary>
/// Anthropic provider implementation with OAuth support
/// </summary>
public class AnthropicProvider : LLMProviderBase
{
    public override string Id => "anthropic";
    public override string Name => "Anthropic";
    public override ProviderType Type => ProviderType.Anthropic;
    public override List<string> SupportedModels => new()
    {
        "claude-opus-4-1-20250805",
        "claude-opus-4",
        "claude-sonnet-4-20250514",
        "claude-3.7-sonnet",
        "claude-3.5-haiku"
    };

    private const string ClaudeCodeSystemPrompt = "You are Claude Code, Anthropic's official CLI for Claude.";
    private const string UserAgent = "Claude-Code/1.0";
    
    private string? _apiKey;
    private string? _oauthToken;
    private ProviderConfiguration? _configuration;
    private HttpClient? _httpClient;
    private readonly Dictionary<string, object> _settings;

    public AnthropicProvider(ProviderConfiguration configuration)
    {
        _configuration = configuration;
        _settings = new Dictionary<string, object>();
    }

    // Legacy constructor for backward compatibility
    public AnthropicProvider(Dictionary<string, object> settings)
    {
        _settings = settings;
    }

    public override async Task InitializeAsync()
    {
        // Try to get API key from environment or settings
        _apiKey = _settings.ContainsKey("ApiKey") ? _settings["ApiKey"]?.ToString() : null;
        _apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        
        if (!string.IsNullOrEmpty(_apiKey) || !string.IsNullOrEmpty(_oauthToken))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            
            if (!string.IsNullOrEmpty(_oauthToken))
            {
                // Use OAuth Bearer token (remove x-api-key header when using OAuth)
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_oauthToken}");
            }
            else if (!string.IsNullOrEmpty(_apiKey))
            {
                // Use API key authentication
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            }
        }
        
        IsInitialized = !string.IsNullOrEmpty(_apiKey) || !string.IsNullOrEmpty(_oauthToken);
        await Task.CompletedTask;
    }

    public override async Task InitializeAsync(ProviderConfiguration configuration)
    {
        _configuration = configuration;
        _apiKey = configuration.ApiKey;
        
        // Check for OAuth token in settings
        if (configuration.Settings.ContainsKey("OAuthToken"))
        {
            _oauthToken = configuration.Settings["OAuthToken"]?.ToString();
        }
        
        // Fallback to environment variable if not in config
        _apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        
        if (!string.IsNullOrEmpty(_apiKey) || !string.IsNullOrEmpty(_oauthToken))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            
            if (!string.IsNullOrEmpty(_oauthToken))
            {
                // Use OAuth Bearer token (remove x-api-key header when using OAuth)
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_oauthToken}");
            }
            else if (!string.IsNullOrEmpty(_apiKey))
            {
                // Use API key authentication
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            }
        }
        
        IsInitialized = !string.IsNullOrEmpty(_apiKey) || !string.IsNullOrEmpty(_oauthToken);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures system prompt starts with the required Claude Code prefix
    /// </summary>
    /// <param name="messages">List of agent messages</param>
    /// <returns>Messages with proper system prompt</returns>
    private List<AgentMessage> EnsureClaudeCodeSystemPrompt(List<AgentMessage> messages)
    {
        var processedMessages = new List<AgentMessage>(messages);
        
        // Find existing system message or create one
        var systemMessage = processedMessages.FirstOrDefault(m => m.Role == MessageRole.System);
        if (systemMessage != null)
        {
            // Ensure system prompt starts with Claude Code identification
            if (!systemMessage.Content.StartsWith(ClaudeCodeSystemPrompt, StringComparison.OrdinalIgnoreCase))
            {
                systemMessage.Content = ClaudeCodeSystemPrompt + "\n\n" + systemMessage.Content;
            }
        }
        else
        {
            // Add Claude Code system prompt at the beginning
            processedMessages.Insert(0, new AgentMessage
            {
                Content = ClaudeCodeSystemPrompt,
                Role = MessageRole.System
            });
        }
        
        return processedMessages;
    }

    public override async IAsyncEnumerable<string> StreamCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_httpClient == null || !IsInitialized)
        {
            throw new InvalidOperationException("Anthropic provider not initialized");
        }

        var processedMessages = EnsureClaudeCodeSystemPrompt(messages);
        var systemMessage = processedMessages.FirstOrDefault(m => m.Role == MessageRole.System);
        var conversationMessages = processedMessages.Where(m => m.Role != MessageRole.System).ToList();

        var request = new AnthropicRequest
        {
            Model = model,
            Messages = conversationMessages.Select(m => new AnthropicMessage
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            System = systemMessage?.Content,
            Stream = true,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Remove("anthropic-version");
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var sseEvents = SseParser.ParseStreamAsync(response, cancellationToken);
        var streamEvents = SseParser.ParseJsonDataAsync<AnthropicStreamEvent>(sseEvents, cancellationToken);

        await foreach (var streamEvent in streamEvents.WithCancellation(cancellationToken))
        {
            if (streamEvent?.Type == "content_block_delta" && streamEvent.Delta?.Text != null)
            {
                yield return streamEvent.Delta.Text;
            }
        }
    }

    public override async Task<string> GetCompletionAsync(
        List<AgentMessage> messages, 
        string model, 
        double temperature = 0.7, 
        int maxTokens = 4096, 
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement completion
        throw new NotImplementedException("Anthropic completion not yet implemented");
    }

    public override async Task<bool> ValidateAsync()
    {
        // Validate API key or OAuth token
        var hasApiKey = !string.IsNullOrEmpty(_apiKey) || 
                       (_settings.ContainsKey("ApiKey") && !string.IsNullOrEmpty(_settings["ApiKey"]?.ToString()));
        var hasOAuthToken = !string.IsNullOrEmpty(_oauthToken) ||
                           (_configuration?.Settings.ContainsKey("OAuthToken") == true && 
                            !string.IsNullOrEmpty(_configuration.Settings["OAuthToken"]?.ToString()));
        
        return hasApiKey || hasOAuthToken;
    }
}