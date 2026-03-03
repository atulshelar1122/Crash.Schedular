using Microsoft.AspNetCore.Mvc;
using TaskScheduler.Core.DTOs;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Api.Controllers;

/// <summary>
/// REST endpoints for submitting, querying, and cancelling tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepo;
    private readonly ITaskExecutionRepository _executionRepo;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskRepository taskRepo,
        ITaskExecutionRepository executionRepo,
        ILogger<TasksController> logger)
    {
        _taskRepo = taskRepo;
        _executionRepo = executionRepo;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new task for immediate or scheduled execution.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = new TaskItem
        {
            Name = request.Name,
            TaskType = request.TaskType,
            Payload = request.Payload,
            ScheduledTime = request.ScheduledTime ?? DateTime.UtcNow,
            Priority = request.Priority,
            MaxRetries = request.MaxRetries,
            TenantId = request.TenantId
        };

        var created = await _taskRepo.CreateAsync(task, ct);
        _logger.LogInformation("Task {TaskId} submitted: {Name} ({TaskType})",
            created.TaskId, created.Name, created.TaskType);

        return CreatedAtAction(nameof(GetTask), new { id = created.TaskId },
            TaskResponse.FromEntity(created));
    }

    /// <summary>
    /// Get task details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id, CancellationToken ct)
    {
        var task = await _taskRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Task {id} not found" });

        return Ok(TaskResponse.FromEntity(task));
    }

    /// <summary>
    /// List tasks with optional filters (status, type, date range) and pagination.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<TaskResponse>>> ListTasks(
        [FromQuery] TaskItemStatus? status,
        [FromQuery] string? taskType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new TaskFilter
        {
            Status = status,
            TaskType = taskType,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Cap at 100
        };

        var (items, totalCount) = await _taskRepo.GetFilteredAsync(filter, ct);

        return Ok(new PaginatedResult<TaskResponse>
        {
            Items = items.Select(TaskResponse.FromEntity).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    /// <summary>
    /// Cancel a pending or queued task.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelTask(Guid id, CancellationToken ct)
    {
        var task = await _taskRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Task {id} not found" });

        if (task.Status is not (TaskItemStatus.Pending or TaskItemStatus.Queued))
            return BadRequest(new { Message = $"Cannot cancel task in {task.Status} status" });

        task.Status = TaskItemStatus.Cancelled;
        await _taskRepo.UpdateAsync(task, ct);

        _logger.LogInformation("Task {TaskId} cancelled", id);
        return Ok(new { Message = "Task cancelled", TaskId = id });
    }

    /// <summary>
    /// Get execution history for a specific task.
    /// </summary>
    [HttpGet("{id:guid}/executions")]
    public async Task<ActionResult<List<TaskExecutionResponse>>> GetExecutions(Guid id, CancellationToken ct)
    {
        var task = await _taskRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Task {id} not found" });

        var executions = await _executionRepo.GetByTaskIdAsync(id, ct);
        return Ok(executions.Select(TaskExecutionResponse.FromEntity).ToList());
    }
}
