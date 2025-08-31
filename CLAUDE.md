# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Building the Project
```bash
# Build in Release mode (default)
dotnet build -c Release

# Build in Debug mode  
dotnet build -c Debug

# Clean build
dotnet clean && dotnet build

# Build specific project
dotnet build src/OrchestratorChat.Web/OrchestratorChat.Web.csproj
```

### Running the Application
```bash
# Run the web application
cd src/OrchestratorChat.Web
dotnet run

# Run with watch (development)
dotnet watch run

# Application will be available at:
# https://localhost:5001
# http://localhost:5000
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/OrchestratorChat.Core.Tests/OrchestratorChat.Core.Tests.csproj

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Operations
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create migration from Web project
cd src/OrchestratorChat.Web
dotnet ef migrations add MigrationName -p ../OrchestratorChat.Data -s .

# Update database
dotnet ef database update -p ../OrchestratorChat.Data -s .

# Remove last migration
dotnet ef migrations remove -p ../OrchestratorChat.Data -s .
```

## Architecture Overview

OrchestratorChat is a .NET-based multi-agent orchestration platform built with ASP.NET Core and Blazor Server that enables simultaneous coordination of multiple AI agents including Claude Code, embedded Saturn, and future extensibility for other agents.

### Core Components

1. **Core Abstractions** (`OrchestratorChat.Core/`)
   - `IAgent`: Base interface for all agents with lifecycle management
   - `IMessage`: Message contracts and models for agent communication
   - `ISession`: Session management interfaces and models
   - `ITool`: Tool system abstractions for agent capabilities
   - `IOrchestrationEngine`: Multi-agent coordination interfaces
   - Event system for real-time agent communication
   - Configuration contracts and models

2. **Data Layer** (`OrchestratorChat.Data/`)
   - Entity Framework Core with SQLite for persistence
   - Repository pattern implementation for clean architecture
   - Entity models for sessions, messages, agents, and tools
   - Database context with proper configurations
   - Migration support for schema evolution

3. **Agent Adapters** (`OrchestratorChat.Agents/`)
   - **ClaudeAgent**: Process-based Claude Code integration with streaming
   - **SaturnAgent**: Embedded Saturn library integration
   - **AgentFactory**: Creates and manages agent instances
   - **AgentHealthMonitor**: Monitors agent status and health
   - Plugin architecture for extending with additional agents

4. **Saturn Integration** (`OrchestratorChat.Saturn/`)
   - Embedded Saturn as library (not CLI process)
   - Removed Terminal.Gui dependencies for headless operation
   - Support for multiple Saturn instances per session
   - Provider abstraction layer for OpenRouter/Anthropic

5. **SignalR Communication** (`OrchestratorChat.SignalR/`)
   - **OrchestratorHub**: Session management and orchestration control
   - **AgentHub**: Agent messaging with real-time streaming
   - **MessageRouter**: Centralized message routing service
   - **ConnectionManager**: Session-aware connection tracking
   - **StreamManager**: Channel-based real-time streaming
   - Event bus integration for Core events

6. **Web UI** (`OrchestratorChat.Web/`)
   - Blazor Server application with MudBlazor components
   - Real-time chat interface with agent communication
   - Agent management dashboard and configuration
   - Session history and management
   - **Sessions page** (`/sessions`) - comprehensive session management with search, filtering, and navigation
   - Responsive design for various screen sizes

### Technology Stack
- **.NET 8.0**: Latest LTS version
- **ASP.NET Core**: Web framework and hosting
- **Blazor Server**: Real-time UI framework
- **SignalR**: WebSocket-based real-time communication
- **Entity Framework Core 8.0**: ORM with SQLite database
- **MudBlazor**: Material Design component library
- **xUnit**: Testing framework

### Project Dependencies

```
OrchestratorChat.Web
├── OrchestratorChat.Core
├── OrchestratorChat.Data
├── OrchestratorChat.Configuration
├── OrchestratorChat.Agents
├── OrchestratorChat.SignalR
└── OrchestratorChat.Saturn

OrchestratorChat.Agents
├── OrchestratorChat.Core
└── OrchestratorChat.Saturn

OrchestratorChat.Data
└── OrchestratorChat.Core

OrchestratorChat.Configuration
└── OrchestratorChat.Core

OrchestratorChat.SignalR
├── OrchestratorChat.Core
└── OrchestratorChat.Agents

OrchestratorChat.Saturn
└── OrchestratorChat.Core

OrchestratorChat.Core (no dependencies)
```

## Key Design Patterns

### Repository Pattern
The project uses the Repository pattern to separate data access logic from business logic:
- **Data Layer**: Implements `ISessionRepository` in `Data.Repositories` namespace
- **Business Logic**: `SessionManager` in Core uses `ISessionRepository` for data operations
- This pattern ensures Core doesn't depend on Data implementation details

Example:
```csharp
// Data implements the repository
public class SessionRepository : ISessionRepository
{
    private readonly OrchestratorDbContext _dbContext;
    // Implementation details...
}

// Core uses it via dependency injection
public class SessionManager : ISessionManager
{
    private readonly ISessionRepository _repository;
    private readonly IEventBus _eventBus;
    
    public SessionManager(ISessionRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }
}
```

### Event-Driven Architecture
- Agent status changes broadcast via `IEventBus`
- Message routing through central orchestrator
- UI updates stream in real-time via SignalR
- Loose coupling between components

### Dependency Injection
- All services registered via DI container
- Interfaces enable testing and loose coupling
- Configuration bound from appsettings.json

### Asynchronous Operations
- All I/O operations use async/await patterns
- Real-time streaming with SignalR
- Background task support for long-running operations

## Database Schema
The SQLite database includes tables for:
- **Sessions**: Chat sessions with metadata and configuration
- **Messages**: Individual messages with agent attribution
- **Agents**: Agent instances and their configurations
- **Tools**: Available tools and their configurations
- **SessionAgents**: Many-to-many relationship between sessions and agents

## Configuration Structure
```json
{
  "Claude": {
    "ExecutablePath": "claude",
    "DefaultModel": "claude-sonnet-4-20250514",
    "EnableMcp": true
  },
  "Saturn": {
    "DefaultProvider": "OpenRouter", 
    "MaxSubAgents": 5,
    "SupportedModels": [...]
  },
  "SignalR": {
    "KeepAliveInterval": 15,
    "ClientTimeoutInterval": 30
  }
}
```

## Development Guidelines

### NuGet Package Management
**CRITICAL**: When updating NuGet packages, always verify actual available versions:

1. **Verify Existing Versions**: Use `dotnet list package --outdated`
2. **Check Actual Versions**: Visit https://www.nuget.org/packages/[PackageName]
3. **Apply Verified Versions**: Update all .csproj files with confirmed versions only
4. **Eliminate NU1603 Warnings**: Find the actual latest available version

#### Current Verified 8.0.x Package Versions:
- **EntityFramework Core**: 8.0.19 (Design, Sqlite, InMemory, etc.)
- **ASP.NET Core**: 8.0.19 (SignalR.Client, Mvc.Testing, TestHost)
- **Extensions**: Mixed versions - verify each individually

### Code Standards
- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Follow async/await patterns consistently
- Implement IDisposable for resources
- Use dependency injection for all services
- Write unit tests for business logic

#### Nullable Reference Type Patterns
To avoid CS8618 compiler warnings with nullable reference types enabled:

**String Properties - Always initialize non-nullable strings:**
```csharp
// ✅ CORRECT - Initialize with default value
public string Name { get; set; } = string.Empty;
public string Description { get; set; } = string.Empty;

// ❌ INCORRECT - Will cause CS8618 warning
public string Name { get; set; }

// ✅ CORRECT - Use nullable if null is valid
public string? OptionalName { get; set; }
```

**Array Properties - Initialize with empty arrays:**
```csharp
// ✅ CORRECT - Initialize with empty array
public byte[] Content { get; set; } = Array.Empty<byte>();
public string[] Tags { get; set; } = Array.Empty<string>();

// ❌ INCORRECT - Will cause CS8618 warning  
public byte[] Content { get; set; }
```

**Collection Properties - Initialize with empty collections:**
```csharp
// ✅ CORRECT - Initialize collections
public List<string> Items { get; set; } = new();
public Dictionary<string, object> Data { get; set; } = new();

// ❌ INCORRECT - Will cause CS8618 warning
public List<string> Items { get; set; }
```

**Constructor Parameters - Validate required parameters:**
```csharp
// ✅ CORRECT - Validate non-null parameters
public MyClass(string requiredParam)
{
    RequiredProperty = requiredParam ?? throw new ArgumentNullException(nameof(requiredParam));
}

// ✅ CORRECT - Use nullable for optional parameters
public MyClass(string? optionalParam = null)
{
    OptionalProperty = optionalParam;
}
```

#### Async/Await Best Practices
To avoid CS1998 compiler warnings (async methods without await expressions):

**1. Only use async when you actually await something:**
```csharp
// ✅ CORRECT - Actually awaiting an operation
public async Task<string> GetDataAsync()
{
    var result = await httpClient.GetStringAsync(url);
    return result;
}

// ❌ INCORRECT - Will cause CS1998 warning
public async Task<string> GetDataAsync()
{
    return "hardcoded result";
}

// ✅ CORRECT - Use Task.FromResult for sync operations
public Task<string> GetDataAsync()
{
    return Task.FromResult("hardcoded result");
}
```

**2. Interface implementation patterns:**
```csharp
// ✅ CORRECT - Interface requires async but no await needed
public Task<bool> ValidateAsync(string input)
{
    bool isValid = input?.Length > 0;
    return Task.FromResult(isValid);
}

// ❌ INCORRECT - Unnecessary async keyword
public async Task<bool> ValidateAsync(string input)
{
    bool isValid = input?.Length > 0;
    return isValid;
}
```

**3. When to keep methods truly async:**
```csharp
// ✅ CORRECT - Future-proofing for async operations
public async Task ProcessAsync()
{
    // Current synchronous work
    ProcessSynchronously();
    
    // Placeholder for future async operations
    await Task.CompletedTask;
}

// ✅ BETTER - Add the actual async operation when needed
public async Task ProcessAsync()
{
    await DatabaseService.SaveAsync();
    ProcessSynchronously();
}
```

**4. Converting async to sync safely:**
```csharp
// BEFORE - Unnecessary async
public async Task<int> CalculateAsync(int a, int b)
{
    return a + b;
}

// AFTER - Converted to sync (if callers can be updated)
public int Calculate(int a, int b)
{
    return a + b;
}

// OR - Keep async interface but remove async keyword
public Task<int> CalculateAsync(int a, int b)
{
    return Task.FromResult(a + b);
}
```

**5. Decision tree for async patterns:**
```csharp
// Use this decision process:
// 1. Does the method perform I/O operations? → Use async/await
// 2. Does it call other async methods? → Use async/await
// 3. Must it implement an async interface? → Use Task.FromResult()
// 4. Is it purely computational? → Make it synchronous
// 5. Future async operations planned? → Use await Task.CompletedTask

// Example - Repository pattern with mixed operations
public class SessionRepository : ISessionRepository
{
    // ✅ CORRECT - Actual database I/O
    public async Task<Session> CreateSessionAsync(Session session)
    {
        await _dbContext.Sessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }
    
    // ✅ CORRECT - Interface requires async, use Task.FromResult
    public Task<bool> IsValidSessionIdAsync(string sessionId)
    {
        bool isValid = !string.IsNullOrWhiteSpace(sessionId) && sessionId.Length == 36;
        return Task.FromResult(isValid);
    }
    
    // ✅ CORRECT - Synchronous method for simple operations
    public bool IsValidSessionId(string sessionId)
    {
        return !string.IsNullOrWhiteSpace(sessionId) && sessionId.Length == 36;
    }
}
```

**6. Common CS1998 scenarios and fixes:**
```csharp
// SCENARIO: Event handlers
// ❌ INCORRECT
private async void OnButtonClick(object sender, EventArgs e)
{
    ProcessData();
}

// ✅ CORRECT
private void OnButtonClick(object sender, EventArgs e)
{
    ProcessData();
}

// ✅ CORRECT - If async work is needed
private async void OnButtonClick(object sender, EventArgs e)
{
    await ProcessDataAsync();
}

// SCENARIO: Property getters
// ❌ INCORRECT
public async Task<string> Status
{
    get { return "Ready"; }
}

// ✅ CORRECT
public Task<string> Status
{
    get { return Task.FromResult("Ready"); }
}

// ✅ BETTER - Make it synchronous if possible
public string Status => "Ready";
```

**7. Performance considerations:**
```csharp
// Unnecessary async creates state machines and allocations
// ❌ AVOID - Creates unnecessary overhead
public async Task<int> GetCountAsync()
{
    return items.Count;
}

// ✅ PREFER - No allocation, immediate completion
public Task<int> GetCountAsync()
{
    return Task.FromResult(items.Count);
}

// ✅ BEST - Synchronous when appropriate
public int GetCount()
{
    return items.Count;
}
```

**8. Exception handling in async methods:**
```csharp
// ✅ CORRECT - Exceptions in Task.FromResult are wrapped properly
public Task<string> ValidateInputAsync(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Input cannot be null or empty");
    
    return Task.FromResult(input.Trim());
}

// ✅ CORRECT - Using Task.FromException for expected exceptions
public Task<string> ValidateInputAsync(string input)
{
    if (string.IsNullOrEmpty(input))
        return Task.FromException<string>(new ArgumentException("Input cannot be null or empty"));
    
    return Task.FromResult(input.Trim());
}
```

**9. Testing async methods without await:**
```csharp
// ✅ CORRECT - Testing Task.FromResult methods
[Fact]
public async Task ValidateAsync_WithValidInput_ReturnsTrue()
{
    // Arrange
    var validator = new InputValidator();
    
    // Act
    var result = await validator.ValidateAsync("valid input");
    
    // Assert
    Assert.True(result);
}

// ✅ CORRECT - Testing synchronous result
[Fact]
public void ValidateAsync_WithValidInput_ReturnsCompletedTask()
{
    // Arrange
    var validator = new InputValidator();
    
    // Act
    var task = validator.ValidateAsync("valid input");
    
    // Assert
    Assert.True(task.IsCompletedSuccessfully);
    Assert.True(task.Result);
}
```

**10. Troubleshooting CS1998 warnings:**
```csharp
// Common causes and fixes:

// CAUSE: Method signature requires async but implementation doesn't need it
// FIX: Use Task.FromResult() or remove async keyword

// CAUSE: Future-proofing for async operations
// FIX: Use await Task.CompletedTask as placeholder

// CAUSE: Interface contract forces async signature
// FIX: Implement with Task.FromResult() instead of async

// CAUSE: Mixed sync/async operations in same class
// FIX: Use consistent patterns - either all sync or all async

// CAUSE: Property returning Task without await
// FIX: Use Task.FromResult() or make property synchronous
```

### Testing Standards
- **Use standard xUnit assertions only** (no FluentAssertions)
- Use xUnit as the primary testing framework
- Use NSubstitute or Moq for mocking dependencies
- Keep tests simple and readable

Example:
```csharp
// Use standard xUnit assertions
Assert.NotNull(result);
Assert.True(result.Success);
Assert.Equal("expected", result.Value);

// NOT FluentAssertions
// result.Should().NotBeNull(); // Don't use this
```

### Security Best Practices
- Never introduce code that exposes or logs secrets
- Use secure storage for API keys (encrypted)
- Input validation and sanitization
- Process isolation for agent execution

## Environment Requirements

### Development Prerequisites
- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (17.8+) or VS Code
- **Claude CLI**: Required for Claude Code integration
- **Git**: Version control
- **SQLite**: Included with .NET SDK

### Environment Variables
```bash
# Claude Code authentication (OAuth preferred)
CLAUDE_API_KEY=your_claude_api_key_here

# OpenRouter for Saturn (if using)
OPENROUTER_API_KEY=your_openrouter_key_here

# GitHub token for MCP GitHub tool
GITHUB_TOKEN=your_github_token_here
```

## Important Files and Directories

### Project Structure
- `src/OrchestratorChat.Core/` - Core abstractions and business logic
- `src/OrchestratorChat.Data/` - Entity Framework data layer
- `src/OrchestratorChat.Configuration/` - Settings and configuration
- `src/OrchestratorChat.Agents/` - Agent adapter implementations
- `src/OrchestratorChat.Saturn/` - Embedded Saturn library
- `src/OrchestratorChat.SignalR/` - Real-time communication layer
- `src/OrchestratorChat.Web/` - Blazor Server web application

### Available Pages and Routes
- **`/` or `/dashboard`** - Main dashboard with agent status and overview
- **`/sessions`** - **Sessions management page** with comprehensive session list, search, filtering, and navigation
- **`/orchestrator`** - Multi-agent orchestration interface
- **`/chat/{AgentId?}`** - Direct chat with specific agent
- **`/session/{SessionId?}`** - View and continue specific session
- **`/settings`** - Application configuration and settings

### Sessions Page Features (`/sessions`)
- **Grid-based session overview** with status badges and metadata
- **Search functionality** by session name, content, or participants
- **Filter options** by session status (Active, Paused, Completed, Cancelled)
- **Sort options** by Last Activity, Created Date, Name, or Message Count  
- **Quick actions** to open, delete, or create new sessions
- **Responsive design** for desktop, tablet, and mobile devices
- **Empty states** with helpful guidance for new users

### Supporting Files
- `OrchestratorChat.sln` - Solution file with all projects
- `docs/planning/` - Architecture and planning documentation
- `tests/` - Unit and integration test projects
- `orchestrator.db` - SQLite database (created on first run)

## Troubleshooting Common Issues

### Build Issues
```bash
# Clear NuGet cache if build fails
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### Database Issues
```bash
# Reset database if corruption occurs
rm orchestrator.db
cd src/OrchestratorChat.Web
dotnet ef database update
```

### SignalR Connection Issues
- Check firewall settings for ports 5000/5001
- Verify CORS configuration in development
- Monitor browser console for WebSocket errors

### Claude Integration Issues
- Verify Claude CLI is installed and in PATH
- Check authentication setup (OAuth preferred)
- Test Claude CLI independently: `claude --version`

## Key Integration Points

### Agent Communication Flow
1. **User Input** → Blazor UI → SignalR Hub
2. **Hub** → AgentFactory → Specific Agent (Claude/Saturn)
3. **Agent** → Tool Execution → Results
4. **Results** → EventBus → SignalR → Real-time UI Updates

### Data Flow
1. **Sessions** managed by `SessionManager` via `ISessionRepository`
2. **Messages** persisted through Entity Framework
3. **Agent States** tracked and broadcasted via events
4. **Real-time Updates** streamed through SignalR

### Authentication Flow
- **Claude Code**: OAuth 2.0 with PKCE (preferred) or API key
- **Saturn**: OpenRouter API key or Anthropic OAuth
- **Secure Storage**: Cross-platform encrypted token storage

## Development Workflow

### Getting Started
1. Clone the repository
2. Ensure .NET 8.0 SDK is installed
3. Install Claude CLI and authenticate
4. Set up environment variables
5. Run `dotnet restore` and `dotnet build`
6. Start with `dotnet run` from `src/OrchestratorChat.Web/`

### Adding New Agents
1. Implement `IAgent` interface
2. Add agent type to `AgentType` enum
3. Register in `AgentFactory`
4. Add configuration support
5. Implement health monitoring
6. Add integration tests

### Adding New Tools
1. Implement `ITool` interface
2. Add to appropriate agent's tool registry
3. Implement approval handling if needed
4. Add unit tests
5. Document tool capabilities

## Performance Considerations
- SignalR connection limits (default: 100 concurrent)
- SQLite database size limits (practical limit: ~1TB)
- Memory usage with multiple agent instances
- WebSocket connection management
- Long-running agent processes

## Cross-Platform Support
- **Primary target**: Windows 10/11
- **Linux support**: Server deployment
- **macOS support**: Development environment