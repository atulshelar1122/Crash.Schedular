using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskScheduler.Core.Interfaces.Handlers;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Infrastructure.Data;
using TaskScheduler.Infrastructure.Messaging;
using TaskScheduler.Infrastructure.Repositories;
using TaskScheduler.Worker.Handlers;
using TaskScheduler.Worker.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Serilog
    builder.Services.AddSerilog((_, config) =>
        config.ReadFrom.Configuration(builder.Configuration));

    // Database
    builder.Services.AddDbContext<TaskSchedulerDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

    // Repositories
    builder.Services.AddScoped<ITaskRepository, TaskRepository>();
    builder.Services.AddScoped<ITaskExecutionRepository, TaskExecutionRepository>();

    // RabbitMQ
    builder.Services.Configure<RabbitMQConfiguration>(
        builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
    builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

    // Task handlers — register each handler, then the registry that maps TaskType → handler
    builder.Services.AddSingleton<ITaskHandler, EmailTaskHandler>();
    builder.Services.AddSingleton<ITaskHandler, ReportTaskHandler>();
    builder.Services.AddSingleton<ITaskHandlerRegistry, TaskHandlerRegistry>();

    // Task executor (does the actual work)
    builder.Services.AddScoped<TaskExecutor>();

    // Worker hosted service
    builder.Services.AddHostedService<WorkerHostedService>();

    var workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    builder.Services.AddSingleton(new WorkerInfo { WorkerId = workerId });

    var host = builder.Build();

    Log.Information("Worker service starting. WorkerId={WorkerId}", workerId);
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker service start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Simple holder for the unique worker ID, injected via DI.
/// </summary>
public class WorkerInfo
{
    public string WorkerId { get; set; } = string.Empty;
}
