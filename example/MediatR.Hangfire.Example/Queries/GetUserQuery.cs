using MediatR;
using MediatR.Hangfire.Example.Models;
using MediatR.Hangfire.Example.Services;

namespace MediatR.Hangfire.Example.Queries;

/// <summary>
/// Query to get a user by ID
/// </summary>
public class GetUserQuery : IRequest<User?>
{
    public int UserId { get; set; }
}

/// <summary>
/// Handler for getting users
/// </summary>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, User?>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(IUserService userService, ILogger<GetUserQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user: {UserId}", request.UserId);

        try
        {
            var user = await _userService.GetUserByIdAsync(request.UserId);
            
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
            }
            else
            {
                _logger.LogInformation("User found: {UserId} - {Name}", user.Id, user.Name);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", request.UserId);
            throw;
        }
    }
}
