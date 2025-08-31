using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.OpenRouter.Models;
using OrchestratorChat.Saturn.Providers.Streaming;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Providers.OpenAI;

/// <summary>
/// OpenAI provider implementation for Saturn using Chat Completions API
/// </summary>
public class OpenAIProvider : LLMProviderBase
{
    public override string Id => "openai";
    public override string Name => "OpenAI";
    public override ProviderType Type => ProviderType.OpenAI;
    public override List<string> SupportedModels => new()
    {
        "gpt-4o-mini",
        "o4-mini",
        "gpt-4o"
    };

    private string? _apiKey;
    private ProviderConfiguration? _configuration;
    private HttpClient? _httpClient;
    private string _baseUrl = "https://api.openai.com/v1";

    public OpenAIProvider(ProviderConfiguration configuration)
    {
        _configuration = configuration;
        _apiKey = configuration.ApiKey;
        if (configuration.Settings.TryGetValue("BaseUrl", out var bu) && bu is string s && !string.IsNullOrWhiteSpace(s))
        {
            _baseUrl = s;
        }
    }

    public override async Task InitializeAsync()
    {
        _apiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        IsInitialized = !string.IsNullOrEmpty(_apiKey);
        await Task.CompletedTask;
    }

    public override async Task InitializeAsync(ProviderConfiguration configuration)
    {
        _configuration = configuration;
        _apiKey = configuration.ApiKey;
        _apiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (configuration.Settings.TryGetValue("BaseUrl", out var bu) && bu is string s && !string.IsNullOrWhiteSpace(s))
        {
            _baseUrl = s;
        }

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
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
            throw new InvalidOperationException("OpenAI provider not initialized");
        }

        var request = new ChatCompletionRequest
        {
            Model = string.IsNullOrEmpty(model) ? (SupportedModels.FirstOrDefault() ?? "gpt-4o-mini") : model,
            Messages = messages.Select(m => new Message
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Stream = true,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var url = $"{_baseUrl.TrimEnd('/')}/chat/completions";
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var sseEvents = SseParser.ParseStreamAsync(response, cancellationToken);
        var streamChunks = SseParser.ParseJsonDataAsync<StreamChunk>(sseEvents, cancellationToken);

        await foreach (var chunk in streamChunks.WithCancellation(cancellationToken))
        {
            if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
            {
                yield return chunk.Choices.First().Delta.Content!;
            }
        }
    }

    public override Task<string> GetCompletionAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default)
    {
        // For now, rely on streaming path; non-streaming can be implemented similarly
        return Task.FromException<string>(new NotImplementedException("OpenAI completion not yet implemented"));
    }

    public override Task<bool> ValidateAsync()
    {
        return Task.FromResult(!string.IsNullOrEmpty(_apiKey));
    }
}

