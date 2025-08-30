using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Tools.MultiAgent;

/// <summary>
/// Tool for checking the status of running agents
/// </summary>
public class GetAgentStatusTool : ToolBase
{
    private readonly IAgentManager _agentManager;

    public GetAgentStatusTool(IAgentManager agentManager)
    {
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
    }

    public override string Name => "get_agent_status";
    public override string Description => "Check the status and progress of running agents";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "agent_id",
            Type = "string",
            Description = "ID of the specific agent to check (optional - if not provided, returns all agents)",
            Required = false
        },
        new ToolParameter
        {
            Name = "include_output",
            Type = "boolean",
            Description = "Whether to include recent output from the agent",
            Required = false
        },
        new ToolParameter
        {
            Name = "include_metadata",
            Type = "boolean", 
            Description = "Whether to include additional metadata about the agent",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            var agentId = call.Parameters.GetValueOrDefault("agent_id")?.ToString();
            var includeOutput = Convert.ToBoolean(call.Parameters.GetValueOrDefault("include_output") ?? false);
            var includeMetadata = Convert.ToBoolean(call.Parameters.GetValueOrDefault("include_metadata") ?? false);

            if (!string.IsNullOrEmpty(agentId))
            {
                // Get status for specific agent
                var agent = await _agentManager.GetAgentAsync(agentId);
                if (agent == null)
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        Error = $"Agent with ID '{agentId}' not found"
                    };
                }

                var agentStatus = await CreateAgentStatusInfo(agent, includeOutput, includeMetadata);

                return new ToolExecutionResult
                {
                    Success = true,
                    Output = JsonSerializer.Serialize(agentStatus),
                    Metadata = new Dictionary<string, object>
                    {
                        ["agent_id"] = agentId,
                        ["status"] = agent.Status.ToString(),
                        ["query_timestamp"] = DateTime.UtcNow
                    }
                };
            }
            else
            {
                // Get status for all agents
                var allAgents = await _agentManager.GetAllAgentsAsync();
                var agentStatuses = new List<object>();

                foreach (var agent in allAgents)
                {
                    var agentStatus = await CreateAgentStatusInfo(agent, includeOutput, includeMetadata);
                    agentStatuses.Add(agentStatus);
                }

                var result = new
                {
                    total_agents = allAgents.Count,
                    query_timestamp = DateTime.UtcNow,
                    agents = agentStatuses
                };

                return new ToolExecutionResult
                {
                    Success = true,
                    Output = JsonSerializer.Serialize(result),
                    Metadata = new Dictionary<string, object>
                    {
                        ["total_agents"] = allAgents.Count,
                        ["query_timestamp"] = DateTime.UtcNow
                    }
                };
            }
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to get agent status: {ex.Message}"
            };
        }
    }

    private async Task<object> CreateAgentStatusInfo(ISaturnAgent agent, bool includeOutput, bool includeMetadata)
    {
        var statusInfo = new Dictionary<string, object>
        {
            ["agent_id"] = agent.Id,
            ["agent_name"] = agent.Name,
            ["status"] = agent.Status.ToString(),
            ["status_description"] = GetStatusDescription(agent.Status)
        };

        if (includeMetadata)
        {
            // Add additional metadata
            statusInfo["uptime"] = "Unknown"; // Would need to track creation time
            statusInfo["last_activity"] = "Unknown"; // Would need to track last message
            
            // Try to get configuration info if available
            if (agent is ISaturnAgent saturnAgent)
            {
                // Add Saturn-specific metadata if accessible
                // Note: Specific configuration access would depend on the ISaturnAgent implementation
                statusInfo["provider_type"] = "Saturn";
                statusInfo["model"] = "Default";
            }
        }

        if (includeOutput)
        {
            // Note: Getting recent output would depend on agent implementation
            // This is a placeholder for where output retrieval would go
            statusInfo["recent_output"] = GetRecentOutput(agent);
        }

        // Add status-specific information
        switch (agent.Status)
        {
            case AgentStatus.Processing:
                statusInfo["current_task"] = "Processing message";
                break;
            case AgentStatus.ExecutingTool:
                statusInfo["current_task"] = "Executing tool";
                break;
            case AgentStatus.Error:
                statusInfo["current_task"] = "Error state";
                statusInfo["error_info"] = "Agent encountered an error";
                break;
            case AgentStatus.Shutdown:
                statusInfo["current_task"] = "Shutdown";
                break;
            case AgentStatus.Idle:
                statusInfo["current_task"] = "Idle";
                break;
        }

        return statusInfo;
    }

    private string GetStatusDescription(AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Idle => "Agent is idle and ready to accept tasks",
            AgentStatus.Processing => "Agent is currently processing a message or task", 
            AgentStatus.ExecutingTool => "Agent is executing a tool call",
            AgentStatus.Error => "Agent has encountered an error and may need intervention",
            AgentStatus.Shutdown => "Agent has been shut down and is no longer active",
            _ => $"Unknown status: {status}"
        };
    }

    private string GetRecentOutput(ISaturnAgent agent)
    {
        // This would need to be implemented based on how agents store their output
        // For now, return a placeholder
        return "Output retrieval not implemented for this agent type";
    }
}