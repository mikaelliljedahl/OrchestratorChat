namespace OrchestratorChat.Agents.Tests.TestHelpers;

/// <summary>
/// Shared test constants for Agent tests.
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
    public const string TestClaudeExecutable = "claude";
    public const string TestAnthropicAuthUrl = "https://auth.anthropic.com";
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

    public static readonly Dictionary<string, object> BashParams = new()
    {
        { "command", "echo 'Hello World'" },
        { "timeout", 10 },
        { "working_directory", "/tmp" }
    };

    // Process Testing
    public const int DefaultProcessTimeoutMs = 5000;
    public const int DefaultStreamReadTimeoutMs = 1000;
    public const string TestProcessOutput = "Test process output";
    public const string TestProcessError = "Test process error";

    // Agent Configuration
    public const string DefaultAgentId = "test-agent-001";
    public const string DefaultAgentName = "Test Agent";
    public const string DefaultAgentType = "ClaudeAgent";

    // Authentication
    public const string TestPkceCodeVerifier = "test_code_verifier_1234567890abcdefghijklmnopqrstuvwxyz";
    public const string TestPkceCodeChallenge = "test_code_challenge_abcdef1234567890";
    public const string TestAuthorizationCode = "auth_code_test_12345";

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

    // JSON Response Templates
    public const string ValidClaudeResponse = @"{
        ""id"": ""msg_123"",
        ""type"": ""message"",
        ""role"": ""assistant"",
        ""content"": [{""type"": ""text"", ""text"": ""Hello from Claude""}],
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
        ""id"": ""msg_123"",
        ""type"": ""content_block_delta"",
        ""delta"": {""type"": ""text_delta"", ""text"": ""Hello""}
    }";

    // Error Messages
    public const string GenericErrorMessage = "An error occurred during testing";
    public const string AuthenticationErrorMessage = "Authentication failed";
    public const string NetworkErrorMessage = "Network request failed";
    public const string FileNotFoundErrorMessage = "File not found";
    public const string ProcessStartErrorMessage = "Failed to start process";

    // Agent Status Values
    public const string AgentStatusReady = "Ready";
    public const string AgentStatusBusy = "Busy";
    public const string AgentStatusError = "Error";
    public const string AgentStatusStopped = "Stopped";

    // Command Approval
    public const string SafeCommand = "ls -la";
    public const string DangerousCommand = "rm -rf /";
    public const string YoloModeReason = "YOLO mode enabled - automatically approved";

    // Multi-Agent Constants
    public const string PrimaryAgentId = "primary-agent";
    public const string SubAgentId = "sub-agent-001";
    public const string HandoffMessage = "Handing off task to sub-agent";
    public const int MaxSubAgents = 5;

    // Health Monitoring
    public const int HealthCheckIntervalMs = 10000;
    public const int UnhealthyThresholdMs = 30000;
    
    // Test Categories (for organizing tests)
    public const string IntegrationTest = "Integration";
    public const string UnitTest = "Unit";
    public const string PerformanceTest = "Performance";
    public const string SecurityTest = "Security";
}