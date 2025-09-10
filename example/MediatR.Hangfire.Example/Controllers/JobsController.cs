using Microsoft.AspNetCore.Mvc;
using MediatR;
using MediatR.Hangfire.Example.Commands;
using MediatR.Hangfire.Extensions.Extensions;
using Hangfire;

namespace MediatR.Hangfire.Example.Controllers;

/// <summary>
/// Controller for managing background jobs and demonstrating various job patterns
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IMediator mediator, ILogger<JobsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Triggers a manual cleanup job
    /// </summary>
    [HttpPost("cleanup")]
    public IActionResult TriggerCleanup([FromQuery] int maxAgeDays = 30, [FromQuery] string? category = null)
    {
        var command = new CleanupCommand
        {
            MaxAge = TimeSpan.FromDays(maxAgeDays),
            Category = category
        };

        _mediator.Enqueue("Manual Cleanup", command);
        
        return Ok(new { message = "Cleanup job has been queued", maxAgeDays, category });
    }

    /// <summary>
    /// Sends a test email to verify email functionality
    /// </summary>
    [HttpPost("test-email")]
    public IActionResult SendTestEmail([FromQuery] string email = "test@example.com")
    {
        var command = new SendEmailCommand
        {
            To = email,
            Subject = "Test Email from MediatR.Hangfire.Example",
            Body = "This is a test email sent via background job processing. If you received this, the integration is working correctly!",
            FromName = "Test System"
        };

        _mediator.Enqueue("Test Email", command);
        
        return Ok(new { message = "Test email has been queued", email });
    }

    /// <summary>
    /// Creates multiple jobs to demonstrate concurrent processing
    /// </summary>
    [HttpPost("bulk-test")]
    public IActionResult CreateBulkJobs([FromQuery] int count = 10)
    {
        _logger.LogInformation("Creating {Count} bulk test jobs", count);

        for (int i = 1; i <= count; i++)
        {
            var command = new SendEmailCommand
            {
                To = $"user{i}@example.com",
                Subject = $"Bulk Test Email #{i}",
                Body = $"This is bulk test email number {i} of {count}",
                FromName = "Bulk Test System"
            };

            _mediator.Enqueue($"Bulk Email #{i}", command);
        }

        return Ok(new { message = $"{count} bulk test jobs have been queued" });
    }

    /// <summary>
    /// Demonstrates job scheduling with different delays
    /// </summary>
    [HttpPost("schedule-demo")]
    public IActionResult ScheduleDemo()
    {
        // Schedule jobs at different intervals
        var schedules = new[]
        {
            (TimeSpan.FromSeconds(30), "30 seconds"),
            (TimeSpan.FromMinutes(2), "2 minutes"),
            (TimeSpan.FromMinutes(5), "5 minutes")
        };

        var jobIds = new List<object>();

        foreach (var (delay, description) in schedules)
        {
            var command = new SendEmailCommand
            {
                To = "scheduled-test@example.com",
                Subject = $"Scheduled Email - {description}",
                Body = $"This email was scheduled to be sent after {description}",
                FromName = "Scheduler"
            };

            var jobId = _mediator.Schedule($"Scheduled Email ({description})", command, delay);
            jobIds.Add(new { jobId, delay = description, scheduledFor = DateTime.UtcNow.Add(delay) });
        }

        return Ok(new { message = "Demo jobs scheduled", jobs = jobIds });
    }

    /// <summary>
    /// Sets up various recurring job examples
    /// </summary>
    [HttpPost("setup-recurring-examples")]
    public IActionResult SetupRecurringExamples()
    {
        // Every 5 minutes cleanup
        _mediator.AddOrUpdate(
            "Frequent Cleanup",
            new CleanupCommand { MaxAge = TimeSpan.FromHours(1), Category = "temp" },
            "*/5 * * * *");

        // Daily email summary at 9 AM
        _mediator.AddOrUpdate(
            "Daily Email Summary",
            new SendEmailCommand
            {
                To = "admin@example.com",
                Subject = "Daily Summary",
                Body = "Daily summary email",
                FromName = "System"
            },
            Cron.Daily(9));

        // Weekly cleanup on Sundays at 2 AM
        _mediator.AddOrUpdate(
            "Weekly Deep Cleanup",
            new CleanupCommand { MaxAge = TimeSpan.FromDays(7) },
            Cron.Weekly(DayOfWeek.Sunday, 2));

        return Ok(new { message = "Recurring job examples have been set up" });
    }

    /// <summary>
    /// Removes all example recurring jobs
    /// </summary>
    [HttpDelete("cleanup-recurring-examples")]
    public IActionResult CleanupRecurringExamples()
    {
        var jobsToRemove = new[]
        {
            "Frequent Cleanup",
            "Daily Email Summary", 
            "Weekly Deep Cleanup"
        };

        foreach (var jobName in jobsToRemove)
        {
            _mediator.RemoveRecurringJob(jobName);
        }

        return Ok(new { message = "Example recurring jobs have been removed", removedJobs = jobsToRemove });
    }

    /// <summary>
    /// Gets information about the Hangfire dashboard
    /// </summary>
    [HttpGet("dashboard-info")]
    public IActionResult GetDashboardInfo()
    {
        var info = new
        {
            dashboardUrl = "/hangfire",
            message = "Access the Hangfire dashboard to monitor jobs",
            features = new[]
            {
                "Real-time job monitoring",
                "Job execution history",
                "Console output from jobs",
                "Retry failed jobs manually",
                "Manage recurring jobs",
                "View job statistics"
            }
        };

        return Ok(info);
    }
}
