namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Represents a request to execute a tool with specific parameters
/// </summary>
public class ToolRequest
{
    /// <summary>
    /// The tool call information
    /// </summary>
    public ToolCall ToolCall { get; init; } = new();
    
    /// <summary>
    /// Context for the tool execution
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// Agent requesting the tool execution
    /// </summary>
    public string? AgentId { get; init; }
    
    /// <summary>
    /// Session context
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// Name of the tool to execute
    /// </summary>
    public string ToolName { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameters to pass to the tool
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
    
    /// <summary>
    /// Unique identifier for this request
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the request was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Initializes a new instance of the ToolRequest class
    /// </summary>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="parameters">Parameters to pass to the tool</param>
    public ToolRequest(string toolName, Dictionary<string, object> parameters)
    {
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        Parameters = parameters ?? new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Initializes a new instance of the ToolRequest class with empty parameters
    /// </summary>
    /// <param name="toolName">Name of the tool to execute</param>
    public ToolRequest(string toolName) : this(toolName, new Dictionary<string, object>())
    {
    }
    
    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public ToolRequest() : this(string.Empty)
    {
    }
}