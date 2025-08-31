using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Approvals;
using OrchestratorChat.Core.Decisions;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Handoffs;
using OrchestratorChat.Core.Messages;
using OrchestratorChat.Core.Tools;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Implementation of orchestration functionality for coordinating multiple agents
/// </summary>
public class Orchestrator : IOrchestrator
{
    private readonly IAgentFactory _agentFactory;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IEventBus _eventBus;
    private readonly IDecisionHandler _decisionHandler;
    private readonly IApprovalHandler _approvalHandler;
    private readonly IHandoffHandler _handoffHandler;
    private readonly ILogger<Orchestrator> _logger;
    private readonly ConcurrentDictionary<string, OrchestrationExecution> _activeExecutions;

    /// <summary>
    /// Initializes a new instance of the Orchestrator
    /// </summary>
    /// <param name="agentFactory">Factory for creating and managing agents</param>
    /// <param name="agentRegistry">Registry for managing active agent instances</param>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="decisionHandler">Handler for orchestration decisions</param>
    /// <param name="approvalHandler">Handler for approval requests</param>
    /// <param name="handoffHandler">Handler for handoff requests</param>
    /// <param name="logger">Logger for recording orchestration activities</param>
    public Orchestrator(
        IAgentFactory agentFactory,
        IAgentRegistry agentRegistry,
        IEventBus eventBus,
        IDecisionHandler decisionHandler,
        IApprovalHandler approvalHandler,
        IHandoffHandler handoffHandler,
        ILogger<Orchestrator> logger)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _decisionHandler = decisionHandler ?? throw new ArgumentNullException(nameof(decisionHandler));
        _approvalHandler = approvalHandler ?? throw new ArgumentNullException(nameof(approvalHandler));
        _handoffHandler = handoffHandler ?? throw new ArgumentNullException(nameof(handoffHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeExecutions = new ConcurrentDictionary<string, OrchestrationExecution>();
    }

    /// <summary>
    /// Creates an orchestration plan based on the request
    /// </summary>
    /// <param name="request">The orchestration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created orchestration plan</returns>
    public async Task<OrchestrationPlan> CreatePlanAsync(OrchestrationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(request.Goal))
            throw new ArgumentException("Goal cannot be empty", nameof(request));

        if (request.AvailableAgentIds.Count == 0)
            throw new ArgumentException("At least one agent must be available", nameof(request));

        _logger.LogInformation("Creating orchestration plan for goal: {Goal} with {AgentCount} agents using {Strategy} strategy", 
            request.Goal, request.AvailableAgentIds.Count, request.Strategy);

        var planId = Guid.NewGuid().ToString();
        var steps = await CreateStepsBasedOnStrategyAsync(request, cancellationToken);

        var plan = new OrchestrationPlan
        {
            Id = planId,
            Name = $"Plan for: {request.Goal}",
            Goal = request.Goal,
            Strategy = request.Strategy,
            Steps = steps,
            SharedContext = new Dictionary<string, object>
            {
                ["originalRequest"] = request,
                ["createdAt"] = DateTime.UtcNow
            },
            RequiredAgents = steps.Select(s => s.AssignedAgentId).Distinct().ToList()
        };

        _logger.LogInformation("Created orchestration plan {PlanId} with {StepCount} steps", 
            planId, steps.Count);

        return plan;
    }

    /// <summary>
    /// Executes an orchestration plan
    /// </summary>
    /// <param name="plan">The plan to execute</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the orchestration</returns>
    public async Task<OrchestrationResult> ExecutePlanAsync(
        OrchestrationPlan plan,
        IProgress<OrchestrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));

        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting execution {ExecutionId} for plan {PlanId}", 
            executionId, plan.Id);

        

        var execution = new OrchestrationExecution
        {
            Id = executionId,
            Plan = plan,
            StartTime = startTime,
            Status = OrchestrationExecutionStatus.Running
        };

        _activeExecutions.TryAdd(executionId, execution);

        try
        {
            // Publish orchestration started event
            await _eventBus.PublishAsync(new OrchestrationStartedEvent(executionId, plan));

            var result = await ExecuteBasedOnStrategyAsync(plan, execution, progress, cancellationToken);
            
            result.TotalDuration = DateTime.UtcNow - startTime;
            execution.Status = result.Success ? OrchestrationExecutionStatus.Completed : OrchestrationExecutionStatus.Failed;

            _logger.LogInformation("Execution {ExecutionId} completed {Status} in {Duration}", 
                executionId, execution.Status, result.TotalDuration);

            // Publish orchestration completed event
            await _eventBus.PublishAsync(new OrchestrationCompletedEvent(executionId, result));

            return result;
        }
        catch (OperationCanceledException)
        {
            execution.Status = OrchestrationExecutionStatus.Cancelled;
            
            _logger.LogWarning("Execution {ExecutionId} was cancelled", executionId);
            
            return new OrchestrationResult
            {
                Success = false,
                FinalOutput = "Orchestration was cancelled",
                TotalDuration = DateTime.UtcNow - startTime,
                FinalContext = plan.SharedContext
            };
        }
        catch (Exception ex)
        {
            execution.Status = OrchestrationExecutionStatus.Failed;
            
            _logger.LogError(ex, "Execution {ExecutionId} failed: {ErrorMessage}", 
                executionId, ex.Message);
            
            return new OrchestrationResult
            {
                Success = false,
                FinalOutput = $"Orchestration failed: {ex.Message}",
                TotalDuration = DateTime.UtcNow - startTime,
                FinalContext = plan.SharedContext
            };
        }
        finally
        {
            _activeExecutions.TryRemove(executionId, out _);
        }
    }

    /// <summary>
    /// Validates that an orchestration plan can be executed
    /// </summary>
    /// <param name="plan">The plan to validate</param>
    /// <returns>True if the plan is valid and can be executed, false otherwise</returns>
    public Task<bool> ValidatePlanAsync(OrchestrationPlan plan)
    {
        if (plan == null)
            return Task.FromResult(false);

        try
        {
            // Check basic plan properties
            if (string.IsNullOrEmpty(plan.Id) || 
                string.IsNullOrEmpty(plan.Goal) || 
                plan.Steps.Count == 0)
            {
                
                return Task.FromResult(false);
            }

            // Check step dependencies
            var stepOrders = plan.Steps.Select(s => s.Order).ToHashSet();
            foreach (var step in plan.Steps)
            {
                foreach (var dependency in step.DependsOn)
                {
                    if (!int.TryParse(dependency, out var depOrder) || !stepOrders.Contains(depOrder))
                    {
                        // Plan validation failed: Invalid dependency
                        return Task.FromResult(false);
                    }
                }
            }

            // Check for circular dependencies
            if (HasCircularDependencies(plan.Steps))
            {
                
                return Task.FromResult(false);
            }

            // Check required agents
            if (plan.RequiredAgents.Count == 0)
            {
                
                return Task.FromResult(false);
            }

            
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Cancels a running orchestration execution
    /// </summary>
    /// <param name="executionId">ID of the execution to cancel</param>
    /// <returns>Task representing the async operation</returns>
    public async Task CancelExecutionAsync(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
            return;

        if (_activeExecutions.TryGetValue(executionId, out var execution))
        {
            _logger.LogWarning("Cancelling execution {ExecutionId}", executionId);
            execution.CancellationTokenSource?.Cancel();
            execution.Status = OrchestrationExecutionStatus.Cancelled;
        }
        else
        {
            _logger.LogDebug("Execution {ExecutionId} not found for cancellation", executionId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the status of a running orchestration execution
    /// </summary>
    /// <param name="executionId">ID of the execution</param>
    /// <returns>The execution status</returns>
    public async Task<object?> GetExecutionStatusAsync(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
            return null;

        if (_activeExecutions.TryGetValue(executionId, out var execution))
        {
            return execution.Status;
        }

        await Task.CompletedTask;
        return null;
    }

    private async Task<List<OrchestrationStep>> CreateStepsBasedOnStrategyAsync(
        OrchestrationRequest request, 
        CancellationToken cancellationToken)
    {
        var steps = new List<OrchestrationStep>();

        switch (request.Strategy)
        {
            case OrchestrationStrategy.Sequential:
                steps = CreateSequentialSteps(request);
                break;
            case OrchestrationStrategy.Parallel:
                steps = CreateParallelSteps(request);
                break;
            case OrchestrationStrategy.Adaptive:
                steps = await CreateAdaptiveStepsAsync(request, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unsupported orchestration strategy: {request.Strategy}", nameof(request));
        }

        return steps;
    }

    private List<OrchestrationStep> CreateSequentialSteps(OrchestrationRequest request)
    {
        var steps = new List<OrchestrationStep>();
        var agentIndex = 0;

        // Create steps that run sequentially
        for (int i = 1; i <= Math.Min(request.MaxSteps, request.AvailableAgentIds.Count); i++)
        {
            var agentId = request.AvailableAgentIds[agentIndex % request.AvailableAgentIds.Count];
            
            steps.Add(new OrchestrationStep
            {
                Order = i,
                Name = $"Sequential Step {i}",
                AgentId = agentId,
                AssignedAgentId = agentId,
                CurrentOwner = agentId,
                Task = $"Step {i}: Process part of goal '{request.Goal}'",
                Description = $"Sequential step {i} for achieving the goal",
                DependsOn = i > 1 ? new List<string> { (i - 1).ToString() } : new List<string>(),
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(5),
                ExpectedDuration = TimeSpan.FromMinutes(3),
                CanRunInParallel = false,
                Priority = 1,
                MaxRetries = 1
            });

            agentIndex++;
        }

        return steps;
    }

    private List<OrchestrationStep> CreateParallelSteps(OrchestrationRequest request)
    {
        var steps = new List<OrchestrationStep>();

        // Create steps that can run in parallel
        for (int i = 1; i <= Math.Min(request.MaxSteps, request.AvailableAgentIds.Count); i++)
        {
            var agentId = request.AvailableAgentIds[i - 1];
            
            steps.Add(new OrchestrationStep
            {
                Order = i,
                Name = $"Parallel Step {i}",
                AgentId = agentId,
                AssignedAgentId = agentId,
                CurrentOwner = agentId,
                Task = $"Parallel step {i}: Process part of goal '{request.Goal}'",
                Description = $"Parallel step {i} for achieving the goal",
                DependsOn = new List<string>(), // No dependencies for parallel execution
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(10),
                ExpectedDuration = TimeSpan.FromMinutes(5),
                CanRunInParallel = true,
                Priority = 2, // Higher priority for parallel steps
                MaxRetries = 2
            });
        }

        return steps;
    }

    private async Task<List<OrchestrationStep>> CreateAdaptiveStepsAsync(
        OrchestrationRequest request, 
        CancellationToken cancellationToken)
    {
        // For adaptive strategy, start with a basic plan and adapt based on context
        var steps = new List<OrchestrationStep>();

        // Create an initial analysis step
        steps.Add(new OrchestrationStep
        {
            Order = 1,
            Name = "Analysis Step",
            AgentId = request.AvailableAgentIds[0],
            AssignedAgentId = request.AvailableAgentIds[0],
            CurrentOwner = request.AvailableAgentIds[0],
            Task = $"Analyze goal and create detailed plan: '{request.Goal}'",
            Description = "Adaptive analysis step to understand the goal and create execution strategy",
            DependsOn = new List<string>(),
            Input = new Dictionary<string, object> { ["goal"] = request.Goal, ["availableAgents"] = request.AvailableAgentIds },
            Timeout = TimeSpan.FromMinutes(5),
            ExpectedDuration = TimeSpan.FromMinutes(2),
            CanRunInParallel = false,
            Priority = 3, // Highest priority for analysis step
            MaxRetries = 1
        });

        // Add follow-up execution steps
        for (int i = 2; i <= Math.Min(request.MaxSteps, request.AvailableAgentIds.Count + 1); i++)
        {
            var agentId = request.AvailableAgentIds[(i - 2) % request.AvailableAgentIds.Count];
            
            steps.Add(new OrchestrationStep
            {
                Order = i,
                Name = $"Adaptive Execution Step {i}",
                AgentId = agentId,
                AssignedAgentId = agentId,
                CurrentOwner = agentId,
                Task = $"Execute adaptive step {i} based on analysis",
                Description = $"Adaptive execution step {i}",
                DependsOn = new List<string> { "1" }, // Depends on analysis step
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(8),
                ExpectedDuration = TimeSpan.FromMinutes(4),
                CanRunInParallel = i > 2, // Steps after analysis can potentially run in parallel
                Priority = 2,
                MaxRetries = 2
            });
        }

        await Task.CompletedTask;
        return steps;
    }

    private async Task<OrchestrationResult> ExecuteBasedOnStrategyAsync(
        OrchestrationPlan plan,
        OrchestrationExecution execution,
        IProgress<OrchestrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new OrchestrationResult
        {
            StepResults = new List<StepResult>(),
            FinalContext = new Dictionary<string, object>(plan.SharedContext)
        };

        var startTime = DateTime.UtcNow;

        try
        {
            switch (plan.Strategy)
            {
                case OrchestrationStrategy.Sequential:
                    await ExecuteSequentiallyAsync(plan, execution, result, progress, cancellationToken);
                    break;
                case OrchestrationStrategy.Parallel:
                    await ExecuteInParallelAsync(plan, execution, result, progress, cancellationToken);
                    break;
                case OrchestrationStrategy.Adaptive:
                    await ExecuteAdaptivelyAsync(plan, execution, result, progress, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unsupported execution strategy: {plan.Strategy}");
            }

            result.Success = result.StepResults.All(r => r.Success);
            result.FinalOutput = result.Success 
                ? "Orchestration completed successfully" 
                : "Orchestration completed with errors";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FinalOutput = $"Orchestration failed: {ex.Message}";
            
        }

        return result;
    }

    private async Task ExecuteSequentiallyAsync(
        OrchestrationPlan plan,
        OrchestrationExecution execution,
        OrchestrationResult result,
        IProgress<OrchestrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var orderedSteps = plan.Steps.OrderBy(s => s.Order).ToList();

        for (int i = 0; i < orderedSteps.Count; i++)
        {
            var step = orderedSteps[i];
            
            progress?.Report(new OrchestrationProgress
            {
                CurrentStep = i + 1,
                TotalSteps = orderedSteps.Count,
                CurrentAgent = step.AssignedAgentId,
                CurrentTask = step.Task,
                PercentComplete = (double)(i) / orderedSteps.Count * 100,
                ElapsedTime = DateTime.UtcNow - execution.StartTime
            });

            var stepResult = await ExecuteStepAsync(step, plan, cancellationToken);
            result.StepResults.Add(stepResult);

            // Publish step completed event
            await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, step, stepResult));

            // If step failed and we're in sequential mode, stop execution
            if (!stepResult.Success)
            {
                
                break;
            }

            // Update shared context with step output
            if (stepResult.OutputData.Count > 0)
            {
                foreach (var kvp in stepResult.OutputData)
                {
                    result.FinalContext[$"step_{step.Order}_{kvp.Key}"] = kvp.Value;
                }
            }
        }
    }

    private async Task ExecuteInParallelAsync(
        OrchestrationPlan plan,
        OrchestrationExecution execution,
        OrchestrationResult result,
        IProgress<OrchestrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var parallelSteps = plan.Steps.Where(s => s.CanRunInParallel).ToList();
        var sequentialSteps = plan.Steps.Where(s => !s.CanRunInParallel).OrderBy(s => s.Order).ToList();

        // Execute sequential steps first
        foreach (var step in sequentialSteps)
        {
            var stepResult = await ExecuteStepAsync(step, plan, cancellationToken);
            result.StepResults.Add(stepResult);
            await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, step, stepResult));
        }

        // Execute parallel steps
        if (parallelSteps.Count > 0)
        {
            var parallelTasks = parallelSteps.Select(step => 
                ExecuteStepAsync(step, plan, cancellationToken));

            var parallelResults = await Task.WhenAll(parallelTasks);
            
            for (int i = 0; i < parallelSteps.Count; i++)
            {
                result.StepResults.Add(parallelResults[i]);
                await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, parallelSteps[i], parallelResults[i]));
            }
        }
    }

    private async Task ExecuteAdaptivelyAsync(
        OrchestrationPlan plan,
        OrchestrationExecution execution,
        OrchestrationResult result,
        IProgress<OrchestrationProgress>? progress,
        CancellationToken cancellationToken)
    {
        // For adaptive execution, start with analysis and adapt based on results
        var orderedSteps = plan.Steps.OrderBy(s => s.Order).ToList();
        
        // Execute first step (analysis)
        if (orderedSteps.Count > 0)
        {
            var analysisStep = orderedSteps[0];
            var analysisResult = await ExecuteStepAsync(analysisStep, plan, cancellationToken);
            result.StepResults.Add(analysisResult);
            await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, analysisStep, analysisResult));

            // Based on analysis result, decide how to execute remaining steps
            var remainingSteps = orderedSteps.Skip(1).ToList();
            
            if (analysisResult.Success && remainingSteps.Count > 0)
            {
                // Determine execution strategy based on analysis output
                var canRunInParallel = analysisResult.OutputData.ContainsKey("parallel") &&
                                     bool.TryParse(analysisResult.OutputData["parallel"].ToString(), out var parallel) &&
                                     parallel;

                if (canRunInParallel)
                {
                    // Execute remaining steps in parallel
                    var parallelTasks = remainingSteps.Select(step => 
                        ExecuteStepAsync(step, plan, cancellationToken));

                    var parallelResults = await Task.WhenAll(parallelTasks);
                    
                    for (int i = 0; i < remainingSteps.Count; i++)
                    {
                        result.StepResults.Add(parallelResults[i]);
                        await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, remainingSteps[i], parallelResults[i]));
                    }
                }
                else
                {
                    // Execute remaining steps sequentially
                    foreach (var step in remainingSteps)
                    {
                        var stepResult = await ExecuteStepAsync(step, plan, cancellationToken);
                        result.StepResults.Add(stepResult);
                        await _eventBus.PublishAsync(new StepCompletedEvent(execution.Id, step, stepResult));
                        
                        if (!stepResult.Success)
                            break; // Stop on failure in sequential mode
                    }
                }
            }
        }
    }

    private async Task<StepResult> ExecuteStepAsync(
        OrchestrationStep step,
        OrchestrationPlan plan,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        // Get the agent to execute the step (use CurrentOwner which may differ from AssignedAgentId after handoffs)
        var agentId = !string.IsNullOrEmpty(step.CurrentOwner) ? step.CurrentOwner : step.AssignedAgentId;
        
        try
        {
            _logger.LogDebug("Starting execution of step {StepOrder}: {Task} with agent {AgentId}", 
                step.Order, step.Task, agentId);

            // Check dependencies before execution
            if (!CheckDependencies(step, plan.SharedContext))
            {
                _logger.LogWarning("Dependencies not satisfied for step {StepOrder}", step.Order);
                return new StepResult
                {
                    StepOrder = step.Order,
                    AgentId = step.AssignedAgentId,
                    Success = false,
                    Output = string.Empty,
                    Error = "Dependencies not satisfied",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    OutputData = new Dictionary<string, object>()
                };
            }

            // Find the agent to execute the step
            var agent = await _agentRegistry.FindAsync(agentId);
            if (agent == null)
            {
                _logger.LogError("Agent {AgentId} not found for step {StepOrder}", 
                    agentId, step.Order);
                
                return new StepResult
                {
                    StepOrder = step.Order,
                    AgentId = agentId,
                    Success = false,
                    Output = string.Empty,
                    Error = $"Agent {agentId} not found",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    OutputData = new Dictionary<string, object>()
                };
            }

            // Execute the step using the actual agent
            _logger.LogDebug("Executing step {StepOrder} with agent {AgentId}", 
                step.Order, agentId);
            
            // Ensure agent is initialized
            if (agent.Status == AgentStatus.Uninitialized)
            {
                var config = new AgentConfiguration
                {
                    Name = $"Agent_{agentId}",
                    Type = agent.Type,
                    WorkingDirectory = agent.WorkingDirectory
                };
                
                var initResult = await agent.InitializeAsync(config);
                if (!initResult.Success)
                {
                    _logger.LogError("Failed to initialize agent {AgentId}: {Error}", 
                        agentId, initResult.ErrorMessage);
                    
                    return new StepResult
                    {
                        StepOrder = step.Order,
                        AgentId = agentId,
                        Success = false,
                        Output = string.Empty,
                        Error = $"Agent initialization failed: {initResult.ErrorMessage}",
                        ExecutionTime = DateTime.UtcNow - startTime,
                        OutputData = new Dictionary<string, object>()
                    };
                }
            }

            // Create message with execution context
            var executionContext = CreateExecutionContext(step, plan);
            var message = new AgentMessage
            {
                Content = step.Task,
                Role = MessageRole.User,
                AgentId = agentId,
                SessionId = plan.Id, // Use plan ID as session context
                Metadata = executionContext
            };

            // Send message to agent and get response
            AgentResponse? finalResponse = null;
            var outputBuilder = new System.Text.StringBuilder();
            
            try
            {
                // Use timeout from step configuration
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(step.Timeout);
                
                finalResponse = await agent.SendMessageAsync(message, timeoutCts.Token);
                
                if (finalResponse != null)
                {
                    outputBuilder.AppendLine(finalResponse.Content);
                    
                    // Execute any tool calls the agent requested
                    if (finalResponse.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in finalResponse.ToolCalls)
                        {
                            try
                            {
                                // Check if approval is required for this step and tool
                                if (step.RequireToolApproval && RequiresApproval(toolCall, step))
                                {
                                    var approvalResult = await RequestAndWaitForApprovalAsync(
                                        plan.Id,
                                        step.Order.ToString(),
                                        agentId,
                                        toolCall,
                                        step,
                                        timeoutCts.Token);
                                    
                                    if (!approvalResult)
                                    {
                                        outputBuilder.AppendLine($"Tool '{toolCall.ToolName}' execution not approved");
                                        continue;
                                    }
                                }
                                
                                var toolResult = await agent.ExecuteToolAsync(toolCall, timeoutCts.Token);
                                if (toolResult.Success)
                                {
                                    outputBuilder.AppendLine($"Tool '{toolCall.ToolName}' executed successfully");
                                    if (!string.IsNullOrEmpty(toolResult.Output))
                                    {
                                        outputBuilder.AppendLine(toolResult.Output);
                                    }
                                }
                                else
                                {
                                    outputBuilder.AppendLine($"Tool '{toolCall.ToolName}' failed: {toolResult.Error}");
                                }
                            }
                            catch (Exception toolEx)
                            {
                                _logger.LogWarning(toolEx, "Tool execution failed for step {StepOrder}", step.Order);
                                outputBuilder.AppendLine($"Tool '{toolCall.ToolName}' execution failed: {toolEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Re-throw if it's the main cancellation token
                throw;
            }
            catch (OperationCanceledException)
            {
                // Step timeout
                _logger.LogWarning("Step {StepOrder} timed out after {Timeout}", step.Order, step.Timeout);
                return new StepResult
                {
                    StepOrder = step.Order,
                    AgentId = agentId,
                    Success = false,
                    Output = outputBuilder.ToString(),
                    Error = $"Step execution timed out after {step.Timeout}",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    OutputData = new Dictionary<string, object>()
                };
            }
            catch (Exception agentEx)
            {
                _logger.LogError(agentEx, "Agent execution failed for step {StepOrder}", step.Order);
                return new StepResult
                {
                    StepOrder = step.Order,
                    AgentId = agentId,
                    Success = false,
                    Output = outputBuilder.ToString(),
                    Error = $"Agent execution failed: {agentEx.Message}",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    OutputData = new Dictionary<string, object>()
                };
            }

            var result = new StepResult
            {
                StepOrder = step.Order,
                AgentId = agentId,
                Success = finalResponse != null,
                Output = outputBuilder.ToString(),
                ExecutionTime = DateTime.UtcNow - startTime,
                OutputData = CreateOutputData(step, finalResponse, plan)
            };

            // Mark step as completed in shared context
            plan.SharedContext[$"step_{step.Order}_completed"] = true;
            plan.SharedContext[$"step_{step.Order}_output"] = result.Output;

            _logger.LogDebug("Step {StepOrder} completed successfully in {Duration}", 
                step.Order, result.ExecutionTime);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step {StepOrder} execution failed: {ErrorMessage}", 
                step.Order, ex.Message);
            
            return new StepResult
            {
                StepOrder = step.Order,
                AgentId = agentId,
                Success = false,
                Output = string.Empty,
                Error = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                OutputData = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Handle decision making when multiple options exist during step execution
    /// </summary>
    /// <param name="sessionId">Session ID for the decision</param>
    /// <param name="proposingAgentId">Agent ID proposing the decision</param>
    /// <param name="proposal">Description of what needs to be decided</param>
    /// <param name="options">Available options for the decision</param>
    /// <param name="teamAgentIds">List of agent IDs that should vote on the decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The committed decision with final choice</returns>
    private async Task<Decision> HandleDecisionAsync(
        string sessionId,
        string proposingAgentId,
        string proposal,
        IEnumerable<string> options,
        IEnumerable<string> teamAgentIds,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling decision for session {SessionId}: {Proposal}", sessionId, proposal);
        
        // Propose the decision
        var decision = await _decisionHandler.ProposeAsync(sessionId, proposingAgentId, proposal, options, cancellationToken);
        
        // Start deliberation
        await _decisionHandler.DeliberateAsync(decision.Id, cancellationToken);
        
        // Collect votes from team members
        var voteTasks = new List<Task>();
        foreach (var agentId in teamAgentIds)
        {
            if (agentId != proposingAgentId) // Don't ask the proposer to vote on their own proposal
            {
                voteTasks.Add(CollectVoteFromAgentAsync(decision.Id, agentId, cancellationToken));
            }
        }
        
        // Wait for all votes with timeout
        using var votingTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        votingTimeout.CancelAfter(TimeSpan.FromMinutes(5)); // 5 minute voting timeout
        
        try
        {
            await Task.WhenAll(voteTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Voting timeout reached for decision {DecisionId}", decision.Id);
        }
        
        // Get the updated decision with votes
        decision = await _decisionHandler.GetDecisionAsync(decision.Id, cancellationToken) ?? decision;
        
        // Determine winning option and commit decision
        var winningOption = DetermineWinningOption(decision);
        var rationale = $"Decision made based on team votes. Winning option: {winningOption}";
        var consequences = "Team decision will be implemented as part of orchestration step execution";
        
        var committedDecision = await _decisionHandler.CommitAsync(decision.Id, rationale, consequences, cancellationToken);
        
        _logger.LogInformation("Decision {DecisionId} committed with option: {Option}", decision.Id, winningOption);
        
        return committedDecision;
    }
    
    /// <summary>
    /// Collect a vote from a specific agent
    /// </summary>
    private async Task CollectVoteFromAgentAsync(string decisionId, string agentId, CancellationToken cancellationToken)
    {
        try
        {
            // Get the decision details
            var decision = await _decisionHandler.GetDecisionAsync(decisionId, cancellationToken);
            if (decision == null)
                return;
            
            // Get the agent
            var agent = await _agentRegistry.FindAsync(agentId);
            if (agent == null)
                return;
            
            // Create voting message
            var optionsText = string.Join(", ", decision.Options.Select((opt, idx) => $"{idx + 1}. {opt}"));
            var votingMessage = new AgentMessage
            {
                Content = $"Please vote on this decision:\n\nProposal: {decision.Proposal}\n\nOptions:\n{optionsText}\n\nRespond with just the number (1, 2, etc.) of your preferred option and optionally provide a brief reason.",
                Role = MessageRole.User,
                AgentId = agentId,
                SessionId = decision.SessionId
            };
            
            // Send message to agent with timeout
            using var voteTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            voteTimeout.CancelAfter(TimeSpan.FromMinutes(2)); // 2 minute timeout per vote
            
            var response = await agent.SendMessageAsync(votingMessage, voteTimeout.Token);
            
            if (response != null && !string.IsNullOrWhiteSpace(response.Content))
            {
                // Parse the vote response
                var (selectedOption, reason) = ParseVoteResponse(response.Content, decision.Options);
                
                if (!string.IsNullOrEmpty(selectedOption))
                {
                    await _decisionHandler.VoteAsync(decisionId, agentId, selectedOption, reason, cancellationToken);
                    _logger.LogDebug("Vote received from agent {AgentId} for decision {DecisionId}: {Option}", 
                        agentId, decisionId, selectedOption);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect vote from agent {AgentId} for decision {DecisionId}", agentId, decisionId);
        }
    }
    
    /// <summary>
    /// Parse vote response from agent to extract option and reason
    /// </summary>
    private (string? selectedOption, string? reason) ParseVoteResponse(string response, IReadOnlyList<string> options)
    {
        var lines = response.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines[0].Trim();
        
        // Try to extract option number
        if (int.TryParse(firstLine.Split(' ')[0], out var optionNumber) && 
            optionNumber >= 1 && optionNumber <= options.Count)
        {
            var selectedOption = options[optionNumber - 1];
            var reason = lines.Length > 1 ? string.Join(" ", lines.Skip(1)) : null;
            return (selectedOption, reason);
        }
        
        // Try to match option text directly
        foreach (var option in options)
        {
            if (firstLine.Contains(option, StringComparison.OrdinalIgnoreCase))
            {
                var reason = lines.Length > 1 ? string.Join(" ", lines.Skip(1)) : null;
                return (option, reason);
            }
        }
        
        return (null, null);
    }
    
    /// <summary>
    /// Determine the winning option based on votes
    /// </summary>
    private string DetermineWinningOption(Decision decision)
    {
        if (!decision.Votes.Any())
        {
            // No votes, return first option as default
            return decision.Options.FirstOrDefault() ?? "No option available";
        }
        
        // Count votes for each option
        var voteCount = decision.Votes
            .GroupBy(v => v.Option)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Return option with most votes (first in case of tie)
        return voteCount.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    /// <summary>
    /// Checks if all dependencies for a step are satisfied
    /// </summary>
    /// <param name="step">The step to check dependencies for</param>
    /// <param name="context">The orchestration context containing completed steps</param>
    /// <returns>True if all dependencies are satisfied, false otherwise</returns>
    private bool CheckDependencies(OrchestrationStep step, Dictionary<string, object> context)
    {
        if (step.DependsOn == null || step.DependsOn.Count == 0)
            return true;

        foreach (var dependency in step.DependsOn)
        {
            // Check if the dependency step has been completed
            var dependencyKey = $"step_{dependency}_completed";
            if (!context.ContainsKey(dependencyKey) || 
                !bool.TryParse(context[dependencyKey].ToString(), out var isCompleted) || 
                !isCompleted)
            {
                _logger.LogDebug("Step {StepOrder} dependency {Dependency} not satisfied", 
                    step.Order, dependency);
                return false;
            }
        }

        _logger.LogDebug("All dependencies satisfied for step {StepOrder}", step.Order);
        return true;
    }

    private bool HasCircularDependencies(List<OrchestrationStep> steps)
    {
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();

        foreach (var step in steps)
        {
            if (HasCircularDependencyRecursive(step.Order, steps, visited, recursionStack))
                return true;
        }

        return false;
    }

    private bool HasCircularDependencyRecursive(
        int stepOrder, 
        List<OrchestrationStep> steps, 
        HashSet<int> visited, 
        HashSet<int> recursionStack)
    {
        if (recursionStack.Contains(stepOrder))
            return true;

        if (visited.Contains(stepOrder))
            return false;

        visited.Add(stepOrder);
        recursionStack.Add(stepOrder);

        var step = steps.FirstOrDefault(s => s.Order == stepOrder);
        if (step != null)
        {
            foreach (var dependency in step.DependsOn)
            {
                if (int.TryParse(dependency, out var depOrder))
                {
                    if (HasCircularDependencyRecursive(depOrder, steps, visited, recursionStack))
                        return true;
                }
            }
        }

        recursionStack.Remove(stepOrder);
        return false;
    }

    /// <summary>
    /// Creates execution context for a step to pass to the agent
    /// </summary>
    /// <param name="step">The step being executed</param>
    /// <param name="plan">The orchestration plan</param>
    /// <returns>Execution context dictionary</returns>
    private Dictionary<string, object> CreateExecutionContext(OrchestrationStep step, OrchestrationPlan plan)
    {
        var context = new Dictionary<string, object>
        {
            ["stepOrder"] = step.Order,
            ["stepName"] = step.Name,
            ["stepDescription"] = step.Description,
            ["planId"] = plan.Id,
            ["planGoal"] = plan.Goal,
            ["strategy"] = plan.Strategy.ToString(),
            ["totalSteps"] = plan.Steps.Count,
            ["canRunInParallel"] = step.CanRunInParallel,
            ["timeout"] = step.Timeout.ToString(),
            ["expectedDuration"] = step.ExpectedDuration.ToString(),
            ["allowedTools"] = step.AllowedTools,
            ["requireToolApproval"] = step.RequireToolApproval,
            ["priority"] = step.Priority,
            ["maxRetries"] = step.MaxRetries
        };

        // Add step input data
        foreach (var kvp in step.Input)
        {
            context[$"input_{kvp.Key}"] = kvp.Value;
        }
        
        // Add step execution context
        foreach (var kvp in step.ExecutionContext)
        {
            context[$"context_{kvp.Key}"] = kvp.Value;
        }

        // Add shared context from previous steps
        foreach (var kvp in plan.SharedContext)
        {
            if (kvp.Key.StartsWith("step_") && kvp.Key.EndsWith("_output"))
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        // Add dependency information
        if (step.DependsOn.Count > 0)
        {
            context["dependencies"] = step.DependsOn;
            context["dependencyOutputs"] = new Dictionary<string, object>();
            
            foreach (var dependency in step.DependsOn)
            {
                var outputKey = $"step_{dependency}_output";
                if (plan.SharedContext.ContainsKey(outputKey))
                {
                    ((Dictionary<string, object>)context["dependencyOutputs"])[dependency] = plan.SharedContext[outputKey];
                }
            }
        }

        return context;
    }

    /// <summary>
    /// Creates output data from step execution results
    /// </summary>
    /// <param name="step">The executed step</param>
    /// <param name="response">The agent response</param>
    /// <param name="plan">The orchestration plan</param>
    /// <returns>Output data dictionary</returns>
    private Dictionary<string, object> CreateOutputData(OrchestrationStep step, AgentResponse? response, OrchestrationPlan plan)
    {
        var outputData = new Dictionary<string, object>
        {
            ["stepOrder"] = step.Order,
            ["agentId"] = !string.IsNullOrEmpty(step.CurrentOwner) ? step.CurrentOwner : step.AssignedAgentId,
            ["task"] = step.Task,
            ["timestamp"] = DateTime.UtcNow,
            ["success"] = response != null
        };

        if (response != null)
        {
            outputData["responseId"] = response.Id;
            outputData["responseType"] = response.Type.ToString();
            outputData["isComplete"] = response.IsComplete;
            
            // Add token usage if available
            if (response.Usage.TotalTokens > 0)
            {
                outputData["tokenUsage"] = new Dictionary<string, object>
                {
                    ["totalTokens"] = response.Usage.TotalTokens,
                    ["inputTokens"] = response.Usage.InputTokens,
                    ["outputTokens"] = response.Usage.OutputTokens
                };
            }
            
            // Add tool call information
            if (response.ToolCalls.Count > 0)
            {
                outputData["toolCalls"] = response.ToolCalls.Select(tc => new Dictionary<string, object>
                {
                    ["name"] = tc.ToolName,
                    ["id"] = tc.Id,
                    ["parameters"] = tc.Parameters
                }).ToList();
            }
            
            // Add metadata from response
            foreach (var kvp in response.Metadata)
            {
                outputData[$"response_{kvp.Key}"] = kvp.Value;
            }
        }

        return outputData;
    }

    /// <summary>
    /// Determines if a tool call requires approval based on safety checks
    /// </summary>
    /// <param name="toolCall">The tool call to evaluate</param>
    /// <param name="step">The orchestration step context</param>
    /// <returns>True if approval is required, false otherwise</returns>
    private bool RequiresApproval(ToolCall toolCall, OrchestrationStep step)
    {
        // Always require approval if step is configured for it
        if (step.RequireToolApproval)
        {
            // Check for dangerous commands
            if (IsDangerousCommand(toolCall))
                return true;
                
            // Check for sensitive file modifications
            if (IsSensitiveFileModification(toolCall))
                return true;
                
            // Check for large diffs (if applicable)
            if (IsLargeDiff(toolCall))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a tool call involves dangerous commands
    /// </summary>
    /// <param name="toolCall">The tool call to check</param>
    /// <returns>True if the command is dangerous</returns>
    private bool IsDangerousCommand(ToolCall toolCall)
    {
        var dangerousPatterns = new[]
        {
            "rm -rf",
            "format",
            "del /s",
            "rmdir /s",
            "shutdown",
            "reboot",
            "mkfs",
            "dd if=",
            "chmod 777",
            "chown root",
            "sudo rm",
            "sudo delete",
            "> /dev/",
            "curl.*|.*sh",
            "wget.*|.*sh"
        };

        var toolName = toolCall.ToolName.ToLowerInvariant();
        var parametersText = string.Join(" ", toolCall.Parameters.Values.Select(v => v?.ToString() ?? "")).ToLowerInvariant();

        // Check tool names that are inherently dangerous
        if (toolName.Contains("delete") || toolName.Contains("remove") || toolName.Contains("format"))
            return true;

        // Check parameters for dangerous patterns
        foreach (var pattern in dangerousPatterns)
        {
            if (parametersText.Contains(pattern.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a tool call involves sensitive file modifications
    /// </summary>
    /// <param name="toolCall">The tool call to check</param>
    /// <returns>True if the operation involves sensitive files</returns>
    private bool IsSensitiveFileModification(ToolCall toolCall)
    {
        var sensitivePaths = new[]
        {
            "/etc/",
            "/boot/",
            "/sys/",
            "/proc/",
            "C:\\Windows\\",
            "C:\\Program Files\\",
            ".ssh/",
            ".aws/",
            ".azure/",
            "credentials",
            "config",
            ".env",
            "secrets",
            "password",
            "key",
            "cert",
            "pem",
            "p12",
            "keystore"
        };

        var parametersText = string.Join(" ", toolCall.Parameters.Values.Select(v => v?.ToString() ?? "")).ToLowerInvariant();

        foreach (var sensitivePath in sensitivePaths)
        {
            if (parametersText.Contains(sensitivePath.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a tool call involves large diffs (>100 lines changed)
    /// </summary>
    /// <param name="toolCall">The tool call to check</param>
    /// <returns>True if the operation involves large changes</returns>
    private bool IsLargeDiff(ToolCall toolCall)
    {
        // Check if this is a diff-related tool and try to estimate size
        if (toolCall.ToolName.ToLowerInvariant().Contains("diff") ||
            toolCall.ToolName.ToLowerInvariant().Contains("patch") ||
            toolCall.ToolName.ToLowerInvariant().Contains("apply"))
        {
            var parametersText = string.Join(" ", toolCall.Parameters.Values.Select(v => v?.ToString() ?? ""));
            
            // Count newlines as a rough estimate of changes
            var lineCount = parametersText.Split('\n').Length;
            return lineCount > 100;
        }

        return false;
    }

    /// <summary>
    /// Requests approval and waits for response
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="stepId">The step ID</param>
    /// <param name="agentId">The requesting agent ID</param>
    /// <param name="toolCall">The tool call requiring approval</param>
    /// <param name="step">The orchestration step</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if approved, false if rejected or timed out</returns>
    private async Task<bool> RequestAndWaitForApprovalAsync(
        string sessionId,
        string stepId,
        string agentId,
        ToolCall toolCall,
        OrchestrationStep step,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine approval type based on tool call analysis
            var approvalType = DetermineApprovalType(toolCall);
            
            // Create detailed approval request
            var details = CreateApprovalDetails(toolCall, step);
            
            // Default approval timeout (configurable in the future)
            var approvalTimeout = TimeSpan.FromMinutes(5);
            
            _logger.LogInformation("Requesting approval for tool '{ToolName}' in step {StepOrder}", 
                toolCall.ToolName, step.Order);

            // Request approval
            var approval = await _approvalHandler.RequestApprovalAsync(
                sessionId,
                stepId,
                agentId,
                approvalType,
                details,
                approvalTimeout,
                cancellationToken);

            // Wait for approval response
            var approved = await _approvalHandler.WaitForApprovalAsync(
                approval.Id,
                approvalTimeout,
                cancellationToken);

            _logger.LogInformation("Approval result for tool '{ToolName}': {Result}", 
                toolCall.ToolName, approved ? "Approved" : "Rejected/Timeout");

            return approved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting approval for tool '{ToolName}' in step {StepOrder}", 
                toolCall.ToolName, step.Order);
            
            // Default to rejected on error for safety
            return false;
        }
    }

    /// <summary>
    /// Determines the approval type based on the tool call
    /// </summary>
    /// <param name="toolCall">The tool call to analyze</param>
    /// <returns>The appropriate approval type</returns>
    private ApprovalType DetermineApprovalType(ToolCall toolCall)
    {
        if (IsDangerousCommand(toolCall))
            return ApprovalType.DangerousCommand;
            
        if (IsSensitiveFileModification(toolCall))
            return ApprovalType.FileModification;
            
        return ApprovalType.ToolExecution;
    }

    /// <summary>
    /// Creates detailed description for approval request
    /// </summary>
    /// <param name="toolCall">The tool call requiring approval</param>
    /// <param name="step">The orchestration step</param>
    /// <returns>Detailed approval description</returns>
    private string CreateApprovalDetails(ToolCall toolCall, OrchestrationStep step)
    {
        var details = new System.Text.StringBuilder();
        
        details.AppendLine($"Tool Execution Request");
        details.AppendLine($"Step: {step.Name} (Order: {step.Order})");
        details.AppendLine($"Tool: {toolCall.ToolName}");
        details.AppendLine($"Agent: {step.AssignedAgentId}");
        details.AppendLine();
        
        details.AppendLine("Parameters:");
        foreach (var param in toolCall.Parameters)
        {
            var value = param.Value?.ToString() ?? "null";
            // Truncate very long values for readability
            if (value.Length > 200)
                value = value.Substring(0, 200) + "... (truncated)";
                
            details.AppendLine($"  {param.Key}: {value}");
        }
        
        details.AppendLine();
        details.AppendLine($"Reason for approval: {GetApprovalReason(toolCall)}");
        
        return details.ToString();
    }

    /// <summary>
    /// Gets the reason why approval is required
    /// </summary>
    /// <param name="toolCall">The tool call to analyze</param>
    /// <returns>Human-readable reason for approval requirement</returns>
    private string GetApprovalReason(ToolCall toolCall)
    {
        if (IsDangerousCommand(toolCall))
            return "Tool involves potentially dangerous commands that could cause system damage";
            
        if (IsSensitiveFileModification(toolCall))
            return "Tool accesses sensitive files or system directories";
            
        if (IsLargeDiff(toolCall))
            return "Tool involves large changes (>100 lines) that should be reviewed";
            
        return "Tool execution requires manual approval as configured for this step";
    }

    /// <summary>
    /// Handles handoff requests during step execution
    /// </summary>
    /// <param name="sessionId">Session ID for the handoff</param>
    /// <param name="step">The orchestration step being handed off</param>
    /// <param name="fromAgentId">Agent initiating the handoff</param>
    /// <param name="toAgentId">Agent receiving the handoff</param>
    /// <param name="reason">Reason for the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if handoff was acknowledged, false otherwise</returns>
    public async Task<bool> HandleHandoffRequestAsync(
        string sessionId,
        OrchestrationStep step,
        string fromAgentId,
        string toAgentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling handoff request for step {StepOrder} from {FromAgent} to {ToAgent}: {Reason}",
            step.Order, fromAgentId, toAgentId, reason);

        try
        {
            // Compile context for the handoff
            var context = await CompileHandoffContextAsync(step, sessionId, cancellationToken);

            // Initiate the handoff
            var handoff = await _handoffHandler.InitiateHandoffAsync(
                sessionId,
                step.Order.ToString(),
                fromAgentId,
                toAgentId,
                reason,
                context,
                cancellationToken);

            // Add handoff to step history
            step.HandoffHistory.Add(handoff);

            // Wait for handoff response with timeout
            var handoffTimeout = TimeSpan.FromMinutes(5); // Configurable timeout
            var acknowledged = await _handoffHandler.WaitForHandoffResponseAsync(
                handoff.Id,
                handoffTimeout,
                cancellationToken);

            if (acknowledged)
            {
                // Update step ownership
                step.CurrentOwner = toAgentId;
                
                _logger.LogInformation("Handoff {HandoffId} acknowledged. Step {StepOrder} ownership transferred to {ToAgent}",
                    handoff.Id, step.Order, toAgentId);
            }
            else
            {
                _logger.LogWarning("Handoff {HandoffId} was not acknowledged within timeout. Step {StepOrder} remains with {FromAgent}",
                    handoff.Id, step.Order, fromAgentId);
            }

            return acknowledged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle handoff request for step {StepOrder}: {Error}",
                step.Order, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Compiles context information for a handoff
    /// </summary>
    /// <param name="step">The orchestration step being handed off</param>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compiled handoff context</returns>
    private async Task<HandoffContext> CompileHandoffContextAsync(
        OrchestrationStep step,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var context = new HandoffContext
        {
            CurrentProgress = $"Step {step.Order}: {step.Name}",
            Notes = $"Task: {step.Task}\nDescription: {step.Description}",
            Dependencies = step.DependsOn.ToList()
        };

        try
        {
            // TODO: Add file modification tracking when available
            // This would require integration with file system monitoring or tool result tracking
            context.FilesModified = new List<string>();

            // Add execution context data
            foreach (var kvp in step.ExecutionContext)
            {
                context.AdditionalData[$"context_{kvp.Key}"] = kvp.Value;
            }

            // Add input data
            foreach (var kvp in step.Input)
            {
                context.AdditionalData[$"input_{kvp.Key}"] = kvp.Value;
            }

            // Add step metadata
            context.AdditionalData["stepOrder"] = step.Order;
            context.AdditionalData["stepName"] = step.Name;
            context.AdditionalData["originalAssignedAgent"] = step.AssignedAgentId;
            context.AdditionalData["currentOwner"] = step.CurrentOwner;
            context.AdditionalData["canRunInParallel"] = step.CanRunInParallel;
            context.AdditionalData["timeout"] = step.Timeout.ToString();
            context.AdditionalData["priority"] = step.Priority;
            context.AdditionalData["allowedTools"] = step.AllowedTools;
            context.AdditionalData["requireToolApproval"] = step.RequireToolApproval;

            // Add handoff history summary
            if (step.HandoffHistory.Count > 0)
            {
                context.AdditionalData["previousHandoffs"] = step.HandoffHistory.Count;
                context.AdditionalData["handoffHistory"] = step.HandoffHistory
                    .Select(h => $"{h.FromAgentId} -> {h.ToAgentId}: {h.Reason}")
                    .ToList();
            }

            await Task.CompletedTask; // Placeholder for async operations if needed
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compile complete handoff context for step {StepOrder}: {Error}",
                step.Order, ex.Message);
        }

        return context;
    }

    /// <summary>
    /// Represents a running orchestration execution
    /// </summary>
    private class OrchestrationExecution
    {
        public string Id { get; set; } = string.Empty;
        public OrchestrationPlan Plan { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public OrchestrationExecutionStatus Status { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; } = new();
    }

    /// <summary>
    /// Status of an orchestration execution
    /// </summary>
    private enum OrchestrationExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Cancelled
    }
}