using System.Net;
using System.Text;

namespace OrchestratorChat.Saturn.Tests.TestHelpers;

/// <summary>
/// Mock HTTP message handler for testing HTTP clients without real network calls.
/// Ported from SaturnFork project with namespace updates.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Gets all requests that have been sent through this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Gets the last request that was sent, or null if no requests have been sent.
    /// </summary>
    public HttpRequestMessage? LastRequest => _requests.LastOrDefault();

    /// <summary>
    /// Enqueues a simple text response with the specified status code.
    /// </summary>
    /// <param name="status">HTTP status code</param>
    /// <param name="content">Response content as string</param>
    /// <param name="contentType">Content type header (default: application/json)</param>
    public void EnqueueResponse(HttpStatusCode status, string content, string contentType = "application/json")
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        };
        _responses.Enqueue(response);
    }

    /// <summary>
    /// Enqueues a response with custom headers.
    /// </summary>
    /// <param name="status">HTTP status code</param>
    /// <param name="content">Response content</param>
    /// <param name="headers">Custom headers to add</param>
    public void EnqueueResponse(HttpStatusCode status, string content, Dictionary<string, string> headers)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        foreach (var header in headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        _responses.Enqueue(response);
    }

    /// <summary>
    /// Enqueues a streaming response for Server-Sent Events (SSE) testing.
    /// </summary>
    /// <param name="chunks">Sequence of SSE chunks to send</param>
    /// <param name="delayMs">Delay between chunks in milliseconds</param>
    public void EnqueueStreamingResponse(IEnumerable<string> chunks, int delayMs = 10)
    {
        var sseContent = string.Join("\n\n", chunks.Select(chunk => $"data: {chunk}"));
        sseContent += "\n\ndata: [DONE]\n\n";
        
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
        };
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        _responses.Enqueue(response);
    }

    /// <summary>
    /// Enqueues a JSON response.
    /// </summary>
    /// <param name="status">HTTP status code</param>
    /// <param name="jsonObject">Object to serialize as JSON</param>
    public void EnqueueJsonResponse(HttpStatusCode status, object jsonObject)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        
        EnqueueResponse(status, json, "application/json");
    }

    /// <summary>
    /// Enqueues an error response.
    /// </summary>
    /// <param name="status">HTTP error status code</param>
    /// <param name="errorMessage">Error message</param>
    public void EnqueueErrorResponse(HttpStatusCode status, string errorMessage)
    {
        var errorResponse = new
        {
            error = new
            {
                message = errorMessage,
                type = "error",
                code = status.ToString()
            }
        };
        
        EnqueueJsonResponse(status, errorResponse);
    }

    /// <summary>
    /// Simulates a network timeout.
    /// </summary>
    /// <param name="timeoutMs">Timeout duration in milliseconds</param>
    public void EnqueueTimeout(int timeoutMs = 5000)
    {
        _responses.Enqueue(new TimeoutResponseMessage(timeoutMs));
    }

    /// <summary>
    /// Verifies that a request with the specified method and URL was made.
    /// </summary>
    /// <param name="method">Expected HTTP method</param>
    /// <param name="url">Expected URL (can be partial)</param>
    /// <returns>True if a matching request was found</returns>
    public bool VerifyRequest(HttpMethod method, string url)
    {
        return _requests.Any(r => r.Method == method && r.RequestUri?.ToString().Contains(url) == true);
    }

    /// <summary>
    /// Verifies that a request with the specified method, URL and body content was made.
    /// </summary>
    /// <param name="method">Expected HTTP method</param>
    /// <param name="url">Expected URL (can be partial)</param>
    /// <param name="bodyContent">Expected body content (can be partial)</param>
    /// <returns>True if a matching request was found</returns>
    public async Task<bool> VerifyRequestAsync(HttpMethod method, string url, string bodyContent)
    {
        foreach (var request in _requests.Where(r => r.Method == method && r.RequestUri?.ToString().Contains(url) == true))
        {
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync();
                if (content.Contains(bodyContent))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Clears all queued responses and recorded requests.
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        _requests.Clear();
    }

    /// <summary>
    /// Gets the number of requests that have been sent.
    /// </summary>
    public int RequestCount => _requests.Count;

    /// <summary>
    /// Gets the number of responses still queued.
    /// </summary>
    public int QueuedResponseCount => _responses.Count;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Store the request for verification
        _requests.Add(request);

        // If no responses are queued, return a default 404 response
        if (_responses.Count == 0)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("No response configured for this request", Encoding.UTF8, "text/plain")
            };
        }

        var response = _responses.Dequeue();

        // Handle timeout simulation
        if (response is TimeoutResponseMessage timeoutResponse)
        {
            await Task.Delay(timeoutResponse.TimeoutMs, cancellationToken);
            throw new TaskCanceledException("The request timed out");
        }

        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            while (_responses.Count > 0)
            {
                _responses.Dequeue().Dispose();
            }
            
            foreach (var request in _requests)
            {
                request.Dispose();
            }
            _requests.Clear();
        }
        
        base.Dispose(disposing);
    }

    /// <summary>
    /// Internal class to represent a timeout response for simulation purposes.
    /// </summary>
    private class TimeoutResponseMessage : HttpResponseMessage
    {
        public int TimeoutMs { get; }

        public TimeoutResponseMessage(int timeoutMs)
        {
            TimeoutMs = timeoutMs;
        }
    }
}