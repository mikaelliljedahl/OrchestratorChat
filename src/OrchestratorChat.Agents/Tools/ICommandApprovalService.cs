namespace OrchestratorChat.Agents.Tools;

/// <summary>
/// Approval policies for command execution
/// </summary>
public enum ApprovalPolicy
{
    /// <summary>
    /// Always approve all commands without prompting
    /// </summary>
    AlwaysApprove,
    
    /// <summary>
    /// Always deny all commands
    /// </summary>
    AlwaysDeny,
    
    /// <summary>
    /// Require user approval for each command
    /// </summary>
    RequireUserApproval
}

/// <summary>
/// Context information for approval requests
/// </summary>
public class ApprovalContext
{
    /// <summary>
    /// Name of the tool requesting approval
    /// </summary>
    public string ToolName { get; set; } = string.Empty;
    
    /// <summary>
    /// Command or operation being requested
    /// </summary>
    public string Command { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameters associated with the operation
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Reason for the approval request
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent ID making the request
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID for context
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Working directory where the operation will be performed
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Result of an approval request
/// </summary>
public class ApprovalResult
{
    /// <summary>
    /// Whether the operation was approved
    /// </summary>
    public bool Approved { get; set; }
    
    /// <summary>
    /// Reason for the decision
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this approval should be cached for similar future requests
    /// </summary>
    public bool CacheResult { get; set; }
    
    /// <summary>
    /// Time when the approval was granted or denied
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Unique identifier for tracking this approval request
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Event arguments for approval requests that need UI handling
/// </summary>
public class ApprovalRequestEventArgs : EventArgs
{
    /// <summary>
    /// Unique identifier for this approval request
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// Context information for the approval request
    /// </summary>
    public ApprovalContext Context { get; set; } = new();
    
    /// <summary>
    /// Whether this operation is considered dangerous
    /// </summary>
    public bool IsDangerous { get; set; }
    
    /// <summary>
    /// Timeout for this approval request
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Configuration settings for command approval service
/// </summary>
public class ApprovalSettings
{
    /// <summary>
    /// YOLO mode - bypasses all approval checks (dangerous but convenient for development)
    /// </summary>
    public bool EnableYoloMode { get; set; } = false;
    
    /// <summary>
    /// Default timeout for UI approval requests
    /// </summary>
    public TimeSpan DefaultApprovalTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Only require approval for operations marked as dangerous
    /// </summary>
    public bool RequireApprovalForDangerousOnly { get; set; } = false;
    
    /// <summary>
    /// Maximum number of pending approval requests
    /// </summary>
    public int MaxPendingRequests { get; set; } = 10;
}

/// <summary>
/// Service for managing command approval workflows
/// </summary>
public interface ICommandApprovalService
{
    /// <summary>
    /// Event fired when an approval request needs UI handling
    /// </summary>
    event EventHandler<ApprovalRequestEventArgs>? ApprovalRequested;
    
    /// <summary>
    /// Configure the approval service settings
    /// </summary>
    /// <param name="settings">Approval settings</param>
    void ConfigureSettings(ApprovalSettings settings);
    
    /// <summary>
    /// Get current approval settings
    /// </summary>
    /// <returns>Current approval settings</returns>
    ApprovalSettings GetSettings();
    
    /// <summary>
    /// Handle approval response from UI
    /// </summary>
    /// <param name="requestId">Unique request identifier</param>
    /// <param name="approved">Whether the operation was approved</param>
    /// <param name="reason">Optional reason for the decision</param>
    /// <returns>True if the request was found and handled</returns>
    bool HandleApprovalResponse(string requestId, bool approved, string? reason = null);
    /// <summary>
    /// Request approval for a dangerous operation
    /// </summary>
    /// <param name="context">Context information for the approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approval result</returns>
    Task<ApprovalResult> RequestApprovalAsync(ApprovalContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the approval policy for all operations
    /// </summary>
    /// <param name="policy">Policy to apply</param>
    void SetApprovalPolicy(ApprovalPolicy policy);
    
    /// <summary>
    /// Get the current approval policy
    /// </summary>
    /// <returns>Current approval policy</returns>
    ApprovalPolicy GetApprovalPolicy();
    
    /// <summary>
    /// Add a command to the whitelist (automatically approved)
    /// </summary>
    /// <param name="command">Command pattern to whitelist</param>
    void AddToWhitelist(string command);
    
    /// <summary>
    /// Add a command to the blacklist (automatically denied)
    /// </summary>
    /// <param name="command">Command pattern to blacklist</param>
    void AddToBlacklist(string command);
    
    /// <summary>
    /// Remove a command from the whitelist
    /// </summary>
    /// <param name="command">Command pattern to remove</param>
    void RemoveFromWhitelist(string command);
    
    /// <summary>
    /// Remove a command from the blacklist
    /// </summary>
    /// <param name="command">Command pattern to remove</param>
    void RemoveFromBlacklist(string command);
    
    /// <summary>
    /// Check if a command is whitelisted
    /// </summary>
    /// <param name="command">Command to check</param>
    /// <returns>True if whitelisted</returns>
    bool IsWhitelisted(string command);
    
    /// <summary>
    /// Check if a command is blacklisted
    /// </summary>
    /// <param name="command">Command to check</param>
    /// <returns>True if blacklisted</returns>
    bool IsBlacklisted(string command);
    
    /// <summary>
    /// Clear the approval cache
    /// </summary>
    void ClearApprovalCache();
    
    /// <summary>
    /// Get approval history for auditing
    /// </summary>
    /// <param name="sessionId">Optional session ID to filter by</param>
    /// <param name="agentId">Optional agent ID to filter by</param>
    /// <returns>List of approval results</returns>
    Task<List<ApprovalResult>> GetApprovalHistoryAsync(string? sessionId = null, string? agentId = null);
    
    /// <summary>
    /// Configure auto-approval for safe operations
    /// </summary>
    /// <param name="toolName">Tool name</param>
    /// <param name="autoApprove">Whether to auto-approve for this tool</param>
    void ConfigureToolAutoApproval(string toolName, bool autoApprove);
    
    /// <summary>
    /// Check if a tool is configured for auto-approval
    /// </summary>
    /// <param name="toolName">Tool name to check</param>
    /// <returns>True if auto-approved</returns>
    bool IsToolAutoApproved(string toolName);
}