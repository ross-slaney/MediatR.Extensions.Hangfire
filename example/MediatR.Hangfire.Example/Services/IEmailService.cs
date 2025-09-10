namespace MediatR.Hangfire.Example.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, string? fromName = null);
    Task SendWelcomeEmailAsync(string email, string name);
}

/// <summary>
/// Mock implementation of email service for demonstration
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, string? fromName = null)
    {
        _logger.LogInformation("Sending email - To: {To}, Subject: {Subject}, From: {From}", 
            to, subject, fromName ?? "System");

        // Simulate email sending delay
        await Task.Delay(Random.Shared.Next(1000, 3000));

        // Simulate occasional failures for retry demonstration
        if (Random.Shared.Next(1, 10) == 1)
        {
            throw new InvalidOperationException("Simulated email service failure");
        }

        _logger.LogInformation("Email sent successfully to: {To}", to);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var subject = "Welcome to our service!";
        var body = $"Hello {name},\n\nWelcome to our service! We're excited to have you on board.\n\nBest regards,\nThe Team";
        
        await SendEmailAsync(email, subject, body, "Welcome Team");
    }
}
