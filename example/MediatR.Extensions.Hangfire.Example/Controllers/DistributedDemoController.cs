using Microsoft.AspNetCore.Mvc;
using MediatR;
using MediatR.Hangfire.Example.Commands;
using MediatR.Hangfire.Example.Queries;
using MediatR.Extensions.Hangfire.Extensions;
using Hangfire;

namespace MediatR.Hangfire.Example.Controllers;

/// <summary>
/// Controller demonstrating distributed processing capabilities with multiple workers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DistributedDemoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DistributedDemoController> _logger;

    public DistributedDemoController(IMediator mediator, ILogger<DistributedDemoController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a massive workload to demonstrate distributed processing
    /// </summary>
    [HttpPost("stress-test")]
    public IActionResult CreateStressTest([FromQuery] int jobCount = 50)
    {
        _logger.LogInformation("Creating {JobCount} jobs for stress testing distributed workers", jobCount);

        var jobIds = new List<string>();

        // Create a mix of different job types to distribute across workers
        for (int i = 1; i <= jobCount; i++)
        {
            string jobId;
            
            if (i % 4 == 0)
            {
                // Heavy processing jobs (reports)
                var reportCommand = new GenerateReportCommand
                {
                    ReportType = "Performance",
                    Period = "Stress Test",
                    StartDate = DateTime.UtcNow.AddMinutes(-i),
                    EndDate = DateTime.UtcNow
                };
                
                var client = new BackgroundJobClient();
                jobId = client.Enqueue("reports", () => 
                    _mediator.Send(reportCommand, CancellationToken.None));
                    
                jobIds.Add($"Report-{i}: {jobId}");
            }
            else if (i % 3 == 0)
            {
                // Email jobs
                var emailCommand = new SendEmailCommand
                {
                    To = $"user{i}@stresstest.com",
                    Subject = $"Stress Test Email #{i}",
                    Body = $"This is stress test email number {i} of {jobCount}. Processing distributed across workers.",
                    FromName = "Stress Test System"
                };
                
                var client = new BackgroundJobClient();
                jobId = client.Enqueue("emails", () => 
                    _mediator.Send(emailCommand, CancellationToken.None));
                    
                jobIds.Add($"Email-{i}: {jobId}");
            }
            else if (i % 5 == 0)
            {
                // Critical priority jobs
                var userCommand = new CreateUserCommand
                {
                    Name = $"Stress User {i}",
                    Email = $"stressuser{i}@test.com"
                };
                
                var client = new BackgroundJobClient();
                jobId = client.Enqueue("critical", () => 
                    _mediator.Send(userCommand, CancellationToken.None));
                    
                jobIds.Add($"User-{i}: {jobId}");
            }
            else
            {
                // Regular cleanup jobs
                var cleanupCommand = new CleanupCommand
                {
                    MaxAge = TimeSpan.FromHours(i),
                    Category = $"stress-test-{i}"
                };
                
                var client = new BackgroundJobClient();
                jobId = client.Enqueue("cleanup", () => 
                    _mediator.Send(cleanupCommand, CancellationToken.None));
                    
                jobIds.Add($"Cleanup-{i}: {jobId}");
            }
        }

        return Ok(new
        {
            Message = $"Created {jobCount} jobs distributed across worker queues",
            JobCount = jobCount,
            Queues = new[] { "critical", "emails", "reports", "cleanup", "default" },
            Jobs = jobIds.Take(10).ToList(), // Show first 10 for brevity
            TotalJobs = jobIds.Count,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Demonstrates mixed workload with return values
    /// </summary>
    [HttpPost("mixed-workload")]
    public async Task<IActionResult> CreateMixedWorkload([FromQuery] int reportCount = 5, [FromQuery] int userCount = 10)
    {
        _logger.LogInformation("Creating mixed workload: {ReportCount} reports, {UserCount} users", reportCount, userCount);

        var tasks = new List<Task<object>>();

        // Create report generation tasks (heavy processing)
        for (int i = 1; i <= reportCount; i++)
        {
            var reportCommand = new GenerateReportCommand
            {
                ReportType = "Mixed Workload",
                Period = $"Batch {i}",
                StartDate = DateTime.UtcNow.AddHours(-i),
                EndDate = DateTime.UtcNow
            };

            var reportTask = _mediator.EnqueueAsync($"Mixed Report {i}", reportCommand, retryAttempts: 1)
                .ContinueWith(t => (object)new { Type = "Report", Index = i, Result = t.Result }, TaskContinuationOptions.OnlyOnRanToCompletion);
            
            tasks.Add(reportTask);
        }

        // Create user creation tasks (lighter processing)
        for (int i = 1; i <= userCount; i++)
        {
            var userCommand = new CreateUserCommand
            {
                Name = $"Mixed User {i}",
                Email = $"mixeduser{i}@workload.com"
            };

            var userTask = _mediator.EnqueueAsync($"Mixed User {i}", userCommand, retryAttempts: 2)
                .ContinueWith(t => (object)new { Type = "User", Index = i, Result = t.Result }, TaskContinuationOptions.OnlyOnRanToCompletion);
            
            tasks.Add(userTask);
        }

        // Wait for all tasks to complete (with timeout)
        try
        {
            var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMinutes(5));
            
            return Ok(new
            {
                Message = "Mixed workload completed successfully",
                ReportCount = reportCount,
                UserCount = userCount,
                CompletedTasks = results.Length,
                Results = results,
                ProcessingTime = "< 5 minutes",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (TimeoutException)
        {
            return StatusCode(408, new
            {
                Message = "Mixed workload timed out",
                CompletedTasks = tasks.Count(t => t.IsCompletedSuccessfully),
                TotalTasks = tasks.Count,
                Timeout = "5 minutes"
            });
        }
    }

    /// <summary>
    /// Gets current worker statistics
    /// </summary>
    [HttpGet("worker-stats")]
    public IActionResult GetWorkerStats()
    {
        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            
            // Get basic statistics
            var statistics = monitoring.GetStatistics();
            
            // Get queue information
            var queues = monitoring.Queues().Select(q => new
            {
                Name = q.Name,
                Length = q.Length,
                Fetched = q.Fetched
            });

            return Ok(new
            {
                QueueStats = queues,
                Statistics = new
                {
                    Enqueued = statistics.Enqueued,
                    Failed = statistics.Failed,
                    Processing = statistics.Processing,
                    Scheduled = statistics.Scheduled,
                    Succeeded = statistics.Succeeded,
                    Deleted = statistics.Deleted,
                    Servers = statistics.Servers
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get worker statistics");
            return StatusCode(500, new { Error = "Failed to retrieve worker statistics", Message = ex.Message });
        }
    }

    /// <summary>
    /// Triggers all recurring jobs immediately for demonstration
    /// </summary>
    [HttpPost("trigger-all-recurring")]
    public IActionResult TriggerAllRecurringJobs()
    {
        var knownRecurringJobs = new[] { "Daily Cleanup", "Hourly Usage Report", "Email Queue Processor" };
        var triggeredJobs = new List<string>();

        foreach (var jobId in knownRecurringJobs)
        {
            try
            {
                RecurringJob.TriggerJob(jobId);
                triggeredJobs.Add(jobId);
                _logger.LogInformation("Triggered recurring job: {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger recurring job: {JobId}", jobId);
            }
        }

        return Ok(new
        {
            Message = "Triggered known recurring jobs",
            TriggeredJobs = triggeredJobs,
            AttemptedJobs = knownRecurringJobs,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a scheduled cascade of jobs to show timing
    /// </summary>
    [HttpPost("scheduled-cascade")]
    public IActionResult CreateScheduledCascade()
    {
        var jobIds = new List<object>();
        var baseTime = DateTime.UtcNow;

        // Schedule jobs at different intervals
        for (int i = 1; i <= 5; i++)
        {
            var delay = TimeSpan.FromSeconds(i * 30); // 30s, 1m, 1.5m, 2m, 2.5m
            var scheduledTime = baseTime.Add(delay);

            var emailCommand = new SendEmailCommand
            {
                To = "cascade@demo.com",
                Subject = $"Cascade Email #{i}",
                Body = $"This is cascade email #{i}, scheduled for {scheduledTime:HH:mm:ss}",
                FromName = "Cascade Demo"
            };

            var jobId = _mediator.Schedule($"Cascade Email {i}", emailCommand, delay);
            
            jobIds.Add(new
            {
                JobId = jobId,
                SequenceNumber = i,
                ScheduledFor = scheduledTime,
                Delay = delay.ToString()
            });
        }

        return Ok(new
        {
            Message = "Created scheduled cascade of 5 jobs",
            Jobs = jobIds,
            StartTime = baseTime,
            Duration = "2.5 minutes",
            Timestamp = DateTime.UtcNow
        });
    }
}
