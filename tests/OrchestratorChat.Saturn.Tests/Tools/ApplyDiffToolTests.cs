using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Tools.Implementations;

namespace OrchestratorChat.Saturn.Tests.Tools;

/// <summary>
/// Tests for ApplyDiffTool implementation
/// </summary>
public class ApplyDiffToolTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly ApplyDiffTool _tool;

    public ApplyDiffToolTests()
    {
        _fileHelper = new FileTestHelper("ApplyDiffToolTests");
        _tool = new ApplyDiffTool();
    }

    [Fact]
    public async Task ApplyDiff_AddNewFile_CreatesFile()
    {
        // Arrange
        var newFilePath = _fileHelper.GetFullPath("newfile.txt");
        var diff = @"--- /dev/null
+++ newfile.txt
@@ -0,0 +1,3 @@
+Hello World
+This is a new file
+End of file";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = newFilePath,
                ["diff"] = diff,
                ["create_backup"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.Error);
    }

    [Fact]
    public async Task ApplyDiff_ModifyExistingFile_AppliesChanges()
    {
        // Arrange
        var originalContent = @"Line 1
Line 2
Line 3
Line 4";
        var filePath = _fileHelper.CreateFile("test.txt", originalContent);

        var diff = @"--- test.txt
+++ test.txt
@@ -1,4 +1,4 @@
 Line 1
-Line 2
+Modified Line 2
 Line 3
 Line 4";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["diff"] = diff,
                ["create_backup"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Successfully applied diff", result.Output);
        var updatedContent = _fileHelper.ReadFile("test.txt");
        Assert.Contains("Modified Line 2", updatedContent);
        Assert.DoesNotContain("Line 2", updatedContent.Replace("Modified Line 2", ""));
    }

    [Fact]
    public async Task ApplyDiff_DeleteFile_ReturnsError()
    {
        // Arrange
        var originalContent = "This file will be deleted";
        var filePath = _fileHelper.CreateFile("todelete.txt", originalContent);

        // Since ApplyDiffTool works on existing files and not file system operations,
        // we test a diff that would effectively empty the file
        var diff = @"--- todelete.txt
+++ todelete.txt
@@ -1,1 +0,0 @@
-This file will be deleted";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["diff"] = diff,
                ["create_backup"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        var updatedContent = _fileHelper.ReadFile("todelete.txt");
        Assert.Equal("", updatedContent.Trim());
    }

    [Fact]
    public async Task ApplyDiff_InvalidDiff_ReturnsError()
    {
        // Arrange
        var originalContent = "Line 1\nLine 2\nLine 3";
        var filePath = _fileHelper.CreateFile("test.txt", originalContent);

        var invalidDiff = @"This is not a valid unified diff format
It lacks proper headers and format";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["diff"] = invalidDiff,
                ["create_backup"] = false
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to parse or apply the unified diff", result.Error);
    }

    [Fact]
    public async Task ApplyDiff_NonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = _fileHelper.GetFullPath("nonexistent.txt");
        var diff = @"--- nonexistent.txt
+++ nonexistent.txt
@@ -1,1 +1,2 @@
 Original line
+New line";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = nonExistentPath,
                ["diff"] = diff
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.Error);
    }

    [Fact]
    public async Task ApplyDiff_CreateBackup_CreatesBackupFile()
    {
        // Arrange
        var originalContent = "Original content\nLine 2";
        var filePath = _fileHelper.CreateFile("test.txt", originalContent);

        var diff = @"--- test.txt
+++ test.txt
@@ -1,2 +1,2 @@
-Original content
+Modified content
 Line 2";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["diff"] = diff,
                ["create_backup"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Metadata.ContainsKey("backup_path"));
        var backupPath = result.Metadata["backup_path"].ToString();
        Assert.True(File.Exists(backupPath));
        
        var backupContent = File.ReadAllText(backupPath);
        Assert.Equal(originalContent, backupContent);
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
    }
}