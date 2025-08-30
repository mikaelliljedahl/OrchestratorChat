using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Exceptions;

/// <summary>
/// Exception thrown when an orchestration operation fails
/// </summary>
public class OrchestrationException : OrchestratorException
{
    /// <summary>
    /// The orchestration plan that was being executed when the exception occurred
    /// </summary>
    public OrchestrationPlan Plan { get; set; }
    
    /// <summary>
    /// The step number where the orchestration failed (0-based)
    /// </summary>
    public int FailedStep { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the OrchestrationException class
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="plan">The orchestration plan that failed</param>
    public OrchestrationException(string message, OrchestrationPlan plan) 
        : base(message, "ORCHESTRATION_ERROR")
    {
        Plan = plan;
    }
    
    /// <summary>
    /// Initializes a new instance of the OrchestrationException class with an inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="plan">The orchestration plan that failed</param>
    /// <param name="innerException">Inner exception that caused this exception</param>
    public OrchestrationException(string message, OrchestrationPlan plan, Exception innerException) 
        : base(message, innerException, "ORCHESTRATION_ERROR")
    {
        Plan = plan;
    }
    
    /// <summary>
    /// Initializes a new instance of the OrchestrationException class with failed step information
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="plan">The orchestration plan that failed</param>
    /// <param name="failedStep">The step number where the orchestration failed</param>
    public OrchestrationException(string message, OrchestrationPlan plan, int failedStep) 
        : base(message, "ORCHESTRATION_ERROR")
    {
        Plan = plan;
        FailedStep = failedStep;
    }
}