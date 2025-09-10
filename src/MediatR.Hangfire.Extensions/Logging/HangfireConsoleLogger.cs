using Microsoft.Extensions.Logging;
using Hangfire.Console;

namespace MediatR.Hangfire.Extensions.Logging;

/// <summary>
/// Logger wrapper that automatically writes to both the regular logger and Hangfire console when available.
/// This provides enhanced visibility for background job execution by showing logs in the Hangfire dashboard.
/// </summary>
/// <typeparam name="T">The type associated with the logger</typeparam>
public class HangfireConsoleLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _innerLogger;

    /// <summary>
    /// Initializes a new instance of the HangfireConsoleLogger.
    /// </summary>
    /// <param name="innerLogger">The underlying logger to wrap</param>
    /// <exception cref="ArgumentNullException">Thrown when innerLogger is null</exception>
    public HangfireConsoleLogger(ILogger<T> innerLogger)
    {
        _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
    }

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of state to begin scope for</typeparam>
    /// <param name="state">The identifier for the scope</param>
    /// <returns>An IDisposable that ends the logical operation scope on dispose</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    /// <summary>
    /// Checks if the given logLevel is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked</param>
    /// <returns>true if enabled; false otherwise</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    /// <summary>
    /// Writes a log entry to both the regular logger and Hangfire console if available.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written</typeparam>
    /// <param name="logLevel">Entry will be written on this level</param>
    /// <param name="eventId">Id of the event</param>
    /// <param name="state">The entry to be written. Can be also an object</param>
    /// <param name="exception">The exception related to this entry</param>
    /// <param name="formatter">Function to create a string message of the state and exception</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Always log to the regular logger first
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);

        // Also log to Hangfire console if we're in a Hangfire context and the log level is appropriate
        if (ShouldLogToHangfireConsole(logLevel))
        {
            var context = HangfireConsoleFilter.Current;
            if (context != null)
            {
                try
                {
                    var message = formatter(state, exception);
                    var logLevelPrefix = GetLogLevelPrefix(logLevel);
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var fullMessage = $"[{timestamp}] {logLevelPrefix}{message}";

                    if (exception != null)
                    {
                        fullMessage += $" | Exception: {exception.Message}";
                        if (!string.IsNullOrEmpty(exception.StackTrace))
                        {
                            // Add stack trace on a new line for better readability
                            fullMessage += $"\n{exception.StackTrace}";
                        }
                    }

                    // Use appropriate console method based on log level
                    switch (logLevel)
                    {
                        case LogLevel.Error:
                        case LogLevel.Critical:
                            context.SetTextColor(ConsoleTextColor.Red);
                            break;
                        case LogLevel.Warning:
                            context.SetTextColor(ConsoleTextColor.Yellow);
                            break;
                        case LogLevel.Information:
                            context.SetTextColor(ConsoleTextColor.White);
                            break;
                        case LogLevel.Debug:
                        case LogLevel.Trace:
                            context.SetTextColor(ConsoleTextColor.Gray);
                            break;
                    }

                    context.WriteLine(fullMessage);
                    context.ResetTextColor();
                }
                catch
                {
                    // Silently fail if Hangfire console is not available or throws an error
                    // We don't want logging failures to break the job execution
                }
            }
        }
    }

    /// <summary>
    /// Determines if the log level should be written to Hangfire console.
    /// Only logs Information level and above to avoid console spam.
    /// </summary>
    /// <param name="logLevel">The log level to check</param>
    /// <returns>true if the level should be logged to console; false otherwise</returns>
    private static bool ShouldLogToHangfireConsole(LogLevel logLevel)
    {
        // Only log Information, Warning, Error, and Critical to console to avoid spam
        return logLevel >= LogLevel.Information;
    }

    /// <summary>
    /// Gets a short prefix string for the log level.
    /// </summary>
    /// <param name="logLevel">The log level</param>
    /// <returns>A short prefix string representing the log level</returns>
    private static string GetLogLevelPrefix(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "[TRC] ",
            LogLevel.Debug => "[DBG] ",
            LogLevel.Information => "[INF] ",
            LogLevel.Warning => "[WRN] ",
            LogLevel.Error => "[ERR] ",
            LogLevel.Critical => "[CRT] ",
            LogLevel.None => "",
            _ => "[LOG] "
        };
    }
}

/// <summary>
/// Extension methods to easily register and use the Hangfire console logger.
/// </summary>
public static class HangfireConsoleLoggerExtensions
{
    /// <summary>
    /// Wraps an existing logger with Hangfire console functionality.
    /// This allows existing logging code to automatically write to the Hangfire dashboard.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger</typeparam>
    /// <param name="logger">The logger to wrap</param>
    /// <returns>A logger that writes to both the original destination and Hangfire console</returns>
    /// <example>
    /// <code>
    /// var enhancedLogger = _logger.WithHangfireConsole();
    /// enhancedLogger.LogInformation("This will appear in both regular logs and Hangfire dashboard");
    /// </code>
    /// </example>
    public static ILogger<T> WithHangfireConsole<T>(this ILogger<T> logger)
    {
        return new HangfireConsoleLogger<T>(logger);
    }

    /// <summary>
    /// Creates a Hangfire-enabled logger from a logger factory.
    /// This is useful when you need to create a new logger instance within a job.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger</typeparam>
    /// <param name="loggerFactory">The logger factory</param>
    /// <returns>A logger that writes to both regular destinations and Hangfire console</returns>
    /// <example>
    /// <code>
    /// var logger = _loggerFactory.CreateHangfireLogger&lt;MyJobHandler&gt;();
    /// logger.LogInformation("This will appear in Hangfire dashboard");
    /// </code>
    /// </example>
    public static ILogger<T> CreateHangfireLogger<T>(this ILoggerFactory loggerFactory)
    {
        var regularLogger = loggerFactory.CreateLogger<T>();
        return new HangfireConsoleLogger<T>(regularLogger);
    }
}
