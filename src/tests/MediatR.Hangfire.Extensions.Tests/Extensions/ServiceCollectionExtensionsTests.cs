using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MediatR.Hangfire.Extensions.Configuration;
using MediatR.Hangfire.Extensions.Extensions;

namespace MediatR.Hangfire.Extensions.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddHangfireMediatR_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            ((IServiceCollection)null!).AddHangfireMediatR(options => options.UseInMemory()));
        Assert.AreEqual("services", exception.ParamName);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            services.AddHangfireMediatR((Action<HangfireMediatorOptionsBuilder>)null!));
        Assert.AreEqual("configure", exception.ParamName);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithInMemoryConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR(options => options.UseInMemory());

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        Assert.IsNotNull(optionsSnapshot);
        
        var options = optionsSnapshot.Value;
        Assert.IsTrue(options.UseInMemoryCoordination);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithDefaultConfiguration_UsesInMemory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHangfireMediatR();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetService<IOptionsSnapshot<HangfireMediatorOptions>>();
        Assert.IsNotNull(optionsSnapshot);
        
        var options = optionsSnapshot.Value;
        Assert.IsTrue(options.UseInMemoryCoordination);
    }

    [TestMethod]
    public void AddHangfireMediatR_WithInvalidConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            services.AddHangfireMediatR(options => 
            {
                options.WithRetryAttempts(-1); // Invalid
            }));
    }
}

[TestClass]
public class HangfireMediatorConfiguratorTests
{
    [TestMethod]
    public void Constructor_WithOptions_DoesNotThrow()
    {
        // Arrange
        var options = new HangfireMediatorOptions();

        // Act & Assert - Should not throw
        var configurator = new HangfireMediatorConfigurator(options);
        Assert.IsNotNull(configurator);
    }

    [TestMethod]
    public void Configure_DoesNotThrow()
    {
        // Arrange
        var options = new HangfireMediatorOptions();
        var configurator = new HangfireMediatorConfigurator(options);

        // Act & Assert - Should not throw
        configurator.Configure();
    }
}

[TestClass]
public class ServiceLocatorSetupTests
{
    [TestMethod]
    public void Setup_WithServiceProvider_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var setup = new ServiceLocatorSetup();

        // Act & Assert - Should not throw
        setup.Setup(serviceProvider);
        
        // Verify it was set
        Assert.AreSame(serviceProvider, ServiceLocator.Current);
        
        // Cleanup
        ServiceLocator.Current = null;
    }
}
