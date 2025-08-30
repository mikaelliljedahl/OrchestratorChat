using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using OrchestratorChat.ConsoleClient;
using OrchestratorChat.ConsoleClient.Services;

namespace OrchestratorChat.ConsoleClient
{
    /// <summary>
    /// Main entry point for the OrchestratorChat Console Client
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        static async Task<int> Main(string[] args)
        {
            // Create command line interface
            var serverOption = new Option<string>(
                aliases: new[] { "--server", "-s" },
                description: "URL of the OrchestratorChat server",
                getDefaultValue: () => "https://localhost:5001");

            var apiPortOption = new Option<int>(
                aliases: new[] { "--api-port", "-p" },
                description: "Port for the HTTP API server",
                getDefaultValue: () => 8080);

            var agentOption = new Option<string>(
                aliases: new[] { "--agent", "-a" },
                description: "Default agent ID to send messages to",
                getDefaultValue: () => "claude-1");

            var sessionNameOption = new Option<string>(
                aliases: new[] { "--session-name", "-n" },
                description: "Name for the chat session",
                getDefaultValue: () => "Console Client Session");

            var rootCommand = new RootCommand("OrchestratorChat Console Client - Connects to SignalR hubs and provides HTTP API for agent communication")
            {
                serverOption,
                apiPortOption,
                agentOption,
                sessionNameOption
            };

            rootCommand.SetHandler(async (serverUrl, apiPort, agentId, sessionName) =>
            {
                try
                {
                    Console.WriteLine($"Starting OrchestratorChat Console Client...");
                    Console.WriteLine($"Server: {serverUrl}");
                    Console.WriteLine($"API Port: {apiPort}");
                    Console.WriteLine($"Default Agent: {agentId}");
                    Console.WriteLine($"Session Name: {sessionName}");
                    Console.WriteLine();

                    // Create and run the host
                    var host = CreateHostBuilder(serverUrl, apiPort, agentId, sessionName).Build();

                    // Handle graceful shutdown
                    var cancellationTokenSource = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        Console.WriteLine("\nShutting down gracefully...");
                        e.Cancel = true;
                        cancellationTokenSource.Cancel();
                    };

                    // Start the host
                    await host.StartAsync(cancellationTokenSource.Token);

                    Console.WriteLine("Console client started successfully!");
                    Console.WriteLine($"HTTP API available at: http://localhost:{apiPort}");
                    Console.WriteLine("Press Ctrl+C to exit");
                    Console.WriteLine();

                    // Wait for cancellation
                    await Task.Delay(-1, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Application cancelled by user");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Application error: {ex.Message}");
                    return;
                }
            }, serverOption, apiPortOption, agentOption, sessionNameOption);

            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Create the host builder with dependency injection configuration
        /// </summary>
        static IHostBuilder CreateHostBuilder(string serverUrl, int apiPort, string agentId, string sessionName)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                    
                    // Reduce SignalR noise
                    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
                    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);
                })
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<MessageHandler>();
                    
                    services.AddSingleton<PersistentSignalRClient>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<PersistentSignalRClient>>();
                        var messageHandler = provider.GetRequiredService<MessageHandler>();
                        return new PersistentSignalRClient(logger, messageHandler, serverUrl, agentId, sessionName);
                    });

                    services.AddSingleton<ApiServer>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<ApiServer>>();
                        var signalRClient = provider.GetRequiredService<PersistentSignalRClient>();
                        var messageHandler = provider.GetRequiredService<MessageHandler>();
                        return new ApiServer(logger, signalRClient, messageHandler, apiPort, agentId);
                    });

                    // Register hosted services
                    services.AddHostedService(provider => provider.GetRequiredService<PersistentSignalRClient>());
                    services.AddHostedService(provider => provider.GetRequiredService<ApiServer>());

                    // Add HTTP client services (might be needed by SignalR)
                    services.AddHttpClient();
                })
                .UseConsoleLifetime();
        }
    }
}