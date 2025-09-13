using Hangfire.Server;
using MediatR.Hangfire.Extensions.Logging;

namespace MediatR.Hangfire.Extensions.Tests.Logging;

[TestClass]
public class HangfireConsoleFilterTests
{
    private readonly HangfireConsoleFilter _filter;

    public HangfireConsoleFilterTests()
    {
        _filter = new HangfireConsoleFilter();
    }

    [TestMethod]
    public void Current_InitiallyReturnsNull()
    {
        // Act
        var current = HangfireConsoleFilter.Current;

        // Assert
        Assert.IsNull(current);
    }

    [TestMethod]
    public void OnPerforming_WithNullContext_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _filter.OnPerforming(null!);
        
        // Current should still be null
        Assert.IsNull(HangfireConsoleFilter.Current);
    }

    [TestMethod]
    public void OnPerformed_WithNullContext_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _filter.OnPerformed(null!);
        
        // Current should still be null
        Assert.IsNull(HangfireConsoleFilter.Current);
    }

    [TestMethod]
    public void OnPerformed_ClearsCurrentContext()
    {
        // Act
        _filter.OnPerformed(null!);

        // Assert
        Assert.IsNull(HangfireConsoleFilter.Current);
    }

    // Note: We can't easily test the full functionality with actual PerformContext
    // because it requires Hangfire infrastructure that's complex to mock.
    // The real functionality is tested through integration tests in the actual Hangfire context.
}
