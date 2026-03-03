using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.DTOs;

public class TaskFilter
{
    public TaskItemStatus? Status { get; set; }
    public string? TaskType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
