using MediatR.Extensions.Hangfire.Configuration;

namespace MediatR.Extensions.Hangfire.Tests.Configuration;

[TestClass]
public class HangfireMediatorOptionsEdgeCasesTests
{
    [TestMethod]
    public void Validate_WithMinimumValidValues_DoesNotThrow()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true, // Set to use in-memory to avoid Redis requirement
            DefaultTaskTimeout = TimeSpan.FromMilliseconds(1), // Minimum valid timeout
            DefaultRetryAttempts = 0, // Minimum valid retry attempts
            MaxConcurrentJobs = 1, // Minimum valid concurrent jobs
            JobExecutionTimeout = TimeSpan.FromMilliseconds(1), // Minimum valid execution timeout
            JobRetentionPeriod = TimeSpan.FromMilliseconds(1), // Minimum valid retention period
            CleanupInterval = TimeSpan.FromMilliseconds(1) // Minimum valid cleanup interval
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    [TestMethod]
    public void Validate_WithMaximumValidValues_DoesNotThrow()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            RedisConnectionString = "localhost:6379", // Set Redis connection instead of in-memory
            UseInMemoryCoordination = false,
            DefaultTaskTimeout = TimeSpan.FromDays(365), // Very large timeout
            DefaultRetryAttempts = int.MaxValue, // Maximum retry attempts
            MaxConcurrentJobs = int.MaxValue, // Maximum concurrent jobs
            JobExecutionTimeout = TimeSpan.FromDays(365), // Very large execution timeout
            JobRetentionPeriod = TimeSpan.FromDays(365), // Very large retention period
            CleanupInterval = TimeSpan.FromDays(365) // Very large cleanup interval
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    [TestMethod]
    public void Validate_WithNeitherRedisNorInMemory_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = false,
            RedisConnectionString = null // Neither set
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("RedisConnectionString", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Redis connection string is required"));
    }

    [TestMethod]
    public void Validate_WithEmptyRedisConnection_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = false,
            RedisConnectionString = string.Empty
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("RedisConnectionString", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithZeroTaskTimeout_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            DefaultTaskTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("DefaultTaskTimeout", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithNegativeTaskTimeout_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            DefaultTaskTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("DefaultTaskTimeout", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithZeroJobExecutionTimeout_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            JobExecutionTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("JobExecutionTimeout", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithNegativeJobExecutionTimeout_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            JobExecutionTimeout = TimeSpan.FromMinutes(-5)
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("JobExecutionTimeout", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithZeroJobRetentionPeriod_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            JobRetentionPeriod = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("JobRetentionPeriod", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithNegativeJobRetentionPeriod_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            JobRetentionPeriod = TimeSpan.FromDays(-1)
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("JobRetentionPeriod", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithNegativeRetryAttempts_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            DefaultRetryAttempts = -1
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("DefaultRetryAttempts", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithZeroMaxConcurrentJobs_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            MaxConcurrentJobs = 0
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("MaxConcurrentJobs", exception.ParamName);
    }

    [TestMethod]
    public void Validate_WithNegativeMaxConcurrentJobs_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true,
            MaxConcurrentJobs = -5
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("MaxConcurrentJobs", exception.ParamName);
    }

    [TestMethod]
    public void DefaultValues_MatchExpectedValues()
    {
        // Arrange & Act
        var options = new HangfireMediatorOptions();

        // Assert - Check actual default values from the implementation
        Assert.AreEqual(TimeSpan.FromMinutes(30), options.DefaultTaskTimeout);
        Assert.AreEqual(0, options.DefaultRetryAttempts); // This is actually 0, not 3
        Assert.AreEqual(Environment.ProcessorCount * 5, options.MaxConcurrentJobs);
        Assert.AreEqual(TimeSpan.FromHours(1), options.JobExecutionTimeout); // Actually 1 hour, not 24
        Assert.AreEqual(TimeSpan.FromDays(7), options.JobRetentionPeriod);
        Assert.IsFalse(options.UseInMemoryCoordination);
        Assert.IsTrue(options.EnableConsoleLogging); // This is actually true by default
        Assert.IsFalse(options.EnableDetailedLogging);
        Assert.IsFalse(options.AutoDeleteSuccessfulJobs);
        Assert.IsNull(options.RedisConnectionString);
        Assert.AreEqual("hangfire-mediatr:", options.RedisKeyPrefix);
    }

    [TestMethod]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new HangfireMediatorOptions();
        var timeout = TimeSpan.FromMinutes(15);
        var executionTimeout = TimeSpan.FromHours(2);
        var retentionPeriod = TimeSpan.FromDays(14);
        var connectionString = "localhost:6379";
        var keyPrefix = "myapp:";

        // Act
        options.DefaultTaskTimeout = timeout;
        options.DefaultRetryAttempts = 5;
        options.MaxConcurrentJobs = 20;
        options.JobExecutionTimeout = executionTimeout;
        options.JobRetentionPeriod = retentionPeriod;
        options.UseInMemoryCoordination = true;
        options.EnableConsoleLogging = true;
        options.EnableDetailedLogging = true;
        options.AutoDeleteSuccessfulJobs = true;
        options.RedisConnectionString = connectionString;
        options.RedisKeyPrefix = keyPrefix;

        // Assert
        Assert.AreEqual(timeout, options.DefaultTaskTimeout);
        Assert.AreEqual(5, options.DefaultRetryAttempts);
        Assert.AreEqual(20, options.MaxConcurrentJobs);
        Assert.AreEqual(executionTimeout, options.JobExecutionTimeout);
        Assert.AreEqual(retentionPeriod, options.JobRetentionPeriod);
        Assert.IsTrue(options.UseInMemoryCoordination);
        Assert.IsTrue(options.EnableConsoleLogging);
        Assert.IsTrue(options.EnableDetailedLogging);
        Assert.IsTrue(options.AutoDeleteSuccessfulJobs);
        Assert.AreEqual(connectionString, options.RedisConnectionString);
        Assert.AreEqual(keyPrefix, options.RedisKeyPrefix);
    }
}