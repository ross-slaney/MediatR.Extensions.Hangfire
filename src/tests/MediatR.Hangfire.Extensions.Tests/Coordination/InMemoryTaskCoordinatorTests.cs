using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MediatR.Hangfire.Extensions.Configuration;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Tests.Coordination;

[TestClass]
public class InMemoryTaskCoordinatorTests : IDisposable
{
    private readonly ILogger<InMemoryTaskCoordinator> _logger;
    private readonly HangfireMediatorOptions _options;
    private readonly InMemoryTaskCoordinator _coordinator;

    public InMemoryTaskCoordinatorTests()
    {
        _logger = NullLogger<InMemoryTaskCoordinator>.Instance;
        _options = new HangfireMediatorOptions
        {
            DefaultTaskTimeout = TimeSpan.FromSeconds(30),
            UseInMemoryCoordination = true
        };
        var optionsWrapper = Options.Create(_options);
        _coordinator = new InMemoryTaskCoordinator(_logger, optionsWrapper);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsWrapper = Options.Create(_options);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new InMemoryTaskCoordinator(null!, optionsWrapper));
        Assert.AreEqual("logger", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new InMemoryTaskCoordinator(_logger, null!));
        Assert.AreEqual("options", exception.ParamName);
    }

    [TestMethod]
    public async Task CreateTask_ReturnsUniqueTaskId()
    {
        // Act
        var taskId1 = await _coordinator.CreateTask<string>();
        var taskId2 = await _coordinator.CreateTask<string>();

        // Assert
        Assert.IsNotNull(taskId1);
        Assert.IsNotNull(taskId2);
        Assert.AreNotEqual(taskId1, taskId2);
        Assert.IsTrue(Guid.TryParse(taskId1, out _));
        Assert.IsTrue(Guid.TryParse(taskId2, out _));
    }

    [TestMethod]
    public async Task CreateTask_WithDifferentTypes_ReturnsValidTaskIds()
    {
        // Act
        var stringTaskId = await _coordinator.CreateTask<string>();
        var intTaskId = await _coordinator.CreateTask<int>();
        var customTaskId = await _coordinator.CreateTask<TestResponse>();

        // Assert
        Assert.IsNotNull(stringTaskId);
        Assert.IsNotNull(intTaskId);
        Assert.IsNotNull(customTaskId);
        Assert.AreNotEqual(stringTaskId, intTaskId);
        Assert.AreNotEqual(intTaskId, customTaskId);
    }

    [TestMethod]
    public async Task CompleteTask_WithValidTaskId_CompletesSuccessfully()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var expectedResult = "test result";

        // Act
        await _coordinator.CompleteTask(taskId, expectedResult);

        // Assert - Should not throw
    }

    [TestMethod]
    public async Task CompleteTask_WithNullTaskId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _coordinator.CompleteTask<string>(null!, "result"));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task CompleteTask_WithEmptyTaskId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _coordinator.CompleteTask<string>(string.Empty, "result"));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task CompleteTask_WithNonExistentTaskId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _coordinator.CompleteTask("non-existent-task", "result");
    }

    [TestMethod]
    public async Task CompleteTask_WithException_CompletesWithException()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        await _coordinator.CompleteTask<string>(taskId, null, expectedException);

        // Assert - Should not throw
    }

    [TestMethod]
    public async Task WaitForCompletion_WithSuccessfulResult_ReturnsResult()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var expectedResult = "test result";

        // Act
        var completionTask = _coordinator.WaitForCompletion<string>(taskId);
        await Task.Delay(10); // Allow task to be set up
        await _coordinator.CompleteTask(taskId, expectedResult);
        var actualResult = await completionTask;

        // Assert
        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public async Task WaitForCompletion_WithException_ThrowsException()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        var completionTask = _coordinator.WaitForCompletion<string>(taskId);
        await Task.Delay(10); // Allow task to be set up
        await _coordinator.CompleteTask<string>(taskId, null, expectedException);

        // Assert
        var actualException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => completionTask);
        Assert.AreEqual(expectedException.Message, actualException.Message);
    }

    [TestMethod]
    public async Task WaitForCompletion_WithNullTaskId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _coordinator.WaitForCompletion<string>(null!));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task WaitForCompletion_WithEmptyTaskId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _coordinator.WaitForCompletion<string>(string.Empty));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task WaitForCompletion_WithNonExistentTaskId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _coordinator.WaitForCompletion<string>("non-existent-task"));
        Assert.IsTrue(exception.Message.Contains("not found or already completed"));
    }

    [TestMethod]
    public async Task WaitForCompletion_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var cts = new CancellationTokenSource();

        // Act
        var completionTask = _coordinator.WaitForCompletion<string>(taskId, cts.Token);
        await Task.Delay(10); // Allow task to be set up
        cts.Cancel();

        // Assert
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => completionTask);
    }

    [TestMethod]
    public async Task CleanupTask_WithValidTaskId_DoesNotThrow()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();

        // Act & Assert - Should not throw
        await _coordinator.CleanupTask(taskId);
    }

    [TestMethod]
    public async Task CleanupTask_WithNullTaskId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _coordinator.CleanupTask(null!);
    }

    [TestMethod]
    public async Task CleanupTask_WithEmptyTaskId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _coordinator.CleanupTask(string.Empty);
    }

    [TestMethod]
    public async Task TaskTimeout_CompletesWithTimeoutException()
    {
        // Arrange
        var shortTimeoutOptions = new HangfireMediatorOptions
        {
            DefaultTaskTimeout = TimeSpan.FromMilliseconds(50),
            UseInMemoryCoordination = true
        };
        var optionsWrapper = Options.Create(shortTimeoutOptions);
        using var shortTimeoutCoordinator = new InMemoryTaskCoordinator(_logger, optionsWrapper);

        var taskId = await shortTimeoutCoordinator.CreateTask<string>();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<TimeoutException>(() => 
            shortTimeoutCoordinator.WaitForCompletion<string>(taskId));
        Assert.IsTrue(exception.Message.Contains("timed out"));
    }

    [TestMethod]
    public async Task CompleteTask_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var taskId = await _coordinator.CreateTask<string>();
        var result = "test result";

        // Act
        await _coordinator.CompleteTask(taskId, result);
        await _coordinator.CompleteTask(taskId, result); // Second completion

        // Assert - Should not throw
    }

    [TestMethod]
    public async Task MultipleTasksSimultaneously_WorkCorrectly()
    {
        // Arrange
        var task1Id = await _coordinator.CreateTask<string>();
        var task2Id = await _coordinator.CreateTask<int>();
        var task3Id = await _coordinator.CreateTask<TestResponse>();

        var expectedString = "test string";
        var expectedInt = 42;
        var expectedCustom = new TestResponse { Message = "test message" };

        // Act
        var completion1 = _coordinator.WaitForCompletion<string>(task1Id);
        var completion2 = _coordinator.WaitForCompletion<int>(task2Id);
        var completion3 = _coordinator.WaitForCompletion<TestResponse>(task3Id);

        await Task.Delay(10); // Allow tasks to be set up

        await _coordinator.CompleteTask(task1Id, expectedString);
        await _coordinator.CompleteTask(task2Id, expectedInt);
        await _coordinator.CompleteTask(task3Id, expectedCustom);

        var result1 = await completion1;
        var result2 = await completion2;
        var result3 = await completion3;

        // Assert
        Assert.AreEqual(expectedString, result1);
        Assert.AreEqual(expectedInt, result2);
        Assert.AreEqual(expectedCustom.Message, result3.Message);
    }

    [TestMethod]
    public async Task Dispose_CancelsAllPendingTasks()
    {
        // Arrange
        var disposableCoordinator = new InMemoryTaskCoordinator(_logger, Options.Create(_options));
        var taskId = await disposableCoordinator.CreateTask<string>();
        var completionTask = disposableCoordinator.WaitForCompletion<string>(taskId);

        // Act
        disposableCoordinator.Dispose();

        // Assert
        try
        {
            await completionTask;
            Assert.Fail("Expected TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
            // Expected exception
        }
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }

    // Test helper class
    private class TestResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
