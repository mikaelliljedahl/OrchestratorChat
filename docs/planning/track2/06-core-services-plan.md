# Core Services Implementation Plan

## Overview
Core services provide essential infrastructure for Saturn including Git repository management, configuration handling, command approval workflows, and error management. These services support the entire Saturn ecosystem and must be ported from the SaturnFork terminal application to the web-based library format.

## Current State vs Required State

### Current State (OrchestratorChat.Saturn)
- Basic SaturnConfiguration class
- No Git management
- No command approval system
- Limited error handling
- No service infrastructure

### Required State (from SaturnFork)
- Complete GitManager for repository validation
- Configuration management system
- Command approval service
- Comprehensive error handling
- Service registration and DI
- Cross-cutting concerns (logging, telemetry)

## Core Service Components

### 1. Git Repository Management

#### 1.1 GitManager
**Location**: `src/OrchestratorChat.Saturn/Core/GitManager.cs`

**Purpose**: Validate and manage Git repository context

**Implementation**:
```csharp
public interface IGitManager
{
    bool IsGitRepository(string path = null);
    string GetRepositoryRoot(string path = null);
    string GetCurrentBranch();
    string GetRemoteUrl(string remoteName = "origin");
    List<string> GetModifiedFiles();
    List<string> GetUntrackedFiles();
    GitStatus GetStatus();
    bool HasUncommittedChanges();
}

public class GitManager : IGitManager
{
    private readonly ILogger<GitManager> _logger;
    private readonly string _workingDirectory;
    
    public GitManager(ILogger<GitManager> logger, string workingDirectory = null)
    {
        _logger = logger;
        _workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
    }
    
    public bool IsGitRepository(string path = null)
    {
        var checkPath = path ?? _workingDirectory;
        
        try
        {
            // Walk up directory tree looking for .git folder
            var currentDir = new DirectoryInfo(checkPath);
            
            while (currentDir != null)
            {
                if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
                {
                    return true;
                }
                currentDir = currentDir.Parent;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if path is git repository: {Path}", checkPath);
            return false;
        }
    }
    
    public string GetRepositoryRoot(string path = null)
    {
        var checkPath = path ?? _workingDirectory;
        var currentDir = new DirectoryInfo(checkPath);
        
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        
        throw new InvalidOperationException("Not in a git repository");
    }
    
    public string GetCurrentBranch()
    {
        var gitDir = Path.Combine(GetRepositoryRoot(), ".git");
        var headFile = Path.Combine(gitDir, "HEAD");
        
        if (!File.Exists(headFile))
            return "unknown";
        
        var content = File.ReadAllText(headFile).Trim();
        
        if (content.StartsWith("ref: refs/heads/"))
        {
            return content.Substring("ref: refs/heads/".Length);
        }
        
        // Detached HEAD state
        return content.Substring(0, Math.Min(7, content.Length));
    }
    
    public string GetRemoteUrl(string remoteName = "origin")
    {
        var gitDir = Path.Combine(GetRepositoryRoot(), ".git");
        var configFile = Path.Combine(gitDir, "config");
        
        if (!File.Exists(configFile))
            return null;
        
        var lines = File.ReadAllLines(configFile);
        var inRemoteSection = false;
        var isTargetRemote = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("[remote"))
            {
                inRemoteSection = true;
                isTargetRemote = trimmed.Contains($"\"{remoteName}\"");
            }
            else if (trimmed.StartsWith("["))
            {
                inRemoteSection = false;
                isTargetRemote = false;
            }
            else if (inRemoteSection && isTargetRemote && trimmed.StartsWith("url = "))
            {
                return trimmed.Substring("url = ".Length);
            }
        }
        
        return null;
    }
    
    public GitStatus GetStatus()
    {
        var status = new GitStatus
        {
            Branch = GetCurrentBranch(),
            ModifiedFiles = GetModifiedFiles(),
            UntrackedFiles = GetUntrackedFiles(),
            HasUncommittedChanges = HasUncommittedChanges()
        };
        
        return status;
    }
    
    public List<string> GetModifiedFiles()
    {
        // This is a simplified implementation
        // In production, use LibGit2Sharp or execute git command
        return ExecuteGitCommand("diff --name-only")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
    
    public List<string> GetUntrackedFiles()
    {
        return ExecuteGitCommand("ls-files --others --exclude-standard")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
    
    public bool HasUncommittedChanges()
    {
        var status = ExecuteGitCommand("status --porcelain");
        return !string.IsNullOrWhiteSpace(status);
    }
    
    private string ExecuteGitCommand(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = GetRepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute git command: {Arguments}", arguments);
            return string.Empty;
        }
    }
}

public class GitStatus
{
    public string Branch { get; set; }
    public List<string> ModifiedFiles { get; set; }
    public List<string> UntrackedFiles { get; set; }
    public List<string> StagedFiles { get; set; }
    public bool HasUncommittedChanges { get; set; }
    public string RemoteUrl { get; set; }
}
```

### 2. Configuration Management

#### 2.1 Configuration Service
**Location**: `src/OrchestratorChat.Saturn/Configuration/ConfigurationService.cs`

**Purpose**: Manage Saturn configuration with persistence

**Implementation**:
```csharp
public interface IConfigurationService
{
    Task<SaturnConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(SaturnConfiguration configuration);
    Task<T> GetSettingAsync<T>(string key);
    Task SetSettingAsync<T>(string key, T value);
    void ResetToDefaults();
}

public class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private readonly ILogger<ConfigurationService> _logger;
    private SaturnConfiguration _currentConfig;
    private readonly SemaphoreSlim _configLock = new(1);
    
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "OrchestratorChat", "Saturn");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "saturn.config.json");
    }
    
    public async Task<SaturnConfiguration> LoadConfigurationAsync()
    {
        await _configLock.WaitAsync();
        try
        {
            if (_currentConfig != null)
                return _currentConfig;
            
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                _currentConfig = JsonSerializer.Deserialize<SaturnConfiguration>(json);
            }
            else
            {
                _currentConfig = CreateDefaultConfiguration();
                await SaveConfigurationInternalAsync(_currentConfig);
            }
            
            return _currentConfig;
        }
        finally
        {
            _configLock.Release();
        }
    }
    
    public async Task SaveConfigurationAsync(SaturnConfiguration configuration)
    {
        await _configLock.WaitAsync();
        try
        {
            await SaveConfigurationInternalAsync(configuration);
            _currentConfig = configuration;
        }
        finally
        {
            _configLock.Release();
        }
    }
    
    private async Task SaveConfigurationInternalAsync(SaturnConfiguration configuration)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(configuration, options);
        await File.WriteAllTextAsync(_configPath, json);
    }
    
    public async Task<T> GetSettingAsync<T>(string key)
    {
        var config = await LoadConfigurationAsync();
        var property = config.GetType().GetProperty(key);
        
        if (property == null)
            throw new ArgumentException($"Setting '{key}' not found");
        
        return (T)property.GetValue(config);
    }
    
    public async Task SetSettingAsync<T>(string key, T value)
    {
        await _configLock.WaitAsync();
        try
        {
            var config = _currentConfig ?? await LoadConfigurationAsync();
            var property = config.GetType().GetProperty(key);
            
            if (property == null)
                throw new ArgumentException($"Setting '{key}' not found");
            
            property.SetValue(config, value);
            await SaveConfigurationInternalAsync(config);
        }
        finally
        {
            _configLock.Release();
        }
    }
    
    public void ResetToDefaults()
    {
        _currentConfig = CreateDefaultConfiguration();
        SaveConfigurationAsync(_currentConfig).Wait();
    }
    
    private SaturnConfiguration CreateDefaultConfiguration()
    {
        return new SaturnConfiguration
        {
            DefaultProvider = "OpenRouter",
            DefaultModel = "anthropic/claude-sonnet-4", // Updated to match SaturnFork default
            Temperature = 0.7,
            MaxTokens = 4096,
            EnableStreaming = true,
            AutoApproveCommands = false,
            MaxSubAgents = 5,
            SessionTimeout = TimeSpan.FromHours(1),
            LogLevel = "Information",
            EnableTelemetry = true
        };
    }
}

public class SaturnConfiguration
{
    // Provider settings
    public string DefaultProvider { get; set; }
    public string DefaultModel { get; set; }
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    
    // Model parameters
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; } = 1.0;
    
    // Behavior settings
    public bool EnableStreaming { get; set; }
    public bool AutoApproveCommands { get; set; }
    public int MaxIterations { get; set; } = 10;
    public int MaxSubAgents { get; set; }
    
    // System settings
    public TimeSpan SessionTimeout { get; set; }
    public string LogLevel { get; set; }
    public bool EnableTelemetry { get; set; }
    public string WorkingDirectory { get; set; }
    
    // Tool settings
    public List<string> EnabledTools { get; set; } = new();
    public Dictionary<string, object> ToolConfigurations { get; set; } = new();
}

public class ProviderSettings
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
```

### 3. Command Approval Service

#### 3.1 Web-Based Command Approval
**Location**: `src/OrchestratorChat.Saturn/Services/CommandApprovalService.cs`

**Purpose**: Handle dangerous command approval in web context

**Implementation**:
```csharp
public interface ICommandApprovalService
{
    Task<bool> RequestApprovalAsync(CommandApprovalRequest request);
    void SetApprovalMode(ApprovalMode mode);
    Task<List<PendingApproval>> GetPendingApprovalsAsync();
    Task ApproveCommandAsync(string approvalId, bool approved, string userId);
}

public class CommandApprovalService : ICommandApprovalService
{
    private readonly ILogger<CommandApprovalService> _logger;
    private readonly Dictionary<string, PendingApproval> _pendingApprovals = new();
    private readonly Dictionary<string, TaskCompletionSource<bool>> _approvalTasks = new();
    private ApprovalMode _mode = ApprovalMode.Ask;
    private readonly HashSet<string> _approvedCommands = new();
    
    public event EventHandler<CommandApprovalRequest> ApprovalRequested;
    public event EventHandler<ApprovalDecision> ApprovalDecided;
    
    public async Task<bool> RequestApprovalAsync(CommandApprovalRequest request)
    {
        switch (_mode)
        {
            case ApprovalMode.Always:
                LogApproval(request, true, "Auto-approved (Always mode)");
                return true;
            
            case ApprovalMode.Never:
                LogApproval(request, false, "Auto-denied (Never mode)");
                return false;
            
            case ApprovalMode.Once:
                if (_approvedCommands.Contains(request.Command))
                {
                    LogApproval(request, true, "Previously approved");
                    return true;
                }
                break;
        }
        
        // Create pending approval
        var approvalId = Guid.NewGuid().ToString();
        var pending = new PendingApproval
        {
            Id = approvalId,
            Request = request,
            RequestedAt = DateTime.UtcNow,
            Status = ApprovalStatus.Pending
        };
        
        _pendingApprovals[approvalId] = pending;
        
        // Create task completion source for async waiting
        var tcs = new TaskCompletionSource<bool>();
        _approvalTasks[approvalId] = tcs;
        
        // Raise event for UI notification
        ApprovalRequested?.Invoke(this, request);
        
        // Set timeout
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        cts.Token.Register(() =>
        {
            if (_approvalTasks.TryRemove(approvalId, out var task))
            {
                task.TrySetResult(false); // Timeout = denied
                pending.Status = ApprovalStatus.Timeout;
            }
        });
        
        // Wait for approval
        var approved = await tcs.Task;
        
        if (approved && _mode == ApprovalMode.Once)
        {
            _approvedCommands.Add(request.Command);
        }
        
        return approved;
    }
    
    public async Task ApproveCommandAsync(string approvalId, bool approved, string userId)
    {
        if (_pendingApprovals.TryGetValue(approvalId, out var pending))
        {
            pending.Status = approved ? ApprovalStatus.Approved : ApprovalStatus.Denied;
            pending.DecidedBy = userId;
            pending.DecidedAt = DateTime.UtcNow;
            
            if (_approvalTasks.TryRemove(approvalId, out var tcs))
            {
                tcs.TrySetResult(approved);
            }
            
            var decision = new ApprovalDecision
            {
                ApprovalId = approvalId,
                Approved = approved,
                DecidedBy = userId,
                Reason = approved ? "User approved" : "User denied"
            };
            
            ApprovalDecided?.Invoke(this, decision);
            LogApproval(pending.Request, approved, $"Decided by {userId}");
        }
    }
    
    public Task<List<PendingApproval>> GetPendingApprovalsAsync()
    {
        var pending = _pendingApprovals.Values
            .Where(p => p.Status == ApprovalStatus.Pending)
            .OrderBy(p => p.RequestedAt)
            .ToList();
        
        return Task.FromResult(pending);
    }
    
    public void SetApprovalMode(ApprovalMode mode)
    {
        _mode = mode;
        _logger.LogInformation("Approval mode changed to: {Mode}", mode);
    }
    
    private void LogApproval(CommandApprovalRequest request, bool approved, string reason)
    {
        _logger.LogInformation(
            "Command approval: {Tool} - {Command} - {Approved} - {Reason}",
            request.ToolName,
            request.Command,
            approved ? "Approved" : "Denied",
            reason);
    }
}

public class CommandApprovalRequest
{
    public string ToolName { get; set; }
    public string Command { get; set; }
    public string Reason { get; set; }
    public string AgentId { get; set; }
    public string SessionId { get; set; }
    public Dictionary<string, object> Context { get; set; }
}

public class PendingApproval
{
    public string Id { get; set; }
    public CommandApprovalRequest Request { get; set; }
    public DateTime RequestedAt { get; set; }
    public ApprovalStatus Status { get; set; }
    public string DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class ApprovalDecision
{
    public string ApprovalId { get; set; }
    public bool Approved { get; set; }
    public string DecidedBy { get; set; }
    public string Reason { get; set; }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Denied,
    Timeout
}

public enum ApprovalMode
{
    Always,  // Always approve
    Never,   // Never approve
    Ask,     // Ask for each command
    Once     // Ask once per unique command
}
```

### 4. Error Handling Framework

#### 4.1 Saturn Exception Types
**Location**: `src/OrchestratorChat.Saturn/Core/Exceptions/`

```csharp
public class SaturnException : Exception
{
    public string ErrorCode { get; set; }
    public ErrorSeverity Severity { get; set; }
    public Dictionary<string, object> Context { get; set; }
    
    public SaturnException(string message, string errorCode = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Context = new Dictionary<string, object>();
    }
    
    public SaturnException(string message, Exception innerException, string errorCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = new Dictionary<string, object>();
    }
}

public class ProviderException : SaturnException
{
    public string Provider { get; set; }
    public int? StatusCode { get; set; }
    
    public ProviderException(string provider, string message, int? statusCode = null)
        : base(message, "PROVIDER_ERROR")
    {
        Provider = provider;
        StatusCode = statusCode;
        Severity = ErrorSeverity.Error;
    }
}

public class ToolExecutionException : SaturnException
{
    public string ToolName { get; set; }
    public string Parameters { get; set; }
    
    public ToolExecutionException(string toolName, string message, Exception innerException = null)
        : base(message, innerException, "TOOL_EXECUTION_ERROR")
    {
        ToolName = toolName;
        Severity = ErrorSeverity.Warning;
    }
}

public class ConfigurationException : SaturnException
{
    public string ConfigKey { get; set; }
    
    public ConfigurationException(string message, string configKey = null)
        : base(message, "CONFIG_ERROR")
    {
        ConfigKey = configKey;
        Severity = ErrorSeverity.Critical;
    }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
```

#### 4.2 Global Error Handler
**Location**: `src/OrchestratorChat.Saturn/Core/ErrorHandler.cs`

```csharp
public interface IErrorHandler
{
    void HandleError(Exception exception, ErrorContext context);
    Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, ErrorContext context);
    void RegisterErrorCallback(Action<ErrorReport> callback);
}

public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly List<Action<ErrorReport>> _errorCallbacks = new();
    
    public void HandleError(Exception exception, ErrorContext context)
    {
        var report = CreateErrorReport(exception, context);
        
        // Log the error
        LogError(report);
        
        // Notify callbacks
        foreach (var callback in _errorCallbacks)
        {
            try
            {
                callback(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in error callback");
            }
        }
    }
    
    public async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation, 
        ErrorContext context)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            throw; // Don't handle cancellation
        }
        catch (Exception ex)
        {
            HandleError(ex, context);
            throw;
        }
    }
    
    public void RegisterErrorCallback(Action<ErrorReport> callback)
    {
        _errorCallbacks.Add(callback);
    }
    
    private ErrorReport CreateErrorReport(Exception exception, ErrorContext context)
    {
        return new ErrorReport
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Exception = exception,
            Context = context,
            Severity = DetermineSeverity(exception),
            StackTrace = exception.StackTrace,
            Source = exception.Source
        };
    }
    
    private ErrorSeverity DetermineSeverity(Exception exception)
    {
        return exception switch
        {
            SaturnException se => se.Severity,
            OperationCanceledException => ErrorSeverity.Info,
            UnauthorizedAccessException => ErrorSeverity.Error,
            OutOfMemoryException => ErrorSeverity.Critical,
            _ => ErrorSeverity.Error
        };
    }
    
    private void LogError(ErrorReport report)
    {
        var logLevel = report.Severity switch
        {
            ErrorSeverity.Info => LogLevel.Information,
            ErrorSeverity.Warning => LogLevel.Warning,
            ErrorSeverity.Error => LogLevel.Error,
            ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Error
        };
        
        _logger.Log(logLevel, report.Exception, 
            "Error in {Component}: {Message}",
            report.Context.Component,
            report.Exception.Message);
    }
}

public class ErrorContext
{
    public string Component { get; set; }
    public string Operation { get; set; }
    public string AgentId { get; set; }
    public string SessionId { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class ErrorReport
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Exception Exception { get; set; }
    public ErrorContext Context { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string StackTrace { get; set; }
    public string Source { get; set; }
}
```

### 5. Service Registration

#### 5.1 Dependency Injection Setup
**Location**: `src/OrchestratorChat.Saturn/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSaturnServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Core services
        services.AddSingleton<IGitManager, GitManager>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ICommandApprovalService, CommandApprovalService>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        
        // Provider services
        services.AddSingleton<ProviderFactory>();
        services.AddScoped<ILLMProvider, AnthropicProvider>();
        services.AddScoped<ILLMProvider, OpenRouterProvider>();
        
        // Tool services
        services.AddSingleton<ToolRegistry>();
        services.AddTransient<ITool, ReadFileTool>();
        services.AddTransient<ITool, WriteFileTool>();
        services.AddTransient<ITool, ExecuteCommandTool>();
        // ... register all tools
        
        // Agent services
        services.AddScoped<IAgentManager, AgentManager>();
        services.AddTransient<IAgent, ManagedAgent>();
        
        // Data services
        services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
        services.AddSingleton<ISessionCache, SessionCache>();
        
        // Configuration
        services.Configure<SaturnConfiguration>(
            configuration.GetSection("Saturn"));
        
        // HTTP clients
        services.AddHttpClient<HttpClientAdapter>();
        
        // Memory cache
        services.AddMemoryCache();
        
        // Background services
        services.AddHostedService<SessionCleanupService>();
        
        return services;
    }
}
```

### 6. Background Services

#### 6.1 Session Cleanup Service
**Location**: `src/OrchestratorChat.Saturn/Services/SessionCleanupService.cs`

```csharp
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                await CleanupExpiredSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session cleanup");
            }
        }
    }
    
    private async Task CleanupExpiredSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IChatHistoryRepository>();
        var agentManager = scope.ServiceProvider.GetRequiredService<IAgentManager>();
        
        // Clean up expired sessions
        var expiredSessions = await repository.GetExpiredSessionsAsync();
        
        foreach (var session in expiredSessions)
        {
            // Terminate associated agents
            await agentManager.TerminateAgentAsync(session.AgentId);
            
            // Archive session
            await repository.ArchiveSessionAsync(session.Id);
        }
        
        _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count());
    }
}
```

## Implementation Priority

### Phase 1: Core Infrastructure (Day 1-2)
1. Implement GitManager
2. Create ConfigurationService
3. Set up DI container

### Phase 2: Approval System (Day 3)
1. Implement CommandApprovalService
2. Create approval UI integration
3. Add event handling

### Phase 3: Error Handling (Day 4)
1. Create exception hierarchy
2. Implement ErrorHandler
3. Add global error handling

### Phase 4: Background Services (Day 5)
1. Implement session cleanup
2. Add telemetry service
3. Create health checks

## Testing Requirements

### Unit Tests
- Git repository detection
- Configuration persistence
- Approval workflow
- Error handling scenarios

### Integration Tests
- Service registration
- Background service execution
- End-to-end approval flow

## Dependencies to Add

```xml
<PackageReference Include="LibGit2Sharp" Version="0.27.2" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
```

## Validation Checklist

- [ ] Git management functional
- [ ] Configuration persistence working
- [ ] Command approval integrated
- [ ] Error handling comprehensive
- [ ] Services registered properly
- [ ] Background services running
- [ ] Tests passing