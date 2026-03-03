namespace TaskScheduler.Core.Interfaces.Handlers;

public interface ITaskHandlerRegistry
{
    ITaskHandler? GetHandler(string taskType);
    IEnumerable<string> GetRegisteredTypes();
}
