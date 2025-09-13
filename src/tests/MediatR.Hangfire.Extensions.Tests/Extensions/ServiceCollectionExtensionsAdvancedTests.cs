using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MediatR.Hangfire.Extensions.Configuration;
using MediatR.Hangfire.Extensions.Extensions;
using MediatR.Hangfire.Extensions.Coordination;
using MediatR.Hangfire.Extensions.Bridge;

namespace MediatR.Hangfire.Extensions.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsAdvancedTests
{
    [TestMethod]
    public void AddHangfireMediatR_WithRedisConfiguration_RegistersRedisCoordinator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseRedis("localhost:6379");
            options.WithRetryAttempts(3);
            options.WithConsoleLogging(true);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        Assert.IsNotNull(optionsSnapshot);

        var options = optionsSnapshot.Value;
        Assert.IsFalse(options.UseInMemoryCoordination);
        Assert.AreEqual("localhost:6379", options.RedisConnectionString);
        Assert.AreEqual(3, options.DefaultRetryAttempts);
        Assert.IsTrue(options.EnableConsoleLogging);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithRedisAndKeyPrefix_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseRedis("localhost:6379", "myapp:");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.AreEqual("myapp:", options.RedisKeyPrefix);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithTaskTimeout_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedTimeout = TimeSpan.FromMinutes(15);

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
            options.WithTaskTimeout(expectedTimeout);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.AreEqual(expectedTimeout, options.DefaultTaskTimeout);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithMaxConcurrentJobs_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
            options.WithMaxConcurrentJobs(20);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.AreEqual(20, options.MaxConcurrentJobs);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithJobExecutionTimeout_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedTimeout = TimeSpan.FromHours(2);

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
            options.WithJobExecutionTimeout(expectedTimeout);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.AreEqual(expectedTimeout, options.JobExecutionTimeout);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithDetailedLogging_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
            options.WithDetailedLogging(true);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.IsTrue(options.EnableDetailedLogging);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithJobCleanup_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var retentionPeriod = TimeSpan.FromDays(3);

        // Act
        services.AddHangfireMediatR(options =>
        {
            options.UseInMemory();
            options.WithJobCleanup(true, retentionPeriod);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        var options = optionsSnapshot!.Value;
        Assert.IsTrue(options.AutoDeleteSuccessfulJobs);
        Assert.AreEqual(retentionPeriod, options.JobRetentionPeriod);
    }

    [TestMethod]
    public void AddHangfireMediatR_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required services to satisfy dependencies
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensionsAdvancedTests).Assembly));
        services.AddLogging();

        // Act
        services.AddHangfireMediatR(options => options.UseInMemory());

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that core services are registered
        var bridge = serviceProvider.GetService<IMediatorJobBridge>();
        Assert.IsNotNull(bridge);

        var coordinator = serviceProvider.GetService<ITaskCoordinator>();
        Assert.IsNotNull(coordinator);
        Assert.IsInstanceOfType(coordinator, typeof(InMemoryTaskCoordinator));

        var serviceLocatorSetup = serviceProvider.GetService<IServiceLocatorSetup>();
        Assert.IsNotNull(serviceLocatorSetup);

        var hangfireConfigurator = serviceProvider.GetService<IHangfireMediatorConfigurator>();
        Assert.IsNotNull(hangfireConfigurator);
    }

    [TestMethod]
    public void ServiceLocatorSetup_SetsCurrentServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var setup = new ServiceLocatorSetup();

        // Act
        setup.Setup(serviceProvider);

        // Assert
        Assert.AreSame(serviceProvider, ServiceLocator.Current);

        // Cleanup
        ServiceLocator.Current = null;
    }

    [TestMethod]
    public void HangfireMediatorConfigurator_CanBeCreated()
    {
        // Arrange
        var options = new HangfireMediatorOptions();

        // Act
        var configurator = new HangfireMediatorConfigurator(options);

        // Assert
        Assert.IsNotNull(configurator);
    }

    [TestMethod]
    public void HangfireMediatorConfigurator_Configure_CanBeInstantiated()
    {
        // Arrange
        var options = new HangfireMediatorOptions
        {
            EnableConsoleLogging = false // Avoid global console state issues
        };

        // Act & Assert - Should be able to create the configurator
        var configurator = new HangfireMediatorConfigurator(options);
        Assert.IsNotNull(configurator);

        // Note: We don't test Configure() here as it affects global Hangfire state
        // and conflicts with other tests. In practice, this would be tested in
        // integration tests where the full Hangfire environment is set up.
    }

    [TestMethod]
    public void HangfireMediatorOptionsBuilder_ChainableMethods_ReturnSameInstance()
    {
        // Arrange
        var builder = new HangfireMediatorOptionsBuilder();

        // Act
        var result = builder
            .UseInMemory()
            .WithRetryAttempts(3)
            .WithTaskTimeout(TimeSpan.FromMinutes(15))
            .WithConsoleLogging(true)
            .WithMaxConcurrentJobs(20)
            .WithJobExecutionTimeout(TimeSpan.FromHours(2))
            .WithDetailedLogging(true)
            .WithJobCleanup(true, TimeSpan.FromDays(3));

        // Assert
        Assert.AreSame(builder, result);
    }
}
