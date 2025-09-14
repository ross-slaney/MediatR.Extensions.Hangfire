using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MediatR.Extensions.Hangfire.Configuration;

namespace MediatR.Extensions.Hangfire.Coordination;

/// <summary>
/// In-memory implementation of ITaskCoordinator for single-process scenarios.
/// This implementation is suitable for development, testing, or single-instance deployments
/// where distributed coordination is not required. All task state is kept in memory.
/// </summary>
public class InMemoryTaskCoordinator : ITaskCoordinator, IDisposable
{
    private readonly ILogger<InMemoryTaskCoordinator> _logger;
    private readonly HangfireMediatorOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingTasks = new();
    private readonly ConcurrentDictionary<string, TaskState> _taskStates = new();
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of the InMemoryTaskCoordinator.
    /// </summary>
    /// <param name="logger">Logger for coordination operations</param>
    /// <param name="options">Configuration options</param>
    public InMemoryTaskCoordinator(
        ILogger<InMemoryTaskCoordinator> logger,
        IOptions<HangfireMediatorOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Setup periodic cleanup of expired tasks
        _cleanupTimer = new Timer(CleanupExpiredTasks, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Creates a new task coordination context with a unique identifier.
    /// </summary>
    public Task<string> CreateTask<TResponse>()
    {
        var taskId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>();
        var taskState = new TaskState
        {
            TaskId = taskId,
            ResponseType = typeof(TResponse),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = TaskStatus.Pending
        };

        _pendingTasks[taskId] = tcs;
        _taskStates[taskId] = taskState;

        _logger.LogDebug("Created task coordination context: {TaskId} for type: {ResponseType}",
            taskId, typeof(TResponse).Name);

        // Setup timeout handling
        _ = Task.Delay(_options.DefaultTaskTimeout).ContinueWith(timeoutTask =>
        {
            if (_pendingTasks.TryGetValue(taskId, out var pendingTcs) && !pendingTcs.Task.IsCompleted)
            {
                _logger.LogWarning("Task {TaskId} timed out after {Timeout}", taskId, _options.DefaultTaskTimeout);
                pendingTcs.SetException(new TimeoutException($"Task {taskId} timed out after {_options.DefaultTaskTimeout}"));
                CleanupTaskInternal(taskId);
            }
        });

        return Task.FromResult(taskId);
    }

    /// <summary>
    /// Completes a task with the specified result or exception.
    /// Immediately notifies any waiting callers through TaskCompletionSource.
    /// </summary>
    public Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null)
    {
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("Task ID cannot be null or empty", nameof(taskId));

        if (!_pendingTasks.TryGetValue(taskId, out var tcs))
        {
            _logger.LogWarning("Attempted to complete non-existent task: {TaskId}", taskId);
            return Task.CompletedTask;
        }

        if (!_taskStates.TryGetValue(taskId, out var taskState))
        {
            _logger.LogError("Task state not found for task: {TaskId}", taskId);
            return Task.CompletedTask;
        }

        try
        {
            // Update task state
            taskState.Status = exception == null ? TaskStatus.Completed : TaskStatus.Failed;
            taskState.CompletedAt = DateTimeOffset.UtcNow;

            if (exception == null)
            {
                taskState.Result = result;
                tcs.SetResult(result);
                _logger.LogDebug("Completing task {TaskId} with success", taskId);
            }
            else
            {
                taskState.Exception = exception;
                tcs.SetException(exception);
                _logger.LogDebug("Completing task {TaskId} with exception: {ExceptionType}",
                    taskId, exception.GetType().Name);
            }

            _logger.LogInformation("Task {TaskId} completed with status: {Status}", taskId, taskState.Status);
        }
        catch (InvalidOperationException)
        {
            // Task was already completed - this can happen in race conditions
            _logger.LogDebug("Task {TaskId} was already completed", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete task: {TaskId}", taskId);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Waits for task completion using the TaskCompletionSource pattern.
    /// </summary>
    public async Task<TResponse> WaitForCompletion<TResponse>(string taskId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("Task ID cannot be null or empty", nameof(taskId));

        if (!_pendingTasks.TryGetValue(taskId, out var tcs))
        {
            throw new InvalidOperationException($"Task {taskId} not found or already completed");
        }

        _logger.LogDebug("Waiting for completion of task: {TaskId}", taskId);

        try
        {
            // Wait for completion with cancellation support
            var result = await tcs.Task.WaitAsync(cancellationToken);

            _logger.LogDebug("Task {TaskId} completed successfully", taskId);

            // Clean up after successful completion
            _ = Task.Run(() => CleanupTaskInternal(taskId));

            return (TResponse)result!;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Wait for task {TaskId} was cancelled", taskId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} failed with exception", taskId);

            // Clean up after failure
            _ = Task.Run(() => CleanupTaskInternal(taskId));

            throw;
        }
    }

    /// <summary>
    /// Cleans up in-memory resources for the specified task.
    /// </summary>
    public Task CleanupTask(string taskId)
    {
        CleanupTaskInternal(taskId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Internal cleanup method that removes task from both dictionaries.
    /// </summary>
    private void CleanupTaskInternal(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
            return;

        try
        {
            _pendingTasks.TryRemove(taskId, out _);
            _taskStates.TryRemove(taskId, out _);
            _logger.LogDebug("Cleaned up task resources for: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup task resources for: {TaskId}", taskId);
        }
    }

    /// <summary>
    /// Periodic cleanup of expired tasks to prevent memory leaks.
    /// </summary>
    private void CleanupExpiredTasks(object? state)
    {
        try
        {
            var expiredTasks = new List<string>();
            var cutoffTime = DateTimeOffset.UtcNow.Subtract(_options.DefaultTaskTimeout);

            foreach (var kvp in _taskStates)
            {
                if (kvp.Value.CreatedAt < cutoffTime && kvp.Value.Status == TaskStatus.Pending)
                {
                    expiredTasks.Add(kvp.Key);
                }
            }

            foreach (var taskId in expiredTasks)
            {
                if (_pendingTasks.TryGetValue(taskId, out var tcs) && !tcs.Task.IsCompleted)
                {
                    tcs.SetException(new TimeoutException($"Task {taskId} expired"));
                }
                CleanupTaskInternal(taskId);
            }

            if (expiredTasks.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired tasks", expiredTasks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic task cleanup");
        }
    }

    /// <summary>
    /// Disposes in-memory resources and stops the cleanup timer.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        // Cancel all pending tasks
        foreach (var kvp in _pendingTasks)
        {
            if (!kvp.Value.Task.IsCompleted)
            {
                kvp.Value.SetCanceled();
            }
        }

        _pendingTasks.Clear();
        _taskStates.Clear();
    }

    /// <summary>
    /// Internal task state representation for in-memory storage.
    /// </summary>
    private class TaskState
    {
        public required string TaskId { get; set; }
        public required Type ResponseType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public TaskStatus Status { get; set; }
        public object? Result { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Task status enumeration.
    /// </summary>
    private enum TaskStatus
    {
        Pending,
        Completed,
        Failed
    }
}
