Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Distributed Task Scheduler - Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "[1/3] Starting infrastructure (PostgreSQL, RabbitMQ, etcd, pgAdmin)..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "[2/3] Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host ""
Write-Host "[3/3] Running EF Core database migrations..." -ForegroundColor Yellow
Push-Location src
dotnet ef database update --project TaskScheduler.Infrastructure --startup-project TaskScheduler.Api
Pop-Location

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host " Setup complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host " Start the services in separate terminals:" -ForegroundColor White
Write-Host "   dotnet run --project src/TaskScheduler.Api" -ForegroundColor Gray
Write-Host "   dotnet run --project src/TaskScheduler.Scheduler" -ForegroundColor Gray
Write-Host "   dotnet run --project src/TaskScheduler.Worker" -ForegroundColor Gray
Write-Host ""
Write-Host " Access points:" -ForegroundColor White
Write-Host "   API:        http://localhost:5000" -ForegroundColor Gray
Write-Host "   Swagger:    http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host "   RabbitMQ:   http://localhost:15672 (admin/admin123)" -ForegroundColor Gray
Write-Host "   pgAdmin:    http://localhost:5050 (admin@taskscheduler.com/admin123)" -ForegroundColor Gray
Write-Host ""
