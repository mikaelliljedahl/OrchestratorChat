using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Providers.Streaming;

/// <summary>
/// Server-Sent Events parser for streaming responses from LLM providers
/// </summary>
public static class SseParser
{
    private const string DataPrefix = "data: ";
    private const string EventPrefix = "event: ";
    private const string CommentPrefix = ":";
    private const string DoneMessage = "[DONE]";
    
    /// <summary>
    /// Parses Server-Sent Events from an HTTP response stream
    /// </summary>
    /// <param name="response">HTTP response containing SSE stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of SSE events</returns>
    public static async IAsyncEnumerable<SseEvent> ParseStreamAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (response.Content == null)
        {
            yield break;
        }

        await foreach (var sseEvent in ParseStreamInternalAsync(response, cancellationToken))
        {
            yield return sseEvent;
        }
    }
    
    private static async IAsyncEnumerable<SseEvent> ParseStreamInternalAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        
        var eventBuilder = new SseEventBuilder();
        var lineBuffer = new StringBuilder();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await ReadLineAsync(reader, lineBuffer, cancellationToken);
                if (line == null)
                {
                    // End of stream
                    break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log error but don't throw to allow partial results
                break;
            }
            
            // Empty line indicates end of event
            if (string.IsNullOrWhiteSpace(line))
            {
                var sseEvent = eventBuilder.Build();
                if (sseEvent != null)
                {
                    // Check for terminal message
                    if (sseEvent.Data == DoneMessage)
                    {
                        yield break;
                    }
                    
                    yield return sseEvent;
                }
                eventBuilder.Reset();
                continue;
            }
            
            // Parse SSE line
            if (line.StartsWith(DataPrefix, StringComparison.Ordinal))
            {
                var data = line.Substring(DataPrefix.Length);
                eventBuilder.AddData(data);
            }
            else if (line.StartsWith(EventPrefix, StringComparison.Ordinal))
            {
                var eventType = line.Substring(EventPrefix.Length);
                eventBuilder.SetEventType(eventType);
            }
            else if (line.StartsWith(CommentPrefix, StringComparison.Ordinal))
            {
                // Comments are ignored in SSE
                continue;
            }
            else if (line.Contains(':'))
            {
                // Handle other SSE fields (id:, retry:, etc.)
                var colonIndex = line.IndexOf(':');
                var field = line.Substring(0, colonIndex);
                var value = line.Substring(colonIndex + 1).TrimStart();
                eventBuilder.AddField(field, value);
            }
        }
    }
    
    /// <summary>
    /// Parses JSON data from SSE events into typed objects
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="sseEvents">Stream of SSE events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of deserialized objects</returns>
    public static async IAsyncEnumerable<T?> ParseJsonDataAsync<T>(
        IAsyncEnumerable<SseEvent> sseEvents,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        await foreach (var sseEvent in sseEvents.WithCancellation(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(sseEvent.Data))
            {
                continue;
            }
            
            T? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<T>(sseEvent.Data, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                // Skip malformed JSON
                continue;
            }
            
            if (parsed != null)
            {
                yield return parsed;
            }
        }
    }
    
    /// <summary>
    /// Reads a line from the stream reader with proper buffer management
    /// </summary>
    private static async Task<string?> ReadLineAsync(
        StreamReader reader, 
        StringBuilder buffer, 
        CancellationToken cancellationToken)
    {
        buffer.Clear();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var ch = reader.Read();
            if (ch == -1)
            {
                // End of stream
                return buffer.Length > 0 ? buffer.ToString() : null;
            }
            
            if (ch == '\n')
            {
                // End of line
                return buffer.ToString();
            }
            
            if (ch == '\r')
            {
                // Handle CRLF
                var next = reader.Peek();
                if (next == '\n')
                {
                    reader.Read(); // consume the \n
                }
                return buffer.ToString();
            }
            
            buffer.Append((char)ch);
        }
        
        return buffer.Length > 0 ? buffer.ToString() : null;
    }
}

/// <summary>
/// Represents a Server-Sent Event
/// </summary>
public class SseEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int? Retry { get; set; }
    public Dictionary<string, string> AdditionalFields { get; set; } = new();
}

/// <summary>
/// Builder for constructing SSE events
/// </summary>
internal class SseEventBuilder
{
    private string _eventType = string.Empty;
    private readonly StringBuilder _data = new();
    private string _id = string.Empty;
    private int? _retry;
    private readonly Dictionary<string, string> _additionalFields = new();
    
    public void SetEventType(string eventType)
    {
        _eventType = eventType.Trim();
    }
    
    public void AddData(string data)
    {
        if (_data.Length > 0)
        {
            _data.AppendLine();
        }
        _data.Append(data);
    }
    
    public void AddField(string field, string value)
    {
        switch (field.ToLowerInvariant())
        {
            case "id":
                _id = value;
                break;
            case "retry":
                if (int.TryParse(value, out var retryValue))
                {
                    _retry = retryValue;
                }
                break;
            default:
                _additionalFields[field] = value;
                break;
        }
    }
    
    public SseEvent? Build()
    {
        // Only return event if we have data
        if (_data.Length == 0)
        {
            return null;
        }
        
        return new SseEvent
        {
            EventType = _eventType,
            Data = _data.ToString(),
            Id = _id,
            Retry = _retry,
            AdditionalFields = new Dictionary<string, string>(_additionalFields)
        };
    }
    
    public void Reset()
    {
        _eventType = string.Empty;
        _data.Clear();
        _id = string.Empty;
        _retry = null;
        _additionalFields.Clear();
    }
}