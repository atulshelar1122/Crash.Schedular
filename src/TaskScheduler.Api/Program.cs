using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Infrastructure.Data;
using TaskScheduler.Infrastructure.Messaging;
using TaskScheduler.Infrastructure.Repositories;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    // Database
    builder.Services.AddDbContext<TaskSchedulerDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

    // Repositories
    builder.Services.AddScoped<ITaskRepository, TaskRepository>();
    builder.Services.AddScoped<IRecurringTaskRepository, RecurringTaskRepository>();
    builder.Services.AddScoped<ITaskExecutionRepository, TaskExecutionRepository>();

    // RabbitMQ Publisher
    builder.Services.Configure<RabbitMQConfiguration>(
        builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

    // Controllers + Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Task Scheduler API", Version = "v1" });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Database")!, name: "postgresql")
        .AddRabbitMQ(rabbitConnectionString:
            $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}:{builder.Configuration["RabbitMQ:Port"]}",
            name: "rabbitmq");

    var app = builder.Build();

    // Run EF Core migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TaskSchedulerDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }

    // Middleware pipeline
    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new()
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));

    Log.Information("Task Scheduler API starting on port 5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
