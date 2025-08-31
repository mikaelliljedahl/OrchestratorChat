using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Exceptions;

/// <summary>
/// Exception thrown when a tool execution fails
/// </summary>
public class ToolExecutionException : OrchestratorException
{
    /// <summary>
    /// Name of the tool that failed to execute
    /// </summary>
    public string ToolName { get; set; } = string.Empty;
    
    /// <summary>
    /// The tool call that caused the exception
    /// </summary>
    public ToolCall Call { get; set; } = new();
    
    /// <summary>
    /// Initializes a new instance of the ToolExecutionException class
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="toolName">Name of the tool that failed</param>
    /// <param name="call">The tool call that caused the exception</param>
    public ToolExecutionException(string message, string toolName, ToolCall call) 
        : base(message, "TOOL_ERROR")
    {
        ToolName = toolName ?? string.Empty;
        Call = call ?? new ToolCall();
    }
    
    /// <summary>
    /// Initializes a new instance of the ToolExecutionException class with an inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="toolName">Name of the tool that failed</param>
    /// <param name="call">The tool call that caused the exception</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public ToolExecutionException(string message, string toolName, ToolCall call, Exception innerException) 
        : base(message, innerException, "TOOL_ERROR")
    {
        ToolName = toolName ?? string.Empty;
        Call = call ?? new ToolCall();
    }
}