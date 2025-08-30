using System.Text.Json.Serialization;

namespace OrchestratorChat.Saturn.Providers.OpenRouter.Models;

// Chat Completion Request
public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();
    
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
    public List<ToolDefinition>? Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public ToolChoice? ToolChoice { get; set; }
    
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }
    
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }
    
    [JsonPropertyName("provider")]
    public ProviderPreferences? Provider { get; set; }
    
    [JsonPropertyName("transforms")]
    public List<string>? Transforms { get; set; }
    
    [JsonPropertyName("route")]
    public ModelRouting? Route { get; set; }
}

// Chat Completion Response
public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }
}

// Streaming Response
public class StreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<StreamingChoice> Choices { get; set; } = new();
}

// Message Structure
public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // system, user, assistant, tool
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
    
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
}

// Tool Definition
public class ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public ToolFunction Function { get; set; } = new();
}

public class ToolFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("parameters")]
    public object? Parameters { get; set; } // JSON Schema
}

// Tool Call
public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
    
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty; // JSON string
    }
}

// Tool Choice
public class ToolChoice
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "auto"; // auto, none, required, function
    
    [JsonPropertyName("function")]
    public FunctionChoice? Function { get; set; }
    
    public class FunctionChoice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

// Choice
public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public Message Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; } // stop, length, tool_calls, etc
}

// Streaming Choice
public class StreamingChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("delta")]
    public Delta Delta { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class Delta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

// Usage Information
public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    
    [JsonPropertyName("prompt_tokens_details")]
    public TokenDetails? PromptTokensDetails { get; set; }
    
    [JsonPropertyName("completion_tokens_details")]
    public TokenDetails? CompletionTokensDetails { get; set; }
    
    public class TokenDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int? CachedTokens { get; set; }
        
        [JsonPropertyName("reasoning_tokens")]
        public int? ReasoningTokens { get; set; }
    }
}

// Model Information
public class ModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("pricing")]
    public PricingInfo? Pricing { get; set; }
    
    [JsonPropertyName("context_length")]
    public int ContextLength { get; set; }
    
    [JsonPropertyName("architecture")]
    public ArchitectureInfo? Architecture { get; set; }
    
    [JsonPropertyName("top_provider")]
    public TopProviderInfo? TopProvider { get; set; }
    
    [JsonPropertyName("per_request_limits")]
    public RequestLimits? PerRequestLimits { get; set; }
}

public class PricingInfo
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; } // Cost per token
    
    [JsonPropertyName("completion")]
    public string? Completion { get; set; }
    
    [JsonPropertyName("image")]
    public string? Image { get; set; }
    
    [JsonPropertyName("request")]
    public string? Request { get; set; }
}

public class ArchitectureInfo
{
    [JsonPropertyName("modality")]
    public string? Modality { get; set; } // text, multimodal
    
    [JsonPropertyName("tokenizer")]
    public string? Tokenizer { get; set; }
    
    [JsonPropertyName("instruct_type")]
    public string? InstructType { get; set; }
}

public class TopProviderInfo
{
    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
    
    [JsonPropertyName("is_moderated")]
    public bool? IsModerated { get; set; }
}

public class RequestLimits
{
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    
    [JsonPropertyName("max_images")]
    public int? MaxImages { get; set; }
}

// Provider Preferences
public class ProviderPreferences
{
    [JsonPropertyName("allow_fallbacks")]
    public bool? AllowFallbacks { get; set; }
    
    [JsonPropertyName("require_parameters")]
    public bool? RequireParameters { get; set; }
    
    [JsonPropertyName("data_collection")]
    public string? DataCollection { get; set; } // allow, deny
    
    [JsonPropertyName("order")]
    public List<string>? Order { get; set; }
    
    [JsonPropertyName("ignore")]
    public List<string>? Ignore { get; set; }
}

// Model Routing
public class ModelRouting
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }
}

// Response Format
public class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text"; // text, json_object, json_schema
    
    [JsonPropertyName("json_schema")]
    public JsonSchema? Schema { get; set; }
    
    public class JsonSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("strict")]
        public bool? Strict { get; set; }
        
        [JsonPropertyName("schema")]
        public object? Schema { get; set; }
    }
}

// Model List Response
public class ModelListResponse
{
    [JsonPropertyName("data")]
    public List<ModelInfo> Data { get; set; } = new();
}