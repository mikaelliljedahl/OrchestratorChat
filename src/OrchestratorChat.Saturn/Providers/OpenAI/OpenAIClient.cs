using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.Streaming;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Providers.OpenAI;

/// <summary>
/// Minimal OpenAI API client supporting chat completions and streaming
/// </summary>
public class OpenAIClient : ILLMClient
{
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAIClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public OpenAIClient(OpenAIOptions options, ILogger<OpenAIClient> logger)
        : this(options, logger, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
    {
    }

    public OpenAIClient(OpenAIOptions options, ILogger<OpenAIClient> logger, ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                _options.ApiKey = envKey;
            }
        }

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> SendMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenAIClient));

        var request = new ChatCompletionRequest
        {
            Model = string.IsNullOrEmpty(model) ? _options.DefaultModel : model,
            Messages = messages.Select(m => new Message
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Stream = false,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
        return completion?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OpenAIClient));

        var request = new ChatCompletionRequest
        {
            Model = string.IsNullOrEmpty(model) ? _options.DefaultModel : model,
            Messages = messages.Select(m => new Message
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Stream = true,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var sseEvents = SseParser.ParseStreamAsync(response, cancellationToken);
        var chunks = SseParser.ParseJsonDataAsync<StreamChunk>(sseEvents, cancellationToken);

        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta!;
            }
        }
    }

    public Task<List<LLMModelInfo>> GetAvailableModelsAsync()
    {
        // Static minimal list; could be extended by calling /models in future
        var models = new List<LLMModelInfo>
        {
            new() { Id = "gpt-4o-mini", Name = "GPT-4o Mini", Provider = "OpenAI" },
            new() { Id = "o4-mini", Name = "O4 Mini", Provider = "OpenAI" },
            new() { Id = "gpt-4o", Name = "GPT-4o", Provider = "OpenAI" }
        };
        return Task.FromResult(models);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(!string.IsNullOrEmpty(_options.ApiKey));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

