# Distributed Task Scheduler

A production-grade background job processing system built with C# .NET 8, featuring fault tolerance, horizontal scaling, and comprehensive observability.

## Architecture

```
                    +------------------+
                    |   REST API       |
                    |  (ASP.NET Core)  |
                    +--------+---------+
                             |
                             v
                    +------------------+
                    |   PostgreSQL     |
                    |   (Tasks DB)     |
                    +--------+---------+
                             |
              +--------------+--------------+
              |                             |
     +--------v---------+         +--------v---------+
     |   Scheduler       |         |   Scheduler       |
     |   (Leader)        |         |   (Follower)       |
     |   Polls DB        |         |   Standby          |
     +--------+----------+         +-------------------+
              |
              v
     +------------------+
     |   RabbitMQ       |
     |   task-exchange  |
     +--+-----+-----+--+
        |     |     |
        v     v     v
     high  normal  low      (Priority Queues)
        |     |     |
        v     v     v
     +-----+ +-----+ +-----+
     |Wrkr1| |Wrkr2| |Wrkr3|   (Horizontally Scalable)
     +-----+ +-----+ +-----+
```

## Features

- **REST API** - Submit, query, cancel tasks via HTTP
- **Priority Queuing** - High (8-10), Normal (4-7), Low (1-3) priority queues
- **Recurring Tasks** - Cron-based scheduling (e.g., daily reports)
- **Leader Election** - etcd-based, only one scheduler active at a time
- **Retry with Backoff** - Exponential backoff with jitter on failures
- **Heartbeat Monitoring** - Detects crashed workers, recovers stale tasks
- **Dead Letter Queue** - Failed tasks after max retries go to DLQ
- **Structured Logging** - JSON logs via Serilog
- **Health Checks** - Liveness and readiness probes

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | C# .NET 8 |
| Database | PostgreSQL 15 |
| Message Queue | RabbitMQ 3 |
| Coordination | etcd v3.5.9 |
| ORM | Entity Framework Core 8 |
| Logging | Serilog (structured JSON) |
| Containers | Docker & Docker Compose |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Quick Start

### 1. Start Infrastructure

```bash
docker-compose up -d
```

Wait ~15 seconds for services to initialize.

### 2. Run Database Migrations

```bash
cd src
dotnet ef database update --project TaskScheduler.Infrastructure --startup-project TaskScheduler.Api
```

### 3. Start Services (each in a separate terminal)

```bash
dotnet run --project src/TaskScheduler.Api
dotnet run --project src/TaskScheduler.Scheduler
dotnet run --project src/TaskScheduler.Worker
```

Or use the setup scripts:

```bash
# Linux/Mac
chmod +x scripts/setup.sh && ./scripts/setup.sh

# Windows PowerShell
.\scripts\setup.ps1
```

## API Usage Examples

### Submit a Task

```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Welcome Email",
    "taskType": "email",
    "scheduledTime": "2024-12-25T09:00:00Z",
    "priority": 5,
    "payload": "{\"to\":\"user@example.com\",\"subject\":\"Welcome!\"}"
  }'
```

### Check Task Status

```bash
curl http://localhost:5000/api/tasks/{taskId}
```

### List Tasks with Filters

```bash
curl "http://localhost:5000/api/tasks?status=Completed&page=1&pageSize=10"
```

### Create a Recurring Task (Daily at 6 AM)

```bash
curl -X POST http://localhost:5000/api/recurring-tasks \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Daily Report",
    "cronExpression": "0 6 * * *",
    "taskType": "report",
    "payload": "{\"reportType\":\"daily\"}"
  }'
```

### Get System Metrics

```bash
curl http://localhost:5000/api/metrics
```

### Cancel a Task

```bash
curl -X DELETE http://localhost:5000/api/tasks/{taskId}
```

## Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| API | http://localhost:5000 | - |
| Swagger UI | http://localhost:5000/swagger | - |
| RabbitMQ Management | http://localhost:15672 | admin / admin123 |
| pgAdmin | http://localhost:5050 | admin@taskscheduler.com / admin123 |
| Health Check | http://localhost:5000/health | - |

## Project Structure

```
src/
  TaskScheduler.Core/           # Domain models, interfaces, DTOs (no dependencies)
  TaskScheduler.Infrastructure/  # EF Core, RabbitMQ, etcd implementations
  TaskScheduler.Api/             # REST API (ASP.NET Core)
  TaskScheduler.Scheduler/       # Leader-elected scheduler service
  TaskScheduler.Worker/          # Task execution workers
```

## Key Design Patterns

1. **Repository Pattern** - Abstracts data access behind interfaces
2. **Strategy Pattern** - Task handlers (email, report) selected by TaskType
3. **Leader Election** - etcd lease-based, prevents duplicate scheduling
4. **Exponential Backoff** - `delay = min(5s * 2^retry, 30min) + jitter`
5. **Dead Letter Queue** - Failed tasks after max retries preserved for analysis
6. **Heartbeat Pattern** - Workers prove liveness, stale tasks auto-recovered

## Troubleshooting

### Services won't connect to PostgreSQL/RabbitMQ
```bash
docker-compose ps          # Check all containers are running
docker-compose logs postgres  # Check for errors
docker-compose logs rabbitmq
```

### Database migration fails
```bash
# Ensure EF Core tools are installed
dotnet tool install --global dotnet-ef

# Recreate the migration
cd src
dotnet ef migrations add InitialCreate --project TaskScheduler.Infrastructure --startup-project TaskScheduler.Api
dotnet ef database update --project TaskScheduler.Infrastructure --startup-project TaskScheduler.Api
```

### Tasks stuck in "Queued" status
- Ensure the Worker service is running
- Check RabbitMQ management UI for queue depth
- Verify worker logs for connection errors

### Leader election not working
- Ensure etcd container is running: `docker-compose logs etcd`
- Check scheduler logs for "elected as leader" messages
