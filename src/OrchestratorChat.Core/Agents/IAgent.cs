using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Base interface for all agent implementations
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Unique identifier for the agent instance
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name for the agent
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Agent type (Claude, Saturn, Custom, etc.)
    /// </summary>
    AgentType Type { get; }
    
    /// <summary>
    /// Current agent status
    /// </summary>
    AgentStatus Status { get; }
    
    /// <summary>
    /// Agent capabilities and metadata
    /// </summary>
    AgentCapabilities Capabilities { get; }
    
    /// <summary>
    /// Working directory for the agent
    /// </summary>
    string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Initialize the agent
    /// </summary>
    /// <param name="configuration">Agent configuration settings</param>
    /// <returns>Result of the initialization process</returns>
    Task<AgentInitializationResult> InitializeAsync(AgentConfiguration configuration);
    
    /// <summary>
    /// Send a message to the agent and get streaming responses
    /// </summary>
    /// <param name="message">Message to send to the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of agent responses</returns>
    Task<IAsyncEnumerable<AgentResponse>> SendMessageStreamAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a message to the agent and get the final response
    /// </summary>
    /// <param name="message">Message to send to the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final agent response</returns>
    Task<AgentResponse> SendMessageAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a tool/function
    /// </summary>
    /// <param name="toolCall">Tool call to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the tool execution</returns>
    Task<ToolExecutionResult> ExecuteToolAsync(
        ToolCall toolCall, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get detailed status information about the agent
    /// </summary>
    /// <returns>Agent status information</returns>
    Task<AgentStatusInfo> GetStatusAsync();
    
    /// <summary>
    /// Shutdown the agent gracefully
    /// </summary>
    /// <returns>Task representing the shutdown operation</returns>
    Task ShutdownAsync();
    
    /// <summary>
    /// Event raised when agent status changes
    /// </summary>
    event EventHandler<AgentStatusChangedEventArgs> StatusChanged;
    
    /// <summary>
    /// Event raised when agent emits output
    /// </summary>
    event EventHandler<AgentOutputEventArgs> OutputReceived;
}