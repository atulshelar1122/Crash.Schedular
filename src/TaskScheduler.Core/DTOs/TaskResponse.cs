using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.DTOs;

public class TaskResponse
{
    public Guid TaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MaxRetries { get; set; }
    public int CurrentRetryCount { get; set; }
    public string? WorkerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    public static TaskResponse FromEntity(TaskItem task) => new()
    {
        TaskId = task.TaskId,
        Name = task.Name,
        TaskType = task.TaskType,
        Payload = task.Payload,
        ScheduledTime = task.ScheduledTime,
        Priority = task.Priority,
        Status = task.Status.ToString(),
        MaxRetries = task.MaxRetries,
        CurrentRetryCount = task.CurrentRetryCount,
        WorkerId = task.WorkerId,
        CreatedAt = task.CreatedAt,
        ExecutedAt = task.ExecutedAt,
        ExecutionTimeMs = task.ExecutionTimeMs,
        ErrorMessage = task.ErrorMessage
    };
}

public class RecurringTaskResponse
{
    public Guid RecurringTaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime NextExecutionTime { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public int Priority { get; set; }
    public int MaxRetries { get; set; }
    public DateTime CreatedAt { get; set; }

    public static RecurringTaskResponse FromEntity(RecurringTask task) => new()
    {
        RecurringTaskId = task.RecurringTaskId,
        Name = task.Name,
        TaskType = task.TaskType,
        Payload = task.Payload,
        CronExpression = task.CronExpression,
        IsEnabled = task.IsEnabled,
        NextExecutionTime = task.NextExecutionTime,
        LastExecutionTime = task.LastExecutionTime,
        Priority = task.Priority,
        MaxRetries = task.MaxRetries,
        CreatedAt = task.CreatedAt
    };
}

public class TaskExecutionResponse
{
    public Guid ExecutionId { get; set; }
    public Guid TaskId { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ExecutionTimeMs { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public static TaskExecutionResponse FromEntity(TaskExecution exec) => new()
    {
        ExecutionId = exec.ExecutionId,
        TaskId = exec.TaskId,
        WorkerId = exec.WorkerId,
        StartTime = exec.StartTime,
        EndTime = exec.EndTime,
        Status = exec.Status,
        ExecutionTimeMs = exec.ExecutionTimeMs,
        RetryCount = exec.RetryCount,
        ErrorMessage = exec.ErrorMessage
    };
}
