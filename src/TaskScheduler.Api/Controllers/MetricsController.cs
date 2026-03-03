using Microsoft.AspNetCore.Mvc;
using TaskScheduler.Core.Interfaces.Repositories;

namespace TaskScheduler.Api.Controllers;

/// <summary>
/// System metrics endpoint — task counts by status for dashboards/monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ITaskRepository _taskRepo;

    public MetricsController(ITaskRepository taskRepo)
    {
        _taskRepo = taskRepo;
    }

    /// <summary>
    /// Returns task counts grouped by status (Pending, Running, Completed, Failed, etc.).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var counts = await _taskRepo.GetTaskCountsByStatusAsync(ct);

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            TaskCounts = counts.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value),
            TotalTasks = counts.Values.Sum()
        });
    }
}
