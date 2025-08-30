using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace OrchestratorChat.SignalR.Streaming
{
    /// <summary>
    /// Implementation of stream management using Channels
    /// </summary>
    public class StreamManager : IStreamManager
    {
        private readonly ConcurrentDictionary<string, Channel<object>> _streams = new();
        private readonly ILogger<StreamManager> _logger;

        /// <summary>
        /// Initializes a new instance of StreamManager
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public StreamManager(ILogger<StreamManager> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<string> CreateStream(string sessionId, string agentId)
        {
            var streamId = $"{sessionId}-{agentId}-{Guid.NewGuid()}";
            var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });

            _streams[streamId] = channel;
            
            _logger.LogDebug("Created stream {StreamId} for session {SessionId} and agent {AgentId}", 
                streamId, sessionId, agentId);
            
            return Task.FromResult(streamId);
        }

        /// <inheritdoc />
        public async Task WriteToStream(string streamId, object data)
        {
            if (_streams.TryGetValue(streamId, out var channel))
            {
                try
                {
                    await channel.Writer.WriteAsync(data);
                    _logger.LogTrace("Wrote data to stream {StreamId}", streamId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write to stream {StreamId}", streamId);
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("Attempted to write to non-existent stream {StreamId}", streamId);
            }
        }

        /// <inheritdoc />
        public Task CloseStream(string streamId)
        {
            if (_streams.TryRemove(streamId, out var channel))
            {
                channel.Writer.TryComplete();
                _logger.LogDebug("Closed stream {StreamId}", streamId);
            }
            else
            {
                _logger.LogWarning("Attempted to close non-existent stream {StreamId}", streamId);
            }
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> GetStream<T>(string streamId)
        {
            if (_streams.TryGetValue(streamId, out var channel))
            {
                _logger.LogTrace("Reading from stream {StreamId}", streamId);
                
                await foreach (var item in channel.Reader.ReadAllAsync())
                {
                    if (item is T typedItem)
                    {
                        yield return typedItem;
                    }
                    else
                    {
                        _logger.LogWarning("Stream {StreamId} contained item of unexpected type {ActualType}, expected {ExpectedType}",
                            streamId, item?.GetType().Name, typeof(T).Name);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Attempted to read from non-existent stream {StreamId}", streamId);
            }
        }
    }
}