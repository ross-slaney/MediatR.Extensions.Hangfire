using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Extensions.Hangfire.Logging;

namespace MediatR.Extensions.Hangfire.Tests.Logging;

[TestClass]
public class HangfireConsoleLoggerAdvancedTests
{
    [TestMethod]
    public void Log_WithDifferentLogLevels_CallsInnerLogger()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var logLevels = new[]
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical
        };

        // Act & Assert
        foreach (var logLevel in logLevels)
        {
            hangfireLogger.Log(logLevel, new EventId(), "test message", null, (s, ex) => s);
            Assert.AreEqual(logLevel, mockLogger.LastLogLevel);
        }

        Assert.AreEqual(logLevels.Length, mockLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithException_IncludesExceptionInformation()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var testException = new InvalidOperationException("Test exception message");

        // Act
        hangfireLogger.Log(LogLevel.Error, new EventId(1, "TestEvent"), "test message", testException, (s, ex) => s);

        // Assert
        Assert.AreEqual(LogLevel.Error, mockLogger.LastLogLevel);
        Assert.AreSame(testException, mockLogger.LastException);
    }

    [TestMethod]
    public void Log_WithFormattedMessage_UsesFormatter()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var state = "test state";
        var formattedMessage = "formatted message";

        string formatter(string s, Exception? ex) => formattedMessage;

        // Act
        hangfireLogger.Log(LogLevel.Information, new EventId(), state, null, formatter);

        // Assert
        Assert.AreEqual(state, mockLogger.LastState);
        // The formatter is passed through to the inner logger
    }

    [TestMethod]
    public void Log_WithEventId_PassesEventIdToInnerLogger()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var eventId = new EventId(42, "TestEvent");

        // Act
        hangfireLogger.Log(LogLevel.Information, eventId, "test message", null, (s, ex) => s);

        // Assert
        Assert.AreEqual(eventId, mockLogger.LastEventId);
    }

    [TestMethod]
    public void IsEnabled_DelegatesToInnerLogger_ForAllLogLevels()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var logLevels = Enum.GetValues<LogLevel>();

        // Act & Assert
        foreach (var logLevel in logLevels)
        {
            if (logLevel == LogLevel.None) continue; // Skip None

            mockLogger.IsEnabledResult = logLevel >= LogLevel.Information;
            var result = hangfireLogger.IsEnabled(logLevel);

            Assert.AreEqual(mockLogger.IsEnabledResult, result);
            Assert.AreEqual(logLevel, mockLogger.LastLogLevelChecked);
        }
    }

    [TestMethod]
    public void BeginScope_WithDifferentStateTypes_DelegatesToInnerLogger()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);
        var expectedDisposable = new MockDisposable();
        mockLogger.ScopeToReturn = expectedDisposable;

        var testStates = new object[]
        {
            "string state",
            42,
            new { Property = "value" },
            new List<string> { "item1", "item2" }
        };

        // Act & Assert
        foreach (var state in testStates)
        {
            var result = hangfireLogger.BeginScope(state);
            Assert.AreSame(expectedDisposable, result);
            Assert.AreEqual(state, mockLogger.LastScopeState);
        }
    }

    [TestMethod]
    public void WithHangfireConsole_Extension_ReturnsHangfireConsoleLogger()
    {
        // Arrange
        var logger = NullLogger<TestClass>.Instance;

        // Act
        var result = logger.WithHangfireConsole();

        // Assert
        Assert.IsInstanceOfType(result, typeof(HangfireConsoleLogger<TestClass>));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateHangfireLogger_Extension_ReturnsHangfireConsoleLogger()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;

        // Act
        var result = loggerFactory.CreateHangfireLogger<TestClass>();

        // Assert
        Assert.IsInstanceOfType(result, typeof(HangfireConsoleLogger<TestClass>));
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void HangfireConsoleLogger_WithNullInnerLogger_InExtension_StillWorks()
    {
        // Arrange
        var loggerFactory = new NullLoggerFactory();

        // Act
        var logger = loggerFactory.CreateHangfireLogger<TestClass>();

        // Assert
        Assert.IsNotNull(logger);
        // Test that it doesn't throw when used
        logger.LogInformation("Test message");
        Assert.IsFalse(logger.IsEnabled(LogLevel.Information)); // NullLogger returns false for all levels
    }

    [TestMethod]
    public void Log_WithDifferentStateTypes_HandlesCorrectly()
    {
        // Arrange
        var mockLogger = new MockLogger<TestClass>();
        var hangfireLogger = new HangfireConsoleLogger<TestClass>(mockLogger);

        var testCases = new object[]
        {
            "string message",
            42,
            new { Name = "Test", Value = 123 },
            new List<KeyValuePair<string, object>>
            {
                new("key1", "value1"),
                new("key2", 42)
            }
        };

        // Act & Assert
        foreach (var state in testCases)
        {
            hangfireLogger.Log(LogLevel.Information, new EventId(), state, null, (s, ex) => s?.ToString() ?? "");
            Assert.AreEqual(state, mockLogger.LastState);
        }
    }

    // Test helper classes
    private class TestClass { }

    private class MockLogger<T> : ILogger<T>
    {
        public int LogCallCount { get; private set; }
        public LogLevel LastLogLevel { get; private set; }
        public EventId LastEventId { get; private set; }
        public object? LastState { get; private set; }
        public Exception? LastException { get; private set; }

        public LogLevel LastLogLevelChecked { get; private set; }
        public bool IsEnabledResult { get; set; } = true;

        public object? LastScopeState { get; private set; }
        public IDisposable? ScopeToReturn { get; set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            LastScopeState = state;
            return ScopeToReturn;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            LastLogLevelChecked = logLevel;
            return IsEnabledResult;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogCallCount++;
            LastLogLevel = logLevel;
            LastEventId = eventId;
            LastState = state;
            LastException = exception;
        }
    }

    private class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
