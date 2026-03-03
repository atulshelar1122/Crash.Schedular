namespace TaskScheduler.Core.Models;

public static class RetryPolicy
{
    private static readonly Random Jitter = new();

    public static TimeSpan CalculateDelay(
        int retryCount,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null)
    {
        var initial = initialDelay ?? TimeSpan.FromSeconds(5);
        var max = maxDelay ?? TimeSpan.FromMinutes(30);

        var delayMs = initial.TotalMilliseconds * Math.Pow(2, retryCount);
        delayMs = Math.Min(delayMs, max.TotalMilliseconds);
        delayMs += Jitter.Next(0, 1000); // Add 0-1000ms jitter

        return TimeSpan.FromMilliseconds(delayMs);
    }

    public static DateTime CalculateNextRetryTime(int retryCount)
    {
        return DateTime.UtcNow.Add(CalculateDelay(retryCount));
    }
}
