using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using MediatR;
using MediatR.Hangfire.Extensions.Extensions;
using MediatR.Hangfire.Example.Commands;
using MediatR.Hangfire.Example.Queries;
using MediatR.Hangfire.Example.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration for container separation
var hangfireServerEnabled = builder.Configuration.GetValue<bool>("HANGFIRE_SERVER_ENABLED", true);
var disableApiEndpoints = builder.Configuration.GetValue<bool>("DISABLE_API_ENDPOINTS", false);
var workerName = builder.Configuration.GetValue<string>("WORKER_NAME", Environment.MachineName);
var maxConcurrentJobs = builder.Configuration.GetValue<int>("MAX_CONCURRENT_JOBS", Environment.ProcessorCount * 5);

Console.WriteLine($"🚀 Starting container: HangfireServer={hangfireServerEnabled}, DisableAPI={disableApiEndpoints}, Worker={workerName}");

// Debug: Print all available connection strings
Console.WriteLine("🔍 Available connection strings:");
var connectionStrings = builder.Configuration.GetSection("ConnectionStrings").GetChildren();
foreach (var cs in connectionStrings)
{
    Console.WriteLine($"  - {cs.Key}: {cs.Value?.Substring(0, Math.Min(50, cs.Value?.Length ?? 0))}...");
}

// Debug: Print relevant environment variables
Console.WriteLine("🔍 Environment variables:");
var envVars = Environment.GetEnvironmentVariables();
foreach (string key in envVars.Keys)
{
    if (key.Contains("CONNECTION", StringComparison.OrdinalIgnoreCase) || 
        key.Contains("SQL", StringComparison.OrdinalIgnoreCase) || 
        key.Contains("REDIS", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("HANGFIRE", StringComparison.OrdinalIgnoreCase))
    {
        var value = envVars[key]?.ToString() ?? "";
        Console.WriteLine($"  - {key}: {value.Substring(0, Math.Min(50, value.Length))}...");
    }
}

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add health checks
builder.Services.AddHealthChecks();

// Add example services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Add Redis connection (use connection string from configuration)
var redisConnectionString = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

// Configure Hangfire with SQL Server storage
builder.Services.AddHangfire((serviceProvider, configuration) =>
{
    // In .NET Aspire, try different connection string naming patterns
    var connectionString = builder.Configuration.GetConnectionString("hangfire") 
        ?? builder.Configuration.GetConnectionString("sql")
        ?? builder.Configuration.GetConnectionString("HangfireDatabase")
        ?? throw new InvalidOperationException($"Hangfire connection string not found. Available connection strings: {string.Join(", ", builder.Configuration.GetSection("ConnectionStrings").GetChildren().Select(x => x.Key))}");
    
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        });
});

// Add Hangfire server only for worker containers
if (hangfireServerEnabled)
{
    Console.WriteLine($"⚙️  Configuring as WORKER container: {workerName} (max {maxConcurrentJobs} concurrent jobs)");
    builder.Services.AddHangfireServer(options =>
    {
        options.ServerName = workerName;
        options.WorkerCount = maxConcurrentJobs;
        options.Queues = new[] { "default", "critical", "emails", "reports", "cleanup" };
    });
}
else
{
    Console.WriteLine("🌐 Configuring as API container (job enqueueing only)");
}

// Add API services only for API containers
if (!disableApiEndpoints)
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "MediatR.Hangfire Distributed API", Version = "v1" });
    });
}

// Add MediatR-Hangfire integration (always needed for job coordination)
builder.Services.AddHangfireMediatR(options =>
{
    // Use Redis for distributed coordination
    var redisConnection = builder.Configuration.GetConnectionString("redis") 
        ?? throw new InvalidOperationException("Redis connection string not found");
    options.UseRedis(redisConnection);
    
    options.WithRetryAttempts(3);
    options.WithConsoleLogging(true);
    options.WithDetailedLogging(true);
    options.WithTaskTimeout(TimeSpan.FromMinutes(10));
    options.WithMaxConcurrentJobs(maxConcurrentJobs);
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

// Configure the HTTP request pipeline based on container type
if (!disableApiEndpoints)
{
    Console.WriteLine("🌐 Configuring API endpoints and middleware");
    
    // Map health check endpoints
    app.MapHealthChecks("/health");
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediatR.Hangfire Distributed API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();

    // Add container info endpoint
    app.MapGet("/container-info", () => new
    {
        ContainerType = "API",
        MachineName = Environment.MachineName,
        ProcessorCount = Environment.ProcessorCount,
        HangfireServerEnabled = hangfireServerEnabled,
        WorkerName = workerName,
        MaxConcurrentJobs = maxConcurrentJobs
    });
}
else
{
    Console.WriteLine("⚙️  Worker container - API endpoints disabled");
    
    // Map only essential endpoints for workers
    app.MapHealthChecks("/health");
    
    // Add worker info endpoint (for monitoring)
    app.MapGet("/worker-info", () => new
    {
        ContainerType = "WORKER",
        MachineName = Environment.MachineName,
        ProcessorCount = Environment.ProcessorCount,
        WorkerName = workerName,
        MaxConcurrentJobs = maxConcurrentJobs,
        StartTime = DateTime.UtcNow
    });
}

// Always add Hangfire Dashboard (useful for monitoring from any container)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() },
    DisplayStorageConnectionString = false,
    DashboardTitle = $"MediatR.Hangfire Dashboard - {(hangfireServerEnabled ? "WORKER" : "API")} Container"
});

// Setup recurring jobs only from one container (API container for simplicity)
if (!hangfireServerEnabled && !disableApiEndpoints)
{
    Console.WriteLine("📅 Setting up recurring jobs (from API container)");
    using (var scope = app.Services.CreateScope())
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        // Daily cleanup job (runs on any available worker)
        mediator.AddOrUpdate(
            "Daily Cleanup",
            new CleanupCommand { MaxAge = TimeSpan.FromDays(30) },
            Cron.Daily(2, 0)); // 2 AM daily

        // Hourly usage report (demonstrates distributed processing)
        mediator.AddOrUpdate(
            "Hourly Usage Report",
            new GenerateReportCommand { ReportType = "Usage", Period = "Hourly" },
            Cron.Hourly());
            
        // Frequent email processing demonstration
        mediator.AddOrUpdate(
            "Email Queue Processor",
            new SendEmailCommand 
            { 
                To = "demo@example.com", 
                Subject = "Scheduled Test Email", 
                Body = "This email demonstrates recurring job processing across distributed workers." 
            },
            "*/2 * * * *"); // Every 2 minutes for demo purposes
    }
}

Console.WriteLine($"🎯 Container started successfully: {(hangfireServerEnabled ? "WORKER" : "API")} mode");
Console.WriteLine($"📊 Hangfire Dashboard: /hangfire");
if (!disableApiEndpoints)
{
    Console.WriteLine($"📝 Swagger UI: /swagger");
    Console.WriteLine($"ℹ️  Container Info: /container-info");
}
else
{
    Console.WriteLine($"ℹ️  Worker Info: /worker-info");
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
