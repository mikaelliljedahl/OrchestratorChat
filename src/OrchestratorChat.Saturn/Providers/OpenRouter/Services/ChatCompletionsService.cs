using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;

namespace OrchestratorChat.Saturn.Providers.OpenRouter.Services;

/// <summary>
/// Service for OpenRouter chat completions API operations
/// </summary>
public class ChatCompletionsService
{
    private readonly HttpClientAdapter _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<ChatCompletionsService> _logger;
    private const string CHAT_COMPLETIONS_ENDPOINT = "/chat/completions";

    public ChatCompletionsService(
        HttpClientAdapter httpClient, 
        OpenRouterOptions options,
        ILogger<ChatCompletionsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a chat completion with non-streaming response
    /// </summary>
    public async Task<ChatCompletionResponse> CreateCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Apply default options if not specified in request
        ApplyDefaultOptions(request);
        
        // Ensure streaming is disabled for non-streaming requests
        request.Stream = false;
        
        _logger.LogDebug("Creating chat completion for model {Model} with {MessageCount} messages", 
            request.Model, request.Messages?.Count ?? 0);

        try
        {
            var response = await _httpClient.PostAsync<ChatCompletionRequest, ChatCompletionResponse>(
                CHAT_COMPLETIONS_ENDPOINT, request, cancellationToken);
            
            _logger.LogDebug("Chat completion successful. Response ID: {Id}, Model: {Model}, Usage: {Usage}",
                response.Id, response.Model, 
                response.Usage != null ? $"{response.Usage.TotalTokens} tokens" : "unknown");
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat completion failed for model {Model}", request.Model);
            throw;
        }
    }

    /// <summary>
    /// Creates a streaming chat completion
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> CreateCompletionStreamAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Apply default options if not specified in request
        ApplyDefaultOptions(request);
        
        // Enable streaming for streaming requests
        request.Stream = true;
        
        _logger.LogDebug("Creating streaming chat completion for model {Model} with {MessageCount} messages", 
            request.Model, request.Messages?.Count ?? 0);

        var chunkCount = 0;
        
        await foreach (var chunk in _httpClient.StreamAsync<ChatCompletionRequest, StreamChunk>(
            CHAT_COMPLETIONS_ENDPOINT, request, cancellationToken))
        {
            chunkCount++;
            _logger.LogTrace("Received stream chunk {ChunkNumber} for completion {Id}", 
                chunkCount, chunk.Id);
            
            yield return chunk;
        }
        
        _logger.LogDebug("Streaming chat completion completed. Received {ChunkCount} chunks", chunkCount);
    }

    /// <summary>
    /// Creates a completion with automatic streaming detection
    /// </summary>
    public async Task<object> CreateCompletionAsync(
        ChatCompletionRequest request,
        bool enableStreaming,
        CancellationToken cancellationToken = default)
    {
        if (enableStreaming)
        {
            var streamingResults = new List<StreamChunk>();
            await foreach (var chunk in CreateCompletionStreamAsync(request, cancellationToken))
            {
                streamingResults.Add(chunk);
            }
            return streamingResults;
        }
        else
        {
            return await CreateCompletionAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Applies default configuration options to the request if not already specified
    /// </summary>
    private void ApplyDefaultOptions(ChatCompletionRequest request)
    {
        // Set default model if not specified
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            request.Model = _options.DefaultModel;
            _logger.LogDebug("Using default model: {Model}", request.Model);
        }

        // Apply default parameters if not specified
        request.Temperature ??= _options.DefaultTemperature;
        request.MaxTokens ??= _options.DefaultMaxTokens;
        request.TopP ??= _options.DefaultTopP;
        request.FrequencyPenalty ??= _options.DefaultFrequencyPenalty;
        request.PresencePenalty ??= _options.DefaultPresencePenalty;

        // Apply default provider preferences if not specified
        if (request.Provider == null && (_options.EnableFallbacks || !string.IsNullOrWhiteSpace(_options.DataCollection)))
        {
            request.Provider = new ProviderPreferences
            {
                AllowFallbacks = _options.EnableFallbacks,
                DataCollection = _options.DataCollection
            };
            _logger.LogDebug("Applied default provider preferences: Fallbacks={Fallbacks}, DataCollection={DataCollection}",
                _options.EnableFallbacks, _options.DataCollection);
        }

        // Validate messages
        if (request.Messages == null || request.Messages.Count == 0)
        {
            throw new ArgumentException("At least one message is required", nameof(request.Messages));
        }

        // Log request configuration
        _logger.LogDebug("Chat completion request configured: Model={Model}, Temperature={Temperature}, MaxTokens={MaxTokens}, Messages={MessageCount}",
            request.Model, request.Temperature, request.MaxTokens, request.Messages.Count);
    }

    /// <summary>
    /// Creates a simple chat completion with just a prompt
    /// </summary>
    public async Task<ChatCompletionResponse> CreateSimpleCompletionAsync(
        string prompt,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var request = new ChatCompletionRequest
        {
            Model = model ?? _options.DefaultModel,
            Temperature = temperature ?? _options.DefaultTemperature,
            MaxTokens = maxTokens ?? _options.DefaultMaxTokens,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = prompt }
            }
        };

        return await CreateCompletionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a simple streaming chat completion with just a prompt
    /// </summary>
    public IAsyncEnumerable<StreamChunk> CreateSimpleCompletionStreamAsync(
        string prompt,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var request = new ChatCompletionRequest
        {
            Model = model ?? _options.DefaultModel,
            Temperature = temperature ?? _options.DefaultTemperature,
            MaxTokens = maxTokens ?? _options.DefaultMaxTokens,
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = prompt }
            }
        };

        return CreateCompletionStreamAsync(request, cancellationToken);
    }
}