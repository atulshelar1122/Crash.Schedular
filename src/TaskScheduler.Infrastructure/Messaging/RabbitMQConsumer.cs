using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskScheduler.Core.Interfaces.Messaging;

namespace TaskScheduler.Infrastructure.Messaging;

/// <summary>
/// Consumes task messages from all 3 priority queues (high, normal, low).
/// Uses manual acknowledgment — messages are only ACKed after successful processing.
/// Failed messages are NACKed and requeued or sent to dead-letter queue.
/// </summary>
public class RabbitMQConsumer : IMessageConsumer
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly RabbitMQConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly List<string> _consumerTags = new();

    public RabbitMQConsumer(IOptions<RabbitMQConfiguration> config, ILogger<RabbitMQConsumer> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Starts consuming from all 3 priority queues. The messageHandler receives the raw JSON
    /// and returns true if processing succeeded (ACK) or false if it failed (NACK).
    /// </summary>
    public Task StartConsumingAsync(Func<string, Task<bool>> messageHandler, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config.Host,
            Port = _config.Port,
            UserName = _config.Username,
            Password = _config.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            DispatchConsumersAsync = true // Enable async consumers
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Prefetch: limit how many unacknowledged messages each worker gets at once
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _config.PrefetchCount, global: false);

        // Subscribe to all 3 priority queues — high gets processed first naturally
        // because RabbitMQ delivers from the queue that has messages ready
        var queues = new[] { "tasks.high", "tasks.normal", "tasks.low" };

        foreach (var queue in queues)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var success = await messageHandler(message);

                    if (success)
                    {
                        // ACK: remove message from queue
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        // NACK without requeue: message goes to dead-letter queue
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from {Queue}", queue);
                    // NACK without requeue on exception
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            var tag = _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
            _consumerTags.Add(tag);
            _logger.LogInformation("Started consuming from queue {Queue} with tag {Tag}", queue, tag);
        }

        return Task.CompletedTask;
    }

    public Task StopConsumingAsync(CancellationToken ct = default)
    {
        foreach (var tag in _consumerTags)
        {
            try
            {
                _channel?.BasicCancel(tag);
                _logger.LogInformation("Cancelled consumer {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cancelling consumer {Tag}", tag);
            }
        }
        _consumerTags.Clear();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
