using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using OrchestratorChat.Saturn.Providers.OpenRouter.Services;
using System;
using Xunit;

namespace OrchestratorChat.Saturn.Tests.Providers.OpenRouter
{
    /// <summary>
    /// Basic tests for ChatCompletionsService functionality
    /// Follows SaturnFork approach - tests basic construction and properties without HTTP mocking
    /// </summary>
    public class ChatCompletionsServiceTests : IDisposable
    {
        private readonly ILogger<ChatCompletionsService> _logger;
        private readonly ILogger<HttpClientAdapter> _httpLogger;
        
        public ChatCompletionsServiceTests()
        {
            _logger = new LoggerFactory().CreateLogger<ChatCompletionsService>();
            _httpLogger = new LoggerFactory().CreateLogger<HttpClientAdapter>();
        }
        
        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var options = new OpenRouterOptions
            {
                ApiKey = "test_key",
                BaseUrl = "https://openrouter.ai/api/v1",
                HttpReferer = "test_referer",
                XTitle = "test_title"
            };
            var httpClient = new HttpClientAdapter(options, _httpLogger);
            
            // Act
            var service = new ChatCompletionsService(httpClient, options, _logger);
            
            // Assert
            Assert.NotNull(service);
        }
        
        public void Dispose()
        {
            // Cleanup if needed
        }
        
        // Note: HTTP interaction tests removed following SaturnFork pattern
        // Complex API calls should be tested through integration tests or manual testing
        // This keeps unit tests focused on construction and basic behavior
    }
}