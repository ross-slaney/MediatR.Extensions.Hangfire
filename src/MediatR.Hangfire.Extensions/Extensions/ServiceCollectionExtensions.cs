using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Hangfire;
using Hangfire.Console;
using MediatR.Hangfire.Extensions.Bridge;
using MediatR.Hangfire.Extensions.Configuration;
using MediatR.Hangfire.Extensions.Coordination;
using MediatR.Hangfire.Extensions.Logging;

namespace MediatR.Hangfire.Extensions.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure MediatR-Hangfire integration.
/// Provides a fluent API for setting up background job processing with MediatR.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MediatR-Hangfire integration to the service collection with configuration options.
    /// This is the main entry point for configuring the integration.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configure">Action to configure the integration options</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configure is null</exception>
    /// <example>
    /// <code>
    /// services.AddHangfireMediatR(options =>
    /// {
    ///     options.UseRedis("localhost:6379");
    ///     options.DefaultRetryAttempts = 3;
    ///     options.EnableConsoleLogging = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddHangfireMediatR(
        this IServiceCollection services,
        Action<HangfireMediatorOptionsBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Create and configure options
        var optionsBuilder = new HangfireMediatorOptionsBuilder();
        configure(optionsBuilder);
        var options = optionsBuilder.Build();

        // Validate configuration
        options.Validate();

        // Register options
        services.Configure<HangfireMediatorOptions>(opt =>
        {
            opt.DefaultRetryAttempts = options.DefaultRetryAttempts;
            opt.EnableConsoleLogging = options.EnableConsoleLogging;
            opt.DefaultTaskTimeout = options.DefaultTaskTimeout;
            opt.RedisConnectionString = options.RedisConnectionString;
            opt.RedisKeyPrefix = options.RedisKeyPrefix;
            opt.UseInMemoryCoordination = options.UseInMemoryCoordination;
            opt.CleanupInterval = options.CleanupInterval;
            opt.EnableDetailedLogging = options.EnableDetailedLogging;
            opt.MaxConcurrentJobs = options.MaxConcurrentJobs;
            opt.JobExecutionTimeout = options.JobExecutionTimeout;
            opt.AutoDeleteSuccessfulJobs = options.AutoDeleteSuccessfulJobs;
            opt.JobRetentionPeriod = options.JobRetentionPeriod;
        });

        // Register core services
        services.AddScoped<IMediatorJobBridge, MediatorJobBridge>();

        // Register task coordinator based on configuration
        if (options.UseInMemoryCoordination)
        {
            services.AddSingleton<ITaskCoordinator, InMemoryTaskCoordinator>();
        }
        else
        {
            // Register Redis connection if not already registered
            services.TryAddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                if (string.IsNullOrEmpty(options.RedisConnectionString))
                {
                    throw new InvalidOperationException("Redis connection string is required when not using in-memory coordination");
                }
                return ConnectionMultiplexer.Connect(options.RedisConnectionString);
            });

            services.AddSingleton<ITaskCoordinator, RedisTaskCoordinator>();
        }

        // Configure Hangfire integration (assumes Hangfire is already configured)
        services.ConfigureHangfireForMediatR(options);

        // Register a service that will set up the service locator when the container is built
        services.AddSingleton<IServiceLocatorSetup, ServiceLocatorSetup>();

        return services;
    }

    /// <summary>
    /// Adds MediatR-Hangfire integration with default configuration.
    /// Uses in-memory coordination suitable for single-instance deployments.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// <code>
    /// services.AddHangfireMediatR(); // Uses in-memory coordination
    /// </code>
    /// </example>
    public static IServiceCollection AddHangfireMediatR(this IServiceCollection services)
    {
        return services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
        });
    }

    /// <summary>
    /// Configures Hangfire specifically for MediatR integration.
    /// Sets up console logging, filters, and job serialization.
    /// Note: This assumes Hangfire is already configured via AddHangfire().
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The configuration options</param>
    private static void ConfigureHangfireForMediatR(this IServiceCollection services, HangfireMediatorOptions options)
    {
        // Configure additional Hangfire settings for MediatR integration
        services.PostConfigure<BackgroundJobServerOptions>(serverOptions =>
        {
            serverOptions.WorkerCount = Math.Max(serverOptions.WorkerCount, options.MaxConcurrentJobs);
        });

        // Register a configuration action to be applied when Hangfire is fully initialized
        services.AddSingleton<IHangfireMediatorConfigurator>(sp => new HangfireMediatorConfigurator(options));
    }
}

/// <summary>
/// Builder class for configuring HangfireMediatorOptions with a fluent API.
/// Provides methods for setting up different coordination strategies and options.
/// </summary>
public class HangfireMediatorOptionsBuilder
{
    private readonly HangfireMediatorOptions _options = new();

    /// <summary>
    /// Configures the integration to use Redis for task coordination.
    /// This is suitable for distributed deployments where multiple servers process jobs.
    /// </summary>
    /// <param name="connectionString">Redis connection string</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when connectionString is null or empty</exception>
    /// <example>
    /// <code>
    /// options.UseRedis("localhost:6379");
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder UseRedis(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        _options.UseInMemoryCoordination = false;
        _options.RedisConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Configures the integration to use Redis with additional options.
    /// </summary>
    /// <param name="connectionString">Redis connection string</param>
    /// <param name="keyPrefix">Prefix for Redis keys to avoid conflicts</param>
    /// <returns>The builder for chaining</returns>
    /// <example>
    /// <code>
    /// options.UseRedis("localhost:6379", "myapp:");
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder UseRedis(string connectionString, string keyPrefix)
    {
        UseRedis(connectionString);
        _options.RedisKeyPrefix = keyPrefix;
        return this;
    }

    /// <summary>
    /// Configures the integration to use in-memory task coordination.
    /// This is suitable for single-instance deployments or development scenarios.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    /// <example>
    /// <code>
    /// options.UseInMemory();
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder UseInMemory()
    {
        _options.UseInMemoryCoordination = true;
        _options.RedisConnectionString = null;
        return this;
    }

    /// <summary>
    /// Sets the default number of retry attempts for failed jobs.
    /// </summary>
    /// <param name="retryAttempts">Number of retry attempts (must be >= 0)</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when retryAttempts is negative</exception>
    /// <example>
    /// <code>
    /// options.WithRetryAttempts(3);
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithRetryAttempts(int retryAttempts)
    {
        if (retryAttempts < 0)
            throw new ArgumentException("Retry attempts cannot be negative", nameof(retryAttempts));

        _options.DefaultRetryAttempts = retryAttempts;
        return this;
    }

    /// <summary>
    /// Sets the default timeout for async task completion.
    /// </summary>
    /// <param name="timeout">Timeout duration (must be positive)</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when timeout is not positive</exception>
    /// <example>
    /// <code>
    /// options.WithTaskTimeout(TimeSpan.FromMinutes(15));
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithTaskTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be positive", nameof(timeout));

        _options.DefaultTaskTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables console logging in the Hangfire dashboard.
    /// </summary>
    /// <param name="enabled">Whether to enable console logging</param>
    /// <returns>The builder for chaining</returns>
    /// <example>
    /// <code>
    /// options.WithConsoleLogging(true);
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithConsoleLogging(bool enabled = true)
    {
        _options.EnableConsoleLogging = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of concurrent jobs per server.
    /// </summary>
    /// <param name="maxJobs">Maximum concurrent jobs (must be > 0)</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when maxJobs is not positive</exception>
    /// <example>
    /// <code>
    /// options.WithMaxConcurrentJobs(20);
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithMaxConcurrentJobs(int maxJobs)
    {
        if (maxJobs <= 0)
            throw new ArgumentException("Max concurrent jobs must be positive", nameof(maxJobs));

        _options.MaxConcurrentJobs = maxJobs;
        return this;
    }

    /// <summary>
    /// Sets the job execution timeout.
    /// </summary>
    /// <param name="timeout">Job execution timeout (must be positive)</param>
    /// <returns>The builder for chaining</returns>
    /// <exception cref="ArgumentException">Thrown when timeout is not positive</exception>
    /// <example>
    /// <code>
    /// options.WithJobExecutionTimeout(TimeSpan.FromHours(2));
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithJobExecutionTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Job execution timeout must be positive", nameof(timeout));

        _options.JobExecutionTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables detailed logging for job execution.
    /// </summary>
    /// <param name="enabled">Whether to enable detailed logging</param>
    /// <returns>The builder for chaining</returns>
    /// <example>
    /// <code>
    /// options.WithDetailedLogging(true);
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithDetailedLogging(bool enabled = true)
    {
        _options.EnableDetailedLogging = enabled;
        return this;
    }

    /// <summary>
    /// Configures automatic cleanup of successful jobs.
    /// </summary>
    /// <param name="enabled">Whether to auto-delete successful jobs</param>
    /// <param name="retentionPeriod">How long to keep completed jobs</param>
    /// <returns>The builder for chaining</returns>
    /// <example>
    /// <code>
    /// options.WithJobCleanup(autoDelete: true, TimeSpan.FromDays(3));
    /// </code>
    /// </example>
    public HangfireMediatorOptionsBuilder WithJobCleanup(bool enabled = true, TimeSpan? retentionPeriod = null)
    {
        _options.AutoDeleteSuccessfulJobs = enabled;
        if (retentionPeriod.HasValue)
        {
            if (retentionPeriod.Value <= TimeSpan.Zero)
                throw new ArgumentException("Retention period must be positive", nameof(retentionPeriod));
            _options.JobRetentionPeriod = retentionPeriod.Value;
        }
        return this;
    }

    /// <summary>
    /// Builds the configured options.
    /// </summary>
    /// <returns>The configured HangfireMediatorOptions</returns>
    internal HangfireMediatorOptions Build()
    {
        return _options;
    }
}

/// <summary>
/// Interface for setting up the service locator
/// </summary>
public interface IServiceLocatorSetup
{
    /// <summary>
    /// Sets up the service locator with the provided service provider
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for service resolution</param>
    void Setup(IServiceProvider serviceProvider);
}

/// <summary>
/// Implementation that sets up the service locator when the DI container is resolved
/// </summary>
public class ServiceLocatorSetup : IServiceLocatorSetup
{
    /// <summary>
    /// Sets up the service locator with the provided service provider
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for service resolution</param>
    public void Setup(IServiceProvider serviceProvider)
    {
        ServiceLocator.Current = serviceProvider;
    }
}

/// <summary>
/// Interface for configuring Hangfire for MediatR integration
/// </summary>
public interface IHangfireMediatorConfigurator
{
    /// <summary>
    /// Configures Hangfire with the MediatR-specific settings
    /// </summary>
    void Configure();
}

/// <summary>
/// Implementation that configures Hangfire when the DI container is resolved
/// </summary>
public class HangfireMediatorConfigurator : IHangfireMediatorConfigurator
{
    private readonly HangfireMediatorOptions _options;

    /// <summary>
    /// Initializes a new instance of the HangfireMediatorConfigurator
    /// </summary>
    /// <param name="options">The configuration options for MediatR-Hangfire integration</param>
    public HangfireMediatorConfigurator(HangfireMediatorOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Configures Hangfire with the MediatR-specific settings
    /// </summary>
    public void Configure()
    {
        var configuration = GlobalConfiguration.Configuration;

        // Enable console logging if requested
        if (_options.EnableConsoleLogging)
        {
            configuration.UseConsole();
            configuration.UseFilter(new HangfireConsoleFilter());
        }
    }
}
