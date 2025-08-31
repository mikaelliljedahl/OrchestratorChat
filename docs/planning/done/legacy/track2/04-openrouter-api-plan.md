# OpenRouter API Implementation Plan

## Overview
OpenRouter provides a unified API for accessing multiple LLM providers. This document details the complete implementation of OpenRouter integration, including all API models, services, HTTP client, and SSE streaming support based on SaturnFork's comprehensive implementation.

## Current State vs Required State

### Current State (OrchestratorChat.Saturn)
- No OpenRouter implementation
- No API models
- No HTTP client
- No streaming support

### Required State (from SaturnFork)
- Complete OpenRouter API client
- 50+ API model classes
- Service-based architecture
- SSE streaming support
- Comprehensive error handling
- Rate limiting and retry logic

## API Models Implementation

### 1. Chat API Models

#### 1.1 Core Chat Models
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Models/Api/Chat/`

```csharp
// ChatCompletionRequest.cs
public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }
    
    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
    
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }
    
    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }
    
    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }
    
    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    [JsonPropertyName("tools")]
    public List<ToolDefinition> Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public ToolChoice ToolChoice { get; set; }
    
    [JsonPropertyName("response_format")]
    public ResponseFormat ResponseFormat { get; set; }
    
    [JsonPropertyName("stop")]
    public List<string> Stop { get; set; }
    
    [JsonPropertyName("provider")]
    public ProviderPreferences Provider { get; set; }
    
    [JsonPropertyName("transforms")]
    public List<string> Transforms { get; set; }
    
    [JsonPropertyName("route")]
    public ModelRouting Route { get; set; }
}

// Message.cs
public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } // system, user, assistant, tool
    
    [JsonPropertyName("content")]
    [JsonConverter(typeof(ContentConverter))]
    public object Content { get; set; } // string or List<ContentPart>
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall> ToolCalls { get; set; }
    
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }
}

// ContentPart types
public abstract class ContentPart
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public class TextContentPart : ContentPart
{
    public override string Type => "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("cache_control")]
    public CacheControl CacheControl { get; set; }
}

public class ImageUrlContentPart : ContentPart
{
    public override string Type => "image_url";
    
    [JsonPropertyName("image_url")]
    public ImageUrl ImageUrl { get; set; }
    
    public class ImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        
        [JsonPropertyName("detail")]
        public string Detail { get; set; } // auto, low, high
    }
}

public class InputAudioContentPart : ContentPart
{
    public override string Type => "input_audio";
    
    [JsonPropertyName("input_audio")]
    public InputAudio Audio { get; set; }
    
    public class InputAudio
    {
        [JsonPropertyName("data")]
        public string Data { get; set; } // base64
        
        [JsonPropertyName("format")]
        public string Format { get; set; } // wav, mp3, etc
    }
}

// Tool-related models
public class ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public ToolFunction Function { get; set; }
}

public class ToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } // JSON Schema
}

public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; }
    
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } // JSON string
    }
}

public class ToolChoice
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // auto, none, required, function
    
    [JsonPropertyName("function")]
    public FunctionChoice Function { get; set; }
    
    public class FunctionChoice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
```

#### 1.2 Response Models
```csharp
// ChatCompletionResponse.cs
public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }
    
    [JsonPropertyName("usage")]
    public ResponseUsage Usage { get; set; }
    
    [JsonPropertyName("system_fingerprint")]
    public string SystemFingerprint { get; set; }
}

// Choice.cs
public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public AssistantMessageResponse Message { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } // stop, length, tool_calls, etc
}

// Streaming models
public class ChatCompletionChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("choices")]
    public List<StreamingChoice> Choices { get; set; }
}

public class StreamingChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("delta")]
    public Delta Delta { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class Delta
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall> ToolCalls { get; set; }
}
```

### 2. Common API Models

#### 2.1 Provider and Routing Models
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Models/Api/Common/`

```csharp
// ProviderPreferences.cs
public class ProviderPreferences
{
    [JsonPropertyName("allow_fallbacks")]
    public bool? AllowFallbacks { get; set; }
    
    [JsonPropertyName("require_parameters")]
    public bool? RequireParameters { get; set; }
    
    [JsonPropertyName("data_collection")]
    public string DataCollection { get; set; } // allow, deny
    
    [JsonPropertyName("order")]
    public List<string> Order { get; set; }
    
    [JsonPropertyName("ignore")]
    public List<string> Ignore { get; set; }
}

// ModelRouting.cs
public class ModelRouting
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("provider")]
    public string Provider { get; set; }
}

// ResponseUsage.cs
public class ResponseUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    
    [JsonPropertyName("prompt_tokens_details")]
    public TokenDetails PromptTokensDetails { get; set; }
    
    [JsonPropertyName("completion_tokens_details")]
    public TokenDetails CompletionTokensDetails { get; set; }
    
    public class TokenDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int? CachedTokens { get; set; }
        
        [JsonPropertyName("reasoning_tokens")]
        public int? ReasoningTokens { get; set; }
    }
}

// ResponseFormat.cs
public class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } // text, json_object, json_schema
    
    [JsonPropertyName("json_schema")]
    public JsonSchema Schema { get; set; }
    
    public class JsonSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("strict")]
        public bool? Strict { get; set; }
        
        [JsonPropertyName("schema")]
        public object Schema { get; set; }
    }
}

// CacheControl.cs
public class CacheControl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ephemeral";
}
```

#### 2.2 Advanced Features
```csharp
// WebSearchOptions.cs
public class WebSearchOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    
    [JsonPropertyName("max_results")]
    public int? MaxResults { get; set; }
}

// ReasoningConfig.cs
public class ReasoningConfig
{
    [JsonPropertyName("effort")]
    public string Effort { get; set; } // low, medium, high
    
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}

// Transforms.cs
public static class Transforms
{
    public const string MiddleOut = "middle-out";
    public const string AutoContinue = "auto-continue";
    public const string Dedupe = "dedupe";
}

// Plugins
public abstract class Plugin
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public class WebPlugin : Plugin
{
    public override string Type => "web";
    
    [JsonPropertyName("config")]
    public WebSearchOptions Config { get; set; }
}

public class FileParserPlugin : Plugin
{
    public override string Type => "file_parser";
    
    [JsonPropertyName("config")]
    public FileParserConfig Config { get; set; }
    
    public class FileParserConfig
    {
        [JsonPropertyName("max_file_size")]
        public int? MaxFileSize { get; set; }
        
        [JsonPropertyName("allowed_types")]
        public List<string> AllowedTypes { get; set; }
    }
}
```

### 3. Model Management APIs

#### 3.1 Models API
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Models/Api/Models/`

```csharp
// ModelListResponse.cs
public class ModelListResponse
{
    [JsonPropertyName("data")]
    public List<Model> Data { get; set; }
}

// Model.cs
public class Model
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("pricing")]
    public PricingInfo Pricing { get; set; }
    
    [JsonPropertyName("context_length")]
    public int ContextLength { get; set; }
    
    [JsonPropertyName("architecture")]
    public ArchitectureInfo Architecture { get; set; }
    
    [JsonPropertyName("top_provider")]
    public TopProviderInfo TopProvider { get; set; }
    
    [JsonPropertyName("per_request_limits")]
    public RequestLimits PerRequestLimits { get; set; }
}

// PricingInfo.cs
public class PricingInfo
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } // Cost per token
    
    [JsonPropertyName("completion")]
    public string Completion { get; set; }
    
    [JsonPropertyName("image")]
    public string Image { get; set; }
    
    [JsonPropertyName("request")]
    public string Request { get; set; }
}

// ArchitectureInfo.cs
public class ArchitectureInfo
{
    [JsonPropertyName("modality")]
    public string Modality { get; set; } // text, multimodal
    
    [JsonPropertyName("tokenizer")]
    public string Tokenizer { get; set; }
    
    [JsonPropertyName("instruct_type")]
    public string InstructType { get; set; }
}

// TopProviderInfo.cs
public class TopProviderInfo
{
    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
    
    [JsonPropertyName("is_moderated")]
    public bool? IsModerated { get; set; }
}
```

### 4. HTTP Client Implementation

#### 4.1 HttpClientAdapter
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Http/HttpClientAdapter.cs`

```csharp
public class HttpClientAdapter : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<HttpClientAdapter> _logger;
    
    public HttpClientAdapter(OpenRouterOptions options, ILogger<HttpClientAdapter> logger)
    {
        _options = options;
        _logger = logger;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
        
        // Set default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", options.HttpReferer);
        _httpClient.DefaultRequestHeaders.Add("X-Title", options.XTitle);
        
        // App attribution headers
        if (!string.IsNullOrEmpty(options.AppName))
        {
            _httpClient.DefaultRequestHeaders.Add("X-App-Name", options.AppName);
            _httpClient.DefaultRequestHeaders.Add("X-App-Version", options.AppVersion);
        }
    }
    
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        var json = Json.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await ExecuteWithRetryAsync(async () => 
            await _httpClient.PostAsync(endpoint, content));
        
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return Json.Deserialize<TResponse>(responseJson);
    }
    
    public async IAsyncEnumerable<T> StreamAsync<TRequest, T>(string endpoint, TRequest request)
    {
        var json = Json.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };
        requestMessage.Headers.Add("Accept", "text/event-stream");
        
        using var response = await _httpClient.SendAsync(
            requestMessage, 
            HttpCompletionOption.ResponseHeadersRead);
        
        response.EnsureSuccessStatusCode();
        
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        
        var sseParser = new SseParser();
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var events = sseParser.ParseLine(line);
            
            foreach (var sseEvent in events)
            {
                if (sseEvent.Data == "[DONE]")
                    yield break;
                
                var chunk = Json.Deserialize<T>(sseEvent.Data);
                yield return chunk;
            }
        }
    }
    
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation)
    {
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => 
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode >= HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms", 
                        retryCount, 
                        timespan.TotalMilliseconds);
                });
        
        return await retryPolicy.ExecuteAsync(operation);
    }
}
```

#### 4.2 SSE Stream Support
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Http/SseStream.cs`

```csharp
public class SseParser
{
    private readonly StringBuilder _buffer = new();
    private SseEvent _currentEvent = new();
    
    public IEnumerable<SseEvent> ParseLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            if (!string.IsNullOrEmpty(_currentEvent.Data))
            {
                yield return _currentEvent;
                _currentEvent = new SseEvent();
            }
            yield break;
        }
        
        if (line.StartsWith("data: "))
        {
            _currentEvent.Data = line.Substring(6);
        }
        else if (line.StartsWith("event: "))
        {
            _currentEvent.Event = line.Substring(7);
        }
        else if (line.StartsWith("id: "))
        {
            _currentEvent.Id = line.Substring(4);
        }
        else if (line.StartsWith("retry: "))
        {
            if (int.TryParse(line.Substring(7), out var retry))
                _currentEvent.Retry = retry;
        }
    }
}

public class SseEvent
{
    public string Id { get; set; }
    public string Event { get; set; }
    public string Data { get; set; }
    public int? Retry { get; set; }
}
```

### 5. Service Layer Implementation

#### 5.1 ChatCompletionsService
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Services/ChatCompletionsService.cs`

```csharp
public class ChatCompletionsService
{
    private readonly HttpClientAdapter _httpClient;
    private const string ENDPOINT = "/api/v1/chat/completions";
    
    public async Task<ChatCompletionResponse> CreateCompletionAsync(
        ChatCompletionRequest request)
    {
        return await _httpClient.PostAsync<ChatCompletionRequest, ChatCompletionResponse>(
            ENDPOINT, request);
    }
    
    public IAsyncEnumerable<ChatCompletionChunk> CreateCompletionStreamAsync(
        ChatCompletionRequest request)
    {
        request.Stream = true;
        return _httpClient.StreamAsync<ChatCompletionRequest, ChatCompletionChunk>(
            ENDPOINT, request);
    }
}
```

#### 5.2 ModelsService
```csharp
public class ModelsService
{
    private readonly HttpClientAdapter _httpClient;
    private const string ENDPOINT = "/api/v1/models";
    
    public async Task<ModelListResponse> GetModelsAsync()
    {
        return await _httpClient.GetAsync<ModelListResponse>(ENDPOINT);
    }
    
    public async Task<Model> GetModelAsync(string modelId)
    {
        return await _httpClient.GetAsync<Model>($"{ENDPOINT}/{modelId}");
    }
}
```

#### 5.3 CreditsService
```csharp
public class CreditsService
{
    private readonly HttpClientAdapter _httpClient;
    private const string ENDPOINT = "/api/v1/auth/credits";
    
    public async Task<CreditsResponse> GetCreditsAsync()
    {
        return await _httpClient.GetAsync<CreditsResponse>(ENDPOINT);
    }
}

public class CreditsResponse
{
    [JsonPropertyName("total_credits")]
    public double TotalCredits { get; set; }
    
    [JsonPropertyName("used_credits")]
    public double UsedCredits { get; set; }
    
    [JsonPropertyName("remaining_credits")]
    public double RemainingCredits { get; set; }
}
```

### 6. Client Configuration

#### 6.1 OpenRouterOptions
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/OpenRouterOptions.cs`

```csharp
public class OpenRouterOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://openrouter.ai";
    public int TimeoutSeconds { get; set; } = 120;
    
    // App attribution
    public string AppName { get; set; } = "OrchestratorChat";
    public string AppVersion { get; set; } = "1.0.0";
    public string HttpReferer { get; set; } = "https://github.com/OrchestratorChat";
    public string XTitle { get; set; } = "OrchestratorChat Saturn";
    
    // Rate limiting
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    
    // Default model settings - Updated to match SaturnFork
    public string DefaultModel { get; set; } = "anthropic/claude-sonnet-4";
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 4096;
}
```

### 7. JSON Serialization

#### 7.1 Custom Converters
**Location**: `src/OrchestratorChat.Saturn/OpenRouter/Serialization/Json.cs`

```csharp
public static class Json
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new ContentConverter(),
            new JsonStringEnumConverter(),
            new ToolChoiceConverter()
        }
    };
    
    public static string Serialize<T>(T value) => 
        JsonSerializer.Serialize(value, _options);
    
    public static T Deserialize<T>(string json) => 
        JsonSerializer.Deserialize<T>(json, _options);
}

public class ContentConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, 
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var parts = new List<ContentPart>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var element = JsonDocument.ParseValue(ref reader).RootElement;
                var type = element.GetProperty("type").GetString();
                
                ContentPart part = type switch
                {
                    "text" => element.Deserialize<TextContentPart>(options),
                    "image_url" => element.Deserialize<ImageUrlContentPart>(options),
                    "input_audio" => element.Deserialize<InputAudioContentPart>(options),
                    _ => throw new JsonException($"Unknown content type: {type}")
                };
                
                parts.Add(part);
            }
            return parts;
        }
        
        throw new JsonException("Invalid content format");
    }
    
    public override void Write(Utf8JsonWriter writer, object value, 
        JsonSerializerOptions options)
    {
        if (value is string str)
        {
            writer.WriteStringValue(str);
        }
        else if (value is List<ContentPart> parts)
        {
            JsonSerializer.Serialize(writer, parts, options);
        }
        else
        {
            throw new JsonException("Invalid content type");
        }
    }
}
```

## Implementation Priority

### Phase 1: Core Models (Week 1)
1. Implement base API models
2. Create request/response structures
3. Add JSON serialization

### Phase 2: HTTP Client (Week 1)
1. Implement HttpClientAdapter
2. Add SSE streaming support
3. Implement retry logic

### Phase 3: Services (Week 2)
1. Create ChatCompletionsService
2. Add ModelsService
3. Implement streaming service

### Phase 4: Integration (Week 2)
1. Wire up with provider system
2. Add error handling
3. Create comprehensive tests

## Testing Requirements

### Unit Tests
- Model serialization/deserialization
- Content converter logic
- SSE parsing
- Retry policy

### Integration Tests
- API communication with mock server
- Streaming response handling
- Error scenarios
- Rate limiting

## Dependencies to Add

```xml
<PackageReference Include="Polly" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

## Validation Checklist

- [ ] All API models implemented
- [ ] HTTP client with retry logic
- [ ] SSE streaming support
- [ ] Service layer complete
- [ ] JSON serialization working
- [ ] Error handling comprehensive
- [ ] Tests passing