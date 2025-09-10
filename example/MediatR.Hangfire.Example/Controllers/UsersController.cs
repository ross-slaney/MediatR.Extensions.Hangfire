using Microsoft.AspNetCore.Mvc;
using MediatR;
using MediatR.Hangfire.Example.Commands;
using MediatR.Hangfire.Example.Queries;
using MediatR.Hangfire.Example.Models;
using MediatR.Hangfire.Extensions.Extensions;

namespace MediatR.Hangfire.Example.Controllers;

/// <summary>
/// Controller demonstrating different MediatR-Hangfire integration patterns
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a user synchronously (traditional approach)
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<OperationResult<User>>> CreateUserSync([FromBody] CreateUserCommand command)
    {
        _logger.LogInformation("Creating user synchronously: {Name}", command.Name);
        
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return BadRequest(result);
    }

    /// <summary>
    /// Creates a user asynchronously using fire-and-forget background job
    /// Returns immediately while user creation happens in the background
    /// </summary>
    [HttpPost("async-fire-and-forget")]
    public IActionResult CreateUserAsync([FromBody] CreateUserCommand command)
    {
        _logger.LogInformation("Enqueuing user creation job: {Name}", command.Name);
        
        // Fire-and-forget: job is queued but we don't wait for the result
        _mediator.Enqueue("Create User", command);
        
        return Accepted(new { message = "User creation job has been queued" });
    }

    /// <summary>
    /// Creates a user asynchronously and waits for the result
    /// Demonstrates background processing with return values
    /// </summary>
    [HttpPost("async-with-result")]
    public async Task<ActionResult<OperationResult<User>>> CreateUserAsyncWithResult([FromBody] CreateUserCommand command)
    {
        _logger.LogInformation("Creating user with background job and waiting for result: {Name}", command.Name);
        
        try
        {
            // Enqueue job and wait for result (with retry logic)
            var result = await _mediator.EnqueueAsync("Create User", command, retryAttempts: 2);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return BadRequest(result);
        }
        catch (TimeoutException)
        {
            return StatusCode(408, new { message = "User creation job timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user via background job");
            return StatusCode(500, new { message = "An error occurred while processing the request" });
        }
    }

    /// <summary>
    /// Gets a user by ID synchronously
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _mediator.Send(new GetUserQuery { UserId = id });
        
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }

    /// <summary>
    /// Gets a user by ID using background processing
    /// Demonstrates using background jobs for queries (useful for expensive operations)
    /// </summary>
    [HttpGet("{id}/async")]
    public async Task<ActionResult<User>> GetUserAsync(int id)
    {
        try
        {
            var user = await _mediator.EnqueueAsync("Get User", new GetUserQuery { UserId = id });
            
            if (user == null)
                return NotFound();
            
            return Ok(user);
        }
        catch (TimeoutException)
        {
            return StatusCode(408, new { message = "User lookup job timed out" });
        }
    }

    /// <summary>
    /// Sends a welcome email to a user (fire-and-forget)
    /// </summary>
    [HttpPost("{id}/send-welcome-email")]
    public async Task<IActionResult> SendWelcomeEmail(int id)
    {
        var user = await _mediator.Send(new GetUserQuery { UserId = id });
        
        if (user == null)
            return NotFound();

        var emailCommand = new SendEmailCommand
        {
            To = user.Email,
            Subject = "Welcome!",
            Body = $"Hello {user.Name}, welcome to our service!",
            FromName = "Welcome Team"
        };

        // Fire-and-forget email sending with retry logic
        _mediator.Enqueue("Send Welcome Email", emailCommand);
        
        return Ok(new { message = "Welcome email has been queued for sending" });
    }

    /// <summary>
    /// Schedules a reminder email to be sent after a delay
    /// </summary>
    [HttpPost("{id}/schedule-reminder")]
    public async Task<IActionResult> ScheduleReminderEmail(int id, [FromQuery] int delayMinutes = 60)
    {
        var user = await _mediator.Send(new GetUserQuery { UserId = id });
        
        if (user == null)
            return NotFound();

        var emailCommand = new SendEmailCommand
        {
            To = user.Email,
            Subject = "Don't forget to complete your profile",
            Body = $"Hi {user.Name}, please complete your profile to get the most out of our service.",
            FromName = "Reminder Team"
        };

        // Schedule email to be sent after delay
        var jobId = _mediator.Schedule("Send Reminder Email", emailCommand, TimeSpan.FromMinutes(delayMinutes));
        
        return Ok(new { message = $"Reminder email scheduled to be sent in {delayMinutes} minutes", jobId });
    }
}
