namespace TaskScheduler.Core.Models;

public class RecurringTask
{
    public Guid RecurringTaskId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime NextExecutionTime { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public int Priority { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
