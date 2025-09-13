# MediatR.Hangfire.Extensions

A comprehensive, production-ready library that provides seamless integration between MediatR's CQRS pattern and Hangfire's robust background job processing capabilities.

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

## Development Flow

### Branch Strategy

- **main** - Production releases (auto-publishes to NuGet)
- **develop** - Integration branch for features
- **feature/\*** - New features and improvements
- **hotfix/\*** - Critical production fixes

### Workflow

1. **Feature Development**: Create `feature/your-feature` from `develop`
2. **Pull Request**: Open PR to `develop` (triggers full CI: tests, coverage, quality checks)
3. **Integration**: Merge approved PRs to `develop` (creates alpha versions)
4. **Release**: PR from `develop` to `main` (publishes to NuGet automatically)

### Versioning

- **main**: `1.0.0` (production)
- **develop**: `1.1.0-alpha.1` (pre-release)
- **feature**: `1.1.0-alpha.2` (development)
- **PR**: `1.1.0-PullRequest.123` (review)

### Quality Gates

All PRs must pass: code formatting, security scans, and coverage thresholds.

## License

MIT License - See LICENSE file for details.
