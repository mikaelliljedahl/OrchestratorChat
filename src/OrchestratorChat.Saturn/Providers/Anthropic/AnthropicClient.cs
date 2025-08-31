using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers.Anthropic.Models;
using OrchestratorChat.Saturn.Providers.Streaming;

namespace OrchestratorChat.Saturn.Providers.Anthropic;

/// <summary>
/// Anthropic API client with OAuth 2.0 support and Messages API implementation
/// </summary>
public class AnthropicClient : IDisposable
{
    // API Configuration
    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const string API_VERSION = "2023-06-01";
    private const string ANTHROPIC_BETA = "prompt-caching-2024-07-31";
    private const string USER_AGENT = "Claude-Code/1.0";
    
    // Required system prompt prefix for Claude Code compatibility
    private const string CLAUDE_CODE_SYSTEM_PROMPT = "You are Claude Code, Anthropic's official CLI for Claude.";
    
    // Header names
    private const string ANTHROPIC_VERSION_HEADER = "anthropic-version";
    private const string ANTHROPIC_BETA_HEADER = "anthropic-beta";
    
    private readonly HttpClient _httpClient;
    private readonly AnthropicAuthService _authService;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of AnthropicClient
    /// </summary>
    public AnthropicClient()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        _httpClient.DefaultRequestHeaders.Add(ANTHROPIC_VERSION_HEADER, API_VERSION);
        _httpClient.DefaultRequestHeaders.Add(ANTHROPIC_BETA_HEADER, ANTHROPIC_BETA);
        
        _authService = new AnthropicAuthService();
    }

    /// <summary>
    /// Sends a chat completion request to the Anthropic Messages API
    /// </summary>
    /// <param name="messages">List of conversation messages</param>
    /// <param name="model">Model to use for completion</param>
    /// <param name="temperature">Sampling temperature</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete response from the API</returns>
    public async Task<string> SendMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        CancellationToken cancellationToken = default)
    {
        var tokens = await EnsureValidTokensAsync();
        if (tokens?.AccessToken == null)
        {
            throw new InvalidOperationException("Authentication required. Please authenticate first.");
        }

        var processedMessages = EnsureClaudeCodeSystemPrompt(messages);
        var request = CreateRequest(processedMessages, model, temperature, maxTokens, stream: false);

        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        // Set up headers with OAuth token
        SetupAuthHeaders(tokens.AccessToken);
        
        var response = await _httpClient.PostAsync(API_URL, content, cancellationToken);
        await HandleErrorResponse(response);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseContent);

        return ExtractTextContent(anthropicResponse);
    }

    /// <summary>
    /// Streams a chat completion from the Anthropic Messages API
    /// </summary>
    /// <param name="messages">List of conversation messages</param>
    /// <param name="model">Model to use for completion</param>
    /// <param name="temperature">Sampling temperature</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming text chunks</returns>
    public async IAsyncEnumerable<string> StreamMessageAsync(
        List<AgentMessage> messages,
        string model,
        double temperature = 0.7,
        int maxTokens = 4096,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tokens = await EnsureValidTokensAsync();
        if (tokens?.AccessToken == null)
        {
            throw new InvalidOperationException("Authentication required. Please authenticate first.");
        }

        var processedMessages = EnsureClaudeCodeSystemPrompt(messages);
        var request = CreateRequest(processedMessages, model, temperature, maxTokens, stream: true);

        using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        // Set up headers with OAuth token
        SetupAuthHeaders(tokens.AccessToken);

        var response = await _httpClient.PostAsync(API_URL, content, cancellationToken);
        await HandleErrorResponse(response);

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

    /// <summary>
    /// Gets the list of available Anthropic models
    /// </summary>
    /// <returns>List of model information</returns>
    public Task<List<ModelInfo>> GetAvailableModelsAsync()
    {
        var models = new List<ModelInfo>
        {
            new() { Id = "claude-opus-4-1-20250805", Name = "Claude Opus 4.1", Provider = "Anthropic" },
            new() { Id = "claude-opus-4", Name = "Claude Opus 4", Provider = "Anthropic" },
            new() { Id = "claude-sonnet-4-20250514", Name = "Claude Sonnet 4", Provider = "Anthropic" },
        };

        return Task.FromResult(models);
    }

    /// <summary>
    /// Initiates OAuth authentication flow
    /// </summary>
    /// <param name="useClaudeMax">True to use Claude.ai, false for Console</param>
    /// <returns>True if authentication succeeded</returns>
    public Task<bool> AuthenticateAsync(bool useClaudeMax = true)
    {
        return _authService.AuthenticateAsync(useClaudeMax);
    }

    /// <summary>
    /// Logs out by clearing stored tokens
    /// </summary>
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
    }

    /// <summary>
    /// Checks if the client has valid authentication
    /// </summary>
    /// <returns>True if authenticated and tokens are valid</returns>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var tokens = await _authService.GetValidTokensAsync();
        return tokens?.AccessToken != null;
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
            if (!systemMessage.Content.StartsWith(CLAUDE_CODE_SYSTEM_PROMPT, StringComparison.OrdinalIgnoreCase))
            {
                systemMessage.Content = CLAUDE_CODE_SYSTEM_PROMPT + "\n\n" + systemMessage.Content;
            }
        }
        else
        {
            // Add Claude Code system prompt at the beginning
            processedMessages.Insert(0, new AgentMessage
            {
                Content = CLAUDE_CODE_SYSTEM_PROMPT,
                Role = MessageRole.System
            });
        }
        
        return processedMessages;
    }

    /// <summary>
    /// Creates an Anthropic API request from agent messages
    /// </summary>
    private AnthropicRequest CreateRequest(
        List<AgentMessage> messages, 
        string model, 
        double temperature, 
        int maxTokens, 
        bool stream)
    {
        var systemMessage = messages.FirstOrDefault(m => m.Role == MessageRole.System);
        var conversationMessages = messages.Where(m => m.Role != MessageRole.System).ToList();

        return new AnthropicRequest
        {
            Model = model,
            Messages = conversationMessages.Select(m => new AnthropicMessage
            {
                Role = m.Role.ToString().ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            System = systemMessage?.Content,
            Stream = stream,
            Temperature = temperature,
            MaxTokens = maxTokens
        };
    }

    /// <summary>
    /// Sets up HTTP headers with OAuth Bearer token
    /// </summary>
    private void SetupAuthHeaders(string accessToken)
    {
        // Remove any existing authorization headers
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        
        // Use OAuth Bearer token (MUST remove x-api-key header when using OAuth)
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
    }

    /// <summary>
    /// Ensures we have valid tokens, refreshing if necessary
    /// </summary>
    private async Task<StoredTokens?> EnsureValidTokensAsync()
    {
        return await _authService.GetValidTokensAsync();
    }

    /// <summary>
    /// Handles error responses from the API
    /// </summary>
    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        
        try
        {
            var error = JsonSerializer.Deserialize<AnthropicError>(errorContent);
            throw new HttpRequestException($"Anthropic API error: {error.Error.Type} - {error.Error.Message}");
        }
        catch (JsonException)
        {
            // If we can't parse the error, throw with status code and content
            throw new HttpRequestException($"Anthropic API error: {response.StatusCode} - {errorContent}");
        }
    }

    /// <summary>
    /// Extracts text content from Anthropic response
    /// </summary>
    private string ExtractTextContent(AnthropicResponse? response)
    {
        if (response?.Content == null)
        {
            return string.Empty;
        }

        var textContent = response.Content.Where(c => c.Type == "text" && !string.IsNullOrEmpty(c.Text));
        return string.Join("", textContent.Select(c => c.Text));
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _authService?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Model information for available models
/// </summary>
public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}