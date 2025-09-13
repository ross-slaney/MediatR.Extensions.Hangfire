using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Hangfire.Extensions.Bridge;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Tests.Bridge;

[TestClass]
public class MediatorJobBridgeRetryTests
{
    [TestMethod]
    public async Task SendAsync_WithRetrySuccess_CompletesOnSecondAttempt()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedResponse = "success";

        // Configure to fail once, then succeed
        mediator.FailuresBeforeSuccess = 1;
        mediator.ResponseToReturn = expectedResponse;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 3);

        // Assert
        Assert.AreEqual(2, mediator.SendCallCount); // Initial attempt + 1 retry
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.AreEqual(expectedResponse, coordinator.LastCompletedResult);
        Assert.IsNull(coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendAsync_WithAllRetriesFailing_CompletesWithException()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");

        // Configure to always fail
        mediator.ExceptionToThrow = expectedException;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 2); // 2 retries = 3 total attempts

        // Assert
        Assert.AreEqual(3, mediator.SendCallCount); // Initial + 2 retries
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.IsNull(coordinator.LastCompletedResult);
        Assert.AreSame(expectedException, coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendAsync_WithZeroRetries_FailsImmediately()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");

        mediator.ExceptionToThrow = expectedException;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 0); // No retries

        // Assert
        Assert.AreEqual(1, mediator.SendCallCount); // Only initial attempt
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.IsNull(coordinator.LastCompletedResult);
        Assert.AreSame(expectedException, coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendAsync_WithSuccessOnLastRetry_CompletesSuccessfully()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedResponse = "final success";

        // Configure to fail 2 times, succeed on 3rd attempt
        mediator.FailuresBeforeSuccess = 2;
        mediator.ResponseToReturn = expectedResponse;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 2); // Allow exactly 2 retries

        // Assert
        Assert.AreEqual(3, mediator.SendCallCount); // Initial + 2 retries
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.AreEqual(expectedResponse, coordinator.LastCompletedResult);
        Assert.IsNull(coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendAsync_WithNullTaskId_ThrowsArgumentException()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var request = new TestRequest { Value = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            bridge.SendAsync("Test Job", request, null!, 3));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithEmptyTaskId_ThrowsArgumentException()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var request = new TestRequest { Value = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            bridge.SendAsync("Test Job", request, string.Empty, 3));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            bridge.SendAsync<string>("Test Job", null!, taskId, 3));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithDifferentExceptionTypes_HandlesCorrectly()
    {
        // Arrange
        var mediator = new MockMediatorWithFailures();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };

        var exceptionTypes = new Exception[]
        {
            new ArgumentException("Argument error"),
            new InvalidOperationException("Operation error"),
            new TimeoutException("Timeout error"),
            new UnauthorizedAccessException("Access denied")
        };

        foreach (var expectedException in exceptionTypes)
        {
            // Reset mocks
            mediator.Reset();
            coordinator.Reset();
            mediator.ExceptionToThrow = expectedException;

            // Act
            await bridge.SendAsync("Test Job", request, taskId, 0); // No retries for quick test

            // Assert
            Assert.AreEqual(1, mediator.SendCallCount);
            Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
            Assert.AreSame(expectedException, coordinator.LastCompletedException);
        }
    }

    // Test helper classes
    private class TestRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class MockMediatorWithFailures : IMediator
    {
        public int SendCallCount { get; private set; }
        public int FailuresBeforeSuccess { get; set; } = 0;
        public object? ResponseToReturn { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public void Reset()
        {
            SendCallCount = 0;
            FailuresBeforeSuccess = 0;
            ResponseToReturn = null;
            ExceptionToThrow = null;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCallCount++;

            // If we should fail more times before success
            if (FailuresBeforeSuccess > 0)
            {
                FailuresBeforeSuccess--;
                throw new InvalidOperationException($"Simulated failure #{SendCallCount}");
            }

            // If we have a specific exception to throw
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            // Success case
            return Task.FromResult((TResponse)ResponseToReturn!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class MockTaskCoordinator : ITaskCoordinator
    {
        public int CompleteTaskCallCount { get; private set; }
        public string? LastCompletedTaskId { get; private set; }
        public object? LastCompletedResult { get; private set; }
        public Exception? LastCompletedException { get; private set; }

        public void Reset()
        {
            CompleteTaskCallCount = 0;
            LastCompletedTaskId = null;
            LastCompletedResult = null;
            LastCompletedException = null;
        }

        public Task<string> CreateTask<TResponse>()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null)
        {
            CompleteTaskCallCount++;
            LastCompletedTaskId = taskId;
            LastCompletedResult = result;
            LastCompletedException = exception;
            return Task.CompletedTask;
        }

        public Task<TResponse> WaitForCompletion<TResponse>(string taskId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(TResponse)!);
        }

        public Task CleanupTask(string taskId)
        {
            return Task.CompletedTask;
        }
    }
}
