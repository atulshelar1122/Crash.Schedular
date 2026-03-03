@echo off
echo Starting Task Scheduler Services...

cd /d "%~dp0"

start "API" cmd /k "cd TaskScheduler.Api && dotnet run"
timeout /t 3 /nobreak >nul

start "Scheduler" cmd /k "cd TaskScheduler.Scheduler && dotnet run"
timeout /t 2 /nobreak >nul

start "Worker" cmd /k "cd TaskScheduler.Worker && dotnet run"

echo All services started!
echo API: http://localhost:5000
echo Swagger: http://localhost:5000/swagger
pause
