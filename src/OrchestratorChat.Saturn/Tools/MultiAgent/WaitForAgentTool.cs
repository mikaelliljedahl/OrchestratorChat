using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Tools.MultiAgent;

/// <summary>
/// Tool for waiting for agent completion with timeout and polling
/// </summary>
public class WaitForAgentTool : ToolBase
{
    private readonly IAgentManager _agentManager;

    public WaitForAgentTool(IAgentManager agentManager)
    {
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
    }

    public override string Name => "wait_for_agent";
    public override string Description => "Wait for an agent to complete its current task with timeout and polling";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "agent_id",
            Type = "string",
            Description = "ID of the agent to wait for",
            Required = true
        },
        new ToolParameter
        {
            Name = "timeout_seconds",
            Type = "integer",
            Description = "Maximum time to wait in seconds (default: 300)",
            Required = false
        },
        new ToolParameter
        {
            Name = "poll_interval_seconds",
            Type = "integer",
            Description = "Interval between status checks in seconds (default: 5)",
            Required = false
        },
        new ToolParameter
        {
            Name = "return_output",
            Type = "boolean",
            Description = "Whether to return the agent's output when complete",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            var agentId = call.Parameters.GetValueOrDefault("agent_id")?.ToString();
            var timeoutSeconds = Convert.ToInt32(call.Parameters.GetValueOrDefault("timeout_seconds") ?? 300);
            var pollIntervalSeconds = Convert.ToInt32(call.Parameters.GetValueOrDefault("poll_interval_seconds") ?? 5);
            var returnOutput = Convert.ToBoolean(call.Parameters.GetValueOrDefault("return_output") ?? false);

            if (string.IsNullOrEmpty(agentId))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "agent_id parameter is required"
                };
            }

            if (timeoutSeconds <= 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "timeout_seconds must be greater than 0"
                };
            }

            if (pollIntervalSeconds <= 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "poll_interval_seconds must be greater than 0"
                };
            }

            // Find the agent
            var agent = await _agentManager.GetAgentAsync(agentId);
            if (agent == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Agent with ID '{agentId}' not found"
                };
            }

            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var pollInterval = TimeSpan.FromSeconds(pollIntervalSeconds);

            AgentStatus finalStatus = agent.Status;
            string statusReason = "";
            var statusHistory = new List<(DateTime timestamp, AgentStatus status)>
            {
                (startTime, agent.Status)
            };

            // Track status changes if possible
            var statusChanged = false;
            EventHandler<StatusChangedEventArgs>? statusHandler = null;
            if (agent is ISaturnAgent saturnAgent)
            {
                statusHandler = (sender, args) =>
                {
                    statusHistory.Add((DateTime.UtcNow, args.CurrentStatus));
                    statusReason = args.Reason;
                    statusChanged = true;
                };
                saturnAgent.OnStatusChanged += statusHandler;
            }

            try
            {
                // Wait for completion with polling
                while (DateTime.UtcNow - startTime < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentStatus = agent.Status;
                    finalStatus = currentStatus;

                    // Check if agent is in a terminal state
                    if (currentStatus == AgentStatus.Idle || 
                        currentStatus == AgentStatus.Error || 
                        currentStatus == AgentStatus.Shutdown)
                    {
                        break;
                    }

                    // Wait for the poll interval or until status change
                    if (statusChanged)
                    {
                        statusChanged = false;
                        continue; // Check immediately if status changed
                    }

                    await Task.Delay(pollInterval, cancellationToken);
                }

                // Check if we timed out
                var elapsed = DateTime.UtcNow - startTime;
                var timedOut = elapsed >= timeout;

                string output = "";
                if (returnOutput && !timedOut && (finalStatus == AgentStatus.Idle || finalStatus == AgentStatus.Error))
                {
                    // Try to get output from agent if possible
                    // Note: This would depend on agent implementation to store output
                    output = "Output retrieval not implemented for this agent type";
                }

                var completionResult = new
                {
                    agent_id = agentId,
                    agent_name = agent.Name,
                    final_status = finalStatus.ToString(),
                    completion_reason = statusReason,
                    elapsed_seconds = (int)elapsed.TotalSeconds,
                    timed_out = timedOut,
                    status_history = statusHistory.Select(h => new { 
                        timestamp = h.timestamp, 
                        status = h.status.ToString() 
                    }).ToList(),
                    output = returnOutput ? output : null
                };

                if (timedOut)
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"Timeout waiting for agent '{agentId}' (waited {elapsed.TotalSeconds:F1}s of {timeoutSeconds}s). Final status: {finalStatus}",
                        Output = JsonSerializer.Serialize(completionResult),
                        Metadata = new Dictionary<string, object>
                        {
                            ["agent_id"] = agentId,
                            ["timed_out"] = true,
                            ["elapsed_seconds"] = elapsed.TotalSeconds,
                            ["final_status"] = finalStatus.ToString()
                        }
                    };
                }

                var success = finalStatus == AgentStatus.Idle;
                var message = success ? 
                    $"Agent '{agentId}' completed successfully" : 
                    $"Agent '{agentId}' finished with status: {finalStatus}";

                return new ToolExecutionResult
                {
                    Success = success,
                    Output = JsonSerializer.Serialize(completionResult),
                    Error = success ? null : $"Agent finished with non-idle status: {finalStatus}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["agent_id"] = agentId,
                        ["final_status"] = finalStatus.ToString(),
                        ["elapsed_seconds"] = elapsed.TotalSeconds,
                        ["timed_out"] = false,
                        ["completion_reason"] = statusReason
                    }
                };
            }
            finally
            {
                // Clean up event handler
                if (statusHandler != null && agent is ISaturnAgent saturnAgent2)
                {
                    saturnAgent2.OnStatusChanged -= statusHandler;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = "Wait operation was cancelled"
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Error while waiting for agent: {ex.Message}"
            };
        }
    }
}