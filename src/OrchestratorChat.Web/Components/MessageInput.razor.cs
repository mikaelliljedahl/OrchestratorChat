using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace OrchestratorChat.Web.Components;

public partial class MessageInput : ComponentBase
{
    [Parameter] public EventCallback<string> OnSendMessage { get; set; }
    [Parameter] public EventCallback OnAttach { get; set; }
    [Parameter] public bool IsEnabled { get; set; } = true;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public string? ErrorAction { get; set; }
    
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    
    private string _currentMessage = string.Empty;
    private int _lines = 1;
    private List<string> _attachments = new();
    private MudTextField<string> _textField = null!;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _textField.FocusAsync();
        }
    }
    
    private async Task OnKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (e.ShiftKey)
            {
                // Allow new line with Shift+Enter
                _lines = Math.Min(_lines + 1, 5);
                return;
            }
            
            // Send message with Enter
            await SendMessage();
        }
    }
    
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_currentMessage) || !IsEnabled)
            return;
            
        var message = _currentMessage;
        _currentMessage = string.Empty;
        _lines = 1;
        _attachments.Clear();
        
        if (OnSendMessage.HasDelegate)
        {
            await OnSendMessage.InvokeAsync(message);
        }
        
        await _textField.FocusAsync();
    }
    
    private async Task AttachFile()
    {
        if (OnAttach.HasDelegate)
        {
            await OnAttach.InvokeAsync();
        }
    }
    
    private void RemoveAttachment(string attachment)
    {
        _attachments.Remove(attachment);
    }
    
    public void AddAttachment(string filePath)
    {
        if (!_attachments.Contains(filePath))
        {
            _attachments.Add(filePath);
            StateHasChanged();
        }
    }
    
    private string GetPlaceholderText()
    {
        if (!IsEnabled && !string.IsNullOrEmpty(ErrorMessage))
        {
            return $"Can't send yet — {ErrorMessage}";
        }
        return "Send a message…";
    }
}