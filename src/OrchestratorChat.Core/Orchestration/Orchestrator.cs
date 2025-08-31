using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Exceptions;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Implementation of orchestration functionality for coordinating multiple agents
/// </summary>
public class Orchestrator : IOrchestrator
{
    private readonly IAgentFactory _agentFactory;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Orchestrator> _logger;
    private readonly ConcurrentDictionary<string, OrchestrationExecution> _activeExecutions;

    /// <summary>
    /// Initializes a new instance of the Orchestrator
    /// </summary>
    /// <param name="agentFactory">Factory for creating and managing agents</param>
    /// <param name="agentRegistry">Registry for managing active agent instances</param>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="logger">Logger for recording orchestration activities</param>
    public Orchestrator(
        IAgentFactory agentFactory,
        IAgentRegistry agentRegistry,
        IEventBus eventBus,
        ILogger<Orchestrator> logger)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
        catch (Exception ex)
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
                AgentId = agentId,
                AssignedAgentId = agentId,
                Task = $"Step {i}: Process part of goal '{request.Goal}'",
                Description = $"Sequential step {i} for achieving the goal",
                DependsOn = i > 1 ? new List<string> { (i - 1).ToString() } : new List<string>(),
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(5),
                ExpectedDuration = TimeSpan.FromMinutes(3),
                CanRunInParallel = false
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
                AgentId = agentId,
                AssignedAgentId = agentId,
                Task = $"Parallel step {i}: Process part of goal '{request.Goal}'",
                Description = $"Parallel step {i} for achieving the goal",
                DependsOn = new List<string>(), // No dependencies for parallel execution
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(10),
                ExpectedDuration = TimeSpan.FromMinutes(5),
                CanRunInParallel = true
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
            AgentId = request.AvailableAgentIds[0],
            AssignedAgentId = request.AvailableAgentIds[0],
            Task = $"Analyze goal and create detailed plan: '{request.Goal}'",
            Description = "Adaptive analysis step to understand the goal and create execution strategy",
            DependsOn = new List<string>(),
            Input = new Dictionary<string, object> { ["goal"] = request.Goal, ["availableAgents"] = request.AvailableAgentIds },
            Timeout = TimeSpan.FromMinutes(5),
            ExpectedDuration = TimeSpan.FromMinutes(2),
            CanRunInParallel = false
        });

        // Add follow-up execution steps
        for (int i = 2; i <= Math.Min(request.MaxSteps, request.AvailableAgentIds.Count + 1); i++)
        {
            var agentId = request.AvailableAgentIds[(i - 2) % request.AvailableAgentIds.Count];
            
            steps.Add(new OrchestrationStep
            {
                Order = i,
                AgentId = agentId,
                AssignedAgentId = agentId,
                Task = $"Execute adaptive step {i} based on analysis",
                Description = $"Adaptive execution step {i}",
                DependsOn = new List<string> { "1" }, // Depends on analysis step
                Input = new Dictionary<string, object> { ["goal"] = request.Goal },
                Timeout = TimeSpan.FromMinutes(8),
                ExpectedDuration = TimeSpan.FromMinutes(4),
                CanRunInParallel = i > 2 // Steps after analysis can potentially run in parallel
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
        
        try
        {
            _logger.LogDebug("Starting execution of step {StepOrder}: {Task} with agent {AgentId}", 
                step.Order, step.Task, step.AssignedAgentId);

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

            // Get the agent to execute the step
            var agent = await _agentRegistry.FindAsync(step.AssignedAgentId);
            if (agent == null)
            {
                _logger.LogError("Agent {AgentId} not found for step {StepOrder}", 
                    step.AssignedAgentId, step.Order);
                
                return new StepResult
                {
                    StepOrder = step.Order,
                    AgentId = step.AssignedAgentId,
                    Success = false,
                    Output = string.Empty,
                    Error = $"Agent {step.AssignedAgentId} not found",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    OutputData = new Dictionary<string, object>()
                };
            }

            // TODO: Execute the step using the actual agent
            // For now, simulate step execution until agent execution is implemented
            _logger.LogDebug("Executing step {StepOrder} with agent {AgentId}", 
                step.Order, step.AssignedAgentId);
            
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken); // Small delay for simulation

            var result = new StepResult
            {
                StepOrder = step.Order,
                AgentId = step.AssignedAgentId,
                Success = true,
                Output = $"Step {step.Order} completed successfully: {step.Task}",
                ExecutionTime = DateTime.UtcNow - startTime,
                OutputData = new Dictionary<string, object>
                {
                    ["stepOrder"] = step.Order,
                    ["agentId"] = step.AssignedAgentId,
                    ["task"] = step.Task,
                    ["timestamp"] = DateTime.UtcNow
                }
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
                AgentId = step.AssignedAgentId,
                Success = false,
                Output = string.Empty,
                Error = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                OutputData = new Dictionary<string, object>()
            };
        }
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