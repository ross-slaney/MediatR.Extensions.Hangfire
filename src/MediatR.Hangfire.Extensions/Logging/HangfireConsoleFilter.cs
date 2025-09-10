using Hangfire.Server;
using Hangfire.Console;
using Hangfire.Common;

namespace MediatR.Hangfire.Extensions.Logging;

/// <summary>
/// Hangfire server filter that provides access to the current job's console context.
/// This filter captures the PerformContext for each job execution, making it available
/// to the HangfireConsoleLogger for writing to the job's console output.
/// </summary>
public class HangfireConsoleFilter : IServerFilter
{
    private static readonly AsyncLocal<PerformContext?> _currentContext = new AsyncLocal<PerformContext?>();

    /// <summary>
    /// Called by Hangfire before a job is performed.
    /// Captures the PerformContext to make it available for console logging.
    /// </summary>
    /// <param name="filterContext">The context for the job being performed</param>
    public void OnPerforming(PerformingContext filterContext)
    {
        // Safely cast to PerformContext (will be null if casting fails)
        _currentContext.Value = filterContext as PerformContext;
    }

    /// <summary>
    /// Called by Hangfire after a job is performed.
    /// Clears the context to prevent it from bleeding into subsequent jobs.
    /// </summary>
    /// <param name="filterContext">The context for the job that was performed</param>
    public void OnPerformed(PerformedContext filterContext)
    {
        // Clear the context so it doesn't affect the next job
        _currentContext.Value = null;
    }

    /// <summary>
    /// Gets the current job's console context.
    /// This is used by HangfireConsoleLogger to write to the job's console output.
    /// </summary>
    /// <returns>The current PerformContext if available; null otherwise</returns>
    public static PerformContext? Current => _currentContext.Value;
}
