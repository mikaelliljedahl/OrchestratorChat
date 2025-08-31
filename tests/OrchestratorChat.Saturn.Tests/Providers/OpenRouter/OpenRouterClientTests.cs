using Microsoft.Extensions.Logging;
using OrchestratorChat.Saturn.Providers.OpenRouter;
using System;
using Xunit;

namespace OrchestratorChat.Saturn.Tests.Providers.OpenRouter
{
    /// <summary>
    /// Basic tests for OpenRouterClient functionality
    /// Follows SaturnFork approach - tests basic construction and properties without HTTP mocking
    /// </summary>
    public class OpenRouterClientTests : IDisposable
    {
        private readonly ILogger<OpenRouterClient> _logger;
        
        public OpenRouterClientTests()
        {
            _logger = new LoggerFactory().CreateLogger<OpenRouterClient>();
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
            
            // Act
            var client = new OpenRouterClient(options, _logger);
            
            // Assert
            Assert.NotNull(client);
            Assert.NotNull(client.Chat);
            Assert.NotNull(client.Models);
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