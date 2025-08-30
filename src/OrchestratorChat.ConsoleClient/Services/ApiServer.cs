using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using OrchestratorChat.ConsoleClient.Models;

namespace OrchestratorChat.ConsoleClient.Services
{
    /// <summary>
    /// HTTP API server for sending messages to agents
    /// </summary>
    public class ApiServer : IHostedService
    {
        private readonly ILogger<ApiServer> _logger;
        private readonly PersistentSignalRClient _signalRClient;
        private readonly MessageHandler _messageHandler;
        private readonly int _port;
        private readonly string _defaultAgentId;

        private WebApplication? _app;

        public ApiServer(
            ILogger<ApiServer> logger,
            PersistentSignalRClient signalRClient,
            MessageHandler messageHandler,
            int port,
            string defaultAgentId)
        {
            _logger = logger;
            _signalRClient = signalRClient;
            _messageHandler = messageHandler;
            _port = port;
            _defaultAgentId = defaultAgentId;
        }

        /// <summary>
        /// Start the API server
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var builder = WebApplication.CreateBuilder();
                
                // Configure logging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Information);

                // Configure web host
                builder.WebHost.UseUrls($"http://localhost:{_port}");

                _app = builder.Build();

                ConfigureEndpoints(_app);

                await _app.StartAsync(cancellationToken);
                _logger.LogInformation("API server started on http://localhost:{Port}", _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start API server on port {Port}", _port);
                throw;
            }
        }

        /// <summary>
        /// Stop the API server
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_app != null)
                {
                    await _app.StopAsync(cancellationToken);
                    await _app.DisposeAsync();
                }
                _logger.LogInformation("API server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping API server");
            }
        }

        /// <summary>
        /// Configure API endpoints
        /// </summary>
        private void ConfigureEndpoints(WebApplication app)
        {
            // Health check endpoint
            app.MapGet("/health", async (HttpContext context) =>
            {
                var health = new
                {
                    Status = _signalRClient.IsConnected ? "Connected" : "Disconnected",
                    SessionId = _signalRClient.CurrentSessionId,
                    ActiveCommands = _messageHandler.GetActiveCommandCount(),
                    ActiveChannels = _messageHandler.GetActiveChannelCount(),
                    Timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(health, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }));
            });

            // Send message endpoint
            app.MapPost("/send-message", async (HttpContext context) =>
            {
                try
                {
                    if (!_signalRClient.IsConnected)
                    {
                        context.Response.StatusCode = 503; // Service Unavailable
                        await context.Response.WriteAsync("SignalR client not connected");
                        return;
                    }

                    // Read request body
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    
                    var command = JsonSerializer.Deserialize<ClientCommand>(requestBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (command == null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid request body");
                        return;
                    }

                    // Set defaults
                    if (string.IsNullOrEmpty(command.CommandId))
                        command.CommandId = Guid.NewGuid().ToString();
                    
                    if (string.IsNullOrEmpty(command.AgentId))
                        command.AgentId = _defaultAgentId;

                    if (string.IsNullOrEmpty(command.WorkingDirectory))
                        command.WorkingDirectory = Environment.CurrentDirectory;

                    _logger.LogInformation("Processing message request for agent {AgentId}: {Message}", 
                        command.AgentId, command.Message);

                    // Register command for response tracking
                    var responseChannel = _messageHandler.RegisterCommand(command.CommandId);

                    // Send message via SignalR
                    var success = await _signalRClient.SendMessageToAgentAsync(command);

                    if (!success)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Failed to send message to agent");
                        _messageHandler.CleanupCommand(command.CommandId);
                        return;
                    }

                    // Set up streaming response
                    context.Response.ContentType = "application/x-ndjson"; // Newline delimited JSON
                    context.Response.Headers["Cache-Control"] = "no-cache";
                    context.Response.Headers["Connection"] = "keep-alive";

                    // Stream responses as they come in
                    await foreach (var response in responseChannel.Reader.ReadAllAsync(context.RequestAborted))
                    {
                        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });
                        
                        await context.Response.WriteAsync(responseJson + "\n");
                        await context.Response.Body.FlushAsync();

                        // Break if command is completed or errored
                        if (response.Status == "completed" || response.Status == "error")
                        {
                            break;
                        }
                    }

                    _logger.LogInformation("Completed streaming response for command {CommandId}", command.CommandId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Client disconnected during message streaming");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing send-message request");
                    
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Internal server error: {ex.Message}");
                    }
                }
            });

            // Get session info endpoint
            app.MapGet("/session", async (HttpContext context) =>
            {
                var sessionInfo = new
                {
                    SessionId = _signalRClient.CurrentSessionId,
                    IsConnected = _signalRClient.IsConnected,
                    DefaultAgentId = _defaultAgentId,
                    ActiveCommands = _messageHandler.GetActiveCommandCount(),
                    Timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(sessionInfo, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }));
            });

            // Simple root endpoint
            app.MapGet("/", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("OrchestratorChat Console Client API\n\n" +
                    "Available endpoints:\n" +
                    "GET /health - Connection health status\n" +
                    "POST /send-message - Send message to agent\n" +
                    "GET /session - Get current session info\n");
            });

            _logger.LogInformation("Configured API endpoints");
        }
    }
}