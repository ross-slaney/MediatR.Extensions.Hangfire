using MediatR.Hangfire.Example.Models;

namespace MediatR.Hangfire.Example.Services;

/// <summary>
/// Service for generating reports
/// </summary>
public interface IReportService
{
    Task<Report> GenerateReportAsync(string reportType, string period, DateTime? startDate = null, DateTime? endDate = null);
}

/// <summary>
/// Mock implementation of report service for demonstration
/// </summary>
public class ReportService : IReportService
{
    private readonly IUserService _userService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUserService userService, ILogger<ReportService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<Report> GenerateReportAsync(string reportType, string period, DateTime? startDate = null, DateTime? endDate = null)
    {
        _logger.LogInformation("Generating {ReportType} report for period: {Period}", reportType, period);

        // Simulate complex report generation work
        await Task.Delay(Random.Shared.Next(2000, 5000));

        var report = new Report
        {
            Type = reportType,
            Period = period,
            GeneratedAt = DateTime.UtcNow
        };

        // Generate different types of reports
        switch (reportType.ToLowerInvariant())
        {
            case "usage":
                report.Data = await GenerateUsageReportData(startDate, endDate);
                break;
            case "users":
                report.Data = await GenerateUsersReportData(startDate, endDate);
                break;
            case "performance":
                report.Data = await GeneratePerformanceReportData(startDate, endDate);
                break;
            default:
                report.Data = new Dictionary<string, object>
                {
                    ["message"] = "Unknown report type",
                    ["reportType"] = reportType
                };
                break;
        }

        report.RecordCount = report.Data.Count;

        _logger.LogInformation("Report generated: {ReportType} - {RecordCount} records", 
            reportType, report.RecordCount);

        return report;
    }

    private async Task<Dictionary<string, object>> GenerateUsageReportData(DateTime? startDate, DateTime? endDate)
    {
        // Simulate usage data generation
        await Task.Delay(1000);

        return new Dictionary<string, object>
        {
            ["totalRequests"] = Random.Shared.Next(1000, 10000),
            ["uniqueUsers"] = Random.Shared.Next(100, 1000),
            ["averageResponseTime"] = Random.Shared.Next(100, 500) + "ms",
            ["errorRate"] = Random.Shared.NextDouble() * 5 + "%",
            ["peakHour"] = Random.Shared.Next(9, 17) + ":00",
            ["startDate"] = startDate?.ToString("yyyy-MM-dd") ?? "N/A",
            ["endDate"] = endDate?.ToString("yyyy-MM-dd") ?? "N/A"
        };
    }

    private async Task<Dictionary<string, object>> GenerateUsersReportData(DateTime? startDate, DateTime? endDate)
    {
        // Get actual user data
        var users = await _userService.GetAllUsersAsync();
        
        var filteredUsers = users.AsQueryable();
        if (startDate.HasValue)
            filteredUsers = filteredUsers.Where(u => u.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            filteredUsers = filteredUsers.Where(u => u.CreatedAt <= endDate.Value);

        var userList = filteredUsers.ToList();

        return new Dictionary<string, object>
        {
            ["totalUsers"] = userList.Count,
            ["activeUsers"] = userList.Count(u => u.IsActive),
            ["inactiveUsers"] = userList.Count(u => !u.IsActive),
            ["newUsersInPeriod"] = userList.Count,
            ["oldestUser"] = userList.OrderBy(u => u.CreatedAt).FirstOrDefault()?.Name ?? "N/A",
            ["newestUser"] = userList.OrderByDescending(u => u.CreatedAt).FirstOrDefault()?.Name ?? "N/A",
            ["startDate"] = startDate?.ToString("yyyy-MM-dd") ?? "N/A",
            ["endDate"] = endDate?.ToString("yyyy-MM-dd") ?? "N/A"
        };
    }

    private async Task<Dictionary<string, object>> GeneratePerformanceReportData(DateTime? startDate, DateTime? endDate)
    {
        // Simulate performance metrics generation
        await Task.Delay(1500);

        return new Dictionary<string, object>
        {
            ["cpuUsage"] = Random.Shared.Next(20, 80) + "%",
            ["memoryUsage"] = Random.Shared.Next(30, 90) + "%",
            ["diskUsage"] = Random.Shared.Next(10, 70) + "%",
            ["networkThroughput"] = Random.Shared.Next(100, 1000) + " MB/s",
            ["jobsProcessed"] = Random.Shared.Next(500, 5000),
            ["averageJobDuration"] = Random.Shared.Next(100, 2000) + "ms",
            ["failedJobs"] = Random.Shared.Next(0, 50),
            ["successRate"] = (100 - Random.Shared.NextDouble() * 5).ToString("F2") + "%",
            ["startDate"] = startDate?.ToString("yyyy-MM-dd") ?? "N/A",
            ["endDate"] = endDate?.ToString("yyyy-MM-dd") ?? "N/A"
        };
    }
}
