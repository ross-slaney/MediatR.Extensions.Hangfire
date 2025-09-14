# MediatR.Extensions.Hangfire - Distributed Example

This example demonstrates the **real-world distributed processing capabilities** of the MediatR.Extensions.Hangfire library using .NET Aspire for orchestration.

## 🏗️ Architecture

The example showcases a **distributed microservices architecture** with:

- **1 API Container** - Handles HTTP requests and enqueues jobs
- **3 Worker Containers** - Process background jobs with different specializations
- **Redis** - Task coordination and caching
- **SQL Server** - Hangfire job persistence and application data

```
┌─────────────────┐    ┌──────────────┐    ┌─────────────────┐
│   API Container │    │    Redis     │    │  SQL Server     │
│                 │◄──►│              │    │                 │
│ • HTTP Endpoints│    │ • Coordination│    │ • Job Storage   │
│ • Job Enqueueing│    │ • Pub/Sub    │    │ • App Data      │
│ • Swagger UI    │    │ • Caching    │    │                 │
└─────────────────┘    └──────────────┘    └─────────────────┘
         │                      ▲                    ▲
         │                      │                    │
         ▼                      │                    │
┌─────────────────────────────────────────────────────────────┐
│                    Worker Containers                        │
├─────────────────┬─────────────────┬─────────────────────────┤
│   Worker 1      │   Worker 2      │      Worker 3           │
│                 │                 │                         │
│ • General Jobs  │ • Balanced Load │ • Heavy Processing      │
│ • 20 Concurrent │ • 15 Concurrent │ • 5 Concurrent          │
│ • All Queues    │ • All Queues    │ • Reports/Analytics     │
└─────────────────┴─────────────────┴─────────────────────────┘
```

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code with C# extension

### Running the Distributed Example

1. **Navigate to the AppHost directory:**

   ```bash
   cd example/MediatR.Extensions.Hangfire.AppHost
   ```

2. **Start the distributed application:**

   ```bash
   dotnet run
   ```

3. **Access the Aspire Dashboard:**

   - Open https://localhost:15001
   - View all containers, logs, and metrics in real-time

4. **Access the API:**
   - API Swagger: https://localhost:7001/swagger
   - Hangfire Dashboard: https://localhost:7001/hangfire

## 🎯 What You'll See

### Aspire Dashboard

- **Real-time container status** and resource usage
- **Distributed tracing** across API and worker containers
- **Logs aggregation** from all containers
- **Service dependencies** visualization

### Hangfire Dashboard

- **Jobs distributed across workers** with different server names
- **Queue processing** by specialized workers
- **Real-time job execution** with console output
- **Performance metrics** per worker

### Container Specialization

#### API Container (`HANGFIRE_SERVER_ENABLED=false`)

```
🌐 Configuring as API container (job enqueueing only)
📝 Swagger UI: /swagger
📊 Hangfire Dashboard: /hangfire
ℹ️  Container Info: /container-info
```

#### Worker Containers (`HANGFIRE_SERVER_ENABLED=true`)

```
⚙️  Configuring as WORKER container: Worker-1 (max 20 concurrent jobs)
⚙️  Worker container - API endpoints disabled
📊 Hangfire Dashboard: /hangfire
ℹ️  Worker Info: /worker-info
```

## 🧪 Testing Distributed Processing

### 1. Stress Test

```bash
curl -X POST "https://localhost:7001/api/distributeddemo/stress-test?jobCount=100"
```

- Creates 100 jobs distributed across different queues
- Watch them process across multiple workers in Hangfire dashboard

### 2. Mixed Workload with Return Values

```bash
curl -X POST "https://localhost:7001/api/distributeddemo/mixed-workload?reportCount=5&userCount=10"
```

- Demonstrates background processing with result coordination
- Heavy reports and light user creation processed in parallel

### 3. Worker Statistics

```bash
curl "https://localhost:7001/api/distributeddemo/worker-stats"
```

- Shows real-time worker status and queue statistics

### 4. Scheduled Cascade

```bash
curl -X POST "https://localhost:7001/api/distributeddemo/scheduled-cascade"
```

- Creates a sequence of jobs scheduled at 30-second intervals

## 📊 Monitoring & Observability

### Aspire Dashboard Features

- **Resource Monitoring**: CPU, memory, network usage per container
- **Distributed Tracing**: Request flow across API → Redis → Workers
- **Log Aggregation**: Centralized logging from all containers
- **Health Checks**: Container and service health status

### Hangfire Dashboard Features

- **Job Distribution**: See jobs processing across different workers
- **Queue Management**: Monitor queue depths and processing rates
- **Console Output**: Real-time job execution logs
- **Performance Metrics**: Job duration, success rates, retry statistics

### Container Info Endpoints

- **API**: `GET /container-info` - API container details
- **Workers**: `GET /worker-info` - Worker container details

## 🎨 Key Demonstrations

### 1. **Resource Isolation**

- API container handles web requests without job processing overhead
- Worker containers dedicated to background processing
- Independent scaling and resource allocation

### 2. **Fault Tolerance**

- Worker container crashes don't affect API availability
- Jobs automatically redistribute to healthy workers
- Redis coordination ensures no job loss

### 3. **Performance Benefits**

- Parallel processing across multiple workers
- Queue-based load balancing
- Specialized workers for different job types

### 4. **Operational Excellence**

- Centralized monitoring and logging
- Real-time performance metrics
- Easy horizontal scaling

## 🔧 Configuration

### Environment Variables

| Variable                  | API Container | Worker Containers            | Purpose                       |
| ------------------------- | ------------- | ---------------------------- | ----------------------------- |
| `HANGFIRE_SERVER_ENABLED` | `false`       | `true`                       | Enable/disable job processing |
| `DISABLE_API_ENDPOINTS`   | `false`       | `true`                       | Enable/disable HTTP endpoints |
| `WORKER_NAME`             | N/A           | `Worker-1`, `Worker-2`, etc. | Worker identification         |
| `MAX_CONCURRENT_JOBS`     | N/A           | `20`, `15`, `5`              | Concurrent job limits         |

### Queue Specialization

- **`critical`**: High-priority jobs (user operations)
- **`emails`**: Email processing jobs
- **`reports`**: Heavy report generation
- **`cleanup`**: Maintenance and cleanup tasks
- **`default`**: General-purpose jobs

### Scaling Strategy

```yaml
# Production scaling example
api:
  replicas: 3 # Scale based on HTTP traffic
  resources:
    cpu: "1"
    memory: "1Gi"

worker-general:
  replicas: 5 # Scale based on queue depth
  resources:
    cpu: "2"
    memory: "2Gi"

worker-heavy:
  replicas: 2 # Fewer replicas, more resources
  resources:
    cpu: "4"
    memory: "8Gi"
```

## Why Use This Library?

### **Before**: Monolithic Processing

- Single server handles web requests AND background jobs
- Resource contention between HTTP and job processing
- Scaling challenges (over-provision for peak job load)
- Fault propagation (job failures can crash web server)

### **After**: Distributed Processing

- **Independent scaling**: Scale API and workers separately
- **Resource optimization**: Right-size containers for their workload
- **Fault isolation**: Worker crashes don't affect API
- **Performance**: Parallel processing across multiple workers
