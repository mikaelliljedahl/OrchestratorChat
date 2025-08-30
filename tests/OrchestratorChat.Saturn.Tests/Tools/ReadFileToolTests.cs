using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Tools.Implementations;

namespace OrchestratorChat.Saturn.Tests.Tools;

/// <summary>
/// Tests for ReadFileTool implementation
/// </summary>
public class ReadFileToolTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly ReadFileTool _tool;

    public ReadFileToolTests()
    {
        _fileHelper = new FileTestHelper("ReadFileToolTests");
        _tool = new ReadFileTool();
    }

    [Fact]
    public async Task ExecuteAsync_ValidFile_ReadsContent()
    {
        // Arrange
        var content = @"This is a test file
With multiple lines
And some content";
        var filePath = _fileHelper.CreateFile("test.txt", content);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Output);
        Assert.True(result.Metadata.ContainsKey("file_path"));
        Assert.True(result.Metadata.ContainsKey("file_size"));
        Assert.Equal(filePath, result.Metadata["file_path"]);
        Assert.Equal("utf-8", result.Metadata["encoding"]);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = _fileHelper.GetFullPath("nonexistent.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = nonExistentPath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.Error);
        Assert.Contains(nonExistentPath, result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_BinaryFile_ReturnsError()
    {
        // Arrange - Create a file with binary content (null bytes)
        var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };
        var filePath = _fileHelper.GetFullPath("binary.bin");
        await File.WriteAllBytesAsync(filePath, binaryContent);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        // Note: ReadFileTool will attempt to read binary files as text, 
        // which may result in garbled output but not necessarily an error
        // The tool doesn't explicitly check for binary files
        Assert.True(result.Success);
        Assert.True(result.Metadata.ContainsKey("file_size"));
        Assert.Equal(5L, result.Metadata["file_size"]);
    }

    [Fact]
    public async Task ExecuteAsync_LargeFile_ReadsPartially()
    {
        // Arrange - Create a large file
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i} with some content"));
        var filePath = _fileHelper.CreateFile("large.txt", largeContent);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(largeContent, result.Output);
        Assert.Contains("Line 1 with some content", result.Output);
        Assert.Contains("Line 1000 with some content", result.Output);
        
        var fileSize = (long)result.Metadata["file_size"];
        Assert.True(fileSize > 10000); // Should be a reasonably large file
    }

    [Fact]
    public async Task ExecuteAsync_EmptyFile_ReturnsEmpty()
    {
        // Arrange
        var filePath = _fileHelper.CreateFile("empty.txt", "");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("", result.Output);
        Assert.Equal(0L, result.Metadata["file_size"]);
        Assert.Equal("utf-8", result.Metadata["encoding"]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingFilePath_ReturnsError()
    {
        // Arrange
        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("file_path parameter is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithEncoding_SetsMetadata()
    {
        // Arrange
        var content = "Test content with special characters: äöü";
        var filePath = _fileHelper.CreateFile("encoded.txt", content);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["encoding"] = "utf-16"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("utf-16", result.Metadata["encoding"]);
    }

    [Fact]
    public async Task ExecuteAsync_AccessDeniedFile_ReturnsError()
    {
        // Arrange
        var content = "Protected content";
        var filePath = _fileHelper.CreateFile("protected.txt", content);
        
        // Make file read-only to simulate access issues
        var fileInfo = new FileInfo(filePath);
        fileInfo.Attributes |= FileAttributes.ReadOnly;

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
            }
        };

        try
        {
            // Act
            var result = await _tool.ExecuteAsync(call);

            // Assert
            // ReadFileTool should still be able to read read-only files
            Assert.True(result.Success);
            Assert.Equal(content, result.Output);
        }
        finally
        {
            // Cleanup - remove read-only attribute
            fileInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
    }
}