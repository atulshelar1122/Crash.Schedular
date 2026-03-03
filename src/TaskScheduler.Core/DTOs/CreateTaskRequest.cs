using System.ComponentModel.DataAnnotations;

namespace TaskScheduler.Core.DTOs;

public class CreateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TaskType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime? ScheduledTime { get; set; }

    [Range(1, 10)]
    public int Priority { get; set; } = 5;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public Guid? TenantId { get; set; }
}

public class CreateRecurringTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TaskType { get; set; } = string.Empty;

    public string? Payload { get; set; }

    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Priority { get; set; } = 5;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
}

public class UpdateRecurringTaskRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public string? Payload { get; set; }

    [MaxLength(100)]
    public string? CronExpression { get; set; }

    public bool? IsEnabled { get; set; }

    [Range(1, 10)]
    public int? Priority { get; set; }

    [Range(0, 10)]
    public int? MaxRetries { get; set; }
}
