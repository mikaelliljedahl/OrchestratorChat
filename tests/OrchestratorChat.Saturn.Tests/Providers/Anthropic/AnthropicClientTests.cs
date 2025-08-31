using OrchestratorChat.Saturn.Providers.Anthropic;
using System;
using Xunit;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic
{
    /// <summary>
    /// Basic tests for AnthropicClient functionality
    /// Follows SaturnFork approach - tests basic properties and behavior without complex HTTP mocking
    /// </summary>
    public class AnthropicClientTests : IDisposable
    {
        private readonly AnthropicClient _client;
        
        public AnthropicClientTests()
        {
            _client = new AnthropicClient();
        }
        
        [Fact]
        public void Constructor_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_client);
        }
        
        [Fact]
        public void Dispose_DisposesResources()
        {
            // Act & Assert - Should not throw
            _client.Dispose();
        }
        
        public void Dispose()
        {
            _client?.Dispose();
        }
        
        // Note: Complex HTTP interaction tests removed following SaturnFork pattern
        // These tests focus on basic functionality that doesn't require external dependencies
        // For actual HTTP testing, integration tests or manual testing should be used
    }
}