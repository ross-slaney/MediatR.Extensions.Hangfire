using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Hangfire.InMemory;
using MediatR;
using MediatR.Hangfire.Extensions.Extensions;
using MediatR.Hangfire.Example.Commands;
using MediatR.Hangfire.Example.Queries;
using MediatR.Hangfire.Example.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add example services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Add Hangfire with SQL Server storage (in-memory for demo)
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage()); // For demo purposes - use SQL Server in production

// Add Hangfire server
builder.Services.AddHangfireServer();

// Add MediatR-Hangfire integration
builder.Services.AddHangfireMediatR(options =>
{
    // For demo purposes, use in-memory coordination
    // In production, use Redis: options.UseRedis("redis-connection-string");
    options.UseInMemory();
    
    options.WithRetryAttempts(3);
    options.WithConsoleLogging(true);
    options.WithDetailedLogging(true);
    options.WithTaskTimeout(TimeSpan.FromMinutes(5));
    options.WithMaxConcurrentJobs(10);
});

var app = builder.Build();

// Set up service locator and Hangfire configuration for MediatR.Hangfire.Extensions
using (var scope = app.Services.CreateScope())
{
    var serviceLocatorSetup = scope.ServiceProvider.GetRequiredService<MediatR.Hangfire.Extensions.Extensions.IServiceLocatorSetup>();
    serviceLocatorSetup.Setup(app.Services);

    // Configure Hangfire for MediatR integration
    var hangfireConfigurator = scope.ServiceProvider.GetRequiredService<MediatR.Hangfire.Extensions.Extensions.IHangfireMediatorConfigurator>();
    hangfireConfigurator.Configure();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
});

app.UseAuthorization();

app.MapControllers();

// Setup some recurring jobs for demonstration
using (var scope = app.Services.CreateScope())
{
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    
    // Daily cleanup job
    mediator.AddOrUpdate(
        "Daily Cleanup",
        new CleanupCommand { MaxAge = TimeSpan.FromDays(30) },
        Cron.Daily(2, 0)); // 2 AM daily

    // Hourly report generation
    mediator.AddOrUpdate(
        "Hourly Usage Report",
        new GenerateReportCommand { ReportType = "Usage", Period = "Hourly" },
        Cron.Hourly());
}

app.Run();

// Allow all users to access Hangfire dashboard (for demo purposes only)
public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // Allow all users - DO NOT USE IN PRODUCTION
    }
}
