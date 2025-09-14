namespace MediatR.Extensions.Hangfire.Configuration;

/// <summary>
/// Configuration options for the MediatR-Hangfire integration.
/// These options control the behavior of background job processing and task coordination.
/// </summary>
public class HangfireMediatorOptions
{
    /// <summary>
    /// Gets or sets the default number of retry attempts for failed jobs.
    /// When a job fails, it will be retried up to this many times before being marked as permanently failed.
    /// Default: 0 (no retries)
    /// </summary>
    public int DefaultRetryAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether console logging is enabled for Hangfire jobs.
    /// When enabled, log messages from job handlers will appear in the Hangfire dashboard.
    /// Default: true
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the default timeout for async task completion.
    /// This is the maximum time to wait for a background job to complete when using EnqueueAsync.
    /// Default: 30 minutes
    /// </summary>
    public TimeSpan DefaultTaskTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the Redis connection string for task coordination.
    /// This is only used when UseRedis() is called during configuration.
    /// Default: null (must be set when using Redis coordination)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Redis key prefix for task coordination data.
    /// This allows multiple applications to share the same Redis instance without conflicts.
    /// Default: "hangfire-mediatr:"
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "hangfire-mediatr:";

    /// <summary>
    /// Gets or sets whether to use in-memory task coordination.
    /// When true, task coordination is handled in-memory (suitable for single-instance deployments).
    /// When false, Redis coordination is used (suitable for distributed deployments).
    /// Default: false (Redis coordination)
    /// </summary>
    public bool UseInMemoryCoordination { get; set; } = false;

    /// <summary>
    /// Gets or sets the interval for cleaning up expired task coordination data.
    /// This prevents memory/storage leaks from abandoned tasks.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to enable detailed job execution logging.
    /// When enabled, additional debug information is logged during job execution.
    /// Default: false
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of concurrent background jobs per server.
    /// This controls how many jobs can execute simultaneously on a single Hangfire server.
    /// Default: Environment.ProcessorCount * 5
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = Environment.ProcessorCount * 5;

    /// <summary>
    /// Gets or sets the job execution timeout.
    /// Jobs that run longer than this timeout will be automatically cancelled.
    /// Default: 1 hour
    /// </summary>
    public TimeSpan JobExecutionTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to automatically delete successful jobs from the dashboard.
    /// When enabled, completed jobs are removed to keep the dashboard clean.
    /// Default: false (keep successful jobs for auditing)
    /// </summary>
    public bool AutoDeleteSuccessfulJobs { get; set; } = false;

    /// <summary>
    /// Gets or sets the retention period for completed jobs.
    /// Jobs older than this period will be automatically cleaned up.
    /// Default: 7 days
    /// </summary>
    public TimeSpan JobRetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Validates the configuration options and throws exceptions for invalid settings.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
        if (DefaultRetryAttempts < 0)
            throw new ArgumentException("Default retry attempts cannot be negative", nameof(DefaultRetryAttempts));

        if (DefaultTaskTimeout <= TimeSpan.Zero)
            throw new ArgumentException("Default task timeout must be positive", nameof(DefaultTaskTimeout));

        if (CleanupInterval <= TimeSpan.Zero)
            throw new ArgumentException("Cleanup interval must be positive", nameof(CleanupInterval));

        if (MaxConcurrentJobs <= 0)
            throw new ArgumentException("Max concurrent jobs must be positive", nameof(MaxConcurrentJobs));

        if (JobExecutionTimeout <= TimeSpan.Zero)
            throw new ArgumentException("Job execution timeout must be positive", nameof(JobExecutionTimeout));

        if (JobRetentionPeriod <= TimeSpan.Zero)
            throw new ArgumentException("Job retention period must be positive", nameof(JobRetentionPeriod));

        if (!UseInMemoryCoordination && string.IsNullOrEmpty(RedisConnectionString))
            throw new ArgumentException("Redis connection string is required when not using in-memory coordination", nameof(RedisConnectionString));

        if (string.IsNullOrEmpty(RedisKeyPrefix))
            throw new ArgumentException("Redis key prefix cannot be null or empty", nameof(RedisKeyPrefix));
    }
}
