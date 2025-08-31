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
using OrchestratorChat.Data.Repositories;
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
        public MockAgentFactory MockAgentFactory { get; }
        public Mock<IEventBus> MockEventBus { get; }
        public Mock<IAgentRegistry> MockAgentRegistry { get; }
        public Mock<IAgentRepository> MockAgentRepository { get; }
        
        public SignalRTestFixture()
        {
            // Create mocks
            MockOrchestrator = new Mock<IOrchestrator>();
            MockSessionManager = new Mock<ISessionManager>();
            MockAgentFactory = new MockAgentFactory();
            MockEventBus = new Mock<IEventBus>();
            MockAgentRegistry = new Mock<IAgentRegistry>();
            MockAgentRepository = new Mock<IAgentRepository>();
            
            // Create test web application
            Factory = new TestWebApplicationFactory();
            
            // Configure test services
            Factory.ConfigureTestServices(services =>
            {
                // Remove existing service registrations and replace with mocks
                RemoveService<IOrchestrator>(services);
                RemoveService<ISessionManager>(services);
                RemoveService<IAgentFactory>(services);
                RemoveService<IEventBus>(services);
                RemoveService<IAgentRegistry>(services);
                RemoveService<IAgentRepository>(services);
                
                // Add mocks with same lifetime as original registrations
                services.AddScoped<IOrchestrator>(provider => MockOrchestrator.Object);
                services.AddScoped<ISessionManager>(provider => MockSessionManager.Object);
                services.AddScoped<IAgentFactory>(provider => MockAgentFactory.Object);
                services.AddScoped<IEventBus>(provider => MockEventBus.Object);
                services.AddScoped<IAgentRegistry>(provider => MockAgentRegistry.Object);
                services.AddScoped<IAgentRepository>(provider => MockAgentRepository.Object);
                
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
                .WithUrl(Factory.Server.BaseAddress + "hubs/agent", options =>
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
            MockAgentFactory.Clear();
            MockEventBus.Reset();
            MockAgentRegistry.Reset();
            MockAgentRepository.Reset();
        }
        
        /// <summary>
        /// Helper method to remove a service from the collection
        /// </summary>
        private static void RemoveService<T>(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
            if (descriptor != null)
                services.Remove(descriptor);
        }
        
        public async ValueTask DisposeAsync()
        {
            HttpClient?.Dispose();
            await Factory.DisposeAsync();
        }
    }
}