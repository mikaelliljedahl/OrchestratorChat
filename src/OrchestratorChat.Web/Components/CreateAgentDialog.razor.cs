using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Web.Models;
using System.Text.Json;

namespace OrchestratorChat.Web.Components;

public partial class CreateAgentDialog : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<AgentConfiguration> OnAgentCreated { get; set; }
    
    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IAnthropicOAuthService AnthropicOAuthService { get; set; } = null!;
    [Inject] private OrchestratorChat.Web.Services.IAgentService AgentService { get; set; } = null!;
    
    private string _agentName = string.Empty;
    private AgentType _selectedType = AgentType.Claude;
    private string _workingDirectory = string.Empty;
    private string _selectedModel = "claude-sonnet-4-20250514";
    private string _selectedProvider = "OpenRouter";
    private string _systemPrompt = string.Empty;
    private double _temperature = 0.7;
    private int _maxTokens = 4096;
    private bool _requireApproval = false;
    
    private bool _isCreating = false;
    private string _nameError = string.Empty;
    
    // Anthropic OAuth state
    private OAuthStatus? _anthropicOAuthStatus = new OAuthStatus { Connected = false, ExpiresAt = null, Scopes = Array.Empty<string>() };
    private bool _oauthInProgress = false;
    private string? _authorizationUrl = null;
    private string? _oauthState = null;
    private string _authorizationCode = string.Empty;
    private bool _showCodeInput = false;
    
    private async Task OnVisibilityChanged(bool isVisible)
    {
        Console.WriteLine($"CreateAgentDialog.OnVisibilityChanged: Visibility changed to {isVisible}");
        IsVisible = isVisible;
        
        // Reset form when dialog opens
        if (isVisible)
        {
            ResetForm();
        }
        
        if (IsVisibleChanged.HasDelegate)
        {
            await IsVisibleChanged.InvokeAsync(isVisible);
        }
    }
    
    private async Task Cancel()
    {
        Console.WriteLine("CreateAgentDialog.Cancel: Cancelling dialog");
        ResetForm();
        await OnVisibilityChanged(false);
    }
    
    private void ResetForm()
    {
        Console.WriteLine("CreateAgentDialog.ResetForm: Resetting form fields");
        _agentName = string.Empty;
        _selectedType = AgentType.Claude;
        _workingDirectory = string.Empty;
        _selectedModel = "claude-sonnet-4-20250514";
        _selectedProvider = "OpenRouter";
        _systemPrompt = string.Empty;
        _temperature = 0.7;
        _maxTokens = 4096;
        _requireApproval = false;
        _nameError = string.Empty;
        _isCreating = false;
        
        // Reset OAuth state
        _anthropicOAuthStatus = new OAuthStatus { Connected = false, ExpiresAt = null, Scopes = Array.Empty<string>() };
        _oauthInProgress = false;
        _authorizationUrl = null;
        _oauthState = null;
        _authorizationCode = string.Empty;
        _showCodeInput = false;
        
        // Load OAuth status when form opens
        if (IsVisible)
        {
            _ = CheckAnthropicOAuthStatus();
        }
    }
    
    private async Task CreateAgent()
    {
        Console.WriteLine("CreateAgentDialog.CreateAgent: Method started");
        _nameError = string.Empty;
        
        if (string.IsNullOrWhiteSpace(_agentName))
        {
            _nameError = "Agent name is required";
            Console.WriteLine("CreateAgentDialog.CreateAgent: Agent name validation failed");
            return;
        }
        
        // Note: Don't block creation if disconnected - agent persists as "requires connect"
        // This allows users to create the agent configuration even if OAuth is not set up
        
        Console.WriteLine($"CreateAgentDialog.CreateAgent: Creating agent '{_agentName}' of type '{_selectedType}'");
        _isCreating = true;
        StateHasChanged();
        
        try
        {
            var configuration = new AgentConfiguration
            {
                Name = _agentName.Trim(),
                Type = _selectedType,
                WorkingDirectory = string.IsNullOrWhiteSpace(_workingDirectory) ? Environment.CurrentDirectory : _workingDirectory.Trim(),
                Model = _selectedModel,
                Temperature = _temperature,
                MaxTokens = _maxTokens,
                SystemPrompt = _systemPrompt.Trim(),
                RequireApproval = _requireApproval
            };
            
            // Add provider setting for Saturn agents
            if (_selectedType == AgentType.Saturn)
            {
                configuration.CustomSettings["Provider"] = _selectedProvider;
            }
            
            Console.WriteLine($"CreateAgentDialog.CreateAgent: Configuration created, invoking callback");
            
            // Notify parent with the configuration first
            Console.WriteLine($"CreateAgentDialog.CreateAgent: OnAgentCreated.HasDelegate = {OnAgentCreated.HasDelegate}");
            if (OnAgentCreated.HasDelegate)
            {
                Console.WriteLine("CreateAgentDialog.CreateAgent: Invoking OnAgentCreated callback");
                await OnAgentCreated.InvokeAsync(configuration);
            }
            
            Console.WriteLine($"CreateAgentDialog.CreateAgent: Closing dialog");
            // Reset form and close dialog after successful callback
            ResetForm();
            await OnVisibilityChanged(false);
            Console.WriteLine("CreateAgentDialog.CreateAgent: Method completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateAgentDialog.CreateAgent: Exception occurred: {ex.Message}");
            Console.WriteLine($"CreateAgentDialog.CreateAgent: Exception details: {ex}");
            _nameError = $"Error creating agent: {ex.Message}";
        }
        finally
        {
            _isCreating = false;
            StateHasChanged();
        }
    }
    
    private async Task OnProviderChanged(string provider)
    {
        _selectedProvider = provider;
        StateHasChanged();
        await Task.CompletedTask;
    }
    
    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && _selectedType == AgentType.Saturn && _selectedProvider == "Anthropic")
        {
            await CheckAnthropicOAuthStatus();
        }
    }
    
    private async Task CheckAnthropicOAuthStatus()
    {
        try
        {
            _anthropicOAuthStatus = await AnthropicOAuthService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to check Anthropic OAuth status: {ex.Message}");
            _anthropicOAuthStatus = new OAuthStatus
            {
                Connected = false,
                ExpiresAt = null,
                Scopes = Array.Empty<string>()
            };
        }
        finally
        {
            StateHasChanged();
        }
    }
    
    private async Task GetAnthropicOAuthUrl()
    {
        try
        {
            _oauthInProgress = true;
            StateHasChanged();
            
            // Get the authorization URL from the service
            var startResult = await AnthropicOAuthService.StartAuthAsync();
            _authorizationUrl = startResult.AuthUrl;
            _oauthState = startResult.State;
            
            if (string.IsNullOrEmpty(_authorizationUrl))
            {
                Console.WriteLine("No authorization URL received");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting OAuth: {ex.Message}");
        }
        finally
        {
            if (!_oauthInProgress) // Only reset if we're not polling
            {
                _oauthInProgress = false;
                StateHasChanged();
            }
        }
    }
    
    private async Task SubmitAuthorizationCode()
    {
        try
        {
            _oauthInProgress = true;
            StateHasChanged();
            
            // Submit the authorization code to the service
            var result = await AnthropicOAuthService.SubmitCodeAsync(_authorizationCode, _oauthState ?? string.Empty);
            
            if (result.Success)
            {
                // Clear the code input
                _authorizationCode = string.Empty;
                _showCodeInput = false;
                
                // Check status to confirm connection
                await CheckAnthropicOAuthStatus();
            }
            else
            {
                Console.WriteLine($"Failed to submit code: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting code: {ex.Message}");
        }
        finally
        {
            _oauthInProgress = false;
            StateHasChanged();
        }
    }
    
    private async Task PollForOAuthCompletion()
    {
        const int maxAttempts = 60; // Poll for up to 5 minutes (5 second intervals)
        const int intervalMs = 5000;
        
        for (int attempt = 0; attempt < maxAttempts && _oauthInProgress; attempt++)
        {
            await Task.Delay(intervalMs);
            
            if (!_oauthInProgress) break;
            
            try
            {
                await CheckAnthropicOAuthStatus();
                
                if (_anthropicOAuthStatus?.Connected == true)
                {
                    _oauthInProgress = false;
                    StateHasChanged();
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error polling OAuth status: {ex.Message}");
            }
        }
        
        if (_oauthInProgress)
        {
            _oauthInProgress = false;
            StateHasChanged();
        }
    }
    
    private async Task DisconnectAnthropicOAuth()
    {
        try
        {
            await AnthropicOAuthService.LogoutAsync();
            await CheckAnthropicOAuthStatus();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disconnecting OAuth: {ex.Message}");
        }
    }
}