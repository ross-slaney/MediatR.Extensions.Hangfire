using MediatR;
using MediatR.Hangfire.Example.Services;

namespace MediatR.Hangfire.Example.Commands;

/// <summary>
/// Command for cleaning up old data (recurring job example)
/// </summary>
public class CleanupCommand : IRequest
{
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(30);
    public string? Category { get; set; }
}

/// <summary>
/// Handler for cleanup operations
/// </summary>
public class CleanupCommandHandler : IRequestHandler<CleanupCommand>
{
    private readonly IUserService _userService;
    private readonly ILogger<CleanupCommandHandler> _logger;

    public CleanupCommandHandler(IUserService userService, ILogger<CleanupCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task Handle(CleanupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cleanup operation - MaxAge: {MaxAge}, Category: {Category}", 
            request.MaxAge, request.Category ?? "All");

        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(request.MaxAge);
            
            // Simulate cleanup operations
            _logger.LogInformation("Cleaning up data older than: {CutoffDate}", cutoffDate);
            
            // Simulate some work
            await Task.Delay(2000, cancellationToken);
            
            var cleanedCount = Random.Shared.Next(0, 50);
            _logger.LogInformation("Cleanup completed - {Count} items cleaned", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup operation failed");
            throw;
        }
    }
}
