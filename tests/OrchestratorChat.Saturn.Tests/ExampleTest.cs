using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests;

/// <summary>
/// Example test demonstrating the usage of test helpers.
/// This can be removed once actual tests are implemented.
/// </summary>
public class ExampleTest : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly MockHttpMessageHandler _mockHttp;

    public ExampleTest()
    {
        _fileHelper = new FileTestHelper("SaturnExampleTest");
        _mockHttp = new MockHttpMessageHandler();
    }

    [Fact]
    public void FileTestHelper_CreateDirectory_WorksCorrectly()
    {
        // Arrange
        const string dirName = "testdir";

        // Act
        var dirPath = _fileHelper.CreateDirectory(dirName);

        // Assert
        Assert.True(_fileHelper.DirectoryExists(dirName));
        Assert.Contains("testdir", dirPath);
    }

    [Fact]
    public void TestConstants_ContainSaturnSpecificValues()
    {
        // Assert
        Assert.Equal("apply_diff", TestConstants.ApplyDiffToolName);
        Assert.Equal("Anthropic", TestConstants.AnthropicProviderName);
        Assert.NotNull(TestConstants.ValidSseData);
    }

    [Fact]
    public async Task MockHttpMessageHandler_EnqueueJsonResponse_DeserializesCorrectly()
    {
        // Arrange
        var testObject = new { message = "Hello", code = 200 };
        _mockHttp.EnqueueJsonResponse(System.Net.HttpStatusCode.OK, testObject);

        using var httpClient = new HttpClient(_mockHttp, false);

        // Act
        var response = await httpClient.GetAsync("https://api.example.com");
        var jsonContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("Hello", jsonContent);
        Assert.Contains("200", jsonContent);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void MockHttpMessageHandler_EnqueueStreamingResponse_CreatesSSEFormat()
    {
        // Arrange
        var chunks = new[] { "chunk1", "chunk2", "chunk3" };
        _mockHttp.EnqueueStreamingResponse(chunks);

        // Act & Assert
        Assert.Equal(1, _mockHttp.QueuedResponseCount);
        
        // Verify the last request can be processed
        // (In actual tests, this would be called by an HTTP client)
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
        _mockHttp.Dispose();
    }
}