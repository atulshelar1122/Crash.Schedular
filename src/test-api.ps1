$baseUrl = "http://localhost:5000"

Write-Host "=== Testing Task Scheduler API ===" -ForegroundColor Cyan

# 1. Health Check
Write-Host "`n1. Health Check..." -ForegroundColor Yellow
$health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
Write-Host "Status: $($health.status)" -ForegroundColor Green

# 2. Create Email Task
Write-Host "`n2. Creating Email Task..." -ForegroundColor Yellow
$emailTask = @{
    name = "Test Email"
    taskType = "email"
    payload = '{"to":"test@example.com","subject":"Test","body":"Hello"}'
    priority = 5
    maxRetries = 3
} | ConvertTo-Json

$task1 = Invoke-RestMethod -Uri "$baseUrl/api/tasks" -Method Post -Body $emailTask -ContentType "application/json"
Write-Host "Created Task ID: $($task1.taskId)" -ForegroundColor Green

# 3. Create Report Task (High Priority)
Write-Host "`n3. Creating High Priority Report Task..." -ForegroundColor Yellow
$reportTask = @{
    name = "Sales Report"
    taskType = "report"
    payload = '{"reportType":"daily-sales"}'
    priority = 9
    maxRetries = 3
} | ConvertTo-Json

$task2 = Invoke-RestMethod -Uri "$baseUrl/api/tasks" -Method Post -Body $reportTask -ContentType "application/json"
Write-Host "Created Task ID: $($task2.taskId)" -ForegroundColor Green

# 4. Wait and check status
Write-Host "`n4. Waiting 10 seconds for tasks to process..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# 5. Get Metrics
Write-Host "`n5. Fetching Metrics..." -ForegroundColor Yellow
$metrics = Invoke-RestMethod -Uri "$baseUrl/api/metrics" -Method Get
Write-Host "Total Tasks: $($metrics.totalTasks)" -ForegroundColor Green
Write-Host "Completed: $($metrics.completedTasks)" -ForegroundColor Green
Write-Host "Failed: $($metrics.failedTasks)" -ForegroundColor Green
Write-Host "Pending: $($metrics.pendingTasks)" -ForegroundColor Green

# 6. Get All Tasks
Write-Host "`n6. Fetching All Tasks..." -ForegroundColor Yellow
$allTasks = Invoke-RestMethod -Uri "$baseUrl/api/tasks?pageSize=10" -Method Get
Write-Host "Total Count: $($allTasks.totalCount)" -ForegroundColor Green
Write-Host "Tasks:" -ForegroundColor Green
$allTasks.items | ForEach-Object {
    Write-Host "  - $($_.name) [$($_.status)] - Priority: $($_.priority)" -ForegroundColor White
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
