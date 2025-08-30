namespace OrchestratorChat.Saturn.Tests.TestHelpers;

/// <summary>
/// Shared test constants for Saturn tests.
/// Based on patterns from SaturnFork project.
/// </summary>
public static class TestConstants
{
    // Claude Models
    public const string ValidClaudeModel = "claude-sonnet-4-20250514";
    public const string ValidClaudeOpusModel = "claude-opus-4-1-20250805";
    public const string ValidClaudeHaikuModel = "claude-haiku-3-5-20241022";

    // OpenRouter Models
    public const string ValidOpenRouterModel = "anthropic/claude-3.5-sonnet";
    public const string ValidOpenRouterOpusModel = "anthropic/claude-3-opus";

    // API Keys and Tokens
    public const string TestApiKey = "test_api_key_12345";
    public const string TestOAuthCode = "test_oauth_code";
    public const string TestAccessToken = "test_access_token_67890";
    public const string TestRefreshToken = "test_refresh_token_abcdef";

    // URLs and Endpoints
    public const string TestAnthropicAuthUrl = "https://auth.anthropic.com";
    public const string TestAnthropicApiUrl = "https://api.anthropic.com";
    public const string TestOpenRouterBaseUrl = "https://openrouter.ai/api/v1";

    // Tool Parameters
    public static readonly Dictionary<string, object> StandardToolParams = new()
    {
        { "timeout", 30 },
        { "max_retries", 3 },
        { "working_directory", "/tmp/test" }
    };

    public static readonly Dictionary<string, object> FileReadParams = new()
    {
        { "file_path", "/test/file.txt" },
        { "encoding", "utf-8" }
    };

    public static readonly Dictionary<string, object> FileWriteParams = new()
    {
        { "file_path", "/test/output.txt" },
        { "content", "Test content" },
        { "encoding", "utf-8" }
    };

    public static readonly Dictionary<string, object> GlobParams = new()
    {
        { "pattern", "**/*.cs" },
        { "exclude_patterns", new[] { "**/bin/**", "**/obj/**" } }
    };

    public static readonly Dictionary<string, object> GrepParams = new()
    {
        { "pattern", "public class" },
        { "file_pattern", "*.cs" },
        { "case_sensitive", false }
    };

    // Authentication
    public const string TestPkceCodeVerifier = "test_code_verifier_1234567890abcdefghijklmnopqrstuvwxyz";
    public const string TestPkceCodeChallenge = "test_code_challenge_abcdef1234567890";
    public const string TestAuthorizationCode = "auth_code_test_12345";
    public const string TestRedirectUri = "http://localhost:8080/callback";
    public const string TestClientId = "test_client_id";

    // Network Testing
    public const int DefaultTimeoutMs = 30000;
    public const int ShortTimeoutMs = 5000;
    public const int NetworkRetryCount = 3;
    public const int NetworkRetryDelayMs = 1000;

    // File System Testing
    public const string TestFileName = "test-file.txt";
    public const string TestDirectoryName = "test-directory";
    public const string TestFileContent = "This is test file content.\nLine 2\nLine 3";
    
    public const string TestDiffContent = @"--- a/test-file.txt
+++ b/test-file.txt
@@ -1,3 +1,3 @@
 This is test file content.
-Line 2
+Modified Line 2
 Line 3";

    public const string TestSearchPattern = "*.txt";
    public const string TestReplacePattern = @"Line \d+";
    public const string TestReplacementText = "Modified Line";

    // JSON Response Templates
    public const string ValidAnthropicResponse = @"{
        ""id"": ""msg_123"",
        ""type"": ""message"",
        ""role"": ""assistant"",
        ""content"": [{""type"": ""text"", ""text"": ""Hello from Anthropic""}],
        ""model"": ""claude-sonnet-4-20250514"",
        ""stop_reason"": ""end_turn"",
        ""usage"": {
            ""input_tokens"": 10,
            ""output_tokens"": 15
        }
    }";

    public const string ValidOpenRouterResponse = @"{
        ""id"": ""chatcmpl-123"",
        ""object"": ""chat.completion"",
        ""created"": 1677652288,
        ""model"": ""anthropic/claude-3.5-sonnet"",
        ""choices"": [{
            ""index"": 0,
            ""message"": {
                ""role"": ""assistant"",
                ""content"": ""Hello from OpenRouter""
            },
            ""finish_reason"": ""stop""
        }],
        ""usage"": {
            ""prompt_tokens"": 10,
            ""completion_tokens"": 15,
            ""total_tokens"": 25
        }
    }";

    public const string StreamingResponseChunk = @"{
        ""type"": ""content_block_delta"",
        ""delta"": {""type"": ""text_delta"", ""text"": ""Hello""}
    }";

    public const string StreamingDoneMarker = "[DONE]";

    // OAuth Response Templates
    public const string ValidTokenResponse = @"{
        ""access_token"": ""test_access_token"",
        ""refresh_token"": ""test_refresh_token"",
        ""token_type"": ""Bearer"",
        ""expires_in"": 3600,
        ""scope"": ""read write""
    }";

    public const string InvalidTokenResponse = @"{
        ""error"": ""invalid_grant"",
        ""error_description"": ""The provided authorization grant is invalid""
    }";

    // SSE (Server-Sent Events) Testing
    public const string ValidSseData = @"data: {""text"": ""Hello""}

data: {""text"": "" World""}

data: [DONE]

";

    public const string SseEventWithId = @"id: 123
data: {""text"": ""Hello""}

";

    public const string SseEventWithEvent = @"event: message
data: {""text"": ""Hello""}

";

    // Error Messages
    public const string GenericErrorMessage = "An error occurred during testing";
    public const string AuthenticationErrorMessage = "Authentication failed";
    public const string NetworkErrorMessage = "Network request failed";
    public const string FileNotFoundErrorMessage = "File not found";
    public const string InvalidParameterErrorMessage = "Invalid parameter provided";
    public const string TokenExpiredErrorMessage = "Access token has expired";

    // Tool Names (matching actual Saturn tools)
    public const string ApplyDiffToolName = "apply_diff";
    public const string DeleteFileToolName = "delete_file";
    public const string GlobToolName = "glob";
    public const string GrepToolName = "grep";
    public const string ListFilesToolName = "list_files";
    public const string ReadFileToolName = "read_file";
    public const string SearchAndReplaceToolName = "search_and_replace";
    public const string WriteFileToolName = "write_file";

    // Multi-Agent Tool Names
    public const string CreateAgentToolName = "create_agent";
    public const string HandOffToAgentToolName = "handoff_to_agent";
    public const string WaitForAgentToolName = "wait_for_agent";
    public const string GetAgentStatusToolName = "get_agent_status";

    // Provider Names
    public const string AnthropicProviderName = "Anthropic";
    public const string OpenRouterProviderName = "OpenRouter";

    // Saturn Configuration
    public const int DefaultMaxTokens = 4096;
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxSubAgents = 5;
    public const bool DefaultEnableStreaming = true;

    // Test Categories (for organizing tests)
    public const string IntegrationTest = "Integration";
    public const string UnitTest = "Unit";
    public const string PerformanceTest = "Performance";
    public const string SecurityTest = "Security";

    // Performance Benchmarks
    public const int MaxToolExecutionTimeMs = 10;
    public const int MaxParallelToolExecutionTimeMs = 50;
    public const int MaxSseProcessingTimeMs = 100;
    public const long MaxSseDataSize = 1024 * 1024; // 1MB

    // Encryption Testing
    public const string TestPlaintext = "This is sensitive test data";
    public const string TestPassword = "test_password_123";
    public const string TestSalt = "test_salt_456";
}