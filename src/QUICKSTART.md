# Task Scheduler - Quick Start Guide

## Prerequisites
- Docker Desktop running
- .NET 8 SDK installed
- Postman installed

## Step 1: Start Infrastructure
```bash
cd c:\Users\ashelar\source\repos\Crash.Schedular\src
docker-compose up -d
```

Wait 10 seconds for services to be ready.

## Step 2: Start All Services
Double-click `start-all.bat` or run:
```bash
start-all.bat
```

This will open 3 windows:
- **API** (port 5000) - REST API
- **Scheduler** - Picks up tasks and publishes to queue
- **Worker** - Executes tasks

## Step 3: Test with Postman

### Import Collection
1. Open Postman
2. Click **Import**
3. Select `TaskScheduler-Postman-Collection.json`
4. Collection will appear in left sidebar

### Test Requests

**1. Health Check**
- Request: `GET http://localhost:5000/health`
- Expected: `{"status":"Healthy"}`

**2. Create Email Task**
- Request: `POST http://localhost:5000/api/tasks`
- Body:
```json
{
  "name": "Send Welcome Email",
  "taskType": "email",
  "payload": "{\"to\":\"user@example.com\",\"subject\":\"Welcome!\",\"body\":\"Hello World\"}",
  "priority": 5,
  "maxRetries": 3
}
```
- Expected: Returns task with `taskId`
- Copy the `taskId` for next request

**3. Get Task Status**
- Request: `GET http://localhost:5000/api/tasks/{taskId}`
- Replace `{taskId}` with the ID from step 2
- Expected: Task status changes: `Pending` → `Queued` → `Running` → `Completed`

**4. Get Metrics**
- Request: `GET http://localhost:5000/api/metrics`
- Expected: Shows counts by status

**5. Create High Priority Report**
- Request: `POST http://localhost:5000/api/tasks`
- Body:
```json
{
  "name": "Generate Sales Report",
  "taskType": "report",
  "payload": "{\"reportType\":\"monthly-sales\"}",
  "priority": 9,
  "maxRetries": 3
}
```
- Expected: Processes faster than normal priority tasks

## Step 4: Monitor

### Watch Logs
Check the 3 console windows to see:
- API: Task creation logs
- Scheduler: Task pickup and queue publishing
- Worker: Task execution logs

### RabbitMQ Management UI
- URL: http://localhost:15672
- Username: `admin`
- Password: `admin123`
- Check queues: `tasks.high`, `tasks.normal`, `tasks.low`

### Swagger UI
- URL: http://localhost:5000/swagger
- Interactive API documentation

## Common Test Scenarios

### Test Priority Queues
Create 3 tasks with different priorities (3, 5, 9) and watch execution order.

### Test Retry Logic
Create a task with invalid `taskType` to trigger failure and retries.

### Test Recurring Tasks
```json
POST http://localhost:5000/api/recurring-tasks
{
  "name": "Every Minute Test",
  "taskType": "email",
  "payload": "{\"to\":\"test@example.com\"}",
  "cronExpression": "* * * * *",
  "priority": 5
}
```

## Troubleshooting

**Services won't start:**
- Check Docker containers: `docker ps`
- Check ports 5000, 5433, 5672, 2379 are free

**Tasks stuck in Pending:**
- Check Scheduler logs for leader election
- Verify RabbitMQ connection

**Tasks fail immediately:**
- Check Worker logs
- Verify `taskType` is "email" or "report"

## Stop Services
Close the 3 console windows or press Ctrl+C in each.

Stop Docker:
```bash
docker-compose down
```
