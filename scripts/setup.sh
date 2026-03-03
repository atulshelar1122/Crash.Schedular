#!/bin/bash
set -e

echo "========================================="
echo " Distributed Task Scheduler - Setup"
echo "========================================="

echo ""
echo "[1/3] Starting infrastructure (PostgreSQL, RabbitMQ, etcd, pgAdmin)..."
docker-compose up -d

echo ""
echo "[2/3] Waiting for services to be ready..."
sleep 15

echo ""
echo "[3/3] Running EF Core database migrations..."
cd src
dotnet ef database update --project TaskScheduler.Infrastructure --startup-project TaskScheduler.Api

echo ""
echo "========================================="
echo " Setup complete!"
echo "========================================="
echo ""
echo " Start the services in separate terminals:"
echo "   dotnet run --project src/TaskScheduler.Api"
echo "   dotnet run --project src/TaskScheduler.Scheduler"
echo "   dotnet run --project src/TaskScheduler.Worker"
echo ""
echo " Access points:"
echo "   API:        http://localhost:5000"
echo "   Swagger:    http://localhost:5000/swagger"
echo "   RabbitMQ:   http://localhost:15672 (admin/admin123)"
echo "   pgAdmin:    http://localhost:5050 (admin@taskscheduler.com/admin123)"
echo ""
