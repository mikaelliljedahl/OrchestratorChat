using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Tools.MultiAgent;

/// <summary>
/// Tool for transferring control to another agent
/// </summary>
public class HandOffToAgentTool : ToolBase
{
    private readonly IAgentManager _agentManager;

    public HandOffToAgentTool(IAgentManager agentManager)
    {
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
    }

    public override string Name => "hand_off_to_agent";
    public override string Description => "Transfer control and context to another agent for task execution";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "agent_id",
            Type = "string",
            Description = "ID of the target agent to hand off to",
            Required = true
        },
        new ToolParameter
        {
            Name = "task",
            Type = "string",
            Description = "Task description to hand off to the target agent",
            Required = true
        },
        new ToolParameter
        {
            Name = "context",
            Type = "object",
            Description = "Context data to pass to the target agent",
            Required = false
        },
        new ToolParameter
        {
            Name = "wait_for_completion",
            Type = "boolean",
            Description = "Whether to wait for the target agent to complete the task",
            Required = false
        },
        new ToolParameter
        {
            Name = "timeout_seconds",
            Type = "integer",
            Description = "Timeout in seconds if waiting for completion",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            var agentId = call.Parameters.GetValueOrDefault("agent_id")?.ToString();
            var task = call.Parameters.GetValueOrDefault("task")?.ToString() ?? "";
            var waitForCompletion = Convert.ToBoolean(call.Parameters.GetValueOrDefault("wait_for_completion") ?? false);
            var timeoutSeconds = Convert.ToInt32(call.Parameters.GetValueOrDefault("timeout_seconds") ?? 300);

            if (string.IsNullOrEmpty(agentId))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "agent_id parameter is required"
                };
            }

            // Parse context
            var context = new Dictionary<string, object>();
            if (call.Parameters.TryGetValue("context", out var contextObj) && contextObj is JsonElement contextElement && contextElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in contextElement.EnumerateObject())
                {
                    context[prop.Name] = prop.Value.ToString() ?? "";
                }
            }

            // Find the target agent
            var targetAgent = await _agentManager.GetAgentAsync(agentId);
            if (targetAgent == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Agent with ID '{agentId}' not found"
                };
            }

            // Check if target agent is available
            if (targetAgent.Status == AgentStatus.Processing || targetAgent.Status == AgentStatus.ExecutingTool)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Agent '{agentId}' is currently busy (status: {targetAgent.Status})"
                };
            }

            if (targetAgent.Status == AgentStatus.Error || targetAgent.Status == AgentStatus.Shutdown)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Agent '{agentId}' is not available (status: {targetAgent.Status})"
                };
            }

            // Create handoff message with context
            var handoffMessage = new AgentMessage
            {
                Content = task,
                Role = MessageRole.User,
                SessionId = Guid.NewGuid().ToString(),
                Metadata = context
            };

            // Add handoff information to metadata
            handoffMessage.Metadata["handoff_source"] = call.Id;
            handoffMessage.Metadata["handoff_timestamp"] = DateTime.UtcNow;

            string agentOutput = "";
            bool isComplete = false;
            Exception? processingError = null;

            // Execute the handoff
            try
            {
                var responseStream = await targetAgent.ProcessMessageAsync(handoffMessage, cancellationToken);
                
                if (waitForCompletion)
                {
                    // Wait for completion with timeout
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    await foreach (var response in responseStream.WithCancellation(combinedCts.Token))
                    {
                        agentOutput += response.Content;
                        
                        if (response.IsComplete)
                        {
                            isComplete = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Fire and forget - just start the processing
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await foreach (var response in responseStream.WithCancellation(cancellationToken))
                            {
                                if (response.IsComplete)
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail the handoff
                            Console.WriteLine($"Agent {agentId} processing error: {ex.Message}");
                        }
                    }, cancellationToken);
                    
                    isComplete = true; // Consider handoff successful
                }
            }
            catch (OperationCanceledException) when (waitForCompletion)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Timeout waiting for agent '{agentId}' to complete task (timeout: {timeoutSeconds}s)"
                };
            }
            catch (Exception ex)
            {
                processingError = ex;
            }

            if (processingError != null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"Error during handoff execution: {processingError.Message}"
                };
            }

            return new ToolExecutionResult
            {
                Success = true,
                Output = JsonSerializer.Serialize(new
                {
                    agent_id = agentId,
                    agent_name = targetAgent.Name,
                    task = task,
                    handoff_completed = isComplete,
                    waited_for_completion = waitForCompletion,
                    output = waitForCompletion ? agentOutput : "Task handed off successfully (not waiting for completion)",
                    message = waitForCompletion ? "Task handed off and completed" : "Task handed off successfully"
                }),
                Metadata = new Dictionary<string, object>
                {
                    ["target_agent_id"] = agentId,
                    ["task"] = task,
                    ["context"] = context,
                    ["waited_for_completion"] = waitForCompletion,
                    ["handoff_timestamp"] = DateTime.UtcNow,
                    ["completion_status"] = isComplete
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to hand off to agent: {ex.Message}"
            };
        }
    }
}