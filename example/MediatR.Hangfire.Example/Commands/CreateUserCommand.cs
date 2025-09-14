using MediatR;
using MediatR.Hangfire.Example.Models;
using MediatR.Hangfire.Example.Services;
using MediatR.Extensions.Hangfire.Logging;

namespace MediatR.Hangfire.Example.Commands;

/// <summary>
/// Command to create a new user
/// </summary>
public class CreateUserCommand : IRequest<OperationResult<User>>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}

/// <summary>
/// Handler for creating users
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, OperationResult<User>>
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserService userService,
        IEmailService emailService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _logger = logger.WithHangfireConsole();
    }

    public async Task<OperationResult<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", request.Name, request.Email);

        try
        {
            // Simulate validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return OperationResult<User>.Failure("Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            {
                return OperationResult<User>.Failure("Valid email is required");
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return OperationResult<User>.Failure("User with this email already exists");
            }

            // Create the user
            var user = await _userService.CreateUserAsync(request.Name, request.Email);
            
            _logger.LogInformation("User created successfully: {UserId}", user.Id);

            // Send welcome email (this could be enqueued as a separate job)
            await _emailService.SendWelcomeEmailAsync(user.Email, user.Name);

            return OperationResult<User>.Success(user, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Name} ({Email})", request.Name, request.Email);
            return OperationResult<User>.Failure("An error occurred while creating the user");
        }
    }
}
