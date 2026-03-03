using TaskScheduler.Core.Interfaces.Messaging;

namespace TaskScheduler.Worker.Services;

/// <summary>
/// Background service that starts the RabbitMQ consumer and routes incoming messages
/// to the TaskExecutor for processing.
/// </summary>
public class WorkerHostedService : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerHostedService> _logger;

    public WorkerHostedService(
        IMessageConsumer consumer,
        IServiceProvider serviceProvider,
        ILogger<WorkerHostedService> logger)
    {
        _consumer = consumer;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker hosted service starting, connecting to RabbitMQ...");

        await _consumer.StartConsumingAsync(async (message) =>
        {
            // Each message gets its own DI scope (fresh DbContext per task)
            using var scope = _serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<TaskExecutor>();
            return await executor.ExecuteAsync(message, stoppingToken);
        }, stoppingToken);

        _logger.LogInformation("Worker hosted service connected and consuming messages");

        // Keep alive until shutdown
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker hosted service stopping...");
        await _consumer.StopConsumingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
