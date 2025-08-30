using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestratorChat.SignalR.EventHandlers;
using OrchestratorChat.SignalR.Hubs;
using OrchestratorChat.SignalR.Services;
using OrchestratorChat.SignalR.Streaming;

namespace OrchestratorChat.SignalR
{
    /// <summary>
    /// Extension methods for configuring SignalR services
    /// </summary>
    public static class SignalRConfiguration
    {
        /// <summary>
        /// Add OrchestratorChat SignalR services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddOrchestratorSignalR(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add SignalR with comprehensive configuration
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = configuration.GetValue<bool>("SignalR:EnableDetailedErrors");
                options.MaximumReceiveMessageSize = configuration.GetValue<long>("SignalR:MaximumReceiveMessageSize", 102400);
                options.StreamBufferCapacity = configuration.GetValue<int>("SignalR:StreamBufferCapacity", 10);
                options.KeepAliveInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:KeepAliveInterval", 15));
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("SignalR:ClientTimeoutInterval", 30));
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.WriteIndented = false;
                options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            // Add connection management
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IStreamManager, StreamManager>();
            
            // Add message routing
            services.AddSingleton<IMessageRouter, MessageRouter>();
            
            // Add event handlers
            services.AddScoped<AgentEventHandler>();
            services.AddScoped<OrchestrationEventHandler>();

            // Add hosted service for cleanup
            services.AddHostedService<SignalRCleanupService>();

            // Add event bus subscriber
            services.AddHostedService<EventBusSubscriber>();

            return services;
        }

        /// <summary>
        /// Configure SignalR hubs in the application
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder UseOrchestratorSignalR(
            this IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OrchestratorHub>("/hubs/orchestrator");
                endpoints.MapHub<AgentHub>("/hubs/agent");
            });
            
            return app;
        }
    }

    /// <summary>
    /// Background service for SignalR cleanup tasks
    /// </summary>
    public class SignalRCleanupService : BackgroundService
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IStreamManager _streamManager;
        private readonly ILogger<SignalRCleanupService> _logger;

        /// <summary>
        /// Initializes a new instance of SignalRCleanupService
        /// </summary>
        public SignalRCleanupService(
            IConnectionManager connectionManager,
            IStreamManager streamManager,
            ILogger<SignalRCleanupService> logger)
        {
            _connectionManager = connectionManager;
            _streamManager = streamManager;
            _logger = logger;
        }

        /// <summary>
        /// Execute the background cleanup service
        /// </summary>
        /// <param name="stoppingToken">Cancellation token</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SignalR cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Cleanup orphaned streams every 5 minutes
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                    // Perform cleanup logic
                    _logger.LogDebug("Running SignalR cleanup");

                    // Here you could add logic to:
                    // - Clean up orphaned connections
                    // - Close inactive streams
                    // - Remove expired sessions
                    // - Log statistics

                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SignalR cleanup service");
                }
            }

            _logger.LogInformation("SignalR cleanup service stopped");
        }
    }
}