namespace MediatR.Extensions.Hangfire.Coordination;

/// <summary>
/// Interface for coordinating async task completion across distributed processes.
/// This abstraction allows different implementations (Redis, in-memory, etc.) for
/// managing task state and coordinating results between job execution and callers.
/// </summary>
public interface ITaskCoordinator
{
    /// <summary>
    /// Creates a new task coordination context and returns a unique task identifier.
    /// This identifier is used to coordinate the async completion of background jobs.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type for the task</typeparam>
    /// <returns>A unique task identifier string</returns>
    Task<string> CreateTask<TResponse>();

    /// <summary>
    /// Completes a task with the specified result or exception.
    /// This method is called by the job execution context to signal completion.
    /// </summary>
    /// <typeparam name="TResponse">The response type for the task</typeparam>
    /// <param name="taskId">The unique task identifier</param>
    /// <param name="result">The result of the task execution (can be null/default if exception occurred)</param>
    /// <param name="exception">Optional exception if the task failed</param>
    /// <returns>A task representing the completion operation</returns>
    Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null);

    /// <summary>
    /// Waits for the completion of a task and returns the result.
    /// This method is called by the original caller to retrieve the async result.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type</typeparam>
    /// <param name="taskId">The unique task identifier to wait for</param>
    /// <param name="cancellationToken">Cancellation token to cancel the wait operation</param>
    /// <returns>The result of the task execution</returns>
    /// <exception cref="TaskCanceledException">Thrown when the operation is cancelled</exception>
    /// <exception cref="TimeoutException">Thrown when the task doesn't complete within the configured timeout</exception>
    Task<TResponse> WaitForCompletion<TResponse>(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up task coordination resources for the specified task.
    /// This should be called after successful task completion to prevent resource leaks.
    /// </summary>
    /// <param name="taskId">The unique task identifier to clean up</param>
    /// <returns>A task representing the cleanup operation</returns>
    Task CleanupTask(string taskId);
}
