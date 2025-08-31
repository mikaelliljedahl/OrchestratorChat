# Orchestrator Test Scenarios

## Component Under Test: Orchestrator
**Location**: `src/OrchestratorChat.Core/Orchestration/Orchestrator.cs`

---

## Test Categories

### 1. Plan Creation Tests

#### Test: CreatePlanAsync_SimpleSequential_CreatesValidPlan
**Given**: OrchestrationRequest with 3 sequential steps
**When**: CreatePlanAsync is called
**Then**:
- Returns OrchestrationPlan with correct structure
- Steps have proper dependencies
- Execution order is correct
- Each step has unique ID

#### Test: CreatePlanAsync_ComplexDependencies_ResolvesCorrectly
**Given**: Request with steps having multiple dependencies
```
Step A -> Step B -> Step D
       -> Step C -> Step D
```
**When**: CreatePlanAsync is called
**Then**:
- Dependency graph correctly resolved
- Step D waits for both B and C
- No circular dependencies

#### Test: CreatePlanAsync_CircularDependency_ThrowsException
**Given**: Request where Step A depends on B, B depends on C, C depends on A
**When**: CreatePlanAsync is called
**Then**: Throws CircularDependencyException with details

#### Test: CreatePlanAsync_EmptyRequest_ReturnsEmptyPlan
**Given**: OrchestrationRequest with no steps
**When**: CreatePlanAsync is called
**Then**: Returns valid plan with empty steps list

#### Test: CreatePlanAsync_InvalidAgentIds_ThrowsException
**Given**: Request with non-existent agent IDs
**When**: CreatePlanAsync is called
**Then**: Throws AgentNotFoundException

---

### 2. Sequential Execution Tests

#### Test: ExecutePlanAsync_Sequential_ExecutesInOrder
**Given**: Plan with 3 sequential steps
**When**: ExecutePlanAsync with Sequential strategy
**Then**:
- Steps execute in exact order
- Each step completes before next starts
- Progress reported at each step
- Final result contains all outputs

#### Test: ExecutePlanAsync_SequentialWithFailure_StopsExecution
**Given**: Plan where step 2 of 3 fails
**When**: ExecutePlanAsync called
**Then**:
- Step 1 executes successfully
- Step 2 fails and logs error
- Step 3 does not execute
- Result indicates partial completion

#### Test: ExecutePlanAsync_SequentialWithRetry_RetriesFailedSteps
**Given**: Plan with retry policy, step fails first time
**When**: ExecutePlanAsync called
**Then**:
- Failed step retried per policy
- Succeeds on retry
- Execution continues

---

### 3. Parallel Execution Tests

#### Test: ExecutePlanAsync_Parallel_ExecutesConcurrently
**Given**: Plan with 3 independent steps
**When**: ExecutePlanAsync with Parallel strategy
**Then**:
- All steps start simultaneously
- Total time less than sum of individual times
- Results collected correctly

#### Test: ExecutePlanAsync_ParallelWithDependencies_RespectsOrder
**Given**: Plan where steps 1&2 are parallel, both feed into step 3
**When**: ExecutePlanAsync called
**Then**:
- Steps 1&2 execute concurrently
- Step 3 waits for both to complete
- Step 3 receives outputs from both

#### Test: ExecutePlanAsync_ParallelPartialFailure_ContinuesOthers
**Given**: 3 parallel steps, one fails
**When**: ExecutePlanAsync called
**Then**:
- Failed step logged
- Other steps continue
- Result shows partial success

#### Test: ExecutePlanAsync_ParallelConcurrencyLimit_RespectsLimit
**Given**: 10 parallel steps, concurrency limit of 3
**When**: ExecutePlanAsync called
**Then**: Maximum 3 steps execute simultaneously

---

### 4. Adaptive Strategy Tests

#### Test: ExecutePlanAsync_Adaptive_AdjustsBasedOnLoad
**Given**: Plan with adaptive strategy
**When**: System under high load
**Then**:
- Switches from parallel to sequential
- Adjusts concurrency dynamically
- Completes successfully

#### Test: ExecutePlanAsync_Adaptive_OptimizesPerformance
**Given**: Plan with mix of fast and slow steps
**When**: ExecutePlanAsync called
**Then**:
- Fast steps executed in parallel
- Slow steps may be sequential
- Overall time optimized

---

### 5. Progress Reporting Tests

#### Test: ExecutePlanAsync_WithProgress_ReportsCorrectly
**Given**: Plan with 5 steps and IProgress<OrchestrationProgress>
**When**: ExecutePlanAsync called
**Then**:
- Progress reported at 0%, 20%, 40%, 60%, 80%, 100%
- Each report has correct step info
- CurrentStepIndex accurate

#### Test: ExecutePlanAsync_WithNullProgress_ExecutesWithoutReporting
**Given**: Valid plan, null IProgress
**When**: ExecutePlanAsync called
**Then**:
- Execution completes normally
- No null reference exceptions
- Result returned correctly

---

### 6. Cancellation Tests

#### Test: CancelExecutionAsync_DuringExecution_StopsCleanly
**Given**: Long-running plan in progress
**When**: CancelExecutionAsync called
**Then**:
- Current step allowed to complete
- Subsequent steps not started
- Resources cleaned up
- CancellationEvent published

#### Test: CancelExecutionAsync_NonExistentExecution_ReturnsFalse
**Given**: No execution with given ID
**When**: CancelExecutionAsync called
**Then**:
- Returns false
- No exceptions thrown
- Warning logged

#### Test: CancelExecutionAsync_AlreadyCompleted_NoOp
**Given**: Execution already finished
**When**: CancelExecutionAsync called
**Then**:
- Returns false
- No side effects

#### Test: ExecutePlanAsync_WithCancellationToken_RespectsToken
**Given**: Plan execution with CancellationToken
**When**: Token cancelled during execution
**Then**:
- Execution stops gracefully
- OperationCanceledException thrown
- Partial results available

---

### 7. Status Tracking Tests

#### Test: GetExecutionStatusAsync_ActiveExecution_ReturnsCorrectStatus
**Given**: Execution in progress
**When**: GetExecutionStatusAsync called
**Then**:
- Returns current status
- Shows active step
- Progress percentage accurate

#### Test: GetExecutionStatusAsync_CompletedExecution_ReturnsFinalStatus
**Given**: Execution completed
**When**: GetExecutionStatusAsync called
**Then**:
- Status shows completed
- Final results available
- Duration recorded

#### Test: GetExecutionStatusAsync_NonExistent_ReturnsNull
**Given**: Invalid execution ID
**When**: GetExecutionStatusAsync called
**Then**: Returns null

---

### 8. Error Handling Tests

#### Test: ExecutePlanAsync_StepThrowsException_HandlesGracefully
**Given**: Step that throws unexpected exception
**When**: ExecutePlanAsync called
**Then**:
- Exception caught and logged
- Error details in result
- Other steps may continue (based on strategy)

#### Test: ExecutePlanAsync_AgentTimeout_HandlesTimeout
**Given**: Step with 5-second timeout, agent takes 10 seconds
**When**: ExecutePlanAsync called
**Then**:
- Step marked as timeout
- Execution continues or stops based on config
- Timeout logged

#### Test: ExecutePlanAsync_InvalidPlan_ThrowsValidationException
**Given**: Plan with null steps or invalid data
**When**: ExecutePlanAsync called
**Then**: Throws PlanValidationException before execution

---

### 9. Performance Tests

#### Test: CreatePlanAsync_LargePlan_PerformsEfficiently
**Given**: Request with 100 steps
**When**: CreatePlanAsync called
**Then**: Completes in under 100ms

#### Test: ExecutePlanAsync_ManySteps_ScalesLinearly
**Given**: Plans with 10, 50, 100 steps
**When**: ExecutePlanAsync called
**Then**: Execution time scales linearly

#### Test: ExecutePlanAsync_MemoryUsage_NoMemoryLeaks
**Given**: Repeated execution of plans
**When**: 100 executions completed
**Then**: Memory usage stable, no leaks

---

### 10. Integration Tests

#### Test: OrchestrationFlow_CompleteWorkflow_Success
**Given**: Real agents and database
**When**: Create plan -> Execute -> Monitor -> Complete
**Then**: Full workflow succeeds

#### Test: OrchestrationWithAgents_RealAgentCalls_WorksCorrectly
**Given**: Plan using actual ClaudeAgent and SaturnAgent
**When**: ExecutePlanAsync called
**Then**: Agents receive correct inputs and produce outputs

---

## Test Data Builders

```csharp
public class OrchestrationTestDataBuilder
{
    public OrchestrationRequest BuildSimpleRequest() => new()
    {
        Goal = "Test orchestration",
        Strategy = OrchestrationStrategy.Sequential,
        AvailableAgentIds = new List<string> { "agent-1", "agent-2" },
        SessionId = "session-123"
    };
    
    public OrchestrationPlan BuildSequentialPlan() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = "Sequential Plan",
        Goal = "Execute steps in order",
        Strategy = OrchestrationStrategy.Sequential,
        Steps = new List<OrchestrationStep>
        {
            new() { 
                Id = "step-1", 
                Order = 1, 
                Description = "First step",
                AssignedAgentId = "agent-1",
                DependsOn = new List<string>()
            },
            new() { 
                Id = "step-2", 
                Order = 2,
                Description = "Second step",
                AssignedAgentId = "agent-2",
                DependsOn = new List<string> { "step-1" }
            }
        }
    };
    
    public OrchestrationPlan BuildParallelPlan() => new()
    {
        // Plan with parallel steps
    };
    
    public OrchestrationPlan BuildComplexPlan() => new()
    {
        // Plan with complex dependencies
    };
}
```

---

## Mock Setup Examples

```csharp
public class OrchestratorTestBase
{
    protected Mock<IAgentFactory> AgentFactoryMock;
    protected Mock<IEventBus> EventBusMock;
    protected Mock<ILogger<Orchestrator>> LoggerMock;
    protected Mock<IAgent> AgentMock;
    
    public OrchestratorTestBase()
    {
        AgentFactoryMock = new Mock<IAgentFactory>();
        EventBusMock = new Mock<IEventBus>();
        LoggerMock = new Mock<ILogger<Orchestrator>>();
        AgentMock = new Mock<IAgent>();
        
        // Setup default agent behavior
        AgentMock.Setup(a => a.ProcessMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResponse { Success = true });
        
        AgentFactoryMock.Setup(f => f.GetAgentAsync(It.IsAny<string>()))
            .ReturnsAsync(AgentMock.Object);
    }
}
```

---

## Progress Tracking Tests

```csharp
[Fact]
public async Task ExecutePlanAsync_TracksProgress()
{
    // Arrange
    var progressReports = new List<OrchestrationProgress>();
    var progress = new Progress<OrchestrationProgress>(p => progressReports.Add(p));
    
    var plan = BuildSequentialPlan();
    var orchestrator = CreateOrchestrator();
    
    // Act
    await orchestrator.ExecutePlanAsync(plan, progress);
    
    // Assert
    progressReports.Should().HaveCount(3); // Start, Step 1, Step 2
    progressReports[0].ProgressPercentage.Should().Be(0);
    progressReports[1].ProgressPercentage.Should().Be(50);
    progressReports[2].ProgressPercentage.Should().Be(100);
}
```

---

## Concurrency Tests

```csharp
[Fact]
public async Task ExecutePlanAsync_Parallel_ExecutesConcurrently()
{
    // Arrange
    var executionTimes = new ConcurrentBag<DateTime>();
    
    AgentMock.Setup(a => a.ProcessMessageAsync(It.IsAny<AgentMessage>(), It.IsAny<CancellationToken>()))
        .Returns(async (AgentMessage m, CancellationToken ct) =>
        {
            executionTimes.Add(DateTime.UtcNow);
            await Task.Delay(100); // Simulate work
            return new AgentResponse { Success = true };
        });
    
    var plan = BuildParallelPlan();
    var orchestrator = CreateOrchestrator();
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    await orchestrator.ExecutePlanAsync(plan);
    stopwatch.Stop();
    
    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Less than sequential time
    executionTimes.Should().HaveCount(3);
    
    var timeDiffs = executionTimes.OrderBy(t => t).ToList();
    (timeDiffs[2] - timeDiffs[0]).TotalMilliseconds.Should().BeLessThan(50); // Started close together
}
```

---

## Test Coverage Matrix

| Method | Happy Path | Error Cases | Edge Cases | Concurrency | Performance |
|--------|------------|-------------|------------|-------------|-------------|
| CreatePlanAsync | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| ExecutePlanAsync (Sequential) | ✅ | ✅ | ✅ | N/A | ✅ |
| ExecutePlanAsync (Parallel) | ✅ | ✅ | ✅ | ✅ | ✅ |
| ExecutePlanAsync (Adaptive) | ✅ | ✅ | ✅ | ✅ | ✅ |
| CancelExecutionAsync | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| GetExecutionStatusAsync | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |

Legend: ✅ Required | ⚠️ Optional | N/A Not Applicable