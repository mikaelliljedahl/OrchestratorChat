# Track 1: Test Implementation Guide

## Step-by-Step Implementation Instructions

---

## Phase 1: Project Setup (Day 1, Morning)

### Step 1: Create Test Project
```bash
cd C:\code\github\OrchestratorChat
mkdir tests
cd tests
dotnet new xunit -n OrchestratorChat.Core.Tests
cd OrchestratorChat.Core.Tests
```

### Step 2: Add NuGet Packages
```bash
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.69
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
dotnet add package BenchmarkDotNet --version 0.13.10
dotnet add package coverlet.collector --version 6.0.0
dotnet add package xunit.runner.visualstudio --version 2.5.3
```

### Step 3: Add Project References
```bash
dotnet add reference ../../src/OrchestratorChat.Core/OrchestratorChat.Core.csproj
dotnet add reference ../../src/OrchestratorChat.Data/OrchestratorChat.Data.csproj
```

### Step 4: Update Solution File
```bash
cd ../..
dotnet sln add tests/OrchestratorChat.Core.Tests/OrchestratorChat.Core.Tests.csproj
```

### Step 5: Create Directory Structure
```bash
cd tests/OrchestratorChat.Core.Tests
mkdir Unit Unit/Sessions Unit/Orchestration Unit/Events Unit/Models
mkdir Integration Integration/Database Integration/Workflows Integration/Events
mkdir Performance
mkdir Fixtures
mkdir TestHelpers
```

---

## Phase 2: Test Infrastructure (Day 1, Afternoon)

### Step 1: Create Base Test Class
**File**: `Fixtures/TestBase.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrchestratorChat.Data;
using System;

namespace OrchestratorChat.Core.Tests.Fixtures
{
    public abstract class TestBase : IDisposable
    {
        protected OrchestratorDbContext DbContext { get; }
        protected Mock<ILogger<T>> CreateLoggerMock<T>() => new Mock<ILogger<T>>();
        
        protected TestBase()
        {
            var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            DbContext = new OrchestratorDbContext(options);
            DbContext.Database.EnsureCreated();
        }
        
        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}
```

### Step 2: Create Test Data Builder
**File**: `Fixtures/TestDataBuilder.cs`
```csharp
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Models;
using System;
using System.Collections.Generic;

namespace OrchestratorChat.Core.Tests.Fixtures
{
    public class TestDataBuilder
    {
        private static int _counter = 0;
        
        public CreateSessionRequest BuildCreateSessionRequest(string? name = null)
        {
            return new CreateSessionRequest
            {
                Name = name ?? $"Test Session {++_counter}",
                Type = SessionType.MultiAgent,
                AgentIds = new List<string> { "agent-1", "agent-2" },
                WorkingDirectory = @"C:\test",
                ProjectId = "project-test"
            };
        }
        
        public Session BuildSession(string? id = null)
        {
            return new Session
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = $"Session {++_counter}",
                Status = SessionStatus.Active,
                Type = SessionType.MultiAgent,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                Messages = new List<AgentMessage>()
            };
        }
        
        public AgentMessage BuildMessage(string? content = null)
        {
            return new AgentMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = content ?? "Test message",
                Role = MessageRole.User,
                SessionId = "session-test",
                Timestamp = DateTime.UtcNow,
                SenderId = "user-1"
            };
        }
        
        public OrchestrationPlan BuildOrchestrationPlan()
        {
            return new OrchestrationPlan
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Plan",
                Goal = "Execute test steps",
                Strategy = OrchestrationStrategy.Sequential,
                Steps = new List<OrchestrationStep>
                {
                    new OrchestrationStep
                    {
                        Id = "step-1",
                        Order = 1,
                        Description = "First step",
                        AssignedAgentId = "agent-1",
                        Task = "Do something",
                        DependsOn = new List<string>(),
                        ExpectedDuration = TimeSpan.FromSeconds(5)
                    },
                    new OrchestrationStep
                    {
                        Id = "step-2",
                        Order = 2,
                        Description = "Second step",
                        AssignedAgentId = "agent-2",
                        Task = "Do something else",
                        DependsOn = new List<string> { "step-1" },
                        ExpectedDuration = TimeSpan.FromSeconds(3)
                    }
                }
            };
        }
    }
}
```

### Step 3: Create Mock Factory
**File**: `Fixtures/MockFactory.cs`
```csharp
using Moq;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Sessions;
using System.Threading.Tasks;

namespace OrchestratorChat.Core.Tests.Fixtures
{
    public static class MockFactory
    {
        public static Mock<IEventBus> CreateEventBusMock()
        {
            var mock = new Mock<IEventBus>();
            mock.Setup(x => x.PublishAsync(It.IsAny<IEvent>()))
                .Returns(Task.CompletedTask);
            return mock;
        }
        
        public static Mock<IAgentFactory> CreateAgentFactoryMock()
        {
            var mock = new Mock<IAgentFactory>();
            var agentMock = new Mock<IAgent>();
            
            agentMock.Setup(a => a.Id).Returns("agent-1");
            agentMock.Setup(a => a.Name).Returns("Test Agent");
            agentMock.Setup(a => a.Status).Returns(AgentStatus.Ready);
            
            mock.Setup(f => f.GetAgentAsync(It.IsAny<string>()))
                .ReturnsAsync(agentMock.Object);
            
            return mock;
        }
        
        public static Mock<ISessionRepository> CreateSessionRepositoryMock()
        {
            var mock = new Mock<ISessionRepository>();
            // Add default setups
            return mock;
        }
    }
}
```

### Step 4: Create Assertion Extensions
**File**: `TestHelpers/AssertionExtensions.cs`
```csharp
using FluentAssertions;
using FluentAssertions.Execution;
using OrchestratorChat.Core.Models;
using OrchestratorChat.Core.Orchestration;

namespace OrchestratorChat.Core.Tests.TestHelpers
{
    public static class AssertionExtensions
    {
        public static void ShouldBeValidSession(this Session session)
        {
            using (new AssertionScope())
            {
                session.Should().NotBeNull();
                session.Id.Should().NotBeNullOrWhiteSpace();
                session.Name.Should().NotBeNullOrWhiteSpace();
                session.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
                session.Status.Should().BeOneOf(SessionStatus.Active, SessionStatus.Paused, SessionStatus.Completed);
            }
        }
        
        public static void ShouldBeValidPlan(this OrchestrationPlan plan)
        {
            using (new AssertionScope())
            {
                plan.Should().NotBeNull();
                plan.Id.Should().NotBeNullOrWhiteSpace();
                plan.Name.Should().NotBeNullOrWhiteSpace();
                plan.Goal.Should().NotBeNullOrWhiteSpace();
                plan.Steps.Should().NotBeNull();
                plan.Strategy.Should().BeOneOf(
                    OrchestrationStrategy.Sequential,
                    OrchestrationStrategy.Parallel,
                    OrchestrationStrategy.Adaptive);
            }
        }
    }
}
```

---

## Phase 3: Unit Test Implementation (Days 2-3)

### Step 1: Implement SessionManager Tests
**File**: `Unit/Sessions/SessionManagerTests.cs`
```csharp
using FluentAssertions;
using Moq;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using OrchestratorChat.Core.Tests.TestHelpers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrchestratorChat.Core.Tests.Unit.Sessions
{
    public class SessionManagerTests : TestBase
    {
        private readonly SessionManager _sessionManager;
        private readonly Mock<IEventBus> _eventBusMock;
        private readonly TestDataBuilder _dataBuilder;
        
        public SessionManagerTests()
        {
            _eventBusMock = MockFactory.CreateEventBusMock();
            _sessionManager = new SessionManager(DbContext, _eventBusMock.Object);
            _dataBuilder = new TestDataBuilder();
        }
        
        [Fact]
        public async Task CreateSessionAsync_WithValidData_ReturnsNewSession()
        {
            // Arrange
            var request = _dataBuilder.BuildCreateSessionRequest();
            
            // Act
            var result = await _sessionManager.CreateSessionAsync(request);
            
            // Assert
            result.ShouldBeValidSession();
            result.Name.Should().Be(request.Name);
            result.Type.Should().Be(request.Type);
            
            _eventBusMock.Verify(x => x.PublishAsync(
                It.Is<SessionCreatedEvent>(e => e.SessionId == result.Id)),
                Times.Once);
        }
        
        [Fact]
        public async Task CreateSessionAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _sessionManager.CreateSessionAsync(null!);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("request");
        }
        
        [Fact]
        public async Task GetSessionAsync_WithExistingId_ReturnsSession()
        {
            // Arrange
            var request = _dataBuilder.BuildCreateSessionRequest();
            var created = await _sessionManager.CreateSessionAsync(request);
            
            // Act
            var result = await _sessionManager.GetSessionAsync(created.Id);
            
            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(created.Id);
            result.Name.Should().Be(created.Name);
        }
        
        // Add more tests following the scenarios document...
    }
}
```

### Step 2: Implement Orchestrator Tests
**File**: `Unit/Orchestration/OrchestratorTests.cs`
```csharp
// Similar structure to SessionManagerTests
// Implement based on orchestrator scenarios document
```

### Step 3: Implement EventBus Tests
**File**: `Unit/Events/EventBusTests.cs`
```csharp
// Similar structure
// Focus on thread safety and error handling
```

---

## Phase 4: Integration Tests (Days 4-5)

### Step 1: Database Integration Tests
**File**: `Integration/Database/SessionPersistenceTests.cs`
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using System.Threading.Tasks;
using Xunit;

namespace OrchestratorChat.Core.Tests.Integration.Database
{
    public class SessionPersistenceTests : TestBase
    {
        [Fact]
        public async Task SessionLifecycle_CreateUpdateDelete_PersistsCorrectly()
        {
            // Arrange
            var eventBusMock = MockFactory.CreateEventBusMock();
            var manager = new SessionManager(DbContext, eventBusMock.Object);
            var dataBuilder = new TestDataBuilder();
            
            // Act - Create
            var request = dataBuilder.BuildCreateSessionRequest();
            var created = await manager.CreateSessionAsync(request);
            
            // Verify persisted
            var fromDb = await DbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == created.Id);
            fromDb.Should().NotBeNull();
            
            // Act - Update
            var message = dataBuilder.BuildMessage();
            await manager.AddMessageAsync(created.Id, message);
            
            // Verify message persisted
            var sessionWithMessages = await DbContext.Sessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == created.Id);
            sessionWithMessages!.Messages.Should().HaveCount(1);
            
            // Act - End
            await manager.EndSessionAsync(created.Id);
            
            // Verify status updated
            var ended = await DbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == created.Id);
            ended!.Status.Should().Be(SessionStatus.Completed);
        }
    }
}
```

---

## Phase 5: Performance Tests (Day 6)

### Step 1: Create Benchmarks
**File**: `Performance/SessionBenchmarks.cs`
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Tests.Fixtures;
using System.Threading.Tasks;

namespace OrchestratorChat.Core.Tests.Performance
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class SessionBenchmarks
    {
        private SessionManager _sessionManager = null!;
        private TestDataBuilder _dataBuilder = null!;
        private string _sessionId = null!;
        
        [GlobalSetup]
        public void Setup()
        {
            // Initialize components
            var testBase = new TestBase();
            var eventBus = MockFactory.CreateEventBusMock();
            _sessionManager = new SessionManager(testBase.DbContext, eventBus.Object);
            _dataBuilder = new TestDataBuilder();
            
            // Create a session for tests
            var request = _dataBuilder.BuildCreateSessionRequest();
            _sessionId = _sessionManager.CreateSessionAsync(request).Result.Id;
        }
        
        [Benchmark]
        public async Task CreateSession()
        {
            var request = _dataBuilder.BuildCreateSessionRequest();
            await _sessionManager.CreateSessionAsync(request);
        }
        
        [Benchmark]
        public async Task AddMessage()
        {
            var message = _dataBuilder.BuildMessage();
            await _sessionManager.AddMessageAsync(_sessionId, message);
        }
        
        [Benchmark]
        public async Task GetRecentSessions()
        {
            await _sessionManager.GetRecentSessions(20);
        }
    }
    
    public class BenchmarkRunner
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SessionBenchmarks>();
        }
    }
}
```

---

## Phase 6: Test Execution & Coverage (Day 7)

### Step 1: Create Test Script
**File**: `run-tests.ps1`
```powershell
# Run all tests with coverage
dotnet test `
    --collect:"XPlat Code Coverage" `
    --results-directory:"./TestResults" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Generate coverage report
reportgenerator `
    -reports:"./TestResults/**/coverage.opencover.xml" `
    -targetdir:"./TestResults/CoverageReport" `
    -reporttypes:"Html;Cobertura"

# Open report
Start-Process "./TestResults/CoverageReport/index.html"
```

### Step 2: Create GitHub Actions Workflow
**File**: `.github/workflows/test.yml`
```yaml
name: Test

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v3
      with:
        files: ./tests/**/coverage.opencover.xml
```

---

## Implementation Checklist

### Week 1
- [ ] Create test project structure
- [ ] Set up test infrastructure
- [ ] Implement fixtures and helpers
- [ ] Write first 10 SessionManager tests

### Week 2
- [ ] Complete SessionManager tests (15+ tests)
- [ ] Complete Orchestrator tests (20+ tests)
- [ ] Complete EventBus tests (10+ tests)
- [ ] Achieve 60% code coverage

### Week 3
- [ ] Implement database integration tests
- [ ] Implement workflow integration tests
- [ ] Implement event flow tests
- [ ] Achieve 75% code coverage

### Week 4
- [ ] Create performance benchmarks
- [ ] Run load tests
- [ ] Fix bugs found during testing
- [ ] Complete documentation
- [ ] Achieve 85% code coverage

---

## Success Criteria

### Quantitative
- ✅ 100+ total tests
- ✅ 85% line coverage
- ✅ 75% branch coverage
- ✅ All tests pass
- ✅ No flaky tests
- ✅ Test run < 30 seconds

### Qualitative
- ✅ All critical paths tested
- ✅ Error conditions handled
- ✅ Thread safety verified
- ✅ Performance validated
- ✅ Integration verified

---

## Common Pitfalls to Avoid

1. **Don't mock what you don't own** - Test with real DbContext
2. **Avoid time dependencies** - Use TimeProvider abstraction
3. **Prevent test pollution** - Clean up after each test
4. **Ensure test isolation** - Each test independent
5. **Keep tests fast** - Mock external dependencies
6. **Name tests clearly** - Method_Scenario_ExpectedResult
7. **One assertion per test** - Or use AssertionScope
8. **Test behavior, not implementation** - Focus on outcomes

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)

---

## Next Steps

1. Review and approve implementation guide
2. Set up development environment
3. Begin Phase 1: Project Setup
4. Daily progress reviews
5. Weekly coverage reports