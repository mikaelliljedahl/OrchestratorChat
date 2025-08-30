using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Orchestration;
using OrchestratorChat.Core.Events;
using OrchestratorChat.SignalR.Services;
using OrchestratorChat.SignalR.IntegrationTests.Helpers;
using Moq;

namespace OrchestratorChat.SignalR.IntegrationTests.Fixtures
{
    /// <summary>
    /// Test fixture for SignalR integration testing
    /// </summary>
    public class SignalRTestFixture : IAsyncDisposable
    {
        public TestWebApplicationFactory Factory { get; }
        public HttpClient HttpClient { get; }
        
        public Mock<IOrchestrator> MockOrchestrator { get; }
        public Mock<ISessionManager> MockSessionManager { get; }
        public Mock<IAgentFactory> MockAgentFactory { get; }
        public Mock<IEventBus> MockEventBus { get; }
        
        public SignalRTestFixture()
        {
            // Create mocks
            MockOrchestrator = new Mock<IOrchestrator>();
            MockSessionManager = new Mock<ISessionManager>();
            MockAgentFactory = new MockAgentFactory();
            MockEventBus = new Mock<IEventBus>();
            
            // Create test web application
            Factory = new TestWebApplicationFactory();
            
            // Configure test services
            Factory.ConfigureTestServices(services =>
            {
                // Replace real services with mocks
                services.AddSingleton(MockOrchestrator.Object);
                services.AddSingleton(MockSessionManager.Object);
                services.AddSingleton(MockAgentFactory.Object);
                services.AddSingleton(MockEventBus.Object);
                
                // Add test-specific services
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            });
            
            HttpClient = Factory.CreateClient();
        }
        
        /// <summary>
        /// Creates a SignalR test client for the orchestrator hub
        /// </summary>
        /// <returns>Configured SignalR test client</returns>
        public async Task<SignalRTestClient> CreateOrchestratorClientAsync()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(Factory.Server.BaseAddress + "hubs/orchestrator", options =>
                {
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                })
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
                .Build();
                
            var client = new SignalRTestClient(connection, "orchestrator");
            await client.StartAsync();
            
            return client;
        }
        
        /// <summary>
        /// Creates a SignalR test client for the agent hub
        /// </summary>
        /// <returns>Configured SignalR test client</returns>
        public async Task<SignalRTestClient> CreateAgentClientAsync()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(Factory.Server.BaseAddress + "hubs/agents", options =>
                {
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                })
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
                .Build();
                
            var client = new SignalRTestClient(connection, "agent");
            await client.StartAsync();
            
            return client;
        }
        
        /// <summary>
        /// Resets all mocks to their default state
        /// </summary>
        public void ResetMocks()
        {
            MockOrchestrator.Reset();
            MockSessionManager.Reset();
            MockAgentFactory.Reset();
            MockEventBus.Reset();
        }
        
        public async ValueTask DisposeAsync()
        {
            HttpClient?.Dispose();
            await Factory.DisposeAsync();
        }
    }
}