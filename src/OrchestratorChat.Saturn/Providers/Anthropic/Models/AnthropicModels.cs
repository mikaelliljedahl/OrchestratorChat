using System.Text.Json.Serialization;

namespace OrchestratorChat.Saturn.Providers.Anthropic.Models;

// Chat Completion Request
public class AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("system")]
    public string? System { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4096;
    
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
    
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }
    
    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }
    
    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; set; }
    
    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
    
    [JsonPropertyName("tools")]
    public List<AnthropicTool>? Tools { get; set; }
    
    [JsonPropertyName("tool_choice")]
    public AnthropicToolChoice? ToolChoice { get; set; }
}

// Message Structure
public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // user, assistant
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

// Tool Definition
public class AnthropicTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("input_schema")]
    public object? InputSchema { get; set; } // JSON Schema
}

// Tool Choice
public class AnthropicToolChoice
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "auto"; // auto, any, tool
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// Chat Completion Response
public class AnthropicResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public List<AnthropicContent> Content { get; set; } = new();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
    
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
    
    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

// Streaming Response
public class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // message_start, content_block_start, content_block_delta, content_block_stop, message_delta, message_stop
    
    [JsonPropertyName("index")]
    public int? Index { get; set; }
    
    [JsonPropertyName("message")]
    public AnthropicResponse? Message { get; set; }
    
    [JsonPropertyName("content_block")]
    public AnthropicContent? ContentBlock { get; set; }
    
    [JsonPropertyName("delta")]
    public AnthropicDelta? Delta { get; set; }
    
    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

// Content Block
public class AnthropicContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // text, tool_use
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("input")]
    public object? Input { get; set; }
}

// Delta for streaming
public class AnthropicDelta
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("partial_json")]
    public string? PartialJson { get; set; }
    
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
    
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
    
    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

// Usage Information
public class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
    
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
    
    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }
    
    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }
}

// Error Response
public class AnthropicError
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("error")]
    public AnthropicErrorDetail Error { get; set; } = new();
}

public class AnthropicErrorDetail
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}