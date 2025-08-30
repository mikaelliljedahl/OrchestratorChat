using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;
using OrchestratorChat.SignalR.Contracts.Responses;
using OrchestratorChat.ConsoleClient.Models;

namespace OrchestratorChat.ConsoleClient.Services
{
    /// <summary>
    /// Handles incoming SignalR messages and streams them to HTTP clients
    /// </summary>
    public class MessageHandler
    {
        private readonly ILogger<MessageHandler> _logger;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ClientResponse>> _pendingCommands;
        private readonly ConcurrentDictionary<string, Channel<ClientResponse>> _responseChannels;

        public MessageHandler(ILogger<MessageHandler> logger)
        {
            _logger = logger;
            _pendingCommands = new ConcurrentDictionary<string, TaskCompletionSource<ClientResponse>>();
            _responseChannels = new ConcurrentDictionary<string, Channel<ClientResponse>>();
        }

        /// <summary>
        /// Register a command and get a channel for streaming responses
        /// </summary>
        public Channel<ClientResponse> RegisterCommand(string commandId)
        {
            var channel = Channel.CreateUnbounded<ClientResponse>();
            _responseChannels[commandId] = channel;
            
            var tcs = new TaskCompletionSource<ClientResponse>();
            _pendingCommands[commandId] = tcs;

            _logger.LogInformation("Registered command {CommandId} for response streaming", commandId);
            return channel;
        }

        /// <summary>
        /// Wait for a specific command to complete
        /// </summary>
        public Task<ClientResponse> WaitForCompletionAsync(string commandId, CancellationToken cancellationToken = default)
        {
            if (_pendingCommands.TryGetValue(commandId, out var tcs))
            {
                return tcs.Task.WaitAsync(cancellationToken);
            }

            return Task.FromResult(new ClientResponse
            {
                CommandId = commandId,
                Status = "error",
                Content = "Command not found",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Handle incoming agent response from SignalR
        /// </summary>
        public async Task HandleAgentResponseAsync(AgentResponseDto response)
        {
            try
            {
                _logger.LogInformation("Processing agent response from {AgentId} for session {SessionId}", 
                    response.AgentId, response.SessionId);

                // Use actual command ID from the request metadata instead of synthetic one
                var commandId = response.Response.Metadata?.ContainsKey("CommandId") == true 
                    ? response.Response.Metadata["CommandId"].ToString()
                    : $"{response.SessionId}_{response.AgentId}";

                var clientResponse = new ClientResponse
                {
                    CommandId = commandId,
                    SessionId = response.SessionId,
                    Status = DetermineStatus(response),
                    Content = response.Response?.Content ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                };

                // Send to response channel if exists
                if (_responseChannels.TryGetValue(commandId, out var channel))
                {
                    await channel.Writer.WriteAsync(clientResponse);
                    
                    // If this is a completion, close the channel
                    if (clientResponse.Status == "completed" || clientResponse.Status == "error")
                    {
                        channel.Writer.Complete();
                        _responseChannels.TryRemove(commandId, out _);

                        // Complete the task completion source
                        if (_pendingCommands.TryRemove(commandId, out var tcs))
                        {
                            tcs.SetResult(clientResponse);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Received response for unknown command {CommandId}", commandId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling agent response");
            }
        }

        /// <summary>
        /// Handle command completion (marks command as completed)
        /// </summary>
        public async Task CompleteCommandAsync(string commandId, string content, string status = "completed")
        {
            try
            {
                var response = new ClientResponse
                {
                    CommandId = commandId,
                    Status = status,
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };

                // Send to response channel
                if (_responseChannels.TryGetValue(commandId, out var channel))
                {
                    await channel.Writer.WriteAsync(response);
                    channel.Writer.Complete();
                    _responseChannels.TryRemove(commandId, out _);
                }

                // Complete the task completion source
                if (_pendingCommands.TryRemove(commandId, out var tcs))
                {
                    tcs.SetResult(response);
                }

                _logger.LogInformation("Completed command {CommandId} with status {Status}", commandId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing command {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Handle command error
        /// </summary>
        public async Task ErrorCommandAsync(string commandId, string error)
        {
            await CompleteCommandAsync(commandId, error, "error");
        }

        /// <summary>
        /// Clean up resources for a command
        /// </summary>
        public void CleanupCommand(string commandId)
        {
            try
            {
                if (_responseChannels.TryRemove(commandId, out var channel))
                {
                    channel.Writer.Complete();
                }

                if (_pendingCommands.TryRemove(commandId, out var tcs))
                {
                    tcs.TrySetCanceled();
                }

                _logger.LogDebug("Cleaned up resources for command {CommandId}", commandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up command {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Determine status from agent response
        /// </summary>
        private static string DetermineStatus(AgentResponseDto response)
        {
            if (response.Response == null)
                return "error";

            // This is a simplified status determination
            // You might want to implement more sophisticated logic based on response content
            if (response.Response.Content?.Contains("Error:") == true)
                return "error";

            if (response.Response.Content?.Contains("Completed:") == true)
                return "completed";

            if (!response.Response.IsComplete)
                return "progress";

            return "completed";
        }

        /// <summary>
        /// Get active command count for health monitoring
        /// </summary>
        public int GetActiveCommandCount()
        {
            return _pendingCommands.Count;
        }

        /// <summary>
        /// Get response channel count for monitoring
        /// </summary>
        public int GetActiveChannelCount()
        {
            return _responseChannels.Count;
        }
    }
}