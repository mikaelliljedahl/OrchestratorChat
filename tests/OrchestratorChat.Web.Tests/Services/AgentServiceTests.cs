using NSubstitute;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Agents;
using OrchestratorChat.Web.Models;
using OrchestratorChat.Web.Services;
using OrchestratorChat.Web.Tests.TestHelpers;
using Xunit;

namespace OrchestratorChat.Web.Tests.Services;

public class AgentServiceTests
{
    private readonly IAgentFactory _mockAgentFactory;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _mockAgentFactory = Substitute.For<IAgentFactory>();
        _service = new AgentService(_mockAgentFactory);
    }

    [Fact]
    public async Task GetConfiguredAgentsAsync_Should_Return_Empty_List_When_No_Agents()
    {
        // Act
        var result = await _service.GetConfiguredAgentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfiguredAgentsAsync_Should_Return_Agent_List_When_Agents_Exist()
    {
        // Arrange
        var mockAgent1 = CreateMockAgent("agent-1", "Claude Agent", AgentStatus.Ready);
        var mockAgent2 = CreateMockAgent("agent-2", "Saturn Agent", AgentStatus.Busy);

        var agentConfig1 = new AgentConfiguration();
        var agentConfig2 = new AgentConfiguration();

        await _service.CreateAgentAsync(AgentType.Claude, agentConfig1);
        await _service.CreateAgentAsync(AgentType.Saturn, agentConfig2);

        // Act
        var result = await _service.GetConfiguredAgentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Name.Contains("Claude"));
        Assert.Contains(result, a => a.Name.Contains("Saturn"));
    }

    [Fact]
    public async Task GetAgentAsync_Should_Return_Agent_When_Exists()
    {
        // Arrange
        var mockAgent = CreateMockAgent("test-agent-1", "Test Agent", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        var createdAgent = await _service.CreateAgentAsync(AgentType.Claude, config);

        // Act
        var result = await _service.GetAgentAsync(createdAgent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdAgent.Id, result.Id);
        Assert.Equal("Test Agent", result.Name);
        Assert.Equal(AgentType.Claude, result.Type);
        Assert.Equal(AgentStatus.Ready, result.Status);
    }

    [Fact]
    public async Task GetAgentAsync_Should_Return_Null_When_Agent_Not_Found()
    {
        // Act
        var result = await _service.GetAgentAsync("nonexistent-agent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAgentAsync_Should_Create_Agent_Successfully()
    {
        // Arrange
        var mockAgent = CreateMockAgent("new-agent", "New Test Agent", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();

        // Act
        var result = await _service.CreateAgentAsync(AgentType.Claude, config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-agent", result.Id);
        Assert.Equal("New Test Agent", result.Name);
        Assert.Equal(AgentType.Claude, result.Type);
        Assert.Equal(AgentStatus.Ready, result.Status);

        // Verify factory was called
        await _mockAgentFactory.Received(1).CreateAgentAsync(AgentType.Claude, config);
    }

    [Fact]
    public async Task CreateAgentAsync_Should_Add_Agent_To_Internal_Collection()
    {
        // Arrange
        var mockAgent = CreateMockAgent("collection-test", "Collection Test Agent", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Saturn, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();

        // Act
        var createdAgent = await _service.CreateAgentAsync(AgentType.Saturn, config);
        var retrievedAgent = await _service.GetAgentAsync(createdAgent.Id);

        // Assert
        Assert.NotNull(retrievedAgent);
        Assert.Equal(createdAgent.Id, retrievedAgent.Id);
    }

    [Fact]
    public async Task UpdateAgentAsync_Should_Update_Agent_Properties()
    {
        // Arrange
        var mockAgent = CreateMockAgent("update-test", "Original Name", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        var createdAgent = await _service.CreateAgentAsync(AgentType.Claude, config);

        var updatedAgentInfo = new AgentInfo
        {
            Id = createdAgent.Id,
            Name = "Updated Name",
            WorkingDirectory = "/updated/path"
        };

        // Act
        await _service.UpdateAgentAsync(updatedAgentInfo);

        // Assert
        mockAgent.Received(1).Name = "Updated Name";
        mockAgent.Received(1).WorkingDirectory = "/updated/path";
    }

    [Fact]
    public async Task UpdateAgentAsync_Should_Handle_Nonexistent_Agent_Gracefully()
    {
        // Arrange
        var nonexistentAgent = new AgentInfo
        {
            Id = "nonexistent",
            Name = "Nonexistent Agent",
            WorkingDirectory = "/test"
        };

        // Act & Assert - Should not throw
        await _service.UpdateAgentAsync(nonexistentAgent);
    }

    [Fact]
    public async Task DeleteAgentAsync_Should_Remove_Agent_From_Collection()
    {
        // Arrange
        var mockAgent = CreateMockAgent("delete-test", "Delete Test Agent", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        var createdAgent = await _service.CreateAgentAsync(AgentType.Claude, config);

        // Act
        await _service.DeleteAgentAsync(createdAgent.Id);

        // Assert
        var retrievedAgent = await _service.GetAgentAsync(createdAgent.Id);
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task DeleteAgentAsync_Should_Handle_Nonexistent_Agent_Gracefully()
    {
        // Act & Assert - Should not throw
        await _service.DeleteAgentAsync("nonexistent-agent");
    }

    [Fact]
    public async Task IsAgentAvailableAsync_Should_Return_True_When_Agent_Ready()
    {
        // Arrange
        var mockAgent = CreateMockAgent("available-test", "Available Agent", AgentStatus.Ready);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        var createdAgent = await _service.CreateAgentAsync(AgentType.Claude, config);

        // Act
        var result = await _service.IsAgentAvailableAsync(createdAgent.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAgentAvailableAsync_Should_Return_False_When_Agent_Not_Ready()
    {
        // Arrange
        var mockAgent = CreateMockAgent("busy-test", "Busy Agent", AgentStatus.Busy);
        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        var createdAgent = await _service.CreateAgentAsync(AgentType.Claude, config);

        // Act
        var result = await _service.IsAgentAvailableAsync(createdAgent.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsAgentAvailableAsync_Should_Return_False_When_Agent_Not_Found()
    {
        // Act
        var result = await _service.IsAgentAvailableAsync("nonexistent-agent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateAgentAsync_Should_Handle_Factory_Errors()
    {
        // Arrange
        _mockAgentFactory.CreateAgentAsync(Arg.Any<AgentType>(), Arg.Any<AgentConfiguration>())
            .Returns(Task.FromException<IAgent>(new InvalidOperationException("Factory error")));

        var config = new AgentConfiguration();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAgentAsync(AgentType.Claude, config));
        
        Assert.Equal("Factory error", exception.Message);
    }

    [Fact]
    public async Task GetConfiguredAgentsAsync_Should_Include_All_Agent_Properties()
    {
        // Arrange
        var mockAgent = CreateMockAgent("properties-test", "Properties Agent", AgentStatus.Ready);
        mockAgent.Capabilities.Returns(new AgentCapabilities 
        { 
            CanExecuteCode = true,
            CanReadFiles = true 
        });
        mockAgent.WorkingDirectory.Returns("/test/working/dir");

        _mockAgentFactory.CreateAgentAsync(AgentType.Claude, Arg.Any<AgentConfiguration>())
            .Returns(Task.FromResult(mockAgent));

        var config = new AgentConfiguration();
        await _service.CreateAgentAsync(AgentType.Claude, config);

        // Act
        var result = await _service.GetConfiguredAgentsAsync();

        // Assert
        var agent = result.First();
        Assert.Equal("properties-test", agent.Id);
        Assert.Equal("Properties Agent", agent.Name);
        Assert.Contains("Claude", agent.Description);
        Assert.Equal(AgentStatus.Ready, agent.Status);
        Assert.NotNull(agent.Capabilities);
        Assert.True(agent.Capabilities.CanExecuteCode);
        Assert.True(agent.Capabilities.CanReadFiles);
        Assert.Equal("/test/working/dir", agent.WorkingDirectory);
        Assert.True(agent.LastActive <= DateTime.UtcNow);
    }

    private static IAgent CreateMockAgent(string id, string name, AgentStatus status)
    {
        var mockAgent = Substitute.For<IAgent>();
        mockAgent.Id.Returns(id);
        mockAgent.Name.Returns(name);
        mockAgent.Status.Returns(status);
        mockAgent.Capabilities.Returns(new AgentCapabilities());
        mockAgent.WorkingDirectory.Returns("/test/directory");
        return mockAgent;
    }
}