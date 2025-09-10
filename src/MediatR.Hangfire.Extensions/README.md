# MediatR.Hangfire.Extensions

Background processing for MediatR commands with return values using Hangfire. This library provides seamless integration between MediatR's CQRS pattern and Hangfire's robust job processing capabilities.

## ✨ Features

- **Fire-and-forget jobs** - Execute MediatR requests asynchronously without blocking
- **Jobs with return values** - Get results from background jobs (unique among job queue systems)
- **Scheduled jobs** - Execute jobs at specific times or after delays
- **Recurring jobs** - Set up cron-based recurring job schedules
- **Retry logic** - Configurable retry attempts for failed jobs
- **Enhanced logging** - Automatic logging to Hangfire dashboard console
- **Distributed coordination** - Redis-based task coordination for multi-server deployments
- **Type safety** - Leverages existing MediatR request/response patterns

## 🚀 Quick Start

### 1. Installation

```bash
Install-Package MediatR.Hangfire.Extensions
```

### 2. Configuration

```csharp
// Program.cs or Startup.cs
services.AddMediatR(typeof(CreateUserCommand));
services.AddHangfire(config => config.UseSqlServer("connection-string"));

// Add the integration
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection-string");
    // OR for single-instance deployments:
    // options.UseInMemory();

    options.WithRetryAttempts(3);
    options.WithConsoleLogging(true);
});
```

### 3. Usage

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    // Fire and forget
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserCommand command)
    {
        _mediator.Enqueue("Create User", command);
        return Accepted();
    }

    // With return value
    [HttpPost("users/validated")]
    public async Task<IActionResult> CreateValidatedUser(CreateUserCommand command)
    {
        var result = await _mediator.EnqueueAsync("Create User", command);
        return Ok(result);
    }

    // Scheduled
    [HttpPost("cleanup")]
    public IActionResult ScheduleCleanup()
    {
        _mediator.Schedule("Daily Cleanup", new CleanupCommand(), TimeSpan.FromHours(24));
        return Ok();
    }

    // Recurring
    [HttpPost("setup-recurring")]
    public IActionResult SetupRecurring()
    {
        _mediator.AddOrUpdate("Hourly Reports", new GenerateReportCommand(), Cron.Hourly);
        return Ok();
    }
}
```

## 📋 API Reference

### Extension Methods

#### Fire-and-Forget Jobs

```csharp
void Enqueue(string jobName, IRequest request)
```

Executes a MediatR request in the background without blocking the caller.

#### Jobs with Return Values

```csharp
Task<TResponse> EnqueueAsync<TResponse>(string jobName, IRequest<TResponse> request, int retryAttempts = 0)
```

Executes a MediatR request in the background and returns the result asynchronously.

#### Notifications

```csharp
void EnqueueNotification(string jobName, INotification notification)
```

Publishes a MediatR notification to all handlers in the background.

#### Scheduled Jobs

```csharp
string Schedule(string jobName, IRequest request, TimeSpan delay)
string Schedule(string jobName, IRequest request, DateTimeOffset enqueueAt)
```

Schedules a job to run after a delay or at a specific time.

#### Recurring Jobs

```csharp
void AddOrUpdate(string jobName, IRequest request, string cronExpression, TimeZoneInfo? timeZone = null)
void TriggerRecurringJob(string jobName)
void RemoveRecurringJob(string jobName)
```

Manages recurring jobs with cron-based scheduling.

### Configuration Options

```csharp
services.AddHangfireMediatR(options =>
{
    // Coordination strategy
    options.UseRedis("connection-string");     // For distributed deployments
    options.UseInMemory();                     // For single-instance deployments

    // Job behavior
    options.WithRetryAttempts(3);              // Default retry attempts
    options.WithTaskTimeout(TimeSpan.FromMinutes(30)); // Async job timeout
    options.WithMaxConcurrentJobs(20);         // Concurrent job limit

    // Logging and monitoring
    options.WithConsoleLogging(true);          // Hangfire dashboard logging
    options.WithDetailedLogging(true);         // Debug information

    // Cleanup
    options.WithJobCleanup(true, TimeSpan.FromDays(7)); // Auto-cleanup
});
```

## 🏗️ Architecture

The library consists of several key components:

### Bridge Layer

- **IMediatorJobBridge** - Executes MediatR requests within Hangfire jobs
- **MediatorJobBridge** - Implementation with retry logic and error handling

### Coordination Layer

- **ITaskCoordinator** - Manages async task completion across processes
- **RedisTaskCoordinator** - Redis-based coordination for distributed scenarios
- **InMemoryTaskCoordinator** - In-memory coordination for single-instance scenarios

### Extension Layer

- **MediatorExtensions** - Clean API surface for consumers
- **ServiceCollectionExtensions** - DI configuration and setup

### Logging Layer

- **HangfireConsoleLogger** - Dual logging to regular logs and Hangfire console
- **HangfireConsoleFilter** - Job context management for console access

## 🔧 Advanced Configuration

### Custom Redis Configuration

```csharp
services.AddHangfireMediatR(options =>
{
    options.UseRedis("localhost:6379", keyPrefix: "myapp:");
});
```

### Production Settings

```csharp
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection-string");
    options.WithRetryAttempts(5);
    options.WithTaskTimeout(TimeSpan.FromHours(1));
    options.WithMaxConcurrentJobs(Environment.ProcessorCount * 10);
    options.WithJobExecutionTimeout(TimeSpan.FromHours(2));
    options.WithJobCleanup(autoDelete: true, TimeSpan.FromDays(30));
});
```

### Development Settings

```csharp
services.AddHangfireMediatR(options =>
{
    options.UseInMemory();
    options.WithConsoleLogging(true);
    options.WithDetailedLogging(true);
});
```

## 🎯 Use Cases

### Background Processing

```csharp
// Send emails without blocking the response
_mediator.Enqueue("Welcome Email", new SendWelcomeEmailCommand { UserId = user.Id });
```

### Long-Running Operations

```csharp
// Process large datasets with result
var report = await _mediator.EnqueueAsync("Generate Report", new GenerateReportCommand
{
    StartDate = DateTime.UtcNow.AddDays(-30),
    EndDate = DateTime.UtcNow
});
```

### Scheduled Tasks

```csharp
// Send reminder 24 hours later
_mediator.Schedule("Payment Reminder", new SendReminderCommand { UserId = user.Id }, TimeSpan.FromDays(1));
```

### Recurring Jobs

```csharp
// Daily cleanup at 2 AM
_mediator.AddOrUpdate("Daily Cleanup", new CleanupCommand(), "0 2 * * *");
```

## 🔍 Monitoring

The library integrates with Hangfire's dashboard for comprehensive monitoring:

- **Job Status** - Track job progress and completion
- **Console Output** - View real-time job logs
- **Retry History** - Monitor failed jobs and retry attempts
- **Performance Metrics** - Job execution times and throughput
- **Recurring Jobs** - Manage and monitor scheduled jobs

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
