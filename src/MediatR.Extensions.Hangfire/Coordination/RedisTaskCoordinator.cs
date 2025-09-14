using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using MediatR.Extensions.Hangfire.Configuration;

namespace MediatR.Extensions.Hangfire.Coordination;

/// <summary>
/// Redis-based implementation of ITaskCoordinator for distributed task coordination.
/// Uses Redis for persistent task state management and pub/sub for completion notifications.
/// This implementation supports distributed scenarios where jobs and callers may be on different processes/servers.
/// </summary>
public class RedisTaskCoordinator : ITaskCoordinator, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisTaskCoordinator> _logger;
    private readonly HangfireMediatorOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingTasks = new();
    private readonly string _keyPrefix = "hangfire-mediatr:task:";
    private readonly string _channelPrefix = "hangfire-mediatr:completion:";
    
    // JSON settings that match Hangfire's recommended settings for consistency
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        // Match System.Text.Json defaults as closely as possible
        NullValueHandling = NullValueHandling.Include, // System.Text.Json includes nulls by default
        DefaultValueHandling = DefaultValueHandling.Include, // Include default values
        TypeNameHandling = TypeNameHandling.Auto, // Required for MediatR commands
        DateFormatHandling = DateFormatHandling.IsoDateFormat, // ISO format like System.Text.Json
        DateTimeZoneHandling = DateTimeZoneHandling.Utc, // Consistent timezone handling
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Match ReferenceHandler.IgnoreCycles
        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() // PascalCase like System.Text.Json default
    };

    /// <summary>
    /// Initializes a new instance of the RedisTaskCoordinator.
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    /// <param name="logger">Logger for coordination operations</param>
    /// <param name="options">Configuration options</param>
    public RedisTaskCoordinator(
        IConnectionMultiplexer redis,
        ILogger<RedisTaskCoordinator> logger,
        IOptions<HangfireMediatorOptions> options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _database = _redis.GetDatabase();
        _subscriber = _redis.GetSubscriber();
    }

    /// <summary>
    /// Creates a new task coordination context with a unique identifier.
    /// </summary>
    public async Task<string> CreateTask<TResponse>()
    {
        var taskId = Guid.NewGuid().ToString();
        var taskState = new TaskState
        {
            TaskId = taskId,
            ResponseType = typeof(TResponse).AssemblyQualifiedName!,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = TaskStatus.Pending
        };

        var key = _keyPrefix + taskId;
        var serializedState = JsonConvert.SerializeObject(taskState, JsonSettings);

        await _database.StringSetAsync(key, serializedState, _options.DefaultTaskTimeout);

        _logger.LogDebug("Created task coordination context: {TaskId} for type: {ResponseType}",
            taskId, typeof(TResponse).Name);

        return taskId;
    }

    /// <summary>
    /// Completes a task with the specified result or exception.
    /// Publishes completion notification via Redis pub/sub.
    /// </summary>
    public async Task CompleteTask<TResponse>(string taskId, TResponse? result, Exception? exception = null)
    {
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("Task ID cannot be null or empty", nameof(taskId));

        var key = _keyPrefix + taskId;
        var channel = _channelPrefix + taskId;

        try
        {
            // Retrieve existing task state
            var existingState = await _database.StringGetAsync(key);
            if (!existingState.HasValue)
            {
                _logger.LogWarning("Attempted to complete non-existent task: {TaskId}", taskId);
                return;
            }

            var taskState = JsonConvert.DeserializeObject<TaskState>(existingState!);
            if (taskState == null)
            {
                _logger.LogError("Failed to deserialize task state for task: {TaskId}", taskId);
                return;
            }

            // Update task state
            taskState.Status = exception == null ? TaskStatus.Completed : TaskStatus.Failed;
            taskState.CompletedAt = DateTimeOffset.UtcNow;

            if (exception == null)
            {
                taskState.Result = JsonConvert.SerializeObject(result, JsonSettings);
                _logger.LogDebug("Completing task {TaskId} with success", taskId);
            }
            else
            {
                taskState.Exception = JsonConvert.SerializeObject(new SerializableException(exception), JsonSettings);
                _logger.LogDebug("Completing task {TaskId} with exception: {ExceptionType}",
                    taskId, exception.GetType().Name);
            }

            // Save updated state
            var serializedState = JsonConvert.SerializeObject(taskState, JsonSettings);
            await _database.StringSetAsync(key, serializedState, _options.DefaultTaskTimeout);

            // Notify completion via pub/sub
            await _subscriber.PublishAsync(RedisChannel.Literal(channel), serializedState);

            _logger.LogInformation("Task {TaskId} completed with status: {Status}", taskId, taskState.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete task: {TaskId}", taskId);
            throw;
        }
    }

    /// <summary>
    /// Waits for task completion using Redis pub/sub notifications with fallback to polling.
    /// </summary>
    public async Task<TResponse> WaitForCompletion<TResponse>(string taskId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(taskId))
            throw new ArgumentException("Task ID cannot be null or empty", nameof(taskId));

        var key = _keyPrefix + taskId;
        var channel = _channelPrefix + taskId;
        var tcs = new TaskCompletionSource<string>();

        _pendingTasks[taskId] = tcs;

        try
        {
            _logger.LogDebug("Waiting for completion of task: {TaskId}", taskId);

            // Subscribe to completion notifications
            await _subscriber.SubscribeAsync(RedisChannel.Literal(channel), (ch, message) =>
            {
                if (_pendingTasks.TryGetValue(taskId, out var completionSource))
                {
                    completionSource.SetResult(message!);
                }
            });

            // Check if task is already completed (race condition handling)
            var existingState = await _database.StringGetAsync(key);
            if (existingState.HasValue)
            {
                var taskState = JsonConvert.DeserializeObject<TaskState>(existingState!, JsonSettings);
                if (taskState?.Status != TaskStatus.Pending)
                {
                    tcs.SetResult(existingState!);
                }
            }

            // Wait for completion with timeout
            using var timeoutCts = new CancellationTokenSource(_options.DefaultTaskTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            string completionMessage;
            try
            {
                var completionTask = tcs.Task;
                await completionTask.WaitAsync(combinedCts.Token);
                completionMessage = completionTask.Result;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Task {taskId} did not complete within the configured timeout of {_options.DefaultTaskTimeout}");
            }

            // Process the result
            var finalState = JsonConvert.DeserializeObject<TaskState>(completionMessage, JsonSettings);
            if (finalState == null)
            {
                throw new InvalidOperationException($"Failed to deserialize completion state for task {taskId}");
            }

            if (finalState.Status == TaskStatus.Failed)
            {
                var serializedException = JsonConvert.DeserializeObject<SerializableException>(finalState.Exception!, JsonSettings);
                var exception = new Exception(serializedException!.Message);
                _logger.LogDebug("Task {TaskId} failed with exception: {ExceptionMessage}", taskId, exception.Message);
                throw exception;
            }

            if (finalState.Status == TaskStatus.Completed)
            {
                var result = JsonConvert.DeserializeObject<TResponse>(finalState.Result!, JsonSettings);
                _logger.LogDebug("Task {TaskId} completed successfully", taskId);
                return result!;
            }

            throw new InvalidOperationException($"Task {taskId} completed with unexpected status: {finalState.Status}");
        }
        finally
        {
            // Cleanup
            _pendingTasks.TryRemove(taskId, out _);
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(channel));
        }
    }

    /// <summary>
    /// Cleans up Redis resources for the specified task.
    /// </summary>
    public async Task CleanupTask(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
            return;

        var key = _keyPrefix + taskId;

        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Cleaned up task resources for: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup task resources for: {TaskId}", taskId);
        }
    }

    /// <summary>
    /// Disposes Redis resources.
    /// </summary>
    public void Dispose()
    {
        // Note: We don't dispose the IConnectionMultiplexer as it's managed by DI container
        foreach (var tcs in _pendingTasks.Values)
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.SetCanceled();
            }
        }
        _pendingTasks.Clear();
    }

    /// <summary>
    /// Internal task state representation for Redis storage.
    /// </summary>
    private class TaskState
    {
        public required string TaskId { get; set; }
        public required string ResponseType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public TaskStatus Status { get; set; }
        public string? Result { get; set; }
        public string? Exception { get; set; }
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

    /// <summary>
    /// Serializable exception wrapper for Redis storage.
    /// </summary>
    private class SerializableException
    {
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string TypeName { get; set; } = string.Empty;

        public SerializableException() { }

        public SerializableException(Exception exception)
        {
            Message = exception.Message;
            StackTrace = exception.StackTrace;
            TypeName = exception.GetType().FullName ?? exception.GetType().Name;
        }
    }
}
