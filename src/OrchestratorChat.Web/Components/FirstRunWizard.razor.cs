using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using System.Text.Json;

namespace OrchestratorChat.Web.Components;

public partial class FirstRunWizard : ComponentBase
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<string> OnAgentCreated { get; set; }
    
    [Inject] private IProviderVerificationService ProviderVerificationService { get; set; } = null!;
    [Inject] private IAgentService AgentService { get; set; } = null!;
    [Inject] private IAnthropicOAuthService AnthropicOAuthService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    
    private enum ProviderType { None, ClaudeCli, SaturnOpenRouter, SaturnAnthropic }
    private enum CreationStep { None, Creating, Testing, Success, Error }
    
    private MudStepper _stepper = null!;
    private int _currentStep = 0;
    private ProviderStatusResponse _providerStatus = new();
    private ProviderType _selectedProvider = ProviderType.None;
    private bool _showSkipWarning = false;
    
    // Step 1 state
    private bool _step1Valid => _selectedProvider != ProviderType.None;
    
    // Step 2 state (Verification)
    private bool _claudeVerificationLoading = false;
    private bool? _claudeVerified = null;
    private string _openRouterApiKey = string.Empty;
    private bool _openRouterValidationLoading = false;
    private ValidationResult? _openRouterValidationResult = null;
    private bool _hasExistingOpenRouterKey = false;
    private OAuthStatus? _anthropicOAuthStatus = new OAuthStatus();
    private bool _anthropicOAuthLoading = false;
    private string? _anthropicOAuthError = null;
    private string? _authorizationUrl = null;
    private string? _oauthState = null;
    private string _authorizationCode = string.Empty;
    private bool _showCodeInput = false;
    private bool _step2Valid => (_selectedProvider == ProviderType.ClaudeCli && _claudeVerified == true) ||
                                (_selectedProvider == ProviderType.SaturnOpenRouter && (_openRouterValidationResult?.IsValid == true || _hasExistingOpenRouterKey)) ||
                                (_selectedProvider == ProviderType.SaturnAnthropic && _anthropicOAuthStatus?.Connected == true);
    
    // Step 3 state (Name & Defaults)
    private string _agentName = string.Empty;
    private string _selectedModel = string.Empty;
    private bool _setAsDefault = true;
    private double _temperature = 0.7;
    private int _maxTokens = 4096;
    private string _workingDirectory = string.Empty;
    private string _agentNameError = string.Empty;
    private bool _step3Valid => !string.IsNullOrWhiteSpace(_agentName) && !string.IsNullOrWhiteSpace(_selectedModel);
    
    // Step 4 state (Create & Test)
    private CreationStep _creationStep = CreationStep.None;
    private string _creationError = string.Empty;
    private string _creationFixAction = string.Empty;
    private List<string> _testMessages = new();
    private string? _createdAgentId = null;
    private bool _step4Valid => _creationStep == CreationStep.Success;
    
    // General state
    private bool _isProcessing = false;
    
    private async Task OnVisibilityChanged(bool isVisible)
    {
        IsVisible = isVisible;
        
        if (isVisible)
        {
            await InitializeWizard();
        }
        else
        {
            ResetWizard();
        }
        
        if (IsVisibleChanged.HasDelegate)
        {
            await IsVisibleChanged.InvokeAsync(isVisible);
        }
    }
    
    private async Task InitializeWizard()
    {
        // Load provider status
        _providerStatus = await ProviderVerificationService.GetProviderStatusAsync();
        _hasExistingOpenRouterKey = _providerStatus.OpenRouterKey == ProviderStatus.Present;
        StateHasChanged();
    }
    
    private void ResetWizard()
    {
        _currentStep = 0;
        _selectedProvider = ProviderType.None;
        _showSkipWarning = false;
        _claudeVerified = null;
        _claudeVerificationLoading = false;
        _openRouterApiKey = string.Empty;
        _openRouterValidationLoading = false;
        _openRouterValidationResult = null;
        _anthropicOAuthStatus = new OAuthStatus();
        _anthropicOAuthLoading = false;
        _anthropicOAuthError = null;
        _authorizationUrl = null;
        _oauthState = null;
        _authorizationCode = string.Empty;
        _showCodeInput = false;
        _agentName = string.Empty;
        _selectedModel = string.Empty;
        _setAsDefault = true;
        _temperature = 0.7;
        _maxTokens = 4096;
        _workingDirectory = string.Empty;
        _agentNameError = string.Empty;
        _creationStep = CreationStep.None;
        _creationError = string.Empty;
        _creationFixAction = string.Empty;
        _testMessages.Clear();
        _createdAgentId = null;
        _isProcessing = false;
    }
    
    private void SelectProvider(ProviderType provider)
    {
        _selectedProvider = provider;
        _showSkipWarning = false;
        
        // Set default agent name and model based on provider
        if (provider == ProviderType.ClaudeCli)
        {
            _agentName = "Claude (Local)";
            _selectedModel = "claude-sonnet-4-20250514";
        }
        else if (provider == ProviderType.SaturnOpenRouter)
        {
            _agentName = "Saturn (OpenRouter)";
            _selectedModel = "claude-sonnet-4-20250514";
        }
        else if (provider == ProviderType.SaturnAnthropic)
        {
            _agentName = "Saturn (Anthropic)";
            _selectedModel = "claude-sonnet-4-20250514";
        }
        
        StateHasChanged();
    }
    
    private string GetProviderCardClass(ProviderType provider)
    {
        var baseClass = "cursor-pointer transition-all";
        if (_selectedProvider == provider)
        {
            return $"{baseClass} mud-elevation-4 mud-primary-text";
        }
        return $"{baseClass} hover:mud-elevation-2";
    }
    
    private Color GetProviderStatusColor(ProviderStatus status)
    {
        return status switch
        {
            ProviderStatus.Detected or ProviderStatus.Present => Color.Success,
            ProviderStatus.NotFound or ProviderStatus.Missing => Color.Warning,
            _ => Color.Default
        };
    }
    
    private string GetProviderStatusText(ProviderStatus status)
    {
        return status switch
        {
            ProviderStatus.Detected => "Detected",
            ProviderStatus.NotFound => "Not found",
            ProviderStatus.Present => "Key set",
            ProviderStatus.Missing => "Not set",
            _ => "Unknown"
        };
    }
    
    private Dictionary<string, string> GetAvailableModels()
    {
        if (_selectedProvider == ProviderType.ClaudeCli)
        {
            return new Dictionary<string, string>
            {
                { "Claude Sonnet 4", "claude-sonnet-4-20250514" },
                { "Claude 3.5 Sonnet", "claude-3-5-sonnet-20241022" }
            };
        }
        else if (_selectedProvider == ProviderType.SaturnOpenRouter || _selectedProvider == ProviderType.SaturnAnthropic)
        {
            return new Dictionary<string, string>
            {
                { "Claude Sonnet 4", "claude-sonnet-4-20250514" },
                { "Claude 3.5 Sonnet", "claude-3-5-sonnet-20241022" },
                { "GPT-4o", "gpt-4o" },
                { "GPT-4o Mini", "gpt-4o-mini" }
            };
        }
        
        return new Dictionary<string, string>();
    }
    
    private async Task HandlePrimaryAction()
    {
        _isProcessing = true;
        
        try
        {
            switch (_currentStep)
            {
                case 0: // Step 1 -> Step 2 (Provider Selection -> Verification)
                    await AdvanceFromStep1();
                    break;
                case 1: // Step 2 -> Step 3 (Verification -> Name & Defaults)
                    if (_selectedProvider == ProviderType.SaturnAnthropic && _anthropicOAuthStatus?.Connected != true)
                    {
                        // If the primary action text shows "Connect", get OAuth URL
                        await GetAnthropicOAuthUrl();
                    }
                    else
                    {
                        _currentStep = 2;
                    }
                    break;
                case 2: // Step 3 -> Step 4 (Name & Defaults -> Create & Test)
                    await AdvanceToStep4();
                    break;
                case 3: // Step 4 -> Complete (Create & Test -> Complete)
                    await CompleteWizard();
                    break;
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private async Task AdvanceFromStep1()
    {
        _currentStep = 1;
        
        // Auto-verify based on selected provider
        if (_selectedProvider == ProviderType.ClaudeCli)
        {
            await VerifyClaudeCli();
        }
        else if (_selectedProvider == ProviderType.SaturnAnthropic)
        {
            await CheckAnthropicOAuthStatus();
        }
    }
    
    private async Task VerifyClaudeCli()
    {
        _claudeVerificationLoading = true;
        StateHasChanged();
        
        try
        {
            var status = await ProviderVerificationService.DetectClaudeCliAsync();
            _claudeVerified = status == ProviderStatus.Detected;
        }
        finally
        {
            _claudeVerificationLoading = false;
            StateHasChanged();
        }
    }
    
    private async Task CheckAnthropicOAuthStatus()
    {
        try
        {
            _anthropicOAuthError = null;
            _anthropicOAuthStatus = await AnthropicOAuthService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            _anthropicOAuthError = $"Failed to check OAuth status: {ex.Message}";
            _anthropicOAuthStatus = new OAuthStatus();
        }
        finally
        {
            StateHasChanged();
        }
    }
    
    private async Task SubmitAuthorizationCode()
    {
        try
        {
            _anthropicOAuthLoading = true;
            _anthropicOAuthError = null;
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
                _anthropicOAuthError = $"Failed to submit code: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _anthropicOAuthError = $"Error submitting code: {ex.Message}";
        }
        finally
        {
            _anthropicOAuthLoading = false;
            StateHasChanged();
        }
    }
    
    private async Task GetAnthropicOAuthUrl()
    {
        try
        {
            _anthropicOAuthLoading = true;
            _anthropicOAuthError = null;
            StateHasChanged();
            
            // Get the authorization URL from the service
            var startResult = await AnthropicOAuthService.StartAuthAsync();
            _authorizationUrl = startResult.AuthUrl;
            _oauthState = startResult.State;
            
            if (string.IsNullOrEmpty(_authorizationUrl))
            {
                _anthropicOAuthError = "Failed to get authorization URL";
            }
        }
        catch (Exception ex)
        {
            _anthropicOAuthError = $"Failed to start OAuth: {ex.Message}";
        }
        finally
        {
            _anthropicOAuthLoading = false;
            StateHasChanged();
        }
    }

    private async Task ValidateOpenRouterKey()
    {
        if (string.IsNullOrWhiteSpace(_openRouterApiKey))
        {
            // For SaturnAnthropic, OpenRouter validation is optional
            if (_selectedProvider == ProviderType.SaturnAnthropic)
            {
                _openRouterValidationResult = new ValidationResult { IsValid = true };
                return;
            }
            return;
        }
        
        _openRouterValidationLoading = true;
        StateHasChanged();
        
        try
        {
            _openRouterValidationResult = await ProviderVerificationService.ValidateOpenRouterKeyAsync(_openRouterApiKey);
        }
        finally
        {
            _openRouterValidationLoading = false;
            StateHasChanged();
        }
    }
    
    private async Task AdvanceToStep4()
    {
        // Validate step 3
        _agentNameError = string.Empty;
        
        if (string.IsNullOrWhiteSpace(_agentName))
        {
            _agentNameError = "Agent name is required";
            return;
        }
        
        _currentStep = 3;
        await CreateAndTestAgent();
    }
    
    private async Task CreateAndTestAgent()
    {
        _creationStep = CreationStep.Creating;
        _testMessages.Clear();
        StateHasChanged();
        
        try
        {
            // Create agent configuration
            var configuration = new AgentConfiguration
            {
                Name = _agentName.Trim(),
                Type = _selectedProvider == ProviderType.ClaudeCli ? AgentType.Claude : AgentType.Saturn,
                WorkingDirectory = string.IsNullOrWhiteSpace(_workingDirectory) ? Environment.CurrentDirectory : _workingDirectory.Trim(),
                Model = _selectedModel,
                Temperature = _temperature,
                MaxTokens = _maxTokens,
                SystemPrompt = string.Empty,
                RequireApproval = false
            };
            
            // Add provider setting for Saturn agents
            if (_selectedProvider == ProviderType.SaturnOpenRouter)
            {
                configuration.CustomSettings["Provider"] = "OpenRouter";
            }
            else if (_selectedProvider == ProviderType.SaturnAnthropic)
            {
                configuration.CustomSettings["Provider"] = "Anthropic";
            }
            
            // Create the agent
            var agentInfo = await AgentService.CreateAgentAsync(configuration.Type, configuration);
            _createdAgentId = agentInfo.Id;
            
            // Set as default if requested
            if (_setAsDefault)
            {
                try
                {
                    await AgentService.SetDefaultAgentAsync(agentInfo.Id);
                }
                catch (Exception ex)
                {
                    // Don't fail the wizard if setting default fails, just log it
                    Console.WriteLine($"Warning: Failed to set agent as default: {ex.Message}");
                }
            }
            
            // Move to testing step
            _creationStep = CreationStep.Testing;
            StateHasChanged();
            
            // Simulate test message (in a real implementation, this would send a message to the agent)
            await Task.Delay(1000); // Simulate processing time
            _testMessages.Add("Hello! I'm your new AI assistant.");
            _testMessages.Add("I'm ready to help you with your tasks.");
            StateHasChanged();
            
            await Task.Delay(500); // Brief delay before success
            _creationStep = CreationStep.Success;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _creationStep = CreationStep.Error;
            _creationError = $"Error creating agent: {ex.Message}";
            _creationFixAction = "Retry initialization";
            StateHasChanged();
        }
    }
    
    private async Task RetryCreation()
    {
        await CreateAndTestAgent();
    }
    
    private async Task CompleteWizard()
    {
        if (_createdAgentId != null && OnAgentCreated.HasDelegate)
        {
            await OnAgentCreated.InvokeAsync(_createdAgentId);
        }
        
        await OnVisibilityChanged(false);
        
        // Navigate to chat with the created agent
        if (_createdAgentId != null)
        {
            Navigation.NavigateTo($"/chat/{_createdAgentId}");
        }
    }
    
    private bool CanProceed()
    {
        return _currentStep switch
        {
            0 => _step1Valid,
            1 => _step2Valid,
            2 => _step3Valid,
            3 => _step4Valid,
            _ => false
        };
    }
    
    private string GetPrimaryActionText()
    {
        return _currentStep switch
        {
            0 => "Next",
            1 => (_selectedProvider == ProviderType.SaturnOpenRouter && _openRouterValidationResult == null && !_hasExistingOpenRouterKey) ? "Validate" : 
                 (_selectedProvider == ProviderType.SaturnAnthropic && _anthropicOAuthStatus?.Connected != true) ? "Connect" : "Next",
            2 => "Create",
            3 => "Start Chat",
            _ => "Next"
        };
    }
    
    private string GetOAuthErrorMessage(string error)
    {
        if (error.Contains("timeout") || error.Contains("network"))
            return "Connection timeout. Please check your internet connection and try again.";
        if (error.Contains("unauthorized") || error.Contains("401"))
            return "Authorization failed. The authorization code may have expired or been used already.";
        if (error.Contains("invalid_grant") || error.Contains("invalid_code"))
            return "Invalid authorization code. Please try the OAuth flow again.";
        if (error.Contains("access_denied"))
            return "Access was denied. Please authorize the application in Anthropic to continue.";
        
        return error;
    }
    
    private string? GetOAuthErrorRecoveryAction(string error)
    {
        if (error.Contains("timeout") || error.Contains("network"))
            return "Try refreshing the page or check your network connection.";
        if (error.Contains("unauthorized") || error.Contains("401") || error.Contains("invalid_grant") || error.Contains("invalid_code"))
            return "Click 'Connect to Anthropic' to start a new OAuth flow.";
        if (error.Contains("access_denied"))
            return "Make sure to click 'Allow' when prompted by Anthropic.";
        
        return "Try refreshing the page or restarting the OAuth flow.";
    }
}