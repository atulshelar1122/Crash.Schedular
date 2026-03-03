using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Infrastructure.Messaging;

/// <summary>
/// Publishes task messages to RabbitMQ. Routes messages to high/normal/low priority queues
/// based on the task's Priority value. Messages are persistent (survive broker restart).
/// </summary>
public class RabbitMQPublisher : IMessagePublisher
{
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQPublisher(IOptions<RabbitMQConfiguration> config, ILogger<RabbitMQPublisher> logger)
    {
        _config = config.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _config.Host,
            Port = _config.Port,
            UserName = _config.Username,
            Password = _config.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        DeclareTopology();
    }

    /// <summary>
    /// Declares the RabbitMQ exchange, queues, and bindings.
    /// - topic exchange for routing by priority
    /// - 3 priority queues + 1 dead-letter queue
    /// - Messages expire after 24 hours
    /// </summary>
    private void DeclareTopology()
    {
        // Declare topic exchange
        _channel.ExchangeDeclare(_config.ExchangeName, ExchangeType.Topic, durable: true);

        // Dead letter exchange and queue
        _channel.ExchangeDeclare("task-dead-letter-exchange", ExchangeType.Direct, durable: true);
        _channel.QueueDeclare("tasks.dead-letter", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("tasks.dead-letter", "task-dead-letter-exchange", "dead-letter");

        // Queue arguments: 24-hour TTL, dead-letter routing
        var queueArgs = new Dictionary<string, object>
        {
            { "x-message-ttl", 86400000 }, // 24 hours in ms
            { "x-dead-letter-exchange", "task-dead-letter-exchange" },
            { "x-dead-letter-routing-key", "dead-letter" }
        };

        // High priority queue (priority >= 8)
        _channel.QueueDeclare("tasks.high", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind("tasks.high", _config.ExchangeName, "task.priority.high");

        // Normal priority queue (priority 4-7)
        _channel.QueueDeclare("tasks.normal", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind("tasks.normal", _config.ExchangeName, "task.priority.normal");

        // Low priority queue (priority < 4)
        _channel.QueueDeclare("tasks.low", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind("tasks.low", _config.ExchangeName, "task.priority.low");

        _logger.LogInformation("RabbitMQ topology declared: exchange={Exchange}, queues=high/normal/low/dead-letter",
            _config.ExchangeName);
    }

    public Task PublishAsync(TaskItem task, CancellationToken ct = default)
    {
        var routingKey = GetRoutingKey(task.Priority);
        var message = JsonSerializer.Serialize(task);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // Survive broker restart
        properties.MessageId = task.TaskId.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _config.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published task {TaskId} to {RoutingKey}", task.TaskId, routingKey);
        return Task.CompletedTask;
    }

    public Task PublishToDeadLetterAsync(TaskItem task, string reason, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(new { Task = task, Reason = reason, Timestamp = DateTime.UtcNow });
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "task-dead-letter-exchange",
            routingKey: "dead-letter",
            basicProperties: properties,
            body: body);

        _logger.LogWarning("Task {TaskId} sent to dead letter queue. Reason: {Reason}", task.TaskId, reason);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Routes task to the correct queue based on priority:
    /// >= 8 → high, 4-7 → normal, &lt; 4 → low
    /// </summary>
    private static string GetRoutingKey(int priority)
    {
        return priority switch
        {
            >= 8 => "task.priority.high",
            >= 4 => "task.priority.normal",
            _ => "task.priority.low"
        };
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
