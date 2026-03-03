using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringTasks",
                columns: table => new
                {
                    RecurringTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NextExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTasks", x => x.RecurringTaskId);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    CurrentRetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionTimeMs = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.TaskId);
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutions",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExecutionTimeMs = table.Column<int>(type: "integer", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutions", x => x.ExecutionId);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_recurring_next_execution",
                table: "RecurringTasks",
                column: "NextExecutionTime",
                filter: "\"IsEnabled\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_executions_task",
                table: "TaskExecutions",
                columns: new[] { "TaskId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_tasks_scheduling",
                table: "Tasks",
                columns: new[] { "ScheduledTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_tasks_status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_tasks_worker_heartbeat",
                table: "Tasks",
                columns: new[] { "WorkerId", "LastHeartbeat" },
                filter: "\"Status\" = 'Running'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringTasks");

            migrationBuilder.DropTable(
                name: "TaskExecutions");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
