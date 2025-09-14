using MediatR;
using MediatR.Hangfire.Example.Services;
using MediatR.Extensions.Hangfire.Logging;

namespace MediatR.Hangfire.Example.Commands;

/// <summary>
/// Command to send an email (fire-and-forget)
/// </summary>
public class SendEmailCommand : IRequest
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public string? FromName { get; set; }
}

/// <summary>
/// Handler for sending emails
/// </summary>
public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(IEmailService emailService, ILogger<SendEmailCommandHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
        _logger = _logger.WithHangfireConsole();
    }

    public async Task Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending email to: {To}, Subject: {Subject}", request.To, request.Subject);

        try
        {
            await _emailService.SendEmailAsync(request.To, request.Subject, request.Body, request.FromName);
            _logger.LogInformation("Email sent successfully to: {To}", request.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {To}", request.To);
            throw; // Re-throw to trigger Hangfire retry logic
        }
    }
}
