using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OrchestratorChat.Agents.Tools;

/// <summary>
/// Service for managing command approval workflows with policy-based approval,
/// caching, and thread-safe operations for concurrent agent use.
/// </summary>
public class CommandApprovalService : ICommandApprovalService
{
    private readonly ILogger<CommandApprovalService> _logger;
    
    // Thread-safe collections for concurrent access
    private readonly ConcurrentDictionary<string, ApprovalResult> _approvalCache = new();
    private readonly ConcurrentBag<ApprovalResult> _approvalHistory = new();
    private readonly ConcurrentDictionary<string, bool> _toolAutoApproval = new();
    private readonly ConcurrentDictionary<string, Regex> _whitelistPatterns = new();
    private readonly ConcurrentDictionary<string, Regex> _blacklistPatterns = new();
    
    // Pending approval requests for UI handling
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ApprovalResult>> _pendingApprovals = new();
    private readonly ConcurrentDictionary<string, ApprovalRequestEventArgs> _pendingRequests = new();
    
    private volatile ApprovalPolicy _currentPolicy = ApprovalPolicy.RequireUserApproval;
    private readonly object _policyLock = new();
    private ApprovalSettings _settings = new();
    private readonly object _settingsLock = new();
    
    /// <summary>
    /// Event fired when an approval request needs UI handling
    /// </summary>
    public event EventHandler<ApprovalRequestEventArgs>? ApprovalRequested;
    
    public CommandApprovalService(ILogger<CommandApprovalService> logger)
    {
        _logger = logger;
        InitializeDefaultSettings();
    }
    
    /// <summary>
    /// Configure the approval service settings
    /// </summary>
    public void ConfigureSettings(ApprovalSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        
        lock (_settingsLock)
        {
            _settings = settings;
        }
        
        _logger.LogInformation("Approval settings configured. YOLO Mode: {YoloMode}, Dangerous Only: {DangerousOnly}, Timeout: {Timeout}",
            settings.EnableYoloMode, settings.RequireApprovalForDangerousOnly, settings.DefaultApprovalTimeout);
    }
    
    /// <summary>
    /// Get current approval settings
    /// </summary>
    public ApprovalSettings GetSettings()
    {
        lock (_settingsLock)
        {
            return new ApprovalSettings
            {
                EnableYoloMode = _settings.EnableYoloMode,
                DefaultApprovalTimeout = _settings.DefaultApprovalTimeout,
                RequireApprovalForDangerousOnly = _settings.RequireApprovalForDangerousOnly,
                MaxPendingRequests = _settings.MaxPendingRequests
            };
        }
    }
    
    /// <summary>
    /// Handle approval response from UI
    /// </summary>
    public bool HandleApprovalResponse(string requestId, bool approved, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }
        
        if (!_pendingApprovals.TryRemove(requestId, out var tcs))
        {
            _logger.LogWarning("Approval response received for unknown request ID: {RequestId}", requestId);
            return false;
        }
        
        _pendingRequests.TryRemove(requestId, out _);
        
        var result = new ApprovalResult
        {
            RequestId = requestId,
            Approved = approved,
            Reason = reason ?? (approved ? "Approved by user" : "Denied by user"),
            CacheResult = false // Don't cache user decisions
        };
        
        _logger.LogInformation("Approval response handled for request {RequestId}: {Status}", requestId, approved ? "Approved" : "Denied");
        
        tcs.SetResult(result);
        return true;
    }
    
    /// <summary>
    /// Initialize default safe commands and settings
    /// </summary>
    private void InitializeDefaultSettings()
    {
        // Configure safe read-only tools for auto-approval
        ConfigureToolAutoApproval("file_read", true);
        ConfigureToolAutoApproval("list_files", true);
        ConfigureToolAutoApproval("glob", true);
        ConfigureToolAutoApproval("grep", true);
        ConfigureToolAutoApproval("web_search", true);
        
        // Add common safe commands to whitelist
        AddToWhitelist(@"^ls\s");
        AddToWhitelist(@"^dir\s");
        AddToWhitelist(@"^pwd$");
        AddToWhitelist(@"^echo\s");
        AddToWhitelist(@"^cat\s.*\.txt$");
        AddToWhitelist(@"^head\s");
        AddToWhitelist(@"^tail\s");
        
        // Add dangerous commands to blacklist
        AddToBlacklist(@"^rm\s+-rf\s+/");
        AddToBlacklist(@"^del\s+/s\s+/q\s+\*");
        AddToBlacklist(@"^format\s");
        AddToBlacklist(@"^fdisk\s");
        AddToBlacklist(@"^mkfs\s");
        AddToBlacklist(@"^dd\s+if=.*of=");
    }
    
    /// <summary>
    /// Request approval for a dangerous operation
    /// </summary>
    public async Task<ApprovalResult> RequestApprovalAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        
        _logger.LogDebug("Approval requested for tool: {ToolName}, command: {Command}", 
            context.ToolName, context.Command);
        
        var settings = GetSettings();
        
        // YOLO MODE: Auto-approve everything (dangerous but convenient for development)
        if (settings.EnableYoloMode)
        {
            var yoloResult = new ApprovalResult
            {
                Approved = true,
                Reason = "YOLO Mode enabled - auto-approving all operations",
                CacheResult = false // Don't cache YOLO decisions
            };
            
            _logger.LogWarning("YOLO MODE: Auto-approved operation for tool: {ToolName}, command: {Command}", 
                context.ToolName, context.Command);
            
            _approvalHistory.Add(yoloResult);
            return yoloResult;
        }
        
        // Check if we should only require approval for dangerous operations
        var isDangerous = IsDangerousOperation(context);
        if (settings.RequireApprovalForDangerousOnly && !isDangerous)
        {
            var safeResult = new ApprovalResult
            {
                Approved = true,
                Reason = "Operation not considered dangerous, auto-approved",
                CacheResult = true
            };
            
            _approvalHistory.Add(safeResult);
            return safeResult;
        }
        
        // Check cache first for identical requests
        var cacheKey = GenerateCacheKey(context);
        if (_approvalCache.TryGetValue(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Using cached approval result for: {Command}", context.Command);
            return cachedResult;
        }
        
        // Check tool auto-approval settings
        if (IsToolAutoApproved(context.ToolName))
        {
            var autoApprovalResult = new ApprovalResult
            {
                Approved = true,
                Reason = $"Tool '{context.ToolName}' is configured for auto-approval",
                CacheResult = true
            };
            
            CacheAndLogResult(cacheKey, autoApprovalResult, context);
            return autoApprovalResult;
        }
        
        // Check whitelist/blacklist
        if (IsWhitelisted(context.Command))
        {
            var whitelistResult = new ApprovalResult
            {
                Approved = true,
                Reason = "Command matches whitelist pattern",
                CacheResult = true
            };
            
            CacheAndLogResult(cacheKey, whitelistResult, context);
            return whitelistResult;
        }
        
        if (IsBlacklisted(context.Command))
        {
            var blacklistResult = new ApprovalResult
            {
                Approved = false,
                Reason = "Command matches blacklist pattern",
                CacheResult = true
            };
            
            CacheAndLogResult(cacheKey, blacklistResult, context);
            return blacklistResult;
        }
        
        // Apply current policy
        var policyResult = await ApplyApprovalPolicyAsync(context, isDangerous, cancellationToken);
        CacheAndLogResult(cacheKey, policyResult, context);
        
        return policyResult;
    }
    
    /// <summary>
    /// Determine if an operation is considered dangerous
    /// </summary>
    private bool IsDangerousOperation(ApprovalContext context)
    {
        // Consider blacklisted commands as dangerous
        if (IsBlacklisted(context.Command))
        {
            return true;
        }
        
        // Consider certain tools as potentially dangerous
        var dangerousTools = new[] { "bash", "powershell", "command_execution", "file_write", "file_delete" };
        if (dangerousTools.Contains(context.ToolName.ToLowerInvariant()))
        {
            return true;
        }
        
        // Check for dangerous command patterns
        var dangerousPatterns = new[]
        {
            @"\brm\s+", @"\bdel\s+", @"\bformat\s+", @"\bfdisk\s+",
            @"\bmkfs\s+", @"\bdd\s+", @"\breboot\s*$", @"\bshutdown\s+",
            @"\bsudo\s+", @"\breadpasswd\s+", @"\bchmod\s+777",
            @"\bwget\s+.*\|.*sh", @"\bcurl\s+.*\|.*sh"
        };
        
        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(context.Command, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Apply the current approval policy
    /// </summary>
    private async Task<ApprovalResult> ApplyApprovalPolicyAsync(ApprovalContext context, bool isDangerous, CancellationToken cancellationToken)
    {
        ApprovalPolicy currentPolicy;
        lock (_policyLock)
        {
            currentPolicy = _currentPolicy;
        }
        
        switch (currentPolicy)
        {
            case ApprovalPolicy.AlwaysApprove:
                return new ApprovalResult
                {
                    Approved = true,
                    Reason = "Policy set to always approve",
                    CacheResult = false // Don't cache policy-based decisions
                };
                
            case ApprovalPolicy.AlwaysDeny:
                return new ApprovalResult
                {
                    Approved = false,
                    Reason = "Policy set to always deny",
                    CacheResult = false
                };
                
            case ApprovalPolicy.RequireUserApproval:
                return await RequestUserApprovalAsync(context, isDangerous, cancellationToken);
                
            default:
                _logger.LogWarning("Unknown approval policy: {Policy}", currentPolicy);
                return new ApprovalResult
                {
                    Approved = false,
                    Reason = "Unknown approval policy",
                    CacheResult = false
                };
        }
    }
    
    /// <summary>
    /// Request user approval via UI
    /// </summary>
    private async Task<ApprovalResult> RequestUserApprovalAsync(ApprovalContext context, bool isDangerous, CancellationToken cancellationToken)
    {
        // Check if we have too many pending requests
        var settings = GetSettings();
        if (_pendingApprovals.Count >= settings.MaxPendingRequests)
        {
            _logger.LogWarning("Too many pending approval requests ({Count}/{Max}). Denying request.", 
                _pendingApprovals.Count, settings.MaxPendingRequests);
            
            return new ApprovalResult
            {
                Approved = false,
                Reason = "Too many pending approval requests",
                CacheResult = false
            };
        }
        
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<ApprovalResult>();
        
        var eventArgs = new ApprovalRequestEventArgs
        {
            RequestId = requestId,
            Context = context,
            IsDangerous = isDangerous,
            Timeout = settings.DefaultApprovalTimeout
        };
        
        _pendingApprovals.TryAdd(requestId, tcs);
        _pendingRequests.TryAdd(requestId, eventArgs);
        
        _logger.LogInformation("Requesting UI approval for operation: {ToolName} - {Command} (Request ID: {RequestId})", 
            context.ToolName, context.Command, requestId);
        
        // Fire event for UI to handle
        try
        {
            ApprovalRequested?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing ApprovalRequested event");
            
            // Clean up
            _pendingApprovals.TryRemove(requestId, out _);
            _pendingRequests.TryRemove(requestId, out _);
            
            return new ApprovalResult
            {
                RequestId = requestId,
                Approved = false,
                Reason = "Error processing approval request",
                CacheResult = false
            };
        }
        
        // Wait for response with timeout
        using var timeoutCts = new CancellationTokenSource(settings.DefaultApprovalTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        try
        {
            var result = await tcs.Task.WaitAsync(combinedCts.Token);
            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            // Timeout occurred
            _pendingApprovals.TryRemove(requestId, out _);
            _pendingRequests.TryRemove(requestId, out _);
            
            _logger.LogWarning("Approval request {RequestId} timed out after {Timeout}", requestId, settings.DefaultApprovalTimeout);
            
            return new ApprovalResult
            {
                RequestId = requestId,
                Approved = false,
                Reason = $"Approval request timed out after {settings.DefaultApprovalTimeout}",
                CacheResult = false
            };
        }
        catch (OperationCanceledException)
        {
            // External cancellation
            _pendingApprovals.TryRemove(requestId, out _);
            _pendingRequests.TryRemove(requestId, out _);
            
            return new ApprovalResult
            {
                RequestId = requestId,
                Approved = false,
                Reason = "Approval request cancelled",
                CacheResult = false
            };
        }
    }
    
    /// <summary>
    /// Generate cache key for approval context
    /// </summary>
    private string GenerateCacheKey(ApprovalContext context)
    {
        // Include dangerous flag in cache key to differentiate dangerous vs safe operations
        var isDangerous = IsDangerousOperation(context);
        return $"{context.ToolName}|{context.Command}|{context.WorkingDirectory}|{isDangerous}";
    }
    
    /// <summary>
    /// Cache and log approval result
    /// </summary>
    private void CacheAndLogResult(string cacheKey, ApprovalResult result, ApprovalContext context)
    {
        if (result.CacheResult)
        {
            _approvalCache.TryAdd(cacheKey, result);
        }
        
        // Set RequestId if not already set
        if (string.IsNullOrEmpty(result.RequestId))
        {
            result.RequestId = Guid.NewGuid().ToString();
        }
        
        _approvalHistory.Add(result);
        
        _logger.LogInformation("Approval {Status} for tool: {ToolName}, command: {Command}, reason: {Reason}",
            result.Approved ? "granted" : "denied",
            context.ToolName,
            context.Command,
            result.Reason);
    }
    
    /// <summary>
    /// Set the approval policy for all operations
    /// </summary>
    public void SetApprovalPolicy(ApprovalPolicy policy)
    {
        lock (_policyLock)
        {
            _currentPolicy = policy;
        }
        
        _logger.LogInformation("Approval policy changed to: {Policy}", policy);
    }
    
    /// <summary>
    /// Get the current approval policy
    /// </summary>
    public ApprovalPolicy GetApprovalPolicy()
    {
        lock (_policyLock)
        {
            return _currentPolicy;
        }
    }
    
    /// <summary>
    /// Add a command to the whitelist (automatically approved)
    /// </summary>
    public void AddToWhitelist(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command pattern cannot be null or whitespace", nameof(command));
        }
        
        try
        {
            var regex = new Regex(command, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _whitelistPatterns.TryAdd(command, regex);
            _logger.LogDebug("Added command pattern to whitelist: {Pattern}", command);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid regex pattern for whitelist: {Pattern}", command);
            throw;
        }
    }
    
    /// <summary>
    /// Add a command to the blacklist (automatically denied)
    /// </summary>
    public void AddToBlacklist(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command pattern cannot be null or whitespace", nameof(command));
        }
        
        try
        {
            var regex = new Regex(command, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _blacklistPatterns.TryAdd(command, regex);
            _logger.LogDebug("Added command pattern to blacklist: {Pattern}", command);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid regex pattern for blacklist: {Pattern}", command);
            throw;
        }
    }
    
    /// <summary>
    /// Remove a command from the whitelist
    /// </summary>
    public void RemoveFromWhitelist(string command)
    {
        if (_whitelistPatterns.TryRemove(command, out _))
        {
            _logger.LogDebug("Removed command pattern from whitelist: {Pattern}", command);
        }
    }
    
    /// <summary>
    /// Remove a command from the blacklist
    /// </summary>
    public void RemoveFromBlacklist(string command)
    {
        if (_blacklistPatterns.TryRemove(command, out _))
        {
            _logger.LogDebug("Removed command pattern from blacklist: {Pattern}", command);
        }
    }
    
    /// <summary>
    /// Check if a command is whitelisted
    /// </summary>
    public bool IsWhitelisted(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }
        
        return _whitelistPatterns.Values.Any(regex => regex.IsMatch(command));
    }
    
    /// <summary>
    /// Check if a command is blacklisted
    /// </summary>
    public bool IsBlacklisted(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }
        
        return _blacklistPatterns.Values.Any(regex => regex.IsMatch(command));
    }
    
    /// <summary>
    /// Clear the approval cache
    /// </summary>
    public void ClearApprovalCache()
    {
        _approvalCache.Clear();
        _logger.LogInformation("Approval cache cleared");
    }
    
    /// <summary>
    /// Get approval history for auditing
    /// </summary>
    public Task<List<ApprovalResult>> GetApprovalHistoryAsync(string? sessionId = null, string? agentId = null)
    {
        var history = _approvalHistory.ToList();
        
        // Note: Filtering by sessionId and agentId would require storing this information
        // in ApprovalResult. For now, return all history.
        // This can be enhanced when the full approval context is stored.
        
        _logger.LogDebug("Retrieved approval history with {Count} entries", history.Count);
        return Task.FromResult(history);
    }
    
    /// <summary>
    /// Configure auto-approval for safe operations
    /// </summary>
    public void ConfigureToolAutoApproval(string toolName, bool autoApprove)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));
        }
        
        _toolAutoApproval.AddOrUpdate(toolName, autoApprove, (key, oldValue) => autoApprove);
        
        _logger.LogDebug("Tool '{ToolName}' auto-approval set to: {AutoApprove}", toolName, autoApprove);
    }
    
    /// <summary>
    /// Check if a tool is configured for auto-approval
    /// </summary>
    public bool IsToolAutoApproved(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }
        
        return _toolAutoApproval.TryGetValue(toolName, out var autoApprove) && autoApprove;
    }
}