using MediatR;
using System.ComponentModel;

namespace MediatR.Extensions.Hangfire.Bridge;

/// <summary>
/// Bridge interface for executing MediatR requests within Hangfire job context.
/// This interface abstracts the execution of MediatR commands, queries, and notifications
/// as background jobs, providing a clean separation between job scheduling and execution.
/// </summary>
public interface IMediatorJobBridge
{
    /// <summary>
    /// Executes a MediatR request (command) as a fire-and-forget background job.
    /// </summary>
    /// <param name="jobName">The display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [DisplayName("{0}")]
    Task Send(string jobName, IRequest request);

    /// <summary>
    /// Executes a MediatR request with response as a fire-and-forget background job.
    /// Note: The response will be discarded when used this way.
    /// </summary>
    /// <param name="jobName">The display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [DisplayName("{0}")]
    Task Send<TResponse>(string jobName, IRequest<TResponse> request);

    /// <summary>
    /// Executes a MediatR request with return value as a background job.
    /// Supports retry logic and task coordination for async result retrieval.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request</typeparam>
    /// <param name="jobName">The display name for the job in Hangfire dashboard</param>
    /// <param name="request">The MediatR request to execute</param>
    /// <param name="taskId">Unique identifier for coordinating the async response</param>
    /// <param name="retryAttempts">Number of retry attempts on failure</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [DisplayName("{0}")]
    Task SendAsync<TResponse>(string jobName, IRequest<TResponse> request, string taskId, int retryAttempts);

    /// <summary>
    /// Publishes a MediatR notification as a background job.
    /// Notifications are published to all registered handlers.
    /// </summary>
    /// <param name="jobName">The display name for the job in Hangfire dashboard</param>
    /// <param name="notification">The MediatR notification to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [DisplayName("{0}")]
    Task SendNotification(string jobName, INotification notification);
}
