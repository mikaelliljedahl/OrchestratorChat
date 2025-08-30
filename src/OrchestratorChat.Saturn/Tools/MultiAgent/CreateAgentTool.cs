using OrchestratorChat.Saturn.Core;
using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Providers;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Tools.MultiAgent;

/// <summary>
/// Tool for creating new agent instances for parallel tasks
/// </summary>
public class CreateAgentTool : ToolBase
{
    private readonly ISaturnCore _saturnCore;
    private readonly IAgentManager _agentManager;

    public CreateAgentTool(ISaturnCore saturnCore, IAgentManager agentManager)
    {
        _saturnCore = saturnCore ?? throw new ArgumentNullException(nameof(saturnCore));
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
    }

    public override string Name => "create_agent";
    public override string Description => "Create new agent instances with specific configurations for parallel task execution";
    public override bool RequiresApproval => false;

    public override List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Name = "agent_name",
            Type = "string",
            Description = "Name for the new agent",
            Required = true
        },
        new ToolParameter
        {
            Name = "task",
            Type = "string", 
            Description = "Initial task description for the agent",
            Required = true
        },
        new ToolParameter
        {
            Name = "model",
            Type = "string",
            Description = "Model to use for the agent (e.g., claude-3-sonnet, gpt-4)",
            Required = false
        },
        new ToolParameter
        {
            Name = "tools",
            Type = "array",
            Description = "List of tool names to enable for the agent",
            Required = false
        },
        new ToolParameter
        {
            Name = "max_iterations",
            Type = "integer",
            Description = "Maximum number of iterations before agent stops",
            Required = false
        },
        new ToolParameter
        {
            Name = "context",
            Type = "object",
            Description = "Additional context data for the agent",
            Required = false
        },
        new ToolParameter
        {
            Name = "provider_type",
            Type = "string",
            Description = "Provider type (OpenRouter, Anthropic)",
            Required = false
        },
        new ToolParameter
        {
            Name = "temperature",
            Type = "number",
            Description = "Temperature setting for the model (0.0-1.0)",
            Required = false
        }
    };

    protected override async Task<ToolExecutionResult> ExecuteInternalAsync(ToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            var agentName = call.Parameters.GetValueOrDefault("agent_name")?.ToString() ?? "SubAgent";
            var task = call.Parameters.GetValueOrDefault("task")?.ToString() ?? "";
            var model = call.Parameters.GetValueOrDefault("model")?.ToString() ?? "claude-3-sonnet";
            var maxIterations = Convert.ToInt32(call.Parameters.GetValueOrDefault("max_iterations") ?? 10);
            var providerType = Enum.TryParse<ProviderType>(call.Parameters.GetValueOrDefault("provider_type")?.ToString() ?? "OpenRouter", true, out var pt) ? pt : ProviderType.OpenRouter;
            var temperature = Convert.ToDouble(call.Parameters.GetValueOrDefault("temperature") ?? 0.7);

            // Parse tools array
            var tools = new List<string>();
            if (call.Parameters.TryGetValue("tools", out var toolsObj) && toolsObj is JsonElement toolsElement && toolsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var tool in toolsElement.EnumerateArray())
                {
                    if (tool.ValueKind == JsonValueKind.String)
                    {
                        tools.Add(tool.GetString() ?? "");
                    }
                }
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

            // Check resource limits
            var currentAgents = await _agentManager.GetAllAgentsAsync();
            if (currentAgents.Count >= 5) // Max concurrent agents from config
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = "Maximum number of concurrent agents (5) reached"
                };
            }

            // Create agent configuration
            var agentConfig = new SaturnAgentConfiguration
            {
                Model = model,
                Temperature = temperature,
                MaxTokens = 4096,
                EnableTools = true,
                ToolNames = tools,
                RequireApproval = false,
                ProviderType = providerType,
                SystemPrompt = $"You are an assistant agent named '{agentName}'. Your task is: {task}"
            };

            // Create provider
            var provider = await _saturnCore.CreateProviderAsync(providerType, new Dictionary<string, object>());

            // Create agent
            var agent = await _saturnCore.CreateAgentAsync(provider, agentConfig);
            agent.Name = agentName;

            // Register with agent manager
            // Note: The specific registration method will depend on the IAgentManager implementation
            // For now, we track it in the manager's internal state

            // Prepare initial message
            var initialMessage = new AgentMessage
            {
                Content = task,
                Role = MessageRole.User,
                SessionId = Guid.NewGuid().ToString()
            };

            // Start the agent with the initial task (don't await - fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var response in await agent.ProcessMessageAsync(initialMessage, cancellationToken))
                    {
                        // Agent is processing the task
                        if (response.IsComplete)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the tool
                    Console.WriteLine($"Agent {agent.Id} encountered error: {ex.Message}");
                }
            }, cancellationToken);

            return new ToolExecutionResult
            {
                Success = true,
                Output = JsonSerializer.Serialize(new
                {
                    agent_id = agent.Id,
                    agent_name = agentName,
                    status = agent.Status.ToString(),
                    model = model,
                    tools = tools,
                    message = "Agent created and started successfully"
                }),
                Metadata = new Dictionary<string, object>
                {
                    ["agent_id"] = agent.Id,
                    ["agent_name"] = agentName,
                    ["task"] = task,
                    ["context"] = context
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = $"Failed to create agent: {ex.Message}"
            };
        }
    }
}