namespace TaskScheduler.Core.Models;

public class TaskExecution
{
    public Guid ExecutionId { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ExecutionTimeMs { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public TaskItem Task { get; set; } = null!;
}