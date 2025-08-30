namespace OrchestratorChat.Core.Exceptions;

/// <summary>
/// Base exception for all orchestrator-related errors
/// </summary>
public class OrchestratorException : Exception
{
    /// <summary>
    /// Error code associated with this exception
    /// </summary>
    public string ErrorCode { get; set; }
    
    /// <summary>
    /// Additional context data related to the exception
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// Initializes a new instance of the OrchestratorException class
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="errorCode">Error code associated with this exception</param>
    public OrchestratorException(string message, string errorCode = null) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    /// <summary>
    /// Initializes a new instance of the OrchestratorException class with an inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    /// <param name="errorCode">Error code associated with this exception</param>
    public OrchestratorException(string message, Exception innerException, string errorCode = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}