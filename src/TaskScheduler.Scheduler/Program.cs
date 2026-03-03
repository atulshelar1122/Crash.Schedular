using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskScheduler.Core.Interfaces.Coordination;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Infrastructure.Coordination;
using TaskScheduler.Infrastructure.Data;
using TaskScheduler.Infrastructure.Messaging;
using TaskScheduler.Infrastructure.Repositories;
using TaskScheduler.Scheduler.Services;

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
    builder.Services.AddScoped<IRecurringTaskRepository, RecurringTaskRepository>();
    builder.Services.AddScoped<ITaskExecutionRepository, TaskExecutionRepository>();

    // RabbitMQ
    builder.Services.Configure<RabbitMQConfiguration>(
        builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

    // Leader election — unique node ID per instance
    var nodeId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    var etcdEndpoint = builder.Configuration["Etcd:Endpoint"] ?? "http://localhost:2379";
    builder.Services.AddSingleton<ILeaderElectionService>(sp =>
        new EtcdLeaderElection(etcdEndpoint, nodeId,
            sp.GetRequiredService<ILogger<EtcdLeaderElection>>()));

    // Hosted services (the actual scheduler workers)
    builder.Services.AddHostedService<SchedulerHostedService>();
    builder.Services.AddHostedService<RecurringTaskSchedulerService>();
    builder.Services.AddHostedService<StaleTaskMonitor>();

    var host = builder.Build();

    Log.Information("Scheduler service starting. NodeId={NodeId}", nodeId);
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scheduler service start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
