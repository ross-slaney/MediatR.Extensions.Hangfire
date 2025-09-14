using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Extensions.Hangfire.Bridge;
using MediatR.Extensions.Hangfire.Coordination;

namespace MediatR.Extensions.Hangfire.Tests.Bridge;

[TestClass]
public class MediatorJobBridgeTests
{
    [TestMethod]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new MediatorJobBridge(null!, coordinator, logger));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullCoordinator_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = new MockMediator();
        var logger = NullLogger<MediatorJobBridge>.Instance;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new MediatorJobBridge(mediator, null!, logger));
        Assert.AreEqual("taskCoordinator", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new MediatorJobBridge(mediator, coordinator, null!));
        Assert.AreEqual("logger", exception.ParamName);
    }

    [TestMethod]
    public async Task Send_WithValidCommand_ExecutesSuccessfully()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var command = new TestCommand { Action = "test action" };

        // Act
        await bridge.Send("Test Job", command);

        // Assert
        Assert.AreEqual(1, mediator.SendCommandCallCount);
        Assert.AreEqual(command, mediator.LastCommand);
    }

    [TestMethod]
    public async Task Send_WithValidRequestWithResponse_ExecutesSuccessfully()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var request = new TestRequest { Value = "test" };
        var expectedResponse = "response";
        mediator.ResponseToReturn = expectedResponse;

        // Act
        await bridge.Send("Test Job", request);

        // Assert
        Assert.AreEqual(1, mediator.SendCallCount);
        Assert.AreEqual(request, mediator.LastRequest);
    }

    [TestMethod]
    public async Task Send_WithMediatorException_ThrowsException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var command = new TestCommand { Action = "test action" };
        var expectedException = new InvalidOperationException("Test exception");
        mediator.ExceptionToThrow = expectedException;

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            bridge.Send("Test Job", command));
        Assert.AreSame(expectedException, exception);
    }

    [TestMethod]
    public async Task Send_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var command = new TestCommand { Action = "test action" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            bridge.Send(null!, command));
        Assert.AreEqual("jobName", exception.ParamName);
    }

    [TestMethod]
    public async Task Send_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var command = new TestCommand { Action = "test action" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            bridge.Send(string.Empty, command));
        Assert.AreEqual("jobName", exception.ParamName);
    }

    [TestMethod]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            bridge.Send("Test Job", (IRequest)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithValidRequest_ExecutesSuccessfully()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedResponse = "response";
        mediator.ResponseToReturn = expectedResponse;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 3);

        // Assert
        Assert.AreEqual(1, mediator.SendCallCount);
        Assert.AreEqual(request, mediator.LastRequest);
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.AreEqual(expectedResponse, coordinator.LastCompletedResult);
        Assert.IsNull(coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendAsync_WithMediatorException_CompletesTaskWithException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var taskId = "test-task-id";
        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");
        mediator.ExceptionToThrow = expectedException;

        // Act
        await bridge.SendAsync("Test Job", request, taskId, 0); // 0 retries for quick test

        // Assert
        Assert.AreEqual(1, mediator.SendCallCount);
        Assert.AreEqual(1, coordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, coordinator.LastCompletedTaskId);
        Assert.IsNull(coordinator.LastCompletedResult);
        Assert.AreSame(expectedException, coordinator.LastCompletedException);
    }

    [TestMethod]
    public async Task SendNotification_WithValidNotification_ExecutesSuccessfully()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var notification = new TestNotification { Message = "test message" };

        // Act
        await bridge.SendNotification("Test Notification Job", notification);

        // Assert
        Assert.AreEqual(1, mediator.PublishCallCount);
        Assert.AreEqual(notification, mediator.LastNotification);
    }

    [TestMethod]
    public async Task SendNotification_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        var notification = new TestNotification { Message = "test message" };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            bridge.SendNotification(null!, notification));
        Assert.AreEqual("jobName", exception.ParamName);
    }

    [TestMethod]
    public async Task SendNotification_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = new MockMediator();
        var coordinator = new MockTaskCoordinator();
        var logger = NullLogger<MediatorJobBridge>.Instance;
        var bridge = new MediatorJobBridge(mediator, coordinator, logger);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            bridge.SendNotification("Test Job", null!));
        Assert.AreEqual("notification", exception.ParamName);
    }

    // Test helper classes
    private class TestRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class TestCommand : IRequest
    {
        public string Action { get; set; } = string.Empty;
    }

    private class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private class MockMediator : IMediator
    {
        public int SendCallCount { get; private set; }
        public int SendCommandCallCount { get; private set; }
        public int PublishCallCount { get; private set; }
        public object? LastRequest { get; private set; }
        public object? LastCommand { get; private set; }
        public object? LastNotification { get; private set; }
        public object? ResponseToReturn { get; set; }
        public Exception? ExceptionToThrow { get; set; }

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
            PublishCallCount++;
            LastNotification = notification;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            PublishCallCount++;
            LastNotification = notification;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCallCount++;
            LastRequest = request;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult((TResponse)ResponseToReturn!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SendCommandCallCount++;
            LastCommand = request;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.CompletedTask;
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