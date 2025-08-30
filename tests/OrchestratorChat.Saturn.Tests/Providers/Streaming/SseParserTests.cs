using System.Net;
using System.Text;
using OrchestratorChat.Saturn.Providers.Streaming;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.Streaming;

/// <summary>
/// Tests for SseParser - Server-Sent Events parsing for streaming responses
/// </summary>
public class SseParserTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;

    public SseParserTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttp);
    }

    [Fact]
    public async Task ParseStreamAsync_ValidSSE_ParsesCorrectly()
    {
        // Arrange
        var sseData = @"data: {""type"": ""text"", ""content"": ""Hello""}

data: {""type"": ""text"", ""content"": "" World""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(2, events.Count);
        
        Assert.Equal(@"{""type"": ""text"", ""content"": ""Hello""}", events[0].Data);
        Assert.Equal(@"{""type"": ""text"", ""content"": "" World""}", events[1].Data);
        
        // All events should have empty event type by default
        Assert.Equal(string.Empty, events[0].EventType);
        Assert.Equal(string.Empty, events[1].EventType);
    }

    [Fact]
    public async Task ParseStreamAsync_WithComments_IgnoresComments()
    {
        // Arrange
        var sseData = @": This is a comment line
data: {""message"": ""Hello""}
: Another comment
: Yet another comment

data: {""message"": ""World""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal(@"{""message"": ""Hello""}", events[0].Data);
        Assert.Equal(@"{""message"": ""World""}", events[1].Data);
    }

    [Fact]
    public async Task ParseStreamAsync_Cancellation_StopsProcessing()
    {
        // Arrange
        var longSseData = @"data: event1

data: event2

data: event3

data: event4

data: [DONE]

";
        var response = CreateSseResponse(longSseData);
        using var cts = new CancellationTokenSource();

        // Act
        var events = new List<SseEvent>();
        var eventCount = 0;
        
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response, cts.Token))
        {
            events.Add(sseEvent);
            eventCount++;
            
            // Cancel after receiving 2 events
            if (eventCount == 2)
            {
                cts.Cancel();
            }
        }

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("event1", events[0].Data);
        Assert.Equal("event2", events[1].Data);
    }

    [Fact]
    public async Task ParseStreamAsync_MultipleDataFields_CombinesCorrectly()
    {
        // Arrange
        var sseData = @"data: First line
data: Second line
data: Third line

data: {""complete"": ""message""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(2, events.Count);
        
        // Multiple data fields should be combined with newlines
        var expectedFirstData = "First line\r\nSecond line\r\nThird line";
        Assert.Equal(expectedFirstData, events[0].Data);
        Assert.Equal(@"{""complete"": ""message""}", events[1].Data);
    }

    [Fact]
    public async Task ParseStreamAsync_ErrorHandling_PropagatesErrors()
    {
        // Arrange
        _mockHttp.EnqueueErrorResponse(HttpStatusCode.InternalServerError, "Server error");
        var response = await _httpClient.GetAsync("https://api.example.com/stream");

        // Act & Assert
        var events = new List<SseEvent>();
        
        // Should not throw but should handle gracefully
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // With error response, no events should be parsed
        Assert.Empty(events);
    }

    [Fact]
    public async Task ParseStreamAsync_WithEventTypes_ParsesCorrectly()
    {
        // Arrange
        var sseData = @"event: message
data: {""text"": ""Hello""}

event: completion
data: {""done"": true}

data: {""text"": ""Default event""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(3, events.Count);
        
        Assert.Equal("message", events[0].EventType);
        Assert.Equal(@"{""text"": ""Hello""}", events[0].Data);
        
        Assert.Equal("completion", events[1].EventType);
        Assert.Equal(@"{""done"": true}", events[1].Data);
        
        Assert.Equal(string.Empty, events[2].EventType);
        Assert.Equal(@"{""text"": ""Default event""}", events[2].Data);
    }

    [Fact]
    public async Task ParseStreamAsync_WithIdAndRetry_ParsesFields()
    {
        // Arrange
        var sseData = @"id: 123
event: message
data: {""text"": ""Hello""}

id: 456
retry: 5000
data: {""text"": ""World""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(2, events.Count);
        
        Assert.Equal("123", events[0].Id);
        Assert.Equal("message", events[0].EventType);
        Assert.Null(events[0].Retry);
        
        Assert.Equal("456", events[1].Id);
        Assert.Equal(5000, events[1].Retry);
        Assert.Equal(string.Empty, events[1].EventType);
    }

    [Fact]
    public async Task ParseStreamAsync_WithCustomFields_StoresInAdditionalFields()
    {
        // Arrange
        var sseData = @"custom-field: custom-value
another: another-value
data: {""text"": ""Hello""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Single(events);
        
        Assert.Equal(2, events[0].AdditionalFields.Count);
        Assert.Equal("custom-value", events[0].AdditionalFields["custom-field"]);
        Assert.Equal("another-value", events[0].AdditionalFields["another"]);
    }

    [Fact]
    public async Task ParseStreamAsync_EmptyDataLines_AreIgnored()
    {
        // Arrange
        var sseData = @"data: 

data: {""text"": ""Hello""}

data:

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert - Only events with non-empty data should be returned
        Assert.Single(events);
        Assert.Equal(@"{""text"": ""Hello""}", events[0].Data);
    }

    [Fact]
    public async Task ParseJsonDataAsync_ValidJson_DeserializesCorrectly()
    {
        // Arrange
        var sseData = @"data: {""type"": ""text"", ""content"": ""Hello""}

data: {""type"": ""text"", ""content"": ""World""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);
        var sseEvents = SseParser.ParseStreamAsync(response);

        // Act
        var jsonObjects = new List<TestJsonObject?>();
        await foreach (var jsonObj in SseParser.ParseJsonDataAsync<TestJsonObject>(sseEvents))
        {
            jsonObjects.Add(jsonObj);
        }

        // Assert
        Assert.Equal(2, jsonObjects.Count);
        Assert.NotNull(jsonObjects[0]);
        Assert.NotNull(jsonObjects[1]);
        Assert.Equal("text", jsonObjects[0]!.Type);
        Assert.Equal("Hello", jsonObjects[0]!.Content);
        Assert.Equal("text", jsonObjects[1]!.Type);
        Assert.Equal("World", jsonObjects[1]!.Content);
    }

    [Fact]
    public async Task ParseJsonDataAsync_InvalidJson_SkipsInvalidEntries()
    {
        // Arrange
        var sseData = @"data: {""type"": ""text"", ""content"": ""Hello""}

data: invalid json content

data: {""type"": ""text"", ""content"": ""World""}

data: [DONE]

";
        var response = CreateSseResponse(sseData);
        var sseEvents = SseParser.ParseStreamAsync(response);

        // Act
        var jsonObjects = new List<TestJsonObject?>();
        await foreach (var jsonObj in SseParser.ParseJsonDataAsync<TestJsonObject>(sseEvents))
        {
            jsonObjects.Add(jsonObj);
        }

        // Assert
        Assert.Equal(2, jsonObjects.Count);
        Assert.Equal("Hello", jsonObjects[0]!.Content);
        Assert.Equal("World", jsonObjects[1]!.Content);
    }

    [Fact]
    public async Task ParseStreamAsync_WithCarriageReturns_HandlesCorrectly()
    {
        // Arrange - Mix of \n and \r\n line endings
        var sseData = "data: Hello\r\n\r\ndata: World\n\ndata: [DONE]\r\n\r\n";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("Hello", events[0].Data);
        Assert.Equal("World", events[1].Data);
    }

    [Fact]
    public async Task ParseStreamAsync_LargeDataChunks_HandlesEfficiently()
    {
        // Arrange - Create a large SSE stream
        var largeContent = new string('x', 10000);
        var sseData = $@"data: {{""content"": ""{largeContent}""}}

data: [DONE]

";
        var response = CreateSseResponse(sseData);

        // Act
        var events = new List<SseEvent>();
        var startTime = DateTime.UtcNow;
        
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }
        
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Single(events);
        Assert.Contains(largeContent, events[0].Data);
        Assert.True(duration.TotalMilliseconds < TestConstants.MaxSseProcessingTimeMs, 
            $"SSE parsing took too long: {duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task ParseStreamAsync_NullContent_HandlesGracefully()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = null
        };

        // Act
        var events = new List<SseEvent>();
        await foreach (var sseEvent in SseParser.ParseStreamAsync(response))
        {
            events.Add(sseEvent);
        }

        // Assert
        Assert.Empty(events);
    }

    private HttpResponseMessage CreateSseResponse(string sseData)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseData, Encoding.UTF8, "text/event-stream")
        };
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");
        return response;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _mockHttp.Dispose();
    }
}

/// <summary>
/// Test JSON object for deserialization tests
/// </summary>
public class TestJsonObject
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}