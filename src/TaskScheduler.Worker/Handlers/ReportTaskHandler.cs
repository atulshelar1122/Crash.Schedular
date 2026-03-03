using System.Text.Json;
using TaskScheduler.Core.Interfaces.Handlers;

namespace TaskScheduler.Worker.Handlers;

/// <summary>
/// Simulates generating a report. Parses "reportType" from payload.
/// In production, this would query data, build a PDF/CSV, and store it.
/// </summary>
public class ReportTaskHandler : ITaskHandler
{
    public string TaskType => "report";

    private readonly ILogger<ReportTaskHandler> _logger;

    public ReportTaskHandler(ILogger<ReportTaskHandler> logger)
    {
        _logger = logger;
    }

    public async Task<TaskResult> ExecuteAsync(string payload, CancellationToken ct)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(payload);
            var reportType = data.GetProperty("reportType").GetString() ?? "unknown";

            _logger.LogInformation("Generating {ReportType} report...", reportType);

            // Simulate report generation (2-5 seconds)
            await Task.Delay(Random.Shared.Next(2000, 5000), ct);

            _logger.LogInformation("{ReportType} report generated successfully", reportType);
            return TaskResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate report");
            return TaskResult.Failure($"Report generation failed: {ex.Message}");
        }
    }
}
