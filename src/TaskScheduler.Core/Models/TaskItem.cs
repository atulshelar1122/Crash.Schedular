namespace TaskScheduler.Core.Models;

public class TaskItem
{
    public Guid TaskId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int Priority { get; set; } = 5;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public int MaxRetries { get; set; } = 3;
    public int CurrentRetryCount { get; set; } = 0;
    public Guid? TenantId { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation property
    public ICollection<TaskExecution> Executions { get; set; } = new List<TaskExecution>();
}
