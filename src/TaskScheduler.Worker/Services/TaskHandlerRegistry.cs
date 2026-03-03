using TaskScheduler.Core.Interfaces.Handlers;

namespace TaskScheduler.Worker.Services;

/// <summary>
/// Maps TaskType strings to their ITaskHandler implementations.
/// Built at startup from all registered ITaskHandler instances via DI.
/// </summary>
public class TaskHandlerRegistry : ITaskHandlerRegistry
{
    private readonly Dictionary<string, ITaskHandler> _handlers;

    public TaskHandlerRegistry(IEnumerable<ITaskHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.TaskType, h => h, StringComparer.OrdinalIgnoreCase);
    }

    public ITaskHandler? GetHandler(string taskType)
    {
        _handlers.TryGetValue(taskType, out var handler);
        return handler;
    }

    public IEnumerable<string> GetRegisteredTypes() => _handlers.Keys;
}
