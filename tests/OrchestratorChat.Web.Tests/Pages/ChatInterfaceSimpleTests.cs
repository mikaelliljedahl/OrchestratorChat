using Bunit;
using Xunit;

namespace OrchestratorChat.Web.Tests.Pages;

/// <summary>
/// Minimal test to isolate the hanging issue in ChatInterface tests
/// </summary>
public class ChatInterfaceSimpleTests : TestContext
{
    [Fact]
    public void Simple_Test_Should_Pass()
    {
        // This is the simplest possible test to verify the test framework works
        Assert.True(true);
    }
    
    [Fact] 
    public void TestContext_Should_Be_Initialized()
    {
        // Test that the base TestContext is working
        Assert.NotNull(Services);
    }
}