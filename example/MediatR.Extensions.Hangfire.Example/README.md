# MediatR.Extensions.Hangfire Example

This example demonstrates the usage of the MediatR.Extensions.Hangfire library in a .NET 9 Web API application.

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

### Running the Example

1. **Navigate to the example directory:**

   ```bash
   cd example/MediatR.Extensions.Hangfire.Example
   ```

2. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

3. **Run the application:**

   ```bash
   dotnet run
   ```

4. **Access the application:**
   - API: https://localhost:7000 (or http://localhost:5000)
   - Swagger UI: https://localhost:7000/swagger
   - Hangfire Dashboard: https://localhost:7000/hangfire

## 📋 What's Included

### Controllers

#### 🧑‍💼 UsersController (`/api/users`)

Demonstrates different patterns for user management:

- **POST `/sync`** - Traditional synchronous user creation
- **POST `/async-fire-and-forget`** - Fire-and-forget background user creation
- **POST `/async-with-result`** - Background user creation with result waiting
- **GET `/{id}`** - Synchronous user retrieval
- **GET `/{id}/async`** - Background user retrieval
- **POST `/{id}/send-welcome-email`** - Fire-and-forget email sending
- **POST `/{id}/schedule-reminder`** - Scheduled email with delay

#### 📊 ReportsController (`/api/reports`)

Shows report generation patterns:

- **POST `/sync`** - Synchronous report generation
- **POST `/async`** - Background report generation with result
- **POST `/queue`** - Fire-and-forget report generation
- **POST `/schedule`** - Scheduled report generation
- **POST `/recurring`** - Recurring report setup
- **POST `/trigger/{jobName}`** - Trigger recurring job manually
- **DELETE `/recurring/{jobName}`** - Remove recurring job
- **GET `/types`** - Available report types
- **GET `/cron-examples`** - Common cron expressions

#### ⚙️ JobsController (`/api/jobs`)

Utility endpoints for job management:

- **POST `/cleanup`** - Manual cleanup job
- **POST `/test-email`** - Send test email
- **POST `/bulk-test`** - Create multiple jobs for testing
- **POST `/schedule-demo`** - Schedule demo jobs
- **POST `/setup-recurring-examples`** - Set up example recurring jobs
- **DELETE `/cleanup-recurring-examples`** - Remove example recurring jobs
- **GET `/dashboard-info`** - Hangfire dashboard information

### Services

- **IUserService** - In-memory user management
- **IEmailService** - Mock email service with simulated delays/failures
- **IReportService** - Report generation with different types

### Commands & Queries

- **CreateUserCommand** - Create new users with validation
- **SendEmailCommand** - Send emails with retry logic
- **CleanupCommand** - Clean up old data
- **GetUserQuery** - Retrieve user by ID
- **GenerateReportCommand** - Generate various report types

## 🎯 Key Features Demonstrated

### 1. Fire-and-Forget Jobs

```csharp
_mediator.Enqueue("Create User", command);
```

### 2. Jobs with Return Values

```csharp
var result = await _mediator.EnqueueAsync("Create User", command, retryAttempts: 2);
```

### 3. Scheduled Jobs

```csharp
var jobId = _mediator.Schedule("Send Reminder", command, TimeSpan.FromMinutes(60));
```

### 4. Recurring Jobs

```csharp
_mediator.AddOrUpdate("Daily Cleanup", command, Cron.Daily(2, 0));
```

### 5. Retry Logic

Jobs automatically retry on failure with configurable retry attempts.

### 6. Console Logging

All job logs appear in both application logs and Hangfire dashboard console.

## 🔍 Monitoring Jobs

### Hangfire Dashboard

Visit `/hangfire` to monitor jobs in real-time:

- **Jobs** - View queued, processing, succeeded, and failed jobs
- **Recurring** - Manage recurring job schedules
- **Servers** - Monitor Hangfire server instances
- **Retries** - View and manage failed jobs

### Console Output

Jobs include detailed console output showing:

- Execution progress
- Log messages with timestamps
- Error information and stack traces
- Performance metrics

## 🧪 Testing the Integration

### 1. Basic Fire-and-Forget

```bash
curl -X POST "https://localhost:7000/api/users/async-fire-and-forget" \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@example.com"}'
```

### 2. Background Job with Result

```bash
curl -X POST "https://localhost:7000/api/users/async-with-result" \
  -H "Content-Type: application/json" \
  -d '{"name": "Jane Smith", "email": "jane@example.com"}'
```

### 3. Generate Report Asynchronously

```bash
curl -X POST "https://localhost:7000/api/reports/async" \
  -H "Content-Type: application/json" \
  -d '{"reportType": "usage", "period": "daily"}'
```

### 4. Schedule a Job

```bash
curl -X POST "https://localhost:7000/api/jobs/schedule-demo"
```

### 5. Set Up Recurring Jobs

```bash
curl -X POST "https://localhost:7000/api/jobs/setup-recurring-examples"
```

## 🎨 Customization

### Configuration

The example uses in-memory storage for simplicity. For production:

```csharp
// Use SQL Server for Hangfire storage
services.AddHangfire(config => config.UseSqlServer("connection-string"));

// Use Redis for task coordination
services.AddHangfireMediatR(options =>
{
    options.UseRedis("redis-connection-string");
    options.WithRetryAttempts(3);
    options.WithMaxConcurrentJobs(20);
});
```

### Adding New Commands

1. Create a command class implementing `IRequest` or `IRequest<TResponse>`
2. Create a handler implementing `IRequestHandler<TCommand>` or `IRequestHandler<TCommand, TResponse>`
3. Use the extension methods to execute as background jobs

### Error Handling

The example includes:

- Simulated failures for testing retry logic
- Comprehensive error logging
- Graceful failure handling with meaningful error messages

## 📚 Learn More

- Check the [main README](../../README.md) for full library documentation
- Explore the Hangfire dashboard for job monitoring
- Review the source code for implementation patterns
- Experiment with different job types and configurations

## 🤝 Contributing

Found an issue or want to improve the example? Contributions are welcome!
