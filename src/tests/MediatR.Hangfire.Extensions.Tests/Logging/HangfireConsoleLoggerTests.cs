using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Hangfire.Extensions.Logging;

namespace MediatR.Hangfire.Extensions.Tests.Logging;

[TestClass]
public class HangfireConsoleLoggerTests
{
    private readonly MockLogger<TestClass> _mockInnerLogger;
    private readonly HangfireConsoleLogger<TestClass> _hangfireLogger;

    public HangfireConsoleLoggerTests()
    {
        _mockInnerLogger = new MockLogger<TestClass>();
        _hangfireLogger = new HangfireConsoleLogger<TestClass>(_mockInnerLogger);
    }

    [TestMethod]
    public void Constructor_WithNullInnerLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new HangfireConsoleLogger<TestClass>(null!));
        Assert.AreEqual("innerLogger", exception.ParamName);
    }

    [TestMethod]
    public void IsEnabled_DelegatesToInnerLogger()
    {
        // Arrange
        _mockInnerLogger.IsEnabledResult = true;

        // Act
        var result = _hangfireLogger.IsEnabled(LogLevel.Information);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(LogLevel.Information, _mockInnerLogger.LastLogLevelChecked);
    }

    [TestMethod]
    public void BeginScope_DelegatesToInnerLogger()
    {
        // Arrange
        var state = "test scope";
        var expectedDisposable = new MockDisposable();
        _mockInnerLogger.ScopeToReturn = expectedDisposable;

        // Act
        var result = _hangfireLogger.BeginScope(state);

        // Assert
        Assert.AreSame(expectedDisposable, result);
        Assert.AreEqual(state, _mockInnerLogger.LastScopeState);
    }

    [TestMethod]
    public void Log_DelegatesToInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "TestEvent");
        var state = "test message";
        var exception = new InvalidOperationException("test exception");
        string formatter(string s, Exception? ex) => s;

        // Act
        _hangfireLogger.Log(logLevel, eventId, state, exception, formatter);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
        Assert.AreEqual(logLevel, _mockInnerLogger.LastLogLevel);
        Assert.AreEqual(eventId, _mockInnerLogger.LastEventId);
        Assert.AreEqual(state, _mockInnerLogger.LastState);
        Assert.AreSame(exception, _mockInnerLogger.LastException);
    }

    [TestMethod]
    public void Log_WithInformationLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        var state = "test message";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithWarningLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Warning;
        var state = "test warning";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithErrorLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Error;
        var state = "test error";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithCriticalLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Critical;
        var state = "test critical";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithDebugLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Debug;
        var state = "test debug";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
    }

    [TestMethod]
    public void Log_WithTraceLevel_CallsInnerLogger()
    {
        // Arrange
        var logLevel = LogLevel.Trace;
        var state = "test trace";

        // Act
        _hangfireLogger.Log(logLevel, new EventId(), state, null, (s, ex) => s);

        // Assert
        Assert.AreEqual(1, _mockInnerLogger.LogCallCount);
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

[TestClass]
public class HangfireConsoleLoggerExtensionsTests
{
    [TestMethod]
    public void WithHangfireConsole_ReturnsHangfireConsoleLogger()
    {
        // Arrange
        var innerLogger = NullLogger<TestClass>.Instance;

        // Act
        var result = innerLogger.WithHangfireConsole();

        // Assert
        Assert.IsInstanceOfType(result, typeof(HangfireConsoleLogger<TestClass>));
    }

    [TestMethod]
    public void CreateHangfireLogger_ReturnsHangfireConsoleLogger()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;

        // Act
        var result = loggerFactory.CreateHangfireLogger<TestClass>();

        // Assert
        Assert.IsInstanceOfType(result, typeof(HangfireConsoleLogger<TestClass>));
    }

    private class TestClass { }
}
