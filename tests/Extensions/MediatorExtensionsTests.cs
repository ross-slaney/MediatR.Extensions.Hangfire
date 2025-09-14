using MediatR;
using MediatR.Extensions.Hangfire.Extensions;

namespace MediatR.Extensions.Hangfire.Tests.Extensions;

[TestClass]
public class MediatorExtensionsTests
{
    private readonly IMediator _mockMediator;

    public MediatorExtensionsTests()
    {
        _mockMediator = new MockMediator();
    }

    [TestMethod]
    public void Enqueue_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.Enqueue("Test Job", request));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void Enqueue_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Enqueue(null!, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void Enqueue_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Enqueue(string.Empty, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void Enqueue_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.Enqueue("Test Job", (IRequest)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void EnqueueGeneric_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.Enqueue("Test Job", request));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void EnqueueGeneric_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Enqueue(null!, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void EnqueueGeneric_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Enqueue(string.Empty, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void EnqueueGeneric_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.Enqueue("Test Job", (IRequest<string>)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void EnqueueNotification_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var notification = new TestNotification();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.EnqueueNotification("Test Job", notification));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void EnqueueNotification_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var notification = new TestNotification();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.EnqueueNotification(null!, notification));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void EnqueueNotification_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var notification = new TestNotification();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.EnqueueNotification(string.Empty, notification));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void EnqueueNotification_WithNullNotification_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.EnqueueNotification("Test Job", (INotification)null!));
        Assert.AreEqual("notification", exception.ParamName);
    }

    [TestMethod]
    public void Schedule_WithTimeSpan_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequest();
        var delay = TimeSpan.FromMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.Schedule("Test Job", request, delay));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void Schedule_WithTimeSpan_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();
        var delay = TimeSpan.FromMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Schedule(null!, request, delay));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void Schedule_WithTimeSpan_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var delay = TimeSpan.FromMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.Schedule("Test Job", (IRequest)null!, delay));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void Schedule_WithDateTimeOffset_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequest();
        var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.Schedule("Test Job", request, enqueueAt));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void Schedule_WithDateTimeOffset_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();
        var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.Schedule(null!, request, enqueueAt));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void Schedule_WithDateTimeOffset_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var enqueueAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.Schedule("Test Job", (IRequest)null!, enqueueAt));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void AddOrUpdate_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequest();
        var cronExpression = "0 9 * * *";

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.AddOrUpdate("Test Job", request, cronExpression));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void AddOrUpdate_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();
        var cronExpression = "0 9 * * *";

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.AddOrUpdate(null!, request, cronExpression));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void AddOrUpdate_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var cronExpression = "0 9 * * *";

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            _mockMediator.AddOrUpdate("Test Job", (IRequest)null!, cronExpression));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void AddOrUpdate_WithNullCronExpression_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.AddOrUpdate("Test Job", request, null!));
        Assert.AreEqual("cronExpression", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Cron expression must be provided"));
    }

    [TestMethod]
    public void AddOrUpdate_WithEmptyCronExpression_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.AddOrUpdate("Test Job", request, string.Empty));
        Assert.AreEqual("cronExpression", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Cron expression must be provided"));
    }

    [TestMethod]
    public void TriggerRecurringJob_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.TriggerRecurringJob("Test Job"));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void TriggerRecurringJob_WithNullJobName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.TriggerRecurringJob(null!));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void TriggerRecurringJob_WithEmptyJobName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.TriggerRecurringJob(string.Empty));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void RemoveRecurringJob_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            nullMediator!.RemoveRecurringJob("Test Job"));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public void RemoveRecurringJob_WithNullJobName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.RemoveRecurringJob(null!));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public void RemoveRecurringJob_WithEmptyJobName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            _mockMediator.RemoveRecurringJob(string.Empty));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    // Test helper classes
    private class TestRequest : IRequest { }

    private class TestRequestWithResponse : IRequest<string> { }

    private class TestNotification : INotification { }

    // Mock mediator for testing (doesn't need to actually work, just needs to exist)
    private class MockMediator : IMediator
    {
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
            throw new NotImplementedException();
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
}
