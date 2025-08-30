namespace OrchestratorChat.Core.Configuration;

/// <summary>
/// SignalR configuration settings
/// </summary>
public class SignalRSettings
{
    /// <summary>
    /// Keep-alive interval in seconds
    /// </summary>
    public int KeepAliveInterval { get; set; } = 15;
    
    /// <summary>
    /// Client timeout interval in seconds
    /// </summary>
    public int ClientTimeoutInterval { get; set; } = 30;
    
    /// <summary>
    /// Handshake timeout in seconds
    /// </summary>
    public int HandshakeTimeout { get; set; } = 15;
    
    /// <summary>
    /// Whether to enable detailed error messages
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
    
    /// <summary>
    /// Maximum size of received messages in bytes
    /// </summary>
    public long MaximumReceiveMessageSize { get; set; } = 32 * 1024;
}