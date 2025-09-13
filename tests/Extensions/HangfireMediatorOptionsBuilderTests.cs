using MediatR.Hangfire.Extensions.Extensions;

namespace MediatR.Hangfire.Extensions.Tests.Extensions;

[TestClass]
public class HangfireMediatorOptionsBuilderTests
{
    [TestMethod]
    public void UseRedis_WithValidConnectionString_ConfiguresOptions()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();
        var connectionString = "localhost:6379";

        // Act
        var result = builder.UseRedis(connectionString);
        var options = GetBuiltOptions(builder);

        // Assert
        Assert.AreSame(builder, result); // Should return same instance for chaining
        Assert.AreEqual(false, options.UseInMemoryCoordination);
        Assert.AreEqual(connectionString, options.RedisConnectionString);
    }

    [TestMethod]
    public void UseRedis_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => builder.UseRedis(null!));
        Assert.AreEqual("Connection string cannot be null or empty (Parameter 'connectionString')", exception.Message);
    }

    [TestMethod]
    public void UseRedis_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => builder.UseRedis(string.Empty));
        Assert.AreEqual("Connection string cannot be null or empty (Parameter 'connectionString')", exception.Message);
    }

    [TestMethod]
    public void UseRedis_WithKeyPrefix_ConfiguresOptionsWithPrefix()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();
        var connectionString = "localhost:6379";
        var keyPrefix = "myapp:";

        // Act
        var result = builder.UseRedis(connectionString, keyPrefix);
        var options = GetBuiltOptions(builder);

        // Assert
        Assert.AreSame(builder, result); // Should return same instance for chaining
        Assert.AreEqual(false, options.UseInMemoryCoordination);
        Assert.AreEqual(connectionString, options.RedisConnectionString);
        Assert.AreEqual(keyPrefix, options.RedisKeyPrefix);
    }

    [TestMethod]
    public void UseInMemory_ConfiguresInMemoryCoordination()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act
        var result = builder.UseInMemory();
        var options = GetBuiltOptions(builder);

        // Assert
        Assert.AreSame(builder, result); // Should return same instance for chaining
        Assert.AreEqual(true, options.UseInMemoryCoordination);
        Assert.IsNull(options.RedisConnectionString);
    }

    [TestMethod]
    public void WithRetryAttempts_WithValidValue_ConfiguresRetryAttempts()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();
        var retryAttempts = 3;

        // Act
        var result = builder.WithRetryAttempts(retryAttempts);
        var options = GetBuiltOptions(builder);

        // Assert
        Assert.AreSame(builder, result); // Should return same instance for chaining
        Assert.AreEqual(retryAttempts, options.DefaultRetryAttempts);
    }

    [TestMethod]
    public void WithRetryAttempts_WithNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => builder.WithRetryAttempts(-1));
        Assert.AreEqual("Retry attempts cannot be negative (Parameter 'retryAttempts')", exception.Message);
    }

    [TestMethod]
    public void WithConsoleLogging_EnablesConsoleLogging()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act
        var result = builder.WithConsoleLogging(true);
        var options = GetBuiltOptions(builder);

        // Assert
        Assert.AreSame(builder, result); // Should return same instance for chaining
        Assert.AreEqual(true, options.EnableConsoleLogging);
    }

    // Helper method to access the internal Build method using reflection
    private static MediatR.Hangfire.Extensions.Configuration.HangfireMediatorOptions GetBuiltOptions(HangfireMediatorOptionsBuilder builder)
    {
        var buildMethod = typeof(HangfireMediatorOptionsBuilder).GetMethod("Build",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (buildMethod == null)
            throw new InvalidOperationException("Build method not found");

        var options = buildMethod.Invoke(builder, null);
        return (MediatR.Hangfire.Extensions.Configuration.HangfireMediatorOptions)options!;
    }
}
