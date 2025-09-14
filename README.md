# MediatR.Extensions.Hangfire

[![NuGet](https://img.shields.io/nuget/v/MediatR.Extensions.Hangfire.svg)](https://www.nuget.org/packages/MediatR.Extensions.Hangfire/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MediatR.Extensions.Hangfire.svg)](https://www.nuget.org/packages/MediatR.Extensions.Hangfire/)

A comprehensive, production-ready library that provides seamless integration between MediatR's CQRS pattern and Hangfire's robust background job processing capabilities.

## 🚀 Getting Started

### 1. Install the Package

```xml
<PackageReference Include="MediatR.Extensions.Hangfire" Version="1.0.x" />
```

### 2. Add Using Statement

```csharp
using MediatR.Extensions.Hangfire.Extensions;
```

### 3. Configure Services

```csharp
// Configure MediatR
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Configure Hangfire with your preferred storage
services.AddHangfire(config => config.UseSqlServer("connection-string"));
services.AddHangfireServer();

// Add MediatR-Hangfire integration (AFTER other services)
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection-string");  // For distributed coordination
    options.WithRetryAttempts(3);
    options.WithConsoleLogging(true);
    options.WithDetailedLogging(true);
});
```

### 4. Initialize Service Locator (After Building App)

```csharp
var app = builder.Build();

// IMPORTANT: Initialize the service locator for MediatR-Hangfire integration
using (var scope = app.Services.CreateScope())
{
    var serviceLocatorSetup = scope.ServiceProvider.GetRequiredService<MediatR.Extensions.Hangfire.Extensions.IServiceLocatorSetup>();
    serviceLocatorSetup.Setup(app.Services);

    // Configure Hangfire for MediatR integration
    var hangfireConfigurator = scope.ServiceProvider.GetRequiredService<MediatR.Extensions.Hangfire.Extensions.IHangfireMediatorConfigurator>();
    hangfireConfigurator.Configure();
}

// Continue with your app configuration...
app.Run();
```

### 5. Use in Your Controllers

public class UserController : ControllerBase
{
private readonly IMediator \_mediator;

    // Fire-and-forget
    [HttpPost("async")]
    public IActionResult CreateUserAsync(CreateUserCommand command)
    {
        _mediator.Enqueue("Create User", command);
        return Accepted();
    }

    // With return value
    [HttpPost("with-result")]
    public async Task<IActionResult> CreateUserWithResult(CreateUserCommand command)
    {
        var result = await _mediator.EnqueueAsync("Create User", command, retryAttempts: 2);
        return Ok(result);
    }

    // Scheduled
    [HttpPost("schedule-reminder")]
    public IActionResult ScheduleReminder(SendEmailCommand command)
    {
        var jobId = _mediator.Schedule("Send Reminder", command, TimeSpan.FromHours(24));
        return Ok(new { jobId });
    }

    // Recurring
    [HttpPost("setup-daily-cleanup")]
    public IActionResult SetupDailyCleanup()
    {
        _mediator.AddOrUpdate("Daily Cleanup", new CleanupCommand(), Cron.Daily(2, 0));
        return Ok();
    }

}

````

## ⚙️ Configuration Options

```csharp
services.AddHangfireMediatR(options =>
{
    // Coordination Strategy (choose one)
    options.UseRedis("localhost:6379");        // For distributed/multi-instance apps
    options.UseInMemory();                     // For single-instance apps

    // Job Behavior
    options.WithRetryAttempts(3);                           // Default retry attempts
    options.WithTaskTimeout(TimeSpan.FromMinutes(30));      // Job timeout
    options.WithMaxConcurrentJobs(20);                      // Concurrent job limit

    // Monitoring & Logging
    options.WithConsoleLogging(true);                       // Enable Hangfire console logs
    options.WithDetailedLogging(true);                      // Detailed execution logs

    // Cleanup
    options.WithJobCleanup(autoDelete: true, TimeSpan.FromDays(7));  // Auto-cleanup completed jobs
});
````

## 🎯 Key Features

- **🔥 Fire-and-Forget Jobs**: `_mediator.Enqueue("Job Name", command)`
- **📤 Jobs with Return Values**: `await _mediator.EnqueueAsync("Job Name", command)`
- **⏰ Scheduled Jobs**: `_mediator.Schedule("Job Name", command, TimeSpan.FromHours(1))`
- **🔄 Recurring Jobs**: `_mediator.AddOrUpdate("Job Name", command, Cron.Daily())`
- **🌐 Distributed Coordination**: Redis-based coordination for multi-instance deployments
- **🔁 Automatic Retries**: Configurable retry logic with exponential backoff
- **📊 Rich Monitoring**: Full integration with Hangfire dashboard
- **🎯 Type Safety**: Strongly-typed commands and queries
- **⚡ High Performance**: Optimized for high-throughput scenarios

## 🏗️ Architecture

Perfect for **distributed microservices** where you need:

- **API containers** that handle HTTP requests and enqueue jobs
- **Worker containers** that process background jobs
- **Redis coordination** for job distribution and result coordination
- **Hangfire dashboard** for monitoring and management

## 📚 Examples

Check out the [comprehensive example project](./example/) that demonstrates:

- Distributed processing with .NET Aspire
- Multiple worker containers with different specializations
- Real-world usage patterns and best practices
- Performance testing and monitoring

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

MIT License - See [LICENSE](./LICENSE) file for details.
