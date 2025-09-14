using MediatR.Hangfire.Example.Models;

namespace MediatR.Hangfire.Example.Services;

/// <summary>
/// Service for managing users
/// </summary>
public interface IUserService
{
    Task<User> CreateUserAsync(string name, string email);
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<List<User>> GetAllUsersAsync();
}

/// <summary>
/// In-memory implementation of user service for demonstration
/// </summary>
public class UserService : IUserService
{
    private readonly List<User> _users = new();
    private readonly ILogger<UserService> _logger;
    private int _nextId = 1;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
        
        // Seed some initial data
        _users.AddRange(new[]
        {
            new User { Id = _nextId++, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new User { Id = _nextId++, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new User { Id = _nextId++, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = DateTime.UtcNow.AddDays(-2) }
        });
    }

    public async Task<User> CreateUserAsync(string name, string email)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", name, email);

        // Simulate database delay
        await Task.Delay(Random.Shared.Next(500, 1500));

        var user = new User
        {
            Id = _nextId++,
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _users.Add(user);
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        _logger.LogDebug("Getting user by ID: {UserId}", id);

        // Simulate database delay
        await Task.Delay(Random.Shared.Next(100, 500));

        return _users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogDebug("Getting user by email: {Email}", email);

        // Simulate database delay
        await Task.Delay(Random.Shared.Next(100, 500));

        return _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        _logger.LogDebug("Getting all users");

        // Simulate database delay
        await Task.Delay(Random.Shared.Next(200, 800));

        return _users.ToList();
    }
}
