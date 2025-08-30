namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Represents token usage statistics for a request/response
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Number of input tokens used
    /// </summary>
    public int InputTokens { get; set; }
    
    /// <summary>
    /// Number of output tokens generated
    /// </summary>
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// Total number of tokens used (input + output)
    /// </summary>
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// Estimated cost for the token usage
    /// </summary>
    public decimal EstimatedCost { get; set; }
}