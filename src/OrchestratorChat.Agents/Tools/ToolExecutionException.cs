using System;

namespace OrchestratorChat.Agents.Tools
{
    /// <summary>
    /// Exception thrown when a tool execution fails
    /// </summary>
    public class ToolExecutionException : Exception
    {
        public string ToolName { get; } = string.Empty;
        public string? ErrorCode { get; }

        public ToolExecutionException(string message) 
            : base(message)
        {
        }

        public ToolExecutionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public ToolExecutionException(string toolName, string message) 
            : base(message)
        {
            ToolName = toolName ?? string.Empty;
        }

        public ToolExecutionException(string toolName, string message, Exception innerException) 
            : base(message, innerException)
        {
            ToolName = toolName ?? string.Empty;
        }

        public ToolExecutionException(string toolName, string message, string errorCode) 
            : base(message)
        {
            ToolName = toolName ?? string.Empty;
            ErrorCode = errorCode;
        }
    }
}