namespace OrchestratorChat.Core.Approvals;

/// <summary>
/// Type of operation requiring approval
/// </summary>
public enum ApprovalType
{
    /// <summary>
    /// General tool execution requiring approval
    /// </summary>
    ToolExecution,
    
    /// <summary>
    /// File modification operations
    /// </summary>
    FileModification,
    
    /// <summary>
    /// Dangerous command execution (rm -rf, format, etc.)
    /// </summary>
    DangerousCommand
}