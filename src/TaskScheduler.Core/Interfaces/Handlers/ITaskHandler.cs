namespace TaskScheduler.Core.Interfaces.Handlers;

public interface ITaskHandler
{
    string TaskType { get; }
    Task<TaskResult> ExecuteAsync(string payload, CancellationToken ct);
}

public class TaskResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static TaskResult Success() => new() { IsSuccess = true };
    public static TaskResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
