using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Hangfire.Extensions.Bridge;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Tests.Bridge;

[TestClass]
public class MediatorJobBridgeTests
{
    private readonly MockMediator _mockMediator;
    private readonly MockTaskCoordinator _mockTaskCoordinator;
    private readonly ILogger<MediatorJobBridge> _logger;
    private readonly MediatorJobBridge _bridge;

    public MediatorJobBridgeTests()
    {
        _mockMediator = new MockMediator();
        _mockTaskCoordinator = new MockTaskCoordinator();
        _logger = NullLogger<MediatorJobBridge>.Instance;
        _bridge = new MediatorJobBridge(_mockMediator, _mockTaskCoordinator, _logger);
    }

    [TestMethod]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new MediatorJobBridge(null!, _mockTaskCoordinator, _logger));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullTaskCoordinator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new MediatorJobBridge(_mockMediator, null!, _logger));
        Assert.AreEqual("taskCoordinator", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new MediatorJobBridge(_mockMediator, _mockTaskCoordinator, null!));
        Assert.AreEqual("logger", exception.ParamName);
    }

    [TestMethod]
    public async Task Send_WithValidRequest_ExecutesSuccessfully()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequest();

        // Act
        await _bridge.Send(jobName, request);

        // Assert
        Assert.AreEqual(1, _mockMediator.SendCallCount);
        Assert.AreSame(request, _mockMediator.LastRequest);
    }

    [TestMethod]
    public async Task Send_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.Send(null!, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task Send_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.Send(string.Empty, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _bridge.Send("Test Job", (IRequest)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public async Task Send_WhenMediatorThrows_RethrowsException()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequest();
        var expectedException = new InvalidOperationException("Test exception");
        _mockMediator.ExceptionToThrow = expectedException;

        // Act & Assert
        var actualException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _bridge.Send(jobName, request));
        Assert.AreSame(expectedException, actualException);
    }

    [TestMethod]
    public async Task SendGeneric_WithValidRequest_ExecutesSuccessfully()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequestWithResponse();
        _mockMediator.ResponseToReturn = "test response";

        // Act
        await _bridge.Send(jobName, request);

        // Assert
        Assert.AreEqual(1, _mockMediator.SendGenericCallCount);
        Assert.AreSame(request, _mockMediator.LastGenericRequest);
    }

    [TestMethod]
    public async Task SendGeneric_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.Send(null!, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task SendGeneric_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _bridge.Send("Test Job", (IRequest<string>)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithValidRequest_CompletesTaskSuccessfully()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequestWithResponse();
        var taskId = "test-task-id";
        var retryAttempts = 0;
        var expectedResponse = "test response";
        _mockMediator.ResponseToReturn = expectedResponse;

        // Act
        await _bridge.SendAsync(jobName, request, taskId, retryAttempts);

        // Assert
        Assert.AreEqual(1, _mockMediator.SendGenericCallCount);
        Assert.AreEqual(1, _mockTaskCoordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, _mockTaskCoordinator.LastTaskId);
        Assert.AreEqual(expectedResponse, _mockTaskCoordinator.LastResult);
        Assert.IsNull(_mockTaskCoordinator.LastException);
    }

    [TestMethod]
    public async Task SendAsync_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.SendAsync(null!, request, "task-id", 0));
        Assert.AreEqual("jobName", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _bridge.SendAsync("Test Job", (IRequest<string>)null!, "task-id", 0));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithNullTaskId_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.SendAsync("Test Job", request, null!, 0));
        Assert.AreEqual("taskId", exception.ParamName);
    }

    [TestMethod]
    public async Task SendAsync_WithException_CompletesTaskWithException()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequestWithResponse();
        var taskId = "test-task-id";
        var retryAttempts = 0;
        var expectedException = new InvalidOperationException("Test exception");
        _mockMediator.ExceptionToThrow = expectedException;

        // Act
        await _bridge.SendAsync(jobName, request, taskId, retryAttempts);

        // Assert
        Assert.AreEqual(1, _mockMediator.SendGenericCallCount);
        Assert.AreEqual(1, _mockTaskCoordinator.CompleteTaskCallCount);
        Assert.AreEqual(taskId, _mockTaskCoordinator.LastTaskId);
        Assert.AreSame(expectedException, _mockTaskCoordinator.LastException);
    }

    [TestMethod]
    public async Task SendAsync_WithRetryAttempts_RetriesOnFailure()
    {
        // Arrange
        var jobName = "Test Job";
        var request = new TestRequestWithResponse();
        var taskId = "test-task-id";
        var retryAttempts = 2;
        var expectedException = new InvalidOperationException("Test exception");
        _mockMediator.ExceptionToThrow = expectedException;

        // Act
        await _bridge.SendAsync(jobName, request, taskId, retryAttempts);

        // Assert
        Assert.AreEqual(3, _mockMediator.SendGenericCallCount); // Original + 2 retries
        Assert.AreEqual(1, _mockTaskCoordinator.CompleteTaskCallCount);
        Assert.AreSame(expectedException, _mockTaskCoordinator.LastException);
    }

    [TestMethod]
    public async Task SendNotification_WithValidNotification_ExecutesSuccessfully()
    {
        // Arrange
        var jobName = "Test Notification Job";
        var notification = new TestNotification();

        // Act
        await _bridge.SendNotification(jobName, notification);

        // Assert
        Assert.AreEqual(1, _mockMediator.PublishCallCount);
        Assert.AreSame(notification, _mockMediator.LastNotification);
    }

    [TestMethod]
    public async Task SendNotification_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var notification = new TestNotification();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _bridge.SendNotification(null!, notification));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task SendNotification_WithNullNotification_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _bridge.SendNotification("Test Job", (INotification)null!));
        Assert.AreEqual("notification", exception.ParamName);
    }

    [TestMethod]
    public async Task SendNotification_WhenMediatorThrows_RethrowsException()
    {
        // Arrange
        var jobName = "Test Notification Job";
        var notification = new TestNotification();
        var expectedException = new InvalidOperationException("Test exception");
        _mockMediator.ExceptionToThrow = expectedException;

        // Act & Assert
        var actualException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _bridge.SendNotification(jobName, notification));
        Assert.AreSame(expectedException, actualException);
    }

    // Test helper classes
    private class TestRequest : IRequest { }
    
    private class TestRequestWithResponse : IRequest<string> { }
    
    private class TestNotification : INotification { }

    // Mock implementations
    private class MockMediator : IMediator
    {
        public int SendCallCount { get; private set; }
        public int SendGenericCallCount { get; private set; }
        public int PublishCallCount { get; private set; }
        public IRequest? LastRequest { get; private set; }
        public object? LastGenericRequest { get; private set; }
        public INotification? LastNotification { get; private set; }
        public object? ResponseToReturn { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SendCallCount++;
            LastRequest = request;
            
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendGenericCallCount++;
            LastGenericRequest = request;
            
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            return Task.FromResult((TResponse)ResponseToReturn!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            PublishCallCount++;
            LastNotification = notification;
            
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            return Task.CompletedTask;
        }

        // Not used in tests
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class MockTaskCoordinator : ITaskCoordinator
    {
        public int CompleteTaskCallCount { get; private set; }
        public string? LastTaskId { get; private set; }
        public object? LastResult { get; private set; }
        public Exception? LastException { get; private set; }

        public Task<string> CreateTask<TResponse>() => throw new NotImplementedException();

        public Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null)
        {
            CompleteTaskCallCount++;
            LastTaskId = taskId;
            LastResult = result;
            LastException = exception;
            return Task.CompletedTask;
        }

        public Task<TResponse> WaitForCompletion<TResponse>(string taskId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task CleanupTask(string taskId) => throw new NotImplementedException();
    }
}
