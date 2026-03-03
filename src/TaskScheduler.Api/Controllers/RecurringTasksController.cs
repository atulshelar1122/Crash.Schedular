using Cronos;
using Microsoft.AspNetCore.Mvc;
using TaskScheduler.Core.DTOs;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Api.Controllers;

/// <summary>
/// CRUD endpoints for recurring task templates (cron-based schedules).
/// </summary>
[ApiController]
[Route("api/recurring-tasks")]
public class RecurringTasksController : ControllerBase
{
    private readonly IRecurringTaskRepository _recurringRepo;
    private readonly ILogger<RecurringTasksController> _logger;

    public RecurringTasksController(
        IRecurringTaskRepository recurringRepo,
        ILogger<RecurringTasksController> logger)
    {
        _recurringRepo = recurringRepo;
        _logger = logger;
    }

    /// <summary>
    /// Create a new recurring task with a cron schedule.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RecurringTaskResponse>> Create(
        [FromBody] CreateRecurringTaskRequest request, CancellationToken ct)
    {
        // Validate cron expression
        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(request.CronExpression);
        }
        catch (CronFormatException ex)
        {
            return BadRequest(new { Message = $"Invalid cron expression: {ex.Message}" });
        }

        var nextExecution = cron.GetNextOccurrence(DateTime.UtcNow);
        if (nextExecution == null)
            return BadRequest(new { Message = "Cron expression has no future occurrences" });

        var task = new RecurringTask
        {
            Name = request.Name,
            TaskType = request.TaskType,
            Payload = request.Payload,
            CronExpression = request.CronExpression,
            Priority = request.Priority,
            MaxRetries = request.MaxRetries,
            NextExecutionTime = nextExecution.Value
        };

        var created = await _recurringRepo.CreateAsync(task, ct);
        _logger.LogInformation("Recurring task {Id} created: {Name} ({Cron})",
            created.RecurringTaskId, created.Name, created.CronExpression);

        return CreatedAtAction(nameof(GetById), new { id = created.RecurringTaskId },
            RecurringTaskResponse.FromEntity(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurringTaskResponse>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _recurringRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Recurring task {id} not found" });

        return Ok(RecurringTaskResponse.FromEntity(task));
    }

    [HttpGet]
    public async Task<ActionResult<List<RecurringTaskResponse>>> GetAll(CancellationToken ct)
    {
        var tasks = await _recurringRepo.GetAllAsync(ct);
        return Ok(tasks.Select(RecurringTaskResponse.FromEntity).ToList());
    }

    /// <summary>
    /// Update a recurring task's properties (name, cron, enabled, etc.).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecurringTaskResponse>> Update(
        Guid id, [FromBody] UpdateRecurringTaskRequest request, CancellationToken ct)
    {
        var task = await _recurringRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Recurring task {id} not found" });

        if (request.Name != null) task.Name = request.Name;
        if (request.Payload != null) task.Payload = request.Payload;
        if (request.IsEnabled.HasValue) task.IsEnabled = request.IsEnabled.Value;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.MaxRetries.HasValue) task.MaxRetries = request.MaxRetries.Value;

        if (request.CronExpression != null)
        {
            CronExpression cron;
            try
            {
                cron = CronExpression.Parse(request.CronExpression);
            }
            catch (CronFormatException ex)
            {
                return BadRequest(new { Message = $"Invalid cron expression: {ex.Message}" });
            }

            task.CronExpression = request.CronExpression;
            var next = cron.GetNextOccurrence(DateTime.UtcNow);
            if (next.HasValue)
                task.NextExecutionTime = next.Value;
        }

        await _recurringRepo.UpdateAsync(task, ct);
        return Ok(RecurringTaskResponse.FromEntity(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var task = await _recurringRepo.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound(new { Message = $"Recurring task {id} not found" });

        await _recurringRepo.DeleteAsync(id, ct);
        _logger.LogInformation("Recurring task {Id} deleted", id);
        return Ok(new { Message = "Recurring task deleted", RecurringTaskId = id });
    }
}
