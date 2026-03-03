using System.Text.Json;
using TaskScheduler.Core.Interfaces.Handlers;

namespace TaskScheduler.Worker.Handlers;

/// <summary>
/// Simulates sending an email. Parses the payload for "to" and "subject" fields.
/// In production, this would call an SMTP service or SendGrid/SES API.
/// </summary>
public class EmailTaskHandler : ITaskHandler
{
    public string TaskType => "email";

    private readonly ILogger<EmailTaskHandler> _logger;

    public EmailTaskHandler(ILogger<EmailTaskHandler> logger)
    {
        _logger = logger;
    }

    public async Task<TaskResult> ExecuteAsync(string payload, CancellationToken ct)
    {
        try
        {
            var emailData = JsonSerializer.Deserialize<JsonElement>(payload);
            var to = emailData.GetProperty("to").GetString() ?? "unknown";
            var subject = emailData.GetProperty("subject").GetString() ?? "no subject";

            _logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);

            // Simulate email sending delay (1-3 seconds)
            await Task.Delay(Random.Shared.Next(1000, 3000), ct);

            _logger.LogInformation("Email sent successfully to {To}", to);
            return TaskResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return TaskResult.Failure($"Email sending failed: {ex.Message}");
        }
    }
}
