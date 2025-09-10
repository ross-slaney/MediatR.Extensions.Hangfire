using Microsoft.AspNetCore.Mvc;
using MediatR;
using MediatR.Hangfire.Example.Models;
using MediatR.Hangfire.Example.Queries;
using MediatR.Hangfire.Extensions.Extensions;
using Hangfire;

namespace MediatR.Hangfire.Example.Controllers;

/// <summary>
/// Controller demonstrating report generation with background processing
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IMediator mediator, ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generates a report synchronously
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<Report>> GenerateReportSync([FromBody] GenerateReportCommand command)
    {
        _logger.LogInformation("Generating {ReportType} report synchronously", command.ReportType);
        
        var report = await _mediator.Send(command);
        return Ok(report);
    }

    /// <summary>
    /// Generates a report asynchronously and returns the result
    /// Useful for long-running report generation
    /// </summary>
    [HttpPost("async")]
    public async Task<ActionResult<Report>> GenerateReportAsync([FromBody] GenerateReportCommand command)
    {
        _logger.LogInformation("Generating {ReportType} report asynchronously", command.ReportType);
        
        try
        {
            var report = await _mediator.EnqueueAsync("Generate Report", command, retryAttempts: 1);
            return Ok(report);
        }
        catch (TimeoutException)
        {
            return StatusCode(408, new { message = "Report generation timed out" });
        }
    }

    /// <summary>
    /// Queues a report generation job (fire-and-forget)
    /// </summary>
    [HttpPost("queue")]
    public IActionResult QueueReportGeneration([FromBody] GenerateReportCommand command)
    {
        _logger.LogInformation("Queueing {ReportType} report generation", command.ReportType);
        
        _mediator.Enqueue("Generate Report", command);
        
        return Accepted(new { message = "Report generation job has been queued" });
    }

    /// <summary>
    /// Schedules a report to be generated at a specific time
    /// </summary>
    [HttpPost("schedule")]
    public IActionResult ScheduleReportGeneration(
        [FromBody] GenerateReportCommand command,
        [FromQuery] DateTime scheduleFor)
    {
        _logger.LogInformation("Scheduling {ReportType} report for {ScheduleTime}", 
            command.ReportType, scheduleFor);
        
        var jobId = _mediator.Schedule("Generate Scheduled Report", command, new DateTimeOffset(scheduleFor));
        
        return Ok(new { message = "Report generation scheduled", jobId, scheduleFor });
    }

    /// <summary>
    /// Sets up a recurring report generation job
    /// </summary>
    [HttpPost("recurring")]
    public IActionResult SetupRecurringReport(
        [FromBody] GenerateReportCommand command,
        [FromQuery] string cronExpression = "0 6 * * *") // Default: daily at 6 AM
    {
        var jobName = $"Recurring {command.ReportType} Report";
        
        _logger.LogInformation("Setting up recurring {ReportType} report with cron: {Cron}", 
            command.ReportType, cronExpression);
        
        _mediator.AddOrUpdate(jobName, command, cronExpression);
        
        return Ok(new { message = "Recurring report job created", jobName, cronExpression });
    }

    /// <summary>
    /// Triggers an existing recurring job immediately
    /// </summary>
    [HttpPost("trigger/{jobName}")]
    public IActionResult TriggerRecurringJob(string jobName)
    {
        _logger.LogInformation("Triggering recurring job: {JobName}", jobName);
        
        _mediator.TriggerRecurringJob(jobName);
        
        return Ok(new { message = "Recurring job triggered", jobName });
    }

    /// <summary>
    /// Removes a recurring job
    /// </summary>
    [HttpDelete("recurring/{jobName}")]
    public IActionResult RemoveRecurringJob(string jobName)
    {
        _logger.LogInformation("Removing recurring job: {JobName}", jobName);
        
        _mediator.RemoveRecurringJob(jobName);
        
        return Ok(new { message = "Recurring job removed", jobName });
    }

    /// <summary>
    /// Gets predefined report types for convenience
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetReportTypes()
    {
        var reportTypes = new[]
        {
            new { type = "usage", description = "System usage statistics" },
            new { type = "users", description = "User activity and demographics" },
            new { type = "performance", description = "System performance metrics" }
        };

        return Ok(reportTypes);
    }

    /// <summary>
    /// Gets common cron expressions for scheduling
    /// </summary>
    [HttpGet("cron-examples")]
    public IActionResult GetCronExamples()
    {
        var cronExamples = new[]
        {
            new { expression = Cron.Minutely(), description = "Every minute" },
            new { expression = Cron.Hourly(), description = "Every hour" },
            new { expression = Cron.Daily(), description = "Daily at midnight" },
            new { expression = Cron.Daily(6), description = "Daily at 6:00 AM" },
            new { expression = Cron.Weekly(), description = "Weekly on Sunday at midnight" },
            new { expression = Cron.Monthly(), description = "Monthly on the 1st at midnight" },
            new { expression = "0 */6 * * *", description = "Every 6 hours" },
            new { expression = "0 9 * * 1-5", description = "Weekdays at 9:00 AM" }
        };

        return Ok(cronExamples);
    }
}
