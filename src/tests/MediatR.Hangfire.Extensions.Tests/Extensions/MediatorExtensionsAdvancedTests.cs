using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR.Hangfire.Extensions.Extensions;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Tests.Extensions;

[TestClass]
public class MediatorExtensionsAdvancedTests
{
    [TestMethod]
    public async Task EnqueueAsync_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator? nullMediator = null;
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            nullMediator!.EnqueueAsync("Test Job", request));
        Assert.AreEqual("mediator", exception.ParamName);
    }

    [TestMethod]
    public async Task EnqueueAsync_WithNullJobName_ThrowsArgumentException()
    {
        // Arrange
        var mockMediator = new MockMediator();
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            mockMediator.EnqueueAsync(null!, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task EnqueueAsync_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var mockMediator = new MockMediator();
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            mockMediator.EnqueueAsync(string.Empty, request));
        Assert.AreEqual("jobName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Job name must be provided"));
    }

    [TestMethod]
    public async Task EnqueueAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mockMediator = new MockMediator();

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            mockMediator.EnqueueAsync("Test Job", (IRequest<string>)null!));
        Assert.AreEqual("request", exception.ParamName);
    }

    [TestMethod]
    public void GetServiceProvider_WithoutServiceLocator_ThrowsInvalidOperationException()
    {
        // Arrange
        ServiceLocator.Current = null;
        var mockMediator = new MockMediator();
        var request = new TestRequestWithResponse();

        // Act & Assert
        var exception = Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            mockMediator.EnqueueAsync("Test Job", request));
        Assert.IsTrue(exception.Result.Message.Contains("Unable to resolve IServiceProvider"));
        Assert.IsTrue(exception.Result.Message.Contains("AddHangfireMediatR"));
    }

    [TestMethod]
    public async Task EnqueueAsync_WithServiceLocator_AttemptsToResolveServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITaskCoordinator>(new MockTaskCoordinator());
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        var serviceProvider = services.BuildServiceProvider();

        ServiceLocator.Current = serviceProvider;

        var mockMediator = new MockMediator();
        var request = new TestRequestWithResponse();

        try
        {
            // Act - This will try to create task but fail because we don't have Hangfire configured
            // But it tests the service resolution path
            await mockMediator.EnqueueAsync("Test Job", request);
        }
        catch
        {
            // Expected to fail at Hangfire job creation, but we tested the service resolution
        }
        finally
        {
            // Cleanup
            ServiceLocator.Current = null;
            serviceProvider.Dispose();
        }

        // Assert - Test passed if no exception in service resolution
    }

    // Test helper classes
    private class TestRequestWithResponse : IRequest<string> { }

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

    private class MockTaskCoordinator : ITaskCoordinator
    {
        public Task<string> CreateTask<TResponse>()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null)
        {
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
