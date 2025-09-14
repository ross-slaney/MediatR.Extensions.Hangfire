using MediatR;
using MediatR.Hangfire.Example.Models;
using MediatR.Hangfire.Example.Services;

namespace MediatR.Hangfire.Example.Queries;

/// <summary>
/// Command to generate a report (can be used for both sync and async operations)
/// </summary>
public class GenerateReportCommand : IRequest<Report>
{
    public required string ReportType { get; set; }
    public required string Period { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Handler for generating reports
/// </summary>
public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, Report>
{
    private readonly IReportService _reportService;
    private readonly ILogger<GenerateReportCommandHandler> _logger;

    public GenerateReportCommandHandler(IReportService reportService, ILogger<GenerateReportCommandHandler> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    public async Task<Report> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating report - Type: {ReportType}, Period: {Period}", 
            request.ReportType, request.Period);

        try
        {
            // Simulate report generation work
            var report = await _reportService.GenerateReportAsync(
                request.ReportType, 
                request.Period, 
                request.StartDate, 
                request.EndDate);

            _logger.LogInformation("Report generated successfully - Type: {ReportType}, Records: {RecordCount}", 
                report.Type, report.RecordCount);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report - Type: {ReportType}, Period: {Period}", 
                request.ReportType, request.Period);
            throw;
        }
    }
}
