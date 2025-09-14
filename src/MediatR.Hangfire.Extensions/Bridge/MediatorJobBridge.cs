using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using MediatR.Extensions.Hangfire.Coordination;

namespace MediatR.Extensions.Hangfire.Bridge;

/// <summary>
/// Implementation of IMediatorJobBridge that executes MediatR requests within Hangfire jobs.
/// This class serves as the bridge between Hangfire's job execution context and MediatR's
/// request/response pipeline, handling job execution, retries, and result coordination.
/// </summary>
public class MediatorJobBridge : IMediatorJobBridge
{
    private readonly IMediator _mediator;
    private readonly ITaskCoordinator _taskCoordinator;
    private readonly ILogger<MediatorJobBridge> _logger;

    /// <summary>
    /// Initializes a new instance of the MediatorJobBridge.
    /// </summary>
    /// <param name="mediator">The MediatR mediator for executing requests</param>
    /// <param name="taskCoordinator">The task coordinator for managing async results</param>
    /// <param name="logger">Logger for job execution tracking</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
    public MediatorJobBridge(
        IMediator mediator,
        ITaskCoordinator taskCoordinator,
        ILogger<MediatorJobBridge> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _taskCoordinator = taskCoordinator ?? throw new ArgumentNullException(nameof(taskCoordinator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a MediatR request as a fire-and-forget background job.
    /// The job name is used as the display name in Hangfire dashboard.
    /// </summary>
    [DisplayName("{0}")]
    public async Task Send(string jobName, IRequest request)
    {
        if (string.IsNullOrEmpty(jobName))
            throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Executing job: {JobName} with request type: {RequestType}",
            jobName, request.GetType().Name);

        try
        {
            await _mediator.Send(request);
            _logger.LogInformation("Successfully completed job: {JobName}", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job failed: {JobName} with request type: {RequestType}",
                jobName, request.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Executes a MediatR request with response as a fire-and-forget background job.
    /// The response will be discarded when used this way.
    /// </summary>
    [DisplayName("{0}")]
    public async Task Send<TResponse>(string jobName, IRequest<TResponse> request)
    {
        if (string.IsNullOrEmpty(jobName))
            throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Executing job: {JobName} with request type: {RequestType}",
            jobName, request.GetType().Name);

        try
        {
            var result = await _mediator.Send(request);
            _logger.LogInformation("Successfully completed job: {JobName} (response discarded)", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job failed: {JobName} with request type: {RequestType}",
                jobName, request.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Executes a MediatR request with return value as a background job.
    /// Implements retry logic and coordinates the async result through the task coordinator.
    /// </summary>
    [DisplayName("{0}")]
    public async Task SendAsync<TResponse>(string jobName, IRequest<TResponse> request, string taskId, int retryAttempts)
    {
        if (string.IsNullOrEmpty(jobName))
            throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("Task ID must be provided", nameof(taskId));

        _logger.LogInformation("Executing async job: {JobName} with task ID: {TaskId}, retry attempts: {RetryAttempts}",
            jobName, taskId, retryAttempts);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= retryAttempts)
        {
            try
            {
                _logger.LogDebug("Attempt {Attempt}/{MaxAttempts} for job: {JobName} (Task ID: {TaskId})",
                    attempt + 1, retryAttempts + 1, jobName, taskId);

                var result = await _mediator.Send(request);

                _logger.LogInformation("Successfully completed async job: {JobName} (Task ID: {TaskId})",
                    jobName, taskId);

                await _taskCoordinator.CompleteTask(taskId, result);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for job: {JobName} (Task ID: {TaskId})",
                    attempt, retryAttempts + 1, jobName, taskId);

                if (attempt > retryAttempts)
                {
                    _logger.LogError(ex, "All retry attempts exhausted for job: {JobName} (Task ID: {TaskId})",
                        jobName, taskId);

                    await _taskCoordinator.CompleteTask<TResponse>(taskId, default, lastException);
                    return;
                }

                // Optional: Add exponential backoff delay between retries
                if (attempt <= retryAttempts)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt} for job: {JobName}",
                        delay.TotalMilliseconds, attempt + 1, jobName);
                    await Task.Delay(delay);
                }
            }
        }
    }

    /// <summary>
    /// Publishes a MediatR notification as a background job.
    /// Notifications are published to all registered handlers.
    /// </summary>
    [DisplayName("{0}")]
    public async Task SendNotification(string jobName, INotification notification)
    {
        if (string.IsNullOrEmpty(jobName))
            throw new ArgumentException("Job name must be provided", nameof(jobName));
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        _logger.LogInformation("Publishing notification job: {JobName} with notification type: {NotificationType}",
            jobName, notification.GetType().Name);

        try
        {
            await _mediator.Publish(notification);
            _logger.LogInformation("Successfully published notification job: {JobName}", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification job failed: {JobName} with notification type: {NotificationType}",
                jobName, notification.GetType().Name);
            throw;
        }
    }
}
