using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.OpenRouter.Services;
using OrchestratorChat.Saturn.Models;

namespace OrchestratorChat.Saturn.Providers.OpenRouter;

/// <summary>
/// Complete OpenRouter API client that coordinates all services
/// </summary>
public class OpenRouterClient : ILLMClient
{
    private readonly HttpClientAdapter _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    /// <summary>
    /// Service for chat completions operations
    /// </summary>
    public ChatCompletionsService Chat { get; }

    /// <summary>
    /// Service for models operations
    /// </summary>
    public ModelsService Models { get; }

    /// <summary>
    /// Creates a new OpenRouter client with the specified options
    /// </summary>
    public OpenRouterClient(OpenRouterOptions options, ILogger<OpenRouterClient> logger)
        : this(options, logger, null)
    {
    }

    /// <summary>
    /// Creates a new OpenRouter client with the specified options and logger factory
    /// </summary>
    public OpenRouterClient(OpenRouterOptions options, ILogger<OpenRouterClient> logger, ILoggerFactory? loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? CreateDefaultLoggerFactory();

        // Validate options
        _options.Validate();

        _logger.LogDebug("Initializing OpenRouter client with base URL: {BaseUrl}", options.BaseUrl);

        // Create HTTP client adapter with proper logger
        var httpLogger = _loggerFactory.CreateLogger<HttpClientAdapter>();
        _httpClient = new HttpClientAdapter(options, httpLogger);

        // Initialize services with proper loggers
        var chatLogger = _loggerFactory.CreateLogger<ChatCompletionsService>();
        Chat = new ChatCompletionsService(_httpClient, options, chatLogger);

        var modelsLogger = _loggerFactory.CreateLogger<ModelsService>();
        Models = new ModelsService(_httpClient, modelsLogger);

        _logger.LogInformation("OpenRouter client initialized successfully");
    }

    private static ILoggerFactory CreateDefaultLoggerFactory()
    {
        return Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Creates a new OpenRouter client with API key from environment or configuration
    /// </summary>
    public static OpenRouterClient Create(
        string? apiKey = null,
        string? baseUrl = null,
        ILogger<OpenRouterClient>? logger = null)
    {
        // Get API key from parameter, environment, or throw
        var effectiveApiKey = apiKey 
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException(
                "OpenRouter API key must be provided via parameter or OPENROUTER_API_KEY environment variable");

        var options = new OpenRouterOptions
        {
            ApiKey = effectiveApiKey,
            BaseUrl = baseUrl ?? "https://openrouter.ai/api/v1"
        };

        var effectiveLogger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenRouterClient>.Instance;
        
        return new OpenRouterClient(options, effectiveLogger);
    }

    /// <summary>
    /// Creates a simple text completion (convenience method)
    /// </summary>
    public async Task<string> CompleteAsync(
        string prompt,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        _logger.LogDebug("Creating simple completion for prompt: {PromptPreview}...", 
            prompt.Length > 100 ? prompt.Substring(0, 100) + "..." : prompt);

        var response = await Chat.CreateSimpleCompletionAsync(
            prompt, model, temperature, maxTokens, cancellationToken);

        var content = response.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
        
        _logger.LogDebug("Simple completion successful. Response length: {Length} characters", content.Length);
        
        return content;
    }

    /// <summary>
    /// Creates a streaming text completion (convenience method)
    /// </summary>
    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        _logger.LogDebug("Creating streaming completion for prompt: {PromptPreview}...", 
            prompt.Length > 100 ? prompt.Substring(0, 100) + "..." : prompt);

        await foreach (var chunk in Chat.CreateSimpleCompletionStreamAsync(
            prompt, model, temperature, maxTokens, cancellationToken))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    /// <summary>
    /// Creates a chat completion with conversation history
    /// </summary>
    public async Task<ChatCompletionResponse> ChatAsync(
        IEnumerable<Message> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        List<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        var messageList = messages.ToList();
        if (messageList.Count == 0)
            throw new ArgumentException("At least one message is required", nameof(messages));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        var request = new ChatCompletionRequest
        {
            Model = model ?? _options.DefaultModel,
            Messages = messageList,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Tools = tools
        };

        _logger.LogDebug("Creating chat completion with {MessageCount} messages using model {Model}", 
            messageList.Count, request.Model);

        return await Chat.CreateCompletionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a streaming chat completion with conversation history
    /// </summary>
    public IAsyncEnumerable<StreamChunk> ChatStreamAsync(
        IEnumerable<Message> messages,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        List<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        var messageList = messages.ToList();
        if (messageList.Count == 0)
            throw new ArgumentException("At least one message is required", nameof(messages));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        var request = new ChatCompletionRequest
        {
            Model = model ?? _options.DefaultModel,
            Messages = messageList,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Tools = tools
        };

        _logger.LogDebug("Creating streaming chat completion with {MessageCount} messages using model {Model}", 
            messageList.Count, request.Model);

        return Chat.CreateCompletionStreamAsync(request, cancellationToken);
    }

    /// <summary>
    /// Lists all available models
    /// </summary>
    public Task<List<ModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));
        return Models.GetModelsAsync(useCache: true, cancellationToken);
    }

    /// <summary>
    /// Gets information about a specific model
    /// </summary>
    public Task<ModelInfo?> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));
        return Models.GetModelAsync(modelId, useCache: true, cancellationToken);
    }

    /// <summary>
    /// Tests the connection to OpenRouter API
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        try
        {
            _logger.LogDebug("Testing OpenRouter API connection");
            
            // Try to fetch models as a connection test
            var models = await Models.GetModelsAsync(useCache: false, cancellationToken);
            
            _logger.LogInformation("OpenRouter API connection test successful. Found {ModelCount} models", 
                models.Count);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter API connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Sends a message and returns a complete response (ILLMClient implementation)
    /// </summary>
    public async Task<string> SendMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default)
    {
        if (messages == null || messages.Count == 0)
            throw new ArgumentException("At least one message is required", nameof(messages));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        var openRouterMessages = messages.Select(m => new Message
        {
            Role = m.Role.ToString().ToLowerInvariant(),
            Content = m.Content
        }).ToList();

        var response = await ChatAsync(openRouterMessages, model, temperature, maxTokens, cancellationToken: cancellationToken);
        return response.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    /// <summary>
    /// Streams a message response as it's generated (ILLMClient implementation)
    /// </summary>
    public async IAsyncEnumerable<string> StreamMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (messages == null || messages.Count == 0)
            throw new ArgumentException("At least one message is required", nameof(messages));

        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        var openRouterMessages = messages.Select(m => new Message
        {
            Role = m.Role.ToString().ToLowerInvariant(),
            Content = m.Content
        }).ToList();

        await foreach (var chunk in ChatStreamAsync(openRouterMessages, model, temperature, maxTokens, cancellationToken: cancellationToken))
        {
            var content = chunk.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    /// <summary>
    /// Gets the list of available models (ILLMClient implementation)
    /// </summary>
    public async Task<List<LLMModelInfo>> GetAvailableModelsAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        var models = await ListModelsAsync();
        return models.Select(m => new LLMModelInfo
        {
            Id = m.Id,
            Name = m.Name,
            Provider = "OpenRouter"
        }).ToList();
    }

    /// <summary>
    /// Checks if the client is properly authenticated (ILLMClient implementation)
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        return await TestConnectionAsync();
    }

    /// <summary>
    /// Gets client configuration information
    /// </summary>
    public OpenRouterClientInfo GetClientInfo()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenRouterClient));

        return new OpenRouterClientInfo
        {
            BaseUrl = _options.BaseUrl,
            DefaultModel = _options.DefaultModel,
            TimeoutSeconds = _options.TimeoutSeconds,
            MaxRetries = _options.MaxRetries,
            EnableFallbacks = _options.EnableFallbacks,
            EnableStreaming = _options.EnableStreaming,
            EnableTools = _options.EnableTools,
            AppName = _options.AppName,
            AppVersion = _options.AppVersion
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing OpenRouter client");
            _httpClient?.Dispose();
            Models?.ClearCache();
            _disposed = true;
            _logger.LogDebug("OpenRouter client disposed");
        }
    }
}

/// <summary>
/// Information about the OpenRouter client configuration
/// </summary>
public class OpenRouterClientInfo
{
    public string BaseUrl { get; init; } = string.Empty;
    public string DefaultModel { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
    public bool EnableFallbacks { get; init; }
    public bool EnableStreaming { get; init; }
    public bool EnableTools { get; init; }
    public string AppName { get; init; } = string.Empty;
    public string AppVersion { get; init; } = string.Empty;
}