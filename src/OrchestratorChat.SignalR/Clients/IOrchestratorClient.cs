using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.SignalR.Contracts.Responses;

namespace OrchestratorChat.SignalR.Clients
{
    /// <summary>
    /// Client methods for orchestrator hub
    /// </summary>
    public interface IOrchestratorClient
    {
        /// <summary>
        /// Notifies client of successful connection
        /// </summary>
        /// <param name="info">Connection information</param>
        Task Connected(ConnectionInfo info);

        /// <summary>
        /// Notifies client that a session has been created
        /// </summary>
        /// <param name="session">The created session</param>
        Task SessionCreated(Session session);

        /// <summary>
        /// Notifies client that they have joined a session
        /// </summary>
        /// <param name="session">The joined session</param>
        Task SessionJoined(Session session);

        /// <summary>
        /// Notifies client of a new orchestration plan
        /// </summary>
        /// <param name="plan">The orchestration plan</param>
        Task OrchestrationPlanCreated(OrchestrationPlan plan);

        /// <summary>
        /// Notifies client of orchestration progress
        /// </summary>
        /// <param name="progress">Progress update</param>
        Task OrchestrationProgress(OrchestrationProgress progress);

        /// <summary>
        /// Notifies client that orchestration is complete
        /// </summary>
        /// <param name="result">The orchestration result</param>
        Task OrchestrationCompleted(OrchestrationResult result);

        /// <summary>
        /// Notifies client of an error
        /// </summary>
        /// <param name="error">Error details</param>
        Task ReceiveError(ErrorResponse error);
    }
}