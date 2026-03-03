namespace TaskScheduler.Infrastructure.Messaging;

/// <summary>
/// Configuration POCO for RabbitMQ connection settings. Bound from appsettings.json "RabbitMQ" section.
/// </summary>
public class RabbitMQConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public string ExchangeName { get; set; } = "task-exchange";
    public ushort PrefetchCount { get; set; } = 10;
}
