using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR.Hangfire.Extensions.Bridge;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Extensions;

/// <summary>
/// Extension methods for IMediator that provide seamless integration with Hangfire for background job processing.
/// These extensions maintain the familiar MediatR API while adding background processing capabilities.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Enqueues a MediatR request as a fire-and-forget background job.
    /// The job will be executed asynchronously without blocking the caller.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// var command = new CreateUserCommand { Name = "John Doe" };
    /// _mediator.Enqueue("Create User", command);
    /// </code>
    /// </example>
    public static void Enqueue(this IMediator mediator, string jobName, IRequest request)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        client.Enqueue<IMediatorJobBridge>(bridge => bridge.Send(jobName, request));
    }

    /// <summary>
    /// Enqueues a MediatR request with response as a fire-and-forget background job.
    /// The job will be executed asynchronously without blocking the caller. The response will be discarded.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    public static void Enqueue<TResponse>(this IMediator mediator, string jobName, IRequest<TResponse> request)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        client.Enqueue<IMediatorJobBridge>(bridge => bridge.Send(jobName, request));
    }

    /// <summary>
    /// Enqueues a MediatR request as a background job and returns the result asynchronously.
    /// This method allows you to get return values from background jobs, which is unique among job queue systems.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type from the request</typeparam>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="retryAttempts">Number of retry attempts on failure (default: 0)</param>
    /// <returns>A task that completes when the background job finishes, containing the result</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <exception cref="TimeoutException">Thrown when the job doesn't complete within the configured timeout</exception>
    /// <example>
    /// <code>
    /// var query = new GetUserByIdQuery { UserId = 123 };
    /// var user = await _mediator.EnqueueAsync("Get User", query, retryAttempts: 2);
    /// </code>
    /// </example>
    public static async Task<TResponse> EnqueueAsync<TResponse>(
        this IMediator mediator, 
        string jobName, 
        IRequest<TResponse> request, 
        int retryAttempts = 0)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        // Resolve dependencies from the current service scope
        // This assumes the mediator is being used within a DI context
        var serviceProvider = GetServiceProvider(mediator);
        var taskCoordinator = serviceProvider.GetRequiredService<ITaskCoordinator>();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("MediatorExtensions");

        // Create task coordination context
        var taskId = await taskCoordinator.CreateTask<TResponse>();
        
        logger?.LogDebug("Created background job task: {JobName} with ID: {TaskId}", jobName, taskId);

        try
        {
            // Enqueue the job with task coordination
            var client = new BackgroundJobClient();
            client.Enqueue<IMediatorJobBridge>(bridge => bridge.SendAsync(jobName, request, taskId, retryAttempts));

            // Wait for completion
            var result = await taskCoordinator.WaitForCompletion<TResponse>(taskId);
            
            // Cleanup task resources
            await taskCoordinator.CleanupTask(taskId);
            
            return result;
        }
        catch
        {
            // Ensure cleanup on failure
            try
            {
                await taskCoordinator.CleanupTask(taskId);
            }
            catch (Exception cleanupEx)
            {
                logger?.LogWarning(cleanupEx, "Failed to cleanup task {TaskId} after failure", taskId);
            }
            throw;
        }
    }

    /// <summary>
    /// Enqueues a MediatR notification as a background job.
    /// Notifications are published to all registered handlers asynchronously.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="notification">The MediatR notification to publish</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator or notification is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// var notification = new UserCreatedNotification { UserId = 123 };
    /// _mediator.EnqueueNotification("User Created", notification);
    /// </code>
    /// </example>
    public static void EnqueueNotification(this IMediator mediator, string jobName, INotification notification)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        var client = new BackgroundJobClient();
        client.Enqueue<IMediatorJobBridge>(bridge => bridge.SendNotification(jobName, notification));
    }

    /// <summary>
    /// Schedules a MediatR request to be executed after a specified delay.
    /// The job will be queued for execution at the specified time in the future.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="delay">The delay before the job should be executed</param>
    /// <returns>The job ID for the scheduled job</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// var command = new SendReminderEmailCommand { UserId = 123 };
    /// var jobId = _mediator.Schedule("Send Reminder", command, TimeSpan.FromHours(24));
    /// </code>
    /// </example>
    public static string Schedule(this IMediator mediator, string jobName, IRequest request, TimeSpan delay)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        return client.Schedule<IMediatorJobBridge>(bridge => bridge.Send(jobName, request), delay);
    }

    /// <summary>
    /// Schedules a MediatR request with response to be executed after a specified delay.
    /// The job will be queued for execution at the specified time in the future.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="delay">The delay before the job should be executed</param>
    /// <returns>The job ID for the scheduled job</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    public static string Schedule<TResponse>(this IMediator mediator, string jobName, IRequest<TResponse> request, TimeSpan delay)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        return client.Schedule<IMediatorJobBridge>(bridge => bridge.Send(jobName, request), delay);
    }

    /// <summary>
    /// Schedules a MediatR request to be executed at a specific date and time.
    /// The job will be queued for execution at the specified moment.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="enqueueAt">The date and time when the job should be executed</param>
    /// <returns>The job ID for the scheduled job</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// var command = new GenerateReportCommand { ReportType = "Monthly" };
    /// var jobId = _mediator.Schedule("Monthly Report", command, DateTime.UtcNow.AddDays(30));
    /// </code>
    /// </example>
    public static string Schedule(this IMediator mediator, string jobName, IRequest request, DateTimeOffset enqueueAt)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        return client.Schedule<IMediatorJobBridge>(bridge => bridge.Send(jobName, request), enqueueAt);
    }

    /// <summary>
    /// Schedules a MediatR request with response to be executed at a specific date and time.
    /// The job will be queued for execution at the specified moment.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="enqueueAt">The date and time when the job should be executed</param>
    /// <returns>The job ID for the scheduled job</returns>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    public static string Schedule<TResponse>(this IMediator mediator, string jobName, IRequest<TResponse> request, DateTimeOffset enqueueAt)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = new BackgroundJobClient();
        return client.Schedule<IMediatorJobBridge>(bridge => bridge.Send(jobName, request), enqueueAt);
    }

    /// <summary>
    /// Creates or updates a recurring job that executes a MediatR request on a schedule.
    /// Uses cron expressions for flexible scheduling (e.g., daily, weekly, monthly).
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Unique name for the recurring job (used as identifier)</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="cronExpression">Cron expression defining the schedule (e.g., "0 9 * * *" for daily at 9 AM)</param>
    /// <param name="timeZone">Optional time zone for the schedule (defaults to UTC)</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName or cronExpression is null or empty</exception>
    /// <example>
    /// <code>
    /// var command = new CleanupTempFilesCommand();
    /// _mediator.AddOrUpdate("Daily Cleanup", command, Cron.Daily, TimeZoneInfo.Local);
    /// </code>
    /// </example>
    public static void AddOrUpdate(
        this IMediator mediator, 
        string jobName, 
        IRequest request, 
        string cronExpression, 
        TimeZoneInfo? timeZone = null)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentException("Cron expression must be provided", nameof(cronExpression));

        var client = new RecurringJobManager();
        var options = new RecurringJobOptions
        {
            TimeZone = timeZone ?? TimeZoneInfo.Utc
        };

        client.AddOrUpdate<IMediatorJobBridge>(
            jobName,
            bridge => bridge.Send(jobName, request),
            cronExpression,
            options);
    }

    /// <summary>
    /// Creates or updates a recurring job that executes a MediatR request with response on a schedule.
    /// Uses cron expressions for flexible scheduling (e.g., daily, weekly, monthly).
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">Unique name for the recurring job (used as identifier)</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="cronExpression">Cron expression defining the schedule (e.g., "0 9 * * *" for daily at 9 AM)</param>
    /// <param name="timeZone">Optional time zone for the schedule (defaults to UTC)</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator or request is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName or cronExpression is null or empty</exception>
    public static void AddOrUpdate<TResponse>(
        this IMediator mediator, 
        string jobName, 
        IRequest<TResponse> request, 
        string cronExpression, 
        TimeZoneInfo? timeZone = null)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentException("Cron expression must be provided", nameof(cronExpression));

        var client = new RecurringJobManager();
        var options = new RecurringJobOptions
        {
            TimeZone = timeZone ?? TimeZoneInfo.Utc
        };

        client.AddOrUpdate<IMediatorJobBridge>(
            jobName,
            bridge => bridge.Send(jobName, request),
            cronExpression,
            options);
    }

    /// <summary>
    /// Triggers an existing recurring job to run immediately.
    /// This is useful for testing recurring jobs or running them on-demand.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">The name of the recurring job to trigger</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// _mediator.TriggerRecurringJob("Daily Cleanup");
    /// </code>
    /// </example>
    public static void TriggerRecurringJob(this IMediator mediator, string jobName)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));

        RecurringJob.TriggerJob(jobName);
    }

    /// <summary>
    /// Removes a recurring job by name.
    /// The job will no longer be scheduled to run.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance</param>
    /// <param name="jobName">The name of the recurring job to remove</param>
    /// <exception cref="ArgumentNullException">Thrown when mediator is null</exception>
    /// <exception cref="ArgumentException">Thrown when jobName is null or empty</exception>
    /// <example>
    /// <code>
    /// _mediator.RemoveRecurringJob("Daily Cleanup");
    /// </code>
    /// </example>
    public static void RemoveRecurringJob(this IMediator mediator, string jobName)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));
        if (string.IsNullOrEmpty(jobName)) throw new ArgumentException("Job name must be provided", nameof(jobName));

        RecurringJob.RemoveIfExists(jobName);
    }

    /// <summary>
    /// Helper method to resolve the service provider from the mediator instance.
    /// This uses the service locator pattern as a fallback when DI context is not available.
    /// </summary>
    private static IServiceProvider GetServiceProvider(IMediator mediator)
    {
        // Use the static service locator (configured during startup)
        if (ServiceLocator.Current != null)
        {
            return ServiceLocator.Current;
        }

        throw new InvalidOperationException(
            "Unable to resolve IServiceProvider. Ensure you have called AddHangfireMediatR() in your service configuration, " +
            "which sets up the required service locator.");
    }
}

/// <summary>
/// Simple service locator pattern for scenarios where DI context is not available.
/// This should be configured during application startup.
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// Gets or sets the current service provider instance.
    /// This should be set during application startup.
    /// </summary>
    public static IServiceProvider? Current { get; set; }
}
