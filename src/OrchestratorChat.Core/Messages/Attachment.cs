namespace OrchestratorChat.Core.Messages;

/// <summary>
/// Represents a file attachment in a message
/// </summary>
public class Attachment
{
    /// <summary>
    /// Unique identifier for the attachment
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Original filename of the attachment
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// MIME type of the attachment
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of the attachment in bytes
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// Binary content of the attachment
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// URL to the attachment if stored externally
    /// </summary>
    public string Url { get; set; } = string.Empty;
}