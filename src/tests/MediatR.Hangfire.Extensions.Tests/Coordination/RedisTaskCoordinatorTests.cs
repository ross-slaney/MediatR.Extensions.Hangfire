using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using MediatR.Hangfire.Extensions.Configuration;
using MediatR.Hangfire.Extensions.Coordination;

namespace MediatR.Hangfire.Extensions.Tests.Coordination;

[TestClass]
public class RedisTaskCoordinatorTests
{
    [TestMethod]
    public void Constructor_WithNullRedis_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<RedisTaskCoordinator>.Instance;
        var options = Options.Create(new HangfireMediatorOptions { RedisConnectionString = "localhost" });

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new RedisTaskCoordinator(null!, logger, options));
        Assert.AreEqual("redis", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRedis = CreateMockRedis();
        var options = Options.Create(new HangfireMediatorOptions { RedisConnectionString = "localhost" });

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new RedisTaskCoordinator(mockRedis, null!, options));
        Assert.AreEqual("logger", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRedis = CreateMockRedis();
        var logger = NullLogger<RedisTaskCoordinator>.Instance;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new RedisTaskCoordinator(mockRedis, logger, null!));
        Assert.AreEqual("options", exception.ParamName);
    }

    private static IConnectionMultiplexer CreateMockRedis()
    {
        // Use a simple mock that just satisfies the constructor requirements
        var mockDatabase = new Moq.Mock<IDatabase>();
        var mockSubscriber = new Moq.Mock<ISubscriber>();
        var mockRedis = new Moq.Mock<IConnectionMultiplexer>();
        
        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
        mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);
        
        return mockRedis.Object;
    }
}
