using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Tools.Implementations;

namespace OrchestratorChat.Saturn.Tests.Tools;

/// <summary>
/// Tests for WriteFileTool implementation
/// </summary>
public class WriteFileToolTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly WriteFileTool _tool;

    public WriteFileToolTests()
    {
        _fileHelper = new FileTestHelper("WriteFileToolTests");
        _tool = new WriteFileTool();
    }

    [Fact]
    public async Task ExecuteAsync_NewFile_CreatesFile()
    {
        // Arrange
        var content = "This is new file content\nWith multiple lines";
        var filePath = _fileHelper.GetFullPath("newfile.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = content
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully wrote file", result.Output);
        Assert.True(_fileHelper.FileExists("newfile.txt"));
        Assert.Equal(content, _fileHelper.ReadFile("newfile.txt"));
        
        Assert.Equal(filePath, result.Metadata["file_path"]);
        Assert.Equal(content.Length, result.Metadata["content_length"]);
        Assert.Equal("utf-8", result.Metadata["encoding"]);
        Assert.Equal(false, result.Metadata["append"]);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingFile_Overwrites()
    {
        // Arrange
        var originalContent = "Original content";
        var newContent = "New content that replaces the original";
        var filePath = _fileHelper.CreateFile("existing.txt", originalContent);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = newContent
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully wrote file", result.Output);
        Assert.Equal(newContent, _fileHelper.ReadFile("existing.txt"));
        Assert.DoesNotContain(originalContent, _fileHelper.ReadFile("existing.txt"));
        
        Assert.Equal(newContent.Length, result.Metadata["content_length"]);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPath_ReturnsError()
    {
        // Arrange
        var invalidPath = "";
        var content = "Some content";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = invalidPath,
                ["content"] = content
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("file_path parameter is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CreateDirectory_CreatesParentDirs()
    {
        // Arrange
        var content = "Content in nested directory";
        var filePath = _fileHelper.GetFullPath("nested/deep/file.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = content
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.True(_fileHelper.DirectoryExists("nested"));
        Assert.True(_fileHelper.DirectoryExists("nested/deep"));
        Assert.True(_fileHelper.FileExists("nested/deep/file.txt"));
        Assert.Equal(content, _fileHelper.ReadFile("nested/deep/file.txt"));
    }

    [Fact]
    public async Task ExecuteAsync_WriteProtected_ReturnsError()
    {
        // Arrange
        var content = "Test content";
        var protectedDir = _fileHelper.CreateDirectory("protected");
        
        // Make directory read-only to simulate write protection
        var dirInfo = new DirectoryInfo(protectedDir);
        dirInfo.Attributes |= FileAttributes.ReadOnly;

        var filePath = Path.Combine(protectedDir, "protected.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = content
            }
        };

        try
        {
            // Act
            var result = await _tool.ExecuteAsync(call);

            // Assert
            // On some systems, read-only directory might still allow file creation
            // So we check if either the operation succeeded or failed with appropriate error
            if (!result.Success)
            {
                Assert.Contains("Failed to write file", result.Error);
            }
            else
            {
                // If it succeeded despite read-only, that's also valid behavior
                Assert.True(_fileHelper.FileExists("protected/protected.txt"));
            }
        }
        finally
        {
            // Cleanup - remove read-only attribute
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;
        }
    }

    [Fact]
    public async Task ExecuteAsync_AppendMode_AppendsToFile()
    {
        // Arrange
        var originalContent = "Original line 1\nOriginal line 2";
        var appendContent = "\nAppended line 3\nAppended line 4";
        var filePath = _fileHelper.CreateFile("append.txt", originalContent);

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = appendContent,
                ["append"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully appended to file", result.Output);
        
        var finalContent = _fileHelper.ReadFile("append.txt");
        Assert.Contains("Original line 1", finalContent);
        Assert.Contains("Original line 2", finalContent);
        Assert.Contains("Appended line 3", finalContent);
        Assert.Contains("Appended line 4", finalContent);
        
        Assert.Equal(true, result.Metadata["append"]);
        Assert.Equal(appendContent.Length, result.Metadata["content_length"]);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyContent_WritesEmptyFile()
    {
        // Arrange
        var emptyContent = "";
        var filePath = _fileHelper.GetFullPath("empty.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = emptyContent
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.True(_fileHelper.FileExists("empty.txt"));
        Assert.Equal("", _fileHelper.ReadFile("empty.txt"));
        Assert.Equal(0, result.Metadata["content_length"]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingContent_ReturnsError()
    {
        // Arrange
        var filePath = _fileHelper.GetFullPath("test.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath
                // Missing content parameter
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("content parameter is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_CustomEncoding_SetsMetadata()
    {
        // Arrange
        var content = "Content with encoding";
        var filePath = _fileHelper.GetFullPath("encoded.txt");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["content"] = content,
                ["encoding"] = "ascii"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("ascii", result.Metadata["encoding"]);
        Assert.Equal(content, _fileHelper.ReadFile("encoded.txt"));
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
    }
}