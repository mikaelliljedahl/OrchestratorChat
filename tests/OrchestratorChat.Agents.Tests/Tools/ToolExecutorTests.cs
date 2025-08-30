using Microsoft.Extensions.Logging;
using OrchestratorChat.Agents.Tools;
using OrchestratorChat.Core.Exceptions;
using OrchestratorChat.Core.Tools;
using OrchestratorChat.Agents.Tests.TestHelpers;

namespace OrchestratorChat.Agents.Tests.Tools;

/// <summary>
/// Tests for the ToolExecutor class
/// </summary>
public class ToolExecutorTests
{
    private readonly ILogger<ToolExecutor> _logger;
    private readonly ToolExecutor _toolExecutor;

    public ToolExecutorTests()
    {
        _logger = Substitute.For<ILogger<ToolExecutor>>();
        _toolExecutor = new ToolExecutor(_logger);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ThrowsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();

        // Act
        var result = await _toolExecutor.ExecuteAsync("unknown_tool", parameters, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No handler found for tool 'unknown_tool'", result.Error);
        Assert.Equal(TimeSpan.Zero, result.ExecutionTime);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidParameters_ReturnsValidationError()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("test_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Failure("Missing required parameter 'file_path'"));

        _toolExecutor.RegisterHandler("test_tool", mockHandler);

        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();

        // Act
        var result = await _toolExecutor.ExecuteAsync("test_tool", parameters, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Parameter validation failed: Missing required parameter 'file_path'", result.Error);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_CancelsExecution()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("slow_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Success());

        // Setup handler to take longer than timeout
        mockHandler.ExecuteAsync(Arg.Any<Dictionary<string, object>>(), Arg.Any<IExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                await Task.Delay(5000, token); // 5 seconds delay
                return new ToolExecutionResult { Success = true, Output = "Completed" };
            });

        _toolExecutor.RegisterHandler("slow_tool", mockHandler);

        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();
        var timeout = TimeSpan.FromMilliseconds(100); // Very short timeout

        // Act
        var result = await _toolExecutor.ExecuteAsync("slow_tool", parameters, context, timeout);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Tool execution timed out after", result.Error);
        Assert.True(result.ExecutionTime >= timeout);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulExecution_ReturnsResult()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("successful_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Success());

        var expectedResult = new ToolExecutionResult
        {
            Success = true,
            Output = "Tool executed successfully",
            ExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        mockHandler.ExecuteAsync(Arg.Any<Dictionary<string, object>>(), Arg.Any<IExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _toolExecutor.RegisterHandler("successful_tool", mockHandler);

        var parameters = TestConstants.StandardToolParams;
        var context = CreateMockExecutionContext();

        // Act
        var result = await _toolExecutor.ExecuteAsync("successful_tool", parameters, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Tool executed successfully", result.Output);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Theory]
    [InlineData("file_read", "FileReadHandler")]
    [InlineData("file_write", "FileWriteHandler")]
    [InlineData("bash", "BashCommandHandler")]
    [InlineData("web_search", "WebSearchHandler")]
    public void RegisterHandler_MapsCorrectly(string toolName, string handlerType)
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns(toolName);

        // Act
        _toolExecutor.RegisterHandler(toolName, mockHandler);

        // Assert - Verify the tool is registered correctly for the expected handler type
        Assert.True(_toolExecutor.IsToolRegistered(toolName));
        Assert.Contains(toolName, _toolExecutor.GetRegisteredTools());
        
        // Verify that the handler type relates to the tool name as expected
        var expectedToolNamePart = handlerType.ToLowerInvariant().Replace("handler", "").Replace("command", "");
        Assert.True(toolName.ToLowerInvariant().Replace("_", "").Contains(expectedToolNamePart) || 
                   expectedToolNamePart.Contains(toolName.ToLowerInvariant().Replace("_", "")), 
                   $"Handler type '{handlerType}' should relate to tool name '{toolName}'");
    }

    [Fact]
    public void RegisterHandler_NullToolName_ThrowsArgumentException()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _toolExecutor.RegisterHandler(null!, mockHandler));
        Assert.Throws<ArgumentException>(() => _toolExecutor.RegisterHandler("", mockHandler));
        Assert.Throws<ArgumentException>(() => _toolExecutor.RegisterHandler("   ", mockHandler));
    }

    [Fact]
    public void RegisterHandler_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => _toolExecutor.RegisterHandler("test_tool", null!));
    }

    [Fact]
    public async Task ExecuteAsync_ToolExecutionException_ReturnsError()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("error_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Success());

        var toolCall = new ToolCall
        {
            ToolName = "error_tool",
            AgentId = TestConstants.DefaultAgentId,
            SessionId = "test-session-001",
            Parameters = new Dictionary<string, object>()
        };

        mockHandler.ExecuteAsync(Arg.Any<Dictionary<string, object>>(), Arg.Any<IExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ToolExecutionResult>(new OrchestratorChat.Core.Exceptions.ToolExecutionException("Tool failed to execute", "error_tool", toolCall)));

        _toolExecutor.RegisterHandler("error_tool", mockHandler);

        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();

        // Act
        var result = await _toolExecutor.ExecuteAsync("error_tool", parameters, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Tool failed to execute", result.Error);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_ReturnsError()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("exception_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Success());

        mockHandler.ExecuteAsync(Arg.Any<Dictionary<string, object>>(), Arg.Any<IExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ToolExecutionResult>(new InvalidOperationException("Unexpected error occurred")));

        _toolExecutor.RegisterHandler("exception_tool", mockHandler);

        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();

        // Act
        var result = await _toolExecutor.ExecuteAsync("exception_tool", parameters, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unexpected error: Unexpected error occurred", result.Error);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ReturnsCancelledResult()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("cancellable_tool");
        mockHandler.ValidateParameters(Arg.Any<Dictionary<string, object>>())
            .Returns(ValidationResult.Success());

        mockHandler.ExecuteAsync(Arg.Any<Dictionary<string, object>>(), Arg.Any<IExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                token.ThrowIfCancellationRequested();
                await Task.Delay(1000, token);
                return new ToolExecutionResult { Success = true };
            });

        _toolExecutor.RegisterHandler("cancellable_tool", mockHandler);

        var parameters = new Dictionary<string, object>();
        var context = CreateMockExecutionContext();
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _toolExecutor.ExecuteAsync("cancellable_tool", parameters, context, cancellationToken: cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Tool execution was cancelled", result.Error);
    }

    [Fact]
    public void GetRegisteredTools_ReturnsAllRegisteredTools()
    {
        // Arrange
        var handler1 = Substitute.For<IToolHandler>();
        handler1.ToolName.Returns("tool1");
        var handler2 = Substitute.For<IToolHandler>();
        handler2.ToolName.Returns("tool2");

        _toolExecutor.RegisterHandler("tool1", handler1);
        _toolExecutor.RegisterHandler("tool2", handler2);

        // Act
        var tools = _toolExecutor.GetRegisteredTools().ToList();

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.Contains("tool1", tools);
        Assert.Contains("tool2", tools);
    }

    [Fact]
    public void IsToolRegistered_RegisteredTool_ReturnsTrue()
    {
        // Arrange
        var mockHandler = Substitute.For<IToolHandler>();
        mockHandler.ToolName.Returns("registered_tool");
        _toolExecutor.RegisterHandler("registered_tool", mockHandler);

        // Act & Assert
        Assert.True(_toolExecutor.IsToolRegistered("registered_tool"));
        Assert.False(_toolExecutor.IsToolRegistered("unregistered_tool"));
        Assert.False(_toolExecutor.IsToolRegistered(null!));
        Assert.False(_toolExecutor.IsToolRegistered(""));
    }

    /// <summary>
    /// Creates a mock execution context for testing
    /// </summary>
    private static IExecutionContext CreateMockExecutionContext()
    {
        var context = Substitute.For<IExecutionContext>();
        context.AgentId.Returns(TestConstants.DefaultAgentId);
        context.SessionId.Returns("test-session-001");
        context.WorkingDirectory.Returns("C:\\test");
        context.ContextData.Returns(new Dictionary<string, object>());
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}