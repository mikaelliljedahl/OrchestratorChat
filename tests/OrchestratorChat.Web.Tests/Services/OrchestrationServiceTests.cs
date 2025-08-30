using NSubstitute;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

public class OrchestrationServiceTests
{
    private readonly IOrchestrator _mockOrchestrator;
    private readonly OrchestrationService _service;

    public OrchestrationServiceTests()
    {
        _mockOrchestrator = Substitute.For<IOrchestrator>();
        _service = new OrchestrationService(_mockOrchestrator);
    }

    [Fact]
    public async Task CreatePlanAsync_Should_Create_Plan_Successfully()
    {
        // Arrange
        var request = new OrchestrationRequest
        {
            Goal = "Test orchestration goal",
            AvailableAgentIds = new List<string> { "agent-1", "agent-2" },
            Strategy = OrchestrationStrategy.Sequential,
            Constraints = new Dictionary<string, object>()
        };

        var expectedPlan = new OrchestrationPlan
        {
            Id = "plan-1",
            Goal = request.Goal,
            Strategy = request.Strategy,
            Steps = new List<OrchestrationStep>
            {
                new OrchestrationStep { Name = "Step 1", AgentId = "agent-1" }
            },
            RequiredAgents = new List<string> { "agent-1", "agent-2" },
            SharedContext = new Dictionary<string, object>()
        };

        _mockOrchestrator.CreatePlanAsync(request)
            .Returns(Task.FromResult(expectedPlan));

        // Act
        var result = await _service.CreatePlanAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("plan-1", result.Id);
        Assert.Equal(request.Goal, result.Goal);
        Assert.Single(result.Steps);
        
        // Verify orchestrator was called
        await _mockOrchestrator.Received(1).CreatePlanAsync(request);
    }

    [Fact]
    public async Task CreatePlanAsync_Should_Add_Plan_To_Recent_Plans()
    {
        // Arrange
        var request = new OrchestrationRequest
        {
            Goal = "Test plan storage",
            AvailableAgentIds = new List<string> { "agent-1" },
            Strategy = OrchestrationStrategy.Sequential,
            Constraints = new Dictionary<string, object>()
        };

        var plan = new OrchestrationPlan
        {
            Id = "storage-plan",
            Goal = request.Goal,
            Strategy = request.Strategy,
            Steps = new List<OrchestrationStep>(),
            RequiredAgents = request.AvailableAgentIds,
            SharedContext = new Dictionary<string, object>()
        };

        _mockOrchestrator.CreatePlanAsync(request)
            .Returns(Task.FromResult(plan));

        // Act
        await _service.CreatePlanAsync(request);
        var recentPlans = await _service.GetRecentPlansAsync(10);

        // Assert
        Assert.Single(recentPlans);
        Assert.Equal("storage-plan", recentPlans[0].Id);
    }

    [Fact]
    public async Task CreatePlanAsync_Should_Maintain_Plan_Limit()
    {
        // Arrange
        var baseRequest = new OrchestrationRequest
        {
            Goal = "Base goal",
            AvailableAgentIds = new List<string> { "agent-1" },
            Strategy = OrchestrationStrategy.Sequential,
            Constraints = new Dictionary<string, object>()
        };

        // Create 52 plans to test the 50 plan limit
        for (int i = 0; i < 52; i++)
        {
            var plan = new OrchestrationPlan
            {
                Id = $"plan-{i}",
                Goal = baseRequest.Goal,
                Strategy = baseRequest.Strategy,
                Steps = new List<OrchestrationStep>(),
                RequiredAgents = baseRequest.AvailableAgentIds,
                SharedContext = new Dictionary<string, object>()
            };

            _mockOrchestrator.CreatePlanAsync(Arg.Any<OrchestrationRequest>())
                .Returns(Task.FromResult(plan));

            await _service.CreatePlanAsync(baseRequest);
        }

        // Act
        var recentPlans = await _service.GetRecentPlansAsync(100);

        // Assert
        Assert.Equal(50, recentPlans.Count); // Should be limited to 50
        Assert.Equal("plan-51", recentPlans[0].Id); // Most recent should be first
    }

    [Fact]
    public async Task ExecutePlanAsync_Should_Execute_Plan_Successfully()
    {
        // Arrange
        var plan = new OrchestrationPlan
        {
            Id = "execute-plan",
            Goal = "Test execution",
            Strategy = OrchestrationStrategy.Sequential,
            Steps = new List<OrchestrationStep>(),
            RequiredAgents = new List<string>(),
            SharedContext = new Dictionary<string, object>()
        };

        var expectedResult = new OrchestrationResult
        {
            Success = true,
            StepResults = new List<StepResult>(),
            FinalOutput = "Execution completed",
            TotalDuration = TimeSpan.FromMinutes(1),
            FinalContext = new Dictionary<string, object>()
        };

        _mockOrchestrator.ExecutePlanAsync(plan, Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>())
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _service.ExecutePlanAsync(plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Execution completed", result.FinalOutput);
        Assert.True(result.Success);

        // Verify orchestrator was called
        await _mockOrchestrator.Received(1)
            .ExecutePlanAsync(plan, Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>());
    }

    [Fact]
    public async Task ExecutePlanAsync_Should_Store_Result()
    {
        // Arrange
        var plan = new OrchestrationPlan
        {
            Id = "store-result-plan",
            Goal = "Test storage",
            Strategy = OrchestrationStrategy.Sequential,
            Steps = new List<OrchestrationStep>(),
            RequiredAgents = new List<string>(),
            SharedContext = new Dictionary<string, object>()
        };

        var result = new OrchestrationResult
        {
            Success = true,
            StepResults = new List<StepResult>(),
            FinalOutput = "Storage test completed",
            TotalDuration = TimeSpan.FromMinutes(1),
            FinalContext = new Dictionary<string, object>()
        };

        _mockOrchestrator.ExecutePlanAsync(plan, Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>())
            .Returns(Task.FromResult(result));

        // Act
        await _service.ExecutePlanAsync(plan);
        var storedResult = await _service.GetExecutionResultAsync("store-result-plan");

        // Assert
        Assert.NotNull(storedResult);
        Assert.Equal("Storage test completed", storedResult.FinalOutput);
        Assert.True(storedResult.Success);
    }

    [Fact]
    public async Task ExecutePlanAsync_Should_Handle_Progress_Updates()
    {
        // Arrange
        var plan = new OrchestrationPlan
        {
            Id = "progress-plan",
            Goal = "Test progress",
            Strategy = OrchestrationStrategy.Sequential,
            Steps = new List<OrchestrationStep>(),
            RequiredAgents = new List<string>(),
            SharedContext = new Dictionary<string, object>()
        };

        var result = new OrchestrationResult
        {
            Success = true,
            StepResults = new List<StepResult>(),
            FinalOutput = "Progress test completed",
            TotalDuration = TimeSpan.FromMinutes(1),
            FinalContext = new Dictionary<string, object>()
        };

        var progressReported = false;
        var progress = new Progress<OrchestratorChat.Web.Models.OrchestrationProgress>(p =>
        {
            progressReported = true;
            Assert.Equal("Running", p.Status);
            Assert.NotNull(p.CurrentStep);
        });

        _mockOrchestrator.ExecutePlanAsync(plan, Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>())
            .Returns(Task.FromResult(result))
            .AndDoes(callInfo =>
            {
                // Simulate progress reporting
                var coreProgress = callInfo.ArgAt<IProgress<Core.Orchestration.OrchestrationProgress>?>(1);
                if (coreProgress != null)
                {
                    var progressData = new Core.Orchestration.OrchestrationProgress
                    {
                        CurrentTask = "Test Step",
                        CurrentStep = 1,
                        TotalSteps = 3
                    };
                    coreProgress.Report(progressData);
                }
            });

        // Act
        await _service.ExecutePlanAsync(plan, progress);

        // Assert
        Assert.True(progressReported);
    }

    [Fact]
    public async Task GetRecentPlansAsync_Should_Return_Plans_In_Correct_Order()
    {
        // Arrange
        var plans = new List<OrchestrationPlan>();
        for (int i = 0; i < 5; i++)
        {
            var request = new OrchestrationRequest
            {
                Goal = $"Goal {i}",
                AvailableAgentIds = new List<string>(),
                Strategy = OrchestrationStrategy.Sequential,
                Constraints = new Dictionary<string, object>()
            };

            var plan = new OrchestrationPlan
            {
                Id = $"plan-{i}",
                Goal = request.Goal,
                Strategy = request.Strategy,
                Steps = new List<OrchestrationStep>(),
                RequiredAgents = request.AvailableAgentIds,
                SharedContext = new Dictionary<string, object>()
            };

            plans.Add(plan);
            
            _mockOrchestrator.CreatePlanAsync(request)
                .Returns(Task.FromResult(plan));

            await _service.CreatePlanAsync(request);
        }

        // Act
        var recentPlans = await _service.GetRecentPlansAsync(3);

        // Assert
        Assert.Equal(3, recentPlans.Count);
        Assert.Equal("plan-4", recentPlans[0].Id); // Most recent first
        Assert.Equal("plan-3", recentPlans[1].Id);
        Assert.Equal("plan-2", recentPlans[2].Id);
    }

    [Fact]
    public async Task GetExecutionResultAsync_Should_Return_Null_When_Result_Not_Found()
    {
        // Act
        var result = await _service.GetExecutionResultAsync("nonexistent-plan");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreatePlanAsync_Should_Handle_Orchestrator_Errors()
    {
        // Arrange
        var request = new OrchestrationRequest
        {
            Goal = "Error test goal",
            AvailableAgentIds = new List<string>(),
            Strategy = OrchestrationStrategy.Sequential,
            Constraints = new Dictionary<string, object>()
        };

        _mockOrchestrator.CreatePlanAsync(request)
            .Returns(Task.FromException<OrchestrationPlan>(new InvalidOperationException("Orchestrator error")));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePlanAsync(request));
        
        Assert.Equal("Orchestrator error", exception.Message);
    }

    [Fact]
    public async Task ExecutePlanAsync_Should_Handle_Execution_Errors()
    {
        // Arrange
        var plan = new OrchestrationPlan
        {
            Id = "error-plan",
            Goal = "Error test",
            Strategy = OrchestrationStrategy.Sequential,
            Steps = new List<OrchestrationStep>(),
            RequiredAgents = new List<string>(),
            SharedContext = new Dictionary<string, object>()
        };

        _mockOrchestrator.ExecutePlanAsync(plan, Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>())
            .Returns(Task.FromException<OrchestrationResult>(new InvalidOperationException("Execution error")));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecutePlanAsync(plan));
        
        Assert.Equal("Execution error", exception.Message);
    }

    [Fact]
    public void MapProgress_Should_Convert_Core_Progress_To_Web_Progress()
    {
        // Arrange
        var coreProgress = new Core.Orchestration.OrchestrationProgress
        {
            CurrentTask = "Test Task",
            CurrentStep = 2,
            TotalSteps = 5
        };

        // Act
        var result = _service.ExecutePlanAsync(
            new OrchestrationPlan
            {
                Id = "progress-mapping-test",
                Goal = "Progress mapping test",
                Strategy = OrchestrationStrategy.Sequential,
                Steps = new List<OrchestrationStep>(),
                RequiredAgents = new List<string>(),
                SharedContext = new Dictionary<string, object>()
            },
            new Progress<OrchestratorChat.Web.Models.OrchestrationProgress>(progress =>
            {
                // Assert
                Assert.Equal("Test Task", progress.CurrentStep);
                Assert.Equal(2, progress.CompletedSteps);
                Assert.Equal(5, progress.TotalSteps);
                Assert.Equal("Running", progress.Status);
                Assert.Null(progress.ErrorMessage);
                Assert.NotNull(progress.Data);
            }));

        // Set up the orchestrator to trigger progress
        _mockOrchestrator.ExecutePlanAsync(Arg.Any<OrchestrationPlan>(), Arg.Any<IProgress<Core.Orchestration.OrchestrationProgress>?>())
            .Returns(Task.FromResult(new OrchestrationResult { Success = true, StepResults = new List<StepResult>(), FinalOutput = "Test", TotalDuration = TimeSpan.Zero, FinalContext = new Dictionary<string, object>() }))
            .AndDoes(callInfo =>
            {
                var progressCallback = callInfo.ArgAt<IProgress<Core.Orchestration.OrchestrationProgress>?>(1);
                progressCallback?.Report(coreProgress);
            });
    }
}