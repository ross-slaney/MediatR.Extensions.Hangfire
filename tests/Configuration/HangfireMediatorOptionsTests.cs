using MediatR.Extensions.Hangfire.Configuration;

namespace MediatR.Extensions.Hangfire.Tests.Configuration;

[TestClass]
public class HangfireMediatorOptionsTests
{
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new HangfireMediatorOptions();

        // Assert
        Assert.AreEqual(0, options.DefaultRetryAttempts);
        Assert.AreEqual(true, options.EnableConsoleLogging);
        Assert.AreEqual(TimeSpan.FromMinutes(30), options.DefaultTaskTimeout);
        Assert.IsNull(options.RedisConnectionString);
        Assert.AreEqual("hangfire-mediatr:", options.RedisKeyPrefix);
        Assert.AreEqual(false, options.UseInMemoryCoordination);
        Assert.AreEqual(TimeSpan.FromMinutes(5), options.CleanupInterval);
        Assert.AreEqual(false, options.EnableDetailedLogging);
        Assert.AreEqual(Environment.ProcessorCount * 5, options.MaxConcurrentJobs);
        Assert.AreEqual(TimeSpan.FromHours(1), options.JobExecutionTimeout);
        Assert.AreEqual(false, options.AutoDeleteSuccessfulJobs);
        Assert.AreEqual(TimeSpan.FromDays(7), options.JobRetentionPeriod);
    }

    [TestMethod]
    public void Validate_WithValidDefaults_DoesNotThrow()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = true // This makes RedisConnectionString not required
        };

        // Act & Assert - Should not throw
        options.Validate();
    }

    [TestMethod]
    public void Validate_WithNegativeRetryAttempts_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            DefaultRetryAttempts = -1,
            UseInMemoryCoordination = true
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("Default retry attempts cannot be negative (Parameter 'DefaultRetryAttempts')", exception.Message);
    }

    [TestMethod]
    public void Validate_WithZeroOrNegativeTaskTimeout_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            DefaultTaskTimeout = TimeSpan.Zero,
            UseInMemoryCoordination = true
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("Default task timeout must be positive (Parameter 'DefaultTaskTimeout')", exception.Message);
    }

    [TestMethod]
    public void Validate_WithRedisCoordinationButNoConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            UseInMemoryCoordination = false,
            RedisConnectionString = null
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("Redis connection string is required when not using in-memory coordination (Parameter 'RedisConnectionString')", exception.Message);
    }

    [TestMethod]
    public void Validate_WithEmptyRedisKeyPrefix_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            RedisKeyPrefix = string.Empty,
            UseInMemoryCoordination = true
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("Redis key prefix cannot be null or empty (Parameter 'RedisKeyPrefix')", exception.Message);
    }

    [TestMethod]
    public void Validate_WithZeroMaxConcurrentJobs_ThrowsArgumentException()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            MaxConcurrentJobs = 0,
            UseInMemoryCoordination = true
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => options.Validate());
        Assert.AreEqual("Max concurrent jobs must be positive (Parameter 'MaxConcurrentJobs')", exception.Message);
    }
}
