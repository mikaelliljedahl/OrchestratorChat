using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polly;
using OrchestratorChat.Saturn.Providers.Streaming;

namespace OrchestratorChat.Saturn.Providers.OpenRouter;

/// <summary>
/// HTTP client adapter for OpenRouter API with retry logic and streaming support
/// </summary>
public class HttpClientAdapter : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<HttpClientAdapter> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private bool _disposed;

    public HttpClientAdapter(OpenRouterOptions options, ILogger<HttpClientAdapter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Validate options
        _options.Validate();
        
        // Create HTTP client with configuration
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        };
        
        // Set authentication and attribution headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _options.HttpReferer);
        _httpClient.DefaultRequestHeaders.Add("X-Title", _options.XTitle);
        
        // Add application attribution headers
        if (!string.IsNullOrEmpty(_options.AppName))
        {
            _httpClient.DefaultRequestHeaders.Add("X-App-Name", _options.AppName);
            if (!string.IsNullOrEmpty(_options.AppVersion))
            {
                _httpClient.DefaultRequestHeaders.Add("X-App-Version", _options.AppVersion);
            }
        }
        
        // Create retry pipeline with exponential backoff
        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = _options.MaxRetries,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(response => 
                        response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode >= HttpStatusCode.InternalServerError),
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry attempt {Attempt} after {Delay}ms due to: {Exception}", 
                        args.AttemptNumber, 
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Makes a GET request to the specified endpoint
    /// </summary>
    public async Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HttpClientAdapter));
        
        var response = await _retryPipeline.ExecuteAsync(async _ => 
        {
            var httpResponse = await _httpClient.GetAsync(endpoint, cancellationToken);
            await EnsureSuccessStatusCodeAsync(httpResponse);
            return httpResponse;
        }, cancellationToken);

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("GET {Endpoint} response: {Json}", endpoint, json);
            
            return JsonSerializer.Deserialize<TResponse>(json, GetJsonOptions()) 
                ?? throw new InvalidOperationException($"Failed to deserialize response for {endpoint}");
        }
    }

    /// <summary>
    /// Makes a POST request with JSON content
    /// </summary>
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest request, 
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HttpClientAdapter));
        
        var json = JsonSerializer.Serialize(request, GetJsonOptions());
        _logger.LogDebug("POST {Endpoint} request: {Json}", endpoint, json);
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _retryPipeline.ExecuteAsync(async _ => 
        {
            var httpResponse = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(httpResponse);
            return httpResponse;
        }, cancellationToken);

        using (response)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("POST {Endpoint} response: {Json}", endpoint, responseJson);
            
            return JsonSerializer.Deserialize<TResponse>(responseJson, GetJsonOptions()) 
                ?? throw new InvalidOperationException($"Failed to deserialize response for {endpoint}");
        }
    }

    /// <summary>
    /// Makes a streaming POST request that returns Server-Sent Events
    /// </summary>
    public async IAsyncEnumerable<T> StreamAsync<TRequest, T>(
        string endpoint, 
        TRequest request, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HttpClientAdapter));
        
        var json = JsonSerializer.Serialize(request, GetJsonOptions());
        _logger.LogDebug("Streaming POST {Endpoint} request: {Json}", endpoint, json);
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };
        
        // Set headers for streaming
        requestMessage.Headers.Add("Accept", "text/event-stream");
        requestMessage.Headers.Add("Cache-Control", "no-cache");
        
        // Send request without retry for streaming (retries would restart the stream)
        var response = await _httpClient.SendAsync(
            requestMessage, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken);

        try
        {
            await EnsureSuccessStatusCodeAsync(response);
            
            _logger.LogDebug("Starting SSE stream for {Endpoint}", endpoint);
            
            // Parse SSE stream and deserialize JSON data
            await foreach (var sseEvent in SseParser.ParseStreamAsync(response, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(sseEvent.Data) || sseEvent.Data == "[DONE]")
                {
                    _logger.LogDebug("SSE stream completed for {Endpoint}", endpoint);
                    yield break;
                }
                
                T? parsed = null;
                try
                {
                    parsed = JsonSerializer.Deserialize<T>(sseEvent.Data, GetJsonOptions());
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to parse SSE data as {Type}: {Data}. Error: {Error}", 
                        typeof(T).Name, sseEvent.Data, ex.Message);
                    continue;
                }
                
                if (parsed != null)
                {
                    yield return parsed;
                }
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>
    /// Gets JSON serialization options for consistent formatting
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Ensures HTTP response indicates success, throwing detailed exceptions for failures
    /// </summary>
    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = string.Empty;
        try
        {
            errorContent = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore errors reading error content
        }

        var message = $"OpenRouter API request failed with status {(int)response.StatusCode} {response.StatusCode}";
        if (!string.IsNullOrWhiteSpace(errorContent))
        {
            message += $". Response: {errorContent}";
        }

        _logger.LogError("HTTP request failed: {StatusCode} {ReasonPhrase}. Content: {Content}", 
            response.StatusCode, response.ReasonPhrase, errorContent);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException("Invalid API key or unauthorized access"),
            HttpStatusCode.TooManyRequests => new InvalidOperationException("Rate limit exceeded"),
            HttpStatusCode.BadRequest => new ArgumentException($"Bad request: {errorContent}"),
            HttpStatusCode.NotFound => new InvalidOperationException("API endpoint not found"),
            HttpStatusCode.InternalServerError => new InvalidOperationException("OpenRouter internal server error"),
            HttpStatusCode.ServiceUnavailable => new InvalidOperationException("OpenRouter service unavailable"),
            _ => new HttpRequestException(message, null, response.StatusCode)
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}