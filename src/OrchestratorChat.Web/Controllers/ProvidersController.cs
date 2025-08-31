using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Core.Authentication;
using OrchestratorChat.Saturn.Providers.Anthropic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace OrchestratorChat.Web.Controllers;

/// <summary>
/// Controller for provider verification endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IProviderVerificationService _providerVerificationService;
    private readonly IAnthropicOAuthService _anthropicOAuthService;
    private readonly ILogger<ProvidersController> _logger;
    
    public ProvidersController(
        IProviderVerificationService providerVerificationService,
        IAnthropicOAuthService anthropicOAuthService,
        ILogger<ProvidersController> logger)
    {
        _providerVerificationService = providerVerificationService;
        _anthropicOAuthService = anthropicOAuthService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets the current status of all providers
    /// </summary>
    /// <returns>Provider status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<ProviderStatusResponse>> GetProviderStatus()
    {
        try
        {
            var status = await _providerVerificationService.GetProviderStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider status");
            return StatusCode(500, new { error = "Failed to retrieve provider status" });
        }
    }
    
    /// <summary>
    /// Validates and stores OpenRouter API key
    /// </summary>
    /// <param name="request">The API key validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("openrouter/validate")]
    public async Task<ActionResult<ValidationResult>> ValidateOpenRouterKey([FromBody] ValidateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var result = await _providerVerificationService.ValidateOpenRouterKeyAsync(request.ApiKey);
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate OpenRouter API key");
            return StatusCode(500, new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Internal server error during validation"
            });
        }
    }
    
    /// <summary>
    /// Validates and stores Anthropic API key
    /// </summary>
    /// <param name="request">The API key validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("anthropic/validate")]
    public async Task<ActionResult<ValidationResult>> ValidateAnthropicKey([FromBody] ValidateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var result = await _providerVerificationService.ValidateAnthropicKeyAsync(request.ApiKey);
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Anthropic API key");
            return StatusCode(500, new ValidationResult
            {
                IsValid = false,
                Status = ProviderStatus.Missing,
                ErrorMessage = "Internal server error during validation"
            });
        }
    }
    
    /// <summary>
    /// Gets Anthropic authentication status
    /// </summary>
    /// <returns>Token presence, expiry, and scopes without exposing secrets</returns>
    [HttpGet("anthropic/status")]
    public async Task<ActionResult<object>> GetAnthropicStatus()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            var status = await _anthropicOAuthService.GetStatusAsync();
            return Ok(new { 
                connected = status.Connected, 
                expiresAt = status.ExpiresAt, 
                scopes = status.Scopes 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Anthropic status");
            return StatusCode(500, new { error = "Failed to retrieve authentication status" });
        }
    }
    
    /// <summary>
    /// Starts Anthropic OAuth flow by generating authorization URL
    /// </summary>
    /// <returns>OAuth authorization URL and state information</returns>
    [HttpPost("anthropic/start")]
    public async Task<IActionResult> StartAnthropicOAuth()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            var result = await _anthropicOAuthService.StartAuthAsync();
            return Ok(new { authUrl = result.AuthUrl, state = result.State });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Anthropic OAuth flow");
            return StatusCode(500, new { error = "Failed to start OAuth flow" });
        }
    }
    
    /// <summary>
    /// Handles OAuth callback and completes authentication flow
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <param name="error">Error parameter if OAuth failed</param>
    /// <returns>Callback redirect page</returns>
    [HttpGet("/oauth/anthropic/callback")]
    public async Task<IActionResult> AnthropicOAuthCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        // Handle OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
            return GenerateCallbackPage("oauth-error", error);
        }
        
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return GenerateCallbackPage("oauth-error", "Missing required parameters");
        }
        
        try
        {
            var result = await _anthropicOAuthService.HandleCallbackAsync(code, state);
            
            if (result.Success)
            {
                return GenerateCallbackPage("oauth-success", result.SuccessMessage ?? "Authentication successful");
            }
            else
            {
                return GenerateCallbackPage("oauth-error", result.ErrorMessage ?? "Authentication failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Anthropic OAuth flow");
            return GenerateCallbackPage("oauth-error", "Internal server error during OAuth completion");
        }
    }

    /// <summary>
    /// Submits an authorization code manually after user copies it from Anthropic
    /// </summary>
    [HttpPost("anthropic/submit-code")]
    public async Task<IActionResult> SubmitAnthropicCode([FromBody] SubmitCodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Code))
            {
                return BadRequest(new { error = "Authorization code is required" });
            }

            if (string.IsNullOrEmpty(request?.State))
            {
                return BadRequest(new { error = "State parameter is required" });
            }

            var result = await _anthropicOAuthService.SubmitCodeAsync(request.Code, request.State);

            if (result.Success)
            {
                return Ok(new { success = true, message = result.SuccessMessage });
            }
            else
            {
                return BadRequest(new { error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit Anthropic code");
            return StatusCode(500, new { error = "Failed to complete OAuth flow" });
        }
    }
    
    
    /// <summary>
    /// Clears stored Anthropic OAuth tokens
    /// </summary>
    /// <returns>Logout result</returns>
    [HttpPost("anthropic/logout")]
    public async Task<IActionResult> LogoutAnthropicOAuth()
    {
        // Enforce HTTPS in production
        if (!Request.IsHttps && !HttpContext.Request.Host.Host.Contains("localhost"))
        {
            return BadRequest(new { error = "HTTPS required" });
        }
        
        try
        {
            await _anthropicOAuthService.LogoutAsync();
            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout from Anthropic OAuth");
            return StatusCode(500, new { error = "Failed to logout" });
        }
    }

    /// <summary>
    /// Generates callback HTML page for OAuth completion
    /// </summary>
    private IActionResult GenerateCallbackPage(string messageType, string message)
    {
        return Content($@"
<!DOCTYPE html>
<html>
<head>
    <title>OAuth {(messageType == "oauth-success" ? "Success" : "Error")}</title>
</head>
<body>
    <script>
        if (window.opener) {{
            window.opener.postMessage({{ type: '{messageType}', {(messageType == "oauth-success" ? "message" : "error")}: '{message}' }}, '*');
        }}
        window.close();
    </script>
</body>
</html>", "text/html");
    }
}