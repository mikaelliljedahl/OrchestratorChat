using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Web.Components;

public partial class AttachmentChip : ComponentBase
{
    [Parameter] public Attachment Attachment { get; set; } = new();
    
    private string GetAttachmentIcon()
    {
        string icon = Icons.Material.Filled.AttachFile;
        return icon;
    }
    
    private string GetAttachmentName()
    {
        return Attachment.FileName ?? "File";
    }
    
    private void DownloadAttachment()
    {
        // TODO: Implement attachment download
    }
}