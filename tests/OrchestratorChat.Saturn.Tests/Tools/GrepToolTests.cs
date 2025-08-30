using OrchestratorChat.Saturn.Models;
using OrchestratorChat.Saturn.Tests.TestHelpers;
using OrchestratorChat.Saturn.Tools.Implementations;

namespace OrchestratorChat.Saturn.Tests.Tools;

/// <summary>
/// Tests for GrepTool implementation
/// </summary>
public class GrepToolTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly GrepTool _tool;

    public GrepToolTests()
    {
        _fileHelper = new FileTestHelper("GrepToolTests");
        _tool = new GrepTool();
    }

    [Fact]
    public async Task ExecuteAsync_SimplePattern_FindsMatches()
    {
        // Arrange
        var testText = @"Hello World
This is a test
Hello Universe
Another line
Hello Again";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "Hello",
                ["text"] = testText,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello World", result.Output);
        Assert.Contains("Hello Universe", result.Output);
        Assert.Contains("Hello Again", result.Output);
        Assert.DoesNotContain("This is a test", result.Output);
        Assert.DoesNotContain("Another line", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(3, matchCount);
    }

    [Fact]
    public async Task ExecuteAsync_RegexPattern_FindsComplexMatches()
    {
        // Arrange
        var testText = @"var name = 'John';
let age = 25;
const city = 'New York';
var country = 'USA';
let state = 'NY';";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = @"(var|let)\s+\w+\s*=",
                ["text"] = testText,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("var name =", result.Output);
        Assert.Contains("let age =", result.Output);
        Assert.Contains("var country =", result.Output);
        Assert.Contains("let state =", result.Output);
        Assert.DoesNotContain("const city =", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(4, matchCount);
    }

    [Fact]
    public async Task ExecuteAsync_CaseInsensitive_FindsMatches()
    {
        // Arrange
        var testText = @"ERROR: Something went wrong
Info: This is fine
error: Another problem
Warning: Be careful
ERROR: Critical issue";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "error",
                ["text"] = testText,
                ["ignore_case"] = true,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ERROR: Something went wrong", result.Output);
        Assert.Contains("error: Another problem", result.Output);
        Assert.Contains("ERROR: Critical issue", result.Output);
        Assert.DoesNotContain("Info: This is fine", result.Output);
        Assert.DoesNotContain("Warning: Be careful", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(3, matchCount);
        Assert.True((bool)result.Metadata["ignore_case"]);
    }

    [Fact]
    public async Task ExecuteAsync_RecursiveSearch_SearchesSubdirectories()
    {
        // Arrange
        _fileHelper.CreateFile("root.txt", "Root file with pattern TEST");
        _fileHelper.CreateFile("subdir/sub1.txt", "Subdirectory file with TEST pattern");
        _fileHelper.CreateFile("subdir/deep/sub2.txt", "Deep file with TEST content");
        _fileHelper.CreateFile("other/other.txt", "No matching content here");

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "TEST",
                ["file_path"] = _fileHelper.TestDirectory,
                ["recursive"] = true,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Root file with pattern TEST", result.Output);
        Assert.Contains("Subdirectory file with TEST pattern", result.Output);
        Assert.Contains("Deep file with TEST content", result.Output);
        Assert.DoesNotContain("No matching content", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(3, matchCount);
        Assert.True((bool)result.Metadata["recursive"]);
        Assert.Equal(3, (int)result.Metadata["processed_files"]);
    }

    [Fact]
    public async Task ExecuteAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var testText = @"This is a simple text
With multiple lines
But no matches for the pattern";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "NONEXISTENT",
                ["text"] = testText,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("No matches found", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(0, matchCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithContext_IncludesContextLines()
    {
        // Arrange
        var testText = @"Line 1
Line 2
MATCH HERE
Line 4
Line 5";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "MATCH HERE",
                ["text"] = testText,
                ["line_numbers"] = true,
                ["context_before"] = 1,
                ["context_after"] = 1
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Line 2", result.Output);
        Assert.Contains("MATCH HERE", result.Output);
        Assert.Contains("Line 4", result.Output);
        Assert.DoesNotContain("Line 1", result.Output);
        Assert.DoesNotContain("Line 5", result.Output);
        
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(1, matchCount);
        Assert.Equal(1, (int)result.Metadata["context_before"]);
        Assert.Equal(1, (int)result.Metadata["context_after"]);
    }

    [Fact]
    public async Task ExecuteAsync_MaxMatches_LimitsResults()
    {
        // Arrange
        var testText = @"match 1
match 2
match 3
match 4
match 5";

        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "match",
                ["text"] = testText,
                ["max_matches"] = 3,
                ["line_numbers"] = true
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.True(result.Success);
        var matchCount = (int)result.Metadata["match_count"];
        Assert.Equal(3, matchCount);
        Assert.Contains("match 1", result.Output);
        Assert.Contains("match 2", result.Output);
        Assert.Contains("match 3", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidParameters_ReturnsError()
    {
        // Arrange - missing pattern
        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["text"] = "some text"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("pattern parameter is required", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_MissingTextAndFile_ReturnsError()
    {
        // Arrange - missing both text and file_path
        var call = new ToolCall
        {
            Parameters = new Dictionary<string, object>
            {
                ["pattern"] = "test"
            }
        };

        // Act
        var result = await _tool.ExecuteAsync(call);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Either text or file_path parameter is required", result.Error);
    }

    public void Dispose()
    {
        _fileHelper.Dispose();
    }
}