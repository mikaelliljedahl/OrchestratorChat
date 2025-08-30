using NSubstitute;
using Microsoft.AspNetCore.Components;
using OrchestratorChat.Core.Sessions;
using OrchestratorChat.Core.Agents;
using OrchestratorChat.Web.Services;

namespace OrchestratorChat.Web.Tests.TestHelpers;

public static class MockServiceFactory
{
    public static ISessionManager CreateMockSessionManager()
    {
        var mock = Substitute.For<ISessionManager>();
        mock.GetCurrentSessionAsync().Returns(Task.FromResult<Session?>(
            TestDataFactory.CreateSession()));
        return mock;
    }
    
    public static IAgentService CreateMockAgentService()
    {
        var mock = Substitute.For<IAgentService>();
        mock.GetConfiguredAgentsAsync().Returns(Task.FromResult(
            new List<AgentInfo> 
            { 
                TestDataFactory.CreateAgent("Claude"),
                TestDataFactory.CreateAgent("Saturn")
            }));
        return mock;
    }
    
    public static NavigationManager CreateMockNavigationManager()
    {
        return new MockNavigationManager("https://localhost/");
    }
}

public class MockNavigationManager : NavigationManager
{
    public MockNavigationManager(string baseUri) : base()
    {
        Initialize(baseUri, baseUri);
    }
}