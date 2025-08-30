# Provider System Implementation Plan

## Overview
The provider system is the foundation for LLM communication in Saturn. Currently, OrchestratorChat.Saturn has only skeleton interfaces. This document provides a complete implementation plan based on the fully functional SaturnFork codebase.

## Current State vs Required State

### Current State (OrchestratorChat.Saturn)
- Basic `ILLMProvider.cs` interface (skeleton)
- No actual provider implementations
- No authentication system
- No token management
- No provider factory

### Required State (from SaturnFork)
- Complete Anthropic provider with OAuth authentication
- Full OpenRouter provider implementation
- Provider factory for dynamic provider selection
- Secure token storage with encryption
- Model management and validation

## Implementation Components

### 1. Anthropic Provider Implementation

#### 1.1 AnthropicAuthService.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/Anthropic/AnthropicAuthService.cs`

**Key Features**:
- OAuth 2.0 flow with PKCE (Proof Key for Code Exchange)
- Support for both Claude.ai and Anthropic Console authentication
- Token refresh mechanism
- State token for CSRF protection

**Implementation Details**:
```csharp
public class AnthropicAuthService : IDisposable
{
    // OAuth Configuration
    private const string CLIENT_ID = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string AUTH_URL_CLAUDE = "https://claude.ai/oauth/authorize";
    private const string AUTH_URL_CONSOLE = "https://console.anthropic.com/oauth/authorize";
    private const string TOKEN_URL = "https://console.anthropic.com/v1/oauth/token";
    private const string REDIRECT_URI = "https://console.anthropic.com/oauth/code/callback";
    private const string SCOPES = "org:create_api_key user:profile user:inference";
    
    // Methods to implement:
    Task<bool> AuthenticateAsync(bool useClaudeMax = true)
    Task<StoredTokens> RefreshTokensAsync(string refreshToken)
    Task<StoredTokens> GetValidTokensAsync()
    void Logout()
}
```

**Dependencies**:
- TokenStore for secure token persistence
- PKCEGenerator for OAuth security
- BrowserLauncher for opening auth URLs

#### 1.2 TokenStore.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/Anthropic/TokenStore.cs`

**Key Features**:
- Cross-platform encryption (DPAPI on Windows, AES-GCM on others)
- Secure key derivation with PBKDF2
- Token migration from legacy formats
- Secure file deletion with overwriting

**Implementation Details**:
```csharp
public class TokenStore
{
    // Storage paths
    private readonly string _tokenPath; // %AppData%/Saturn/auth/anthropic.tokens
    private readonly string _keyPath;   // %AppData%/Saturn/auth/.keystore
    private readonly string _saltPath;  // %AppData%/Saturn/auth/.salt
    
    // Encryption parameters
    private const int KeySize = 256 / 8;  // 256-bit key
    private const int NonceSize = 12;     // 96-bit nonce for AES-GCM
    private const int TagSize = 16;       // 128-bit authentication tag
    private const int SaltSize = 16;      // 128-bit salt
    
    // Methods to implement:
    Task SaveTokensAsync(StoredTokens tokens)
    Task<StoredTokens> LoadTokensAsync()
    void DeleteTokens()
    
    // Platform-specific encryption
    Task<string> EncryptWithDpapiAsync(string plainText)  // Windows
    Task<string> EncryptWithAesAsync(string plainText)    // Cross-platform
}
```

#### 1.3 AnthropicClient.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/Anthropic/AnthropicClient.cs`

**Key Features**:
- Messages API implementation
- Streaming support with SSE
- Tool calling support
- Automatic token refresh
- System prompt handling for Claude Code compatibility
- **CRITICAL**: System prompt must start with exact text: "You are Claude Code, Anthropic's official CLI for Claude."

**Implementation Details**:
```csharp
public class AnthropicClient : ILLMClient
{
    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const string API_VERSION = "2023-06-01";
    private const string ANTHROPIC_BETA = "prompt-caching-2024-07-31";
    
    // Required headers
    private const string ANTHROPIC_VERSION_HEADER = "anthropic-version";
    private const string ANTHROPIC_BETA_HEADER = "anthropic-beta";
    
    // Methods to implement:
    Task<ChatResponse> SendMessageAsync(ChatRequest request)
    IAsyncEnumerable<StreamChunk> StreamMessageAsync(ChatRequest request)
    Task<List<ModelInfo>> GetAvailableModelsAsync()
    
    // User-Agent must be: "Claude-Code/1.0"
    // MUST remove x-api-key header when using OAuth Bearer token
}
```

#### 1.4 Supporting Classes

**PKCEGenerator.cs**:
```csharp
public static class PKCEGenerator
{
    public class PKCEPair
    {
        public string Verifier { get; set; }
        public string Challenge { get; set; }
    }
    
    public static PKCEPair Generate()
    {
        // Generate cryptographically secure random verifier
        // Create SHA256 hash as challenge
    }
}
```

**BrowserLauncher.cs**:
```csharp
public static class BrowserLauncher
{
    public static bool OpenUrl(string url)
    {
        // Cross-platform browser launching
        // Windows: Process.Start with UseShellExecute
        // Linux: xdg-open
        // macOS: open
    }
}
```

**Models/StoredTokens.cs**:
```csharp
public class StoredTokens
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}
```

### 2. OpenRouter Provider Implementation

#### 2.1 OpenRouterProvider.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/OpenRouter/OpenRouterProvider.cs`

**Key Features**:
- Simple API key authentication
- Model routing and selection
- Cost management
- Provider preferences

**Implementation Details**:
```csharp
public class OpenRouterProvider : ILLMProvider
{
    private readonly OpenRouterClient _client;
    
    public async Task<bool> InitializeAsync(ProviderConfiguration config)
    {
        // Initialize with API key from environment or config
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        _client = new OpenRouterClient(new OpenRouterOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://openrouter.ai/api/v1"
        });
    }
}
```

#### 2.2 OpenRouterClient.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/OpenRouter/OpenRouterClient.cs`

**Key Features**:
- Full OpenRouter API implementation
- Service-based architecture
- Automatic retry with exponential backoff
- Rate limiting support

**Implementation Details**:
```csharp
public class OpenRouterClient : ILLMClient
{
    private readonly HttpClientAdapter _httpClient;
    private readonly OpenRouterOptions _options;
    
    // Service properties
    public ChatCompletionsService Chat { get; }
    public ModelsService Models { get; }
    public CreditsService Credits { get; }
    public GenerationService Generation { get; }
    
    // Core methods
    Task<ChatResponse> SendMessageAsync(ChatRequest request)
    IAsyncEnumerable<StreamChunk> StreamMessageAsync(ChatRequest request)
}
```

### 3. Provider Factory

#### 3.1 ProviderFactory.cs
**Location**: `src/OrchestratorChat.Saturn/Providers/ProviderFactory.cs`

**Implementation Details**:
```csharp
public static class ProviderFactory
{
    public static async Task<ILLMProvider> CreateProviderAsync(string providerType, ProviderConfiguration config)
    {
        return providerType.ToLower() switch
        {
            "anthropic" => new AnthropicProvider(),
            "openrouter" => new OpenRouterProvider(),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
        
        await provider.InitializeAsync(config);
        return provider;
    }
}
```

### 4. Model Definitions

#### 4.1 Current Models Support
Based on SaturnFork implementation:

**Anthropic Models** (via OAuth):
- claude-opus-4-1-20250805 (Opus 4.1 - Most Advanced)
- claude-opus-4 (Opus 4.0)
- claude-sonnet-4-20250514 (Sonnet 4.0 - Default)
- claude-3.7-sonnet (Sonnet 3.7)
- claude-3.5-haiku (Haiku 3.5)

**Model Name Mappings**:
```csharp
// MessageConverter mappings
["claude-sonnet-4"] = "claude-sonnet-4-20250514"
["anthropic/claude-sonnet-4"] = "claude-sonnet-4-20250514"  
["anthropic/claude-opus-4.1"] = "claude-opus-4-1-20250805"
```

**OpenRouter Models** (Popular):
- anthropic/claude-sonnet-4 (maps to Sonnet 4.0)
- anthropic/claude-opus-4.1 (maps to Opus 4.1)
- anthropic/claude-3.5-sonnet (legacy)
- openai/gpt-4o
- google/gemini-pro-1.5
- meta-llama/llama-3.1-70b-instruct
- deepseek/deepseek-chat

## Implementation Priority

### Phase 1: Core Provider Infrastructure (Week 1)
1. Implement ILLMProvider and ILLMClient interfaces
2. Create ProviderConfiguration class
3. Implement basic ProviderFactory

### Phase 2: Anthropic Provider (Week 2)
1. Implement TokenStore with encryption
2. Create AnthropicAuthService with OAuth flow
3. Implement AnthropicClient with Messages API
4. Add streaming support

### Phase 3: OpenRouter Provider (Week 3)
1. Port OpenRouter models and API definitions
2. Implement OpenRouterClient
3. Add service layer (Chat, Models, Credits)
4. Implement SSE streaming

### Phase 4: Integration & Testing (Week 4)
1. Integrate providers with OrchestratorChat.Saturn
2. Add provider selection logic
3. Implement error handling and retries
4. Create unit and integration tests

## Key Integration Points

### With Core Project
- Providers must implement `IAgent` interface from OrchestratorChat.Core
- Use message models from Core project
- Integrate with configuration system

### With Agent System
- Providers supply LLM capabilities to agents
- Handle tool calling through provider APIs
- Stream responses through agent pipeline

### With Web UI
- Provider status displayed in UI
- Authentication flows triggered from UI
- Model selection in chat interface

## Testing Requirements

### Unit Tests
- Token encryption/decryption
- OAuth flow components
- Model validation
- API request building

### Integration Tests
- End-to-end authentication flow
- API communication with mock servers
- Token refresh scenarios
- Error handling and retries

## Security Considerations

1. **Token Storage**:
   - Never store tokens in plain text
   - Use platform-specific encryption (DPAPI/AES-GCM)
   - Implement secure deletion

2. **API Keys**:
   - Load from environment variables
   - Never commit to source control
   - Support key rotation

3. **OAuth Security**:
   - Always use PKCE for OAuth flows
   - Validate state tokens
   - Implement secure redirect handling

## Dependencies to Add

### NuGet Packages
```xml
<PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
```

## Migration Notes

When porting from SaturnFork:
1. Remove Terminal.Gui dependencies
2. Replace console I/O with event-based communication
3. Adapt authentication flow for web-based interaction
4. Ensure thread-safety for concurrent provider instances

## Validation Checklist

- [ ] All provider interfaces implemented
- [ ] Authentication flows working
- [ ] Token persistence and encryption functional
- [ ] Streaming responses operational
- [ ] Error handling comprehensive
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Security review completed