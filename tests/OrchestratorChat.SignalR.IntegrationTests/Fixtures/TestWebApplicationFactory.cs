using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OrchestratorChat.SignalR.Hubs;
using OrchestratorChat.SignalR.Services;
using OrchestratorChat.Core.Events;
using OrchestratorChat.Data;

namespace OrchestratorChat.SignalR.IntegrationTests.Fixtures
{
    /// <summary>
    /// Test program class for WebApplicationFactory
    /// </summary>
    public class TestProgram
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TestStartup>();
                });
    }
    
    /// <summary>
    /// Test startup class
    /// </summary>
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });
            
            // Add in-memory database for tests
            services.AddDbContext<OrchestratorDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString()));
            
            // Add SignalR services
            services.AddScoped<IMessageRouter, MessageRouter>();
            services.AddScoped<IConnectionManager, ConnectionManager>();
            
            // Add placeholder services (will be replaced by mocks)
            services.AddScoped<OrchestratorChat.Core.Orchestration.IOrchestrator>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            services.AddScoped<OrchestratorChat.Core.Sessions.ISessionManager>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            services.AddScoped<OrchestratorChat.Core.Agents.IAgentFactory>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            services.AddScoped<OrchestratorChat.Core.Events.IEventBus>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            services.AddScoped<OrchestratorChat.Core.Agents.IAgentRegistry>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            services.AddScoped<OrchestratorChat.Data.Repositories.IAgentRepository>(provider => 
                throw new InvalidOperationException("Mock should be provided"));
            
            // Add logging
            services.AddLogging();
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OrchestratorHub>("/hubs/orchestrator");
                endpoints.MapHub<AgentHub>("/hubs/agent");
            });
        }
    }

    /// <summary>
    /// Custom web application factory for SignalR integration tests
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<TestProgram>
    {
        private Action<IServiceCollection>? _configureTestServices;
        
        /// <summary>
        /// Configures test-specific services
        /// </summary>
        /// <param name="configureServices">Service configuration action</param>
        public void ConfigureTestServices(Action<IServiceCollection> configureServices)
        {
            _configureTestServices = configureServices;
        }
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Logging:LogLevel:Default", "Debug"),
                    new KeyValuePair<string, string?>("Logging:LogLevel:Microsoft.AspNetCore.SignalR", "Debug"),
                    new KeyValuePair<string, string?>("Logging:LogLevel:Microsoft.AspNetCore.Http.Connections", "Debug")
                });
            });
            
            builder.UseEnvironment("Test");
            
            builder.ConfigureServices(services =>
            {
                // Override real services with test mocks  
                _configureTestServices?.Invoke(services);
            });
        }
    }
}