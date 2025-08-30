namespace OrchestratorChat.Core.Tools;

/// <summary>
/// Represents the result of a tool execution
/// </summary>
public class ToolResult
{
    /// <summary>
    /// Whether the tool execution was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Content returned by the tool (output for successful operations)
    /// </summary>
    public string? Content { get; init; }
    
    /// <summary>
    /// Error message or warning (null for successful operations without warnings)
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Unique identifier for this result
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the result was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Creates a successful tool result with content
    /// </summary>
    /// <param name="content">The content returned by the tool</param>
    /// <returns>A successful ToolResult</returns>
    public static ToolResult CreateSuccessful(string content)
    {
        return new ToolResult { Success = true, Content = content };
    }
    
    /// <summary>
    /// Creates a successful tool result with content and warning
    /// </summary>
    /// <param name="content">The content returned by the tool</param>
    /// <param name="warning">Warning message</param>
    /// <returns>A successful ToolResult with warning</returns>
    public static ToolResult CreateSuccessfulWithWarning(string content, string warning)
    {
        return new ToolResult { Success = true, Content = content, Error = warning };
    }
    
    /// <summary>
    /// Creates a failed tool result with error message
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed ToolResult</returns>
    public static ToolResult CreateFailure(string error)
    {
        return new ToolResult { Success = false, Error = error };
    }
    
    /// <summary>
    /// Creates a failed tool result with content and error message
    /// </summary>
    /// <param name="content">Partial content that was produced before failure</param>
    /// <param name="error">The error message</param>
    /// <returns>A failed ToolResult</returns>
    public static ToolResult CreateFailureWithContent(string? content, string error)
    {
        return new ToolResult { Success = false, Content = content, Error = error };
    }
}