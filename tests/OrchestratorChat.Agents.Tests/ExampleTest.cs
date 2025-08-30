using OrchestratorChat.Agents.Tests.TestHelpers;

namespace OrchestratorChat.Agents.Tests;

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
        _fileHelper = new FileTestHelper("ExampleTest");
        _mockHttp = new MockHttpMessageHandler();
    }

    [Fact]
    public void FileTestHelper_CreateAndReadFile_WorksCorrectly()
    {
        // Arrange
        const string fileName = "test.txt";
        const string content = "Hello World";

        // Act
        var filePath = _fileHelper.CreateFile(fileName, content);
        var readContent = _fileHelper.ReadFile(fileName);

        // Assert
        Assert.True(_fileHelper.FileExists(fileName));
        Assert.Equal(content, readContent);
        Assert.Contains("test.txt", filePath);
    }


    public void Dispose()
    {
        _fileHelper.Dispose();
        _mockHttp.Dispose();
    }
}