# MediatR.Hangfire.Extensions

A comprehensive, production-ready library that provides seamless integration between MediatR's CQRS pattern and Hangfire's robust background job processing capabilities.

## 🌟 What Makes This Special

This library solves a unique problem in the .NET ecosystem: **getting return values from background jobs**. While most job queue systems are fire-and-forget, this integration allows you to:

- Execute MediatR requests in the background **and get results back**
- Maintain type safety with existing MediatR request/response patterns
- Use familiar MediatR APIs with zero learning curve
- Scale from single-instance to distributed deployments seamlessly

## 🚀 Features

### Core Capabilities

- **🔥 Fire-and-forget jobs** - Execute commands without blocking
- **📤 Jobs with return values** - Get results from background processing
- **⏰ Scheduled jobs** - Execute at specific times or after delays
- **🔄 Recurring jobs** - Cron-based recurring schedules
- **🔁 Retry logic** - Configurable retry attempts with exponential backoff
- **📊 Enhanced logging** - Automatic Hangfire dashboard console integration

### Architecture Benefits

- **🏗️ Clean separation of concerns** - Bridge, coordination, extension, and logging layers
- **🔧 Flexible coordination** - Redis for distributed, in-memory for single-instance
- **⚡ Type-safe** - Leverages existing MediatR contracts
- **🎯 Production-ready** - Comprehensive error handling and monitoring

## 📁 Project Structure

```
/src/MediatR.Hangfire.Extensions/
├── Bridge/                    # Job execution bridge
│   ├── IMediatorJobBridge.cs
│   └── MediatorJobBridge.cs
├── Coordination/              # Task result coordination
│   ├── ITaskCoordinator.cs
│   ├── RedisTaskCoordinator.cs
│   └── InMemoryTaskCoordinator.cs
├── Extensions/                # Clean API surface
│   ├── MediatorExtensions.cs
│   └── ServiceCollectionExtensions.cs
├── Logging/                   # Hangfire console integration
│   ├── HangfireConsoleLogger.cs
│   └── HangfireConsoleFilter.cs
└── Configuration/             # Options and settings
    └── HangfireMediatorOptions.cs

/example/MediatR.Hangfire.Example/  # .NET 9 demo application
```

## 🎯 Usage Examples

### Quick Start

```csharp
// 1. Configure services
services.AddMediatR(typeof(CreateUserCommand));
services.AddHangfire(config => config.UseSqlServer("..."));
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection");
    options.WithRetryAttempts(3);
    options.WithConsoleLogging(true);
});

// 2. Use in controllers
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

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
```

### Configuration Options

```csharp
services.AddHangfireMediatR(options =>
{
    // Coordination strategy
    options.UseRedis("localhost:6379");        // Distributed
    options.UseInMemory();                     // Single instance

    // Job behavior
    options.WithRetryAttempts(3);
    options.WithTaskTimeout(TimeSpan.FromMinutes(30));
    options.WithMaxConcurrentJobs(20);

    // Monitoring
    options.WithConsoleLogging(true);
    options.WithDetailedLogging(true);

    // Cleanup
    options.WithJobCleanup(autoDelete: true, TimeSpan.FromDays(7));
});
```

## 🔧 Architecture Deep Dive

### 1. Bridge Layer (`IMediatorJobBridge`)

- **Purpose**: Execute MediatR requests within Hangfire job context
- **Features**: Retry logic, error handling, performance logging
- **Supports**: Commands, queries, and notifications

### 2. Coordination Layer (`ITaskCoordinator`)

- **Purpose**: Manage async task completion across processes
- **Redis Implementation**: Distributed coordination with pub/sub
- **InMemory Implementation**: Single-process coordination
- **Features**: Timeout handling, cleanup, fault tolerance

### 3. Extension Layer (`MediatorExtensions`)

- **Purpose**: Clean, familiar API surface for consumers
- **Methods**: `Enqueue`, `EnqueueAsync`, `Schedule`, `AddOrUpdate`
- **Overloads**: Support for both `IRequest` and `IRequest<T>`

### 4. Logging Layer

- **`HangfireConsoleLogger`**: Dual logging to app logs + Hangfire console
- **`HangfireConsoleFilter`**: Job context management
- **Features**: Colored output, structured logging, error details

## 🎨 Example Application

The `/example` directory contains a comprehensive .NET 9 Web API demonstrating:

- **Multiple job patterns** (fire-and-forget, with results, scheduled, recurring)
- **Real-world scenarios** (user creation, email sending, report generation)
- **Error handling** (simulated failures, retry logic)
- **Monitoring** (Hangfire dashboard integration)

### Running the Example

```bash
cd example/MediatR.Hangfire.Example
dotnet run
```

Visit:

- **API**: https://localhost:7000
- **Swagger**: https://localhost:7000/swagger
- **Hangfire Dashboard**: https://localhost:7000/hangfire

## 🎯 Use Cases

### Background Processing

```csharp
// Send welcome email without blocking user registration
_mediator.Enqueue("Welcome Email", new SendWelcomeEmailCommand { UserId = user.Id });
```

### Long-Running Operations

```csharp
// Generate large report in background, get result when done
var report = await _mediator.EnqueueAsync("Monthly Report", new GenerateReportCommand
{
    StartDate = DateTime.UtcNow.AddDays(-30),
    EndDate = DateTime.UtcNow
});
```

### Scheduled Tasks

```csharp
// Send payment reminder 24 hours after due date
_mediator.Schedule("Payment Reminder", new SendReminderCommand { UserId = user.Id },
    TimeSpan.FromDays(1));
```

### Recurring Jobs

```csharp
// Daily cleanup at 2 AM
_mediator.AddOrUpdate("Daily Cleanup", new CleanupCommand(), "0 2 * * *");
```

## 🔍 Monitoring & Observability

### Hangfire Dashboard

- **Real-time job monitoring** - Track progress and completion
- **Console output** - View logs directly in dashboard
- **Retry management** - Monitor and manually retry failed jobs
- **Performance metrics** - Execution times and throughput
- **Recurring job management** - Schedule and manage recurring tasks

### Logging Integration

- **Structured logging** with job context
- **Automatic console output** in Hangfire dashboard
- **Error tracking** with full stack traces
- **Performance monitoring** with execution timings

## 🚀 Production Deployment

### Recommended Configuration

```csharp
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection-string");
    options.WithRetryAttempts(5);
    options.WithTaskTimeout(TimeSpan.FromHours(1));
    options.WithMaxConcurrentJobs(Environment.ProcessorCount * 10);
    options.WithJobExecutionTimeout(TimeSpan.FromHours(2));
    options.WithJobCleanup(autoDelete: true, TimeSpan.FromDays(30));
    options.WithConsoleLogging(true);
});
```

### Scaling Considerations

- **Redis coordination** for multi-server deployments
- **Connection pooling** for high-throughput scenarios
- **Job partitioning** by priority or category
- **Resource monitoring** (CPU, memory, Redis connections)

## 📄 License

MIT License - See LICENSE file for details.
