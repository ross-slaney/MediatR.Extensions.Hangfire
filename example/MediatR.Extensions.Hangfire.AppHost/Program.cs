var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var redis = builder.AddRedis("redis").WithDataVolume();

var sql = builder.AddSqlServer("sql")
    .WithDataVolume(); // Persist data between runs

var db = sql.AddDatabase("hangfire");

// API Container - Only handles HTTP requests and enqueues jobs
var api = builder.AddProject("api", "../MediatR.Extensions.Hangfire.Example/MediatR.Extensions.Hangfire.Example.csproj")
    .WaitFor(db)
    .WaitFor(redis)
    .WithReference(redis)
    .WithReference(db)
    .WithEnvironment("HANGFIRE_SERVER_ENABLED", "false")
    .WithEnvironment("DISABLE_API_ENDPOINTS", "false")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpsEndpoint(port: 7001, name: "https")
    .WithHttpEndpoint(port: 5001, name: "http");

// Worker Container 1 - Only processes background jobs
var worker1 = builder.AddProject("worker1", "../MediatR.Extensions.Hangfire.Example/MediatR.Extensions.Hangfire.Example.csproj")
    .WaitFor(db)
    .WaitFor(redis)
    .WithReference(redis)
    .WithReference(db)
    .WithEnvironment("HANGFIRE_SERVER_ENABLED", "true")
    .WithEnvironment("DISABLE_API_ENDPOINTS", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("WORKER_NAME", "Worker-1")
    .WithEnvironment("MAX_CONCURRENT_JOBS", "20")
    .WithHttpEndpoint(port: 5002, name: "worker1");

// Worker Container 2 - Additional processing capacity
var worker2 = builder.AddProject("worker2", "../MediatR.Extensions.Hangfire.Example/MediatR.Extensions.Hangfire.Example.csproj")
    .WaitFor(db)
    .WaitFor(redis)
    .WithReference(redis)
    .WithReference(db)
    .WithEnvironment("HANGFIRE_SERVER_ENABLED", "true")
    .WithEnvironment("DISABLE_API_ENDPOINTS", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("WORKER_NAME", "Worker-2")
    .WithEnvironment("MAX_CONCURRENT_JOBS", "15")
    .WithHttpEndpoint(port: 5003, name: "worker2");

// Worker Container 3 - Specialized for heavy processing
var worker3 = builder.AddProject("worker3", "../MediatR.Extensions.Hangfire.Example/MediatR.Extensions.Hangfire.Example.csproj")
    .WaitFor(db)
    .WithReference(redis)
    .WithReference(db)
    .WithEnvironment("HANGFIRE_SERVER_ENABLED", "true")
    .WithEnvironment("DISABLE_API_ENDPOINTS", "true")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("WORKER_NAME", "Worker-3-Heavy")
    .WithEnvironment("MAX_CONCURRENT_JOBS", "5")
    .WithHttpEndpoint(port: 5004, name: "worker3");

builder.Build().Run();
