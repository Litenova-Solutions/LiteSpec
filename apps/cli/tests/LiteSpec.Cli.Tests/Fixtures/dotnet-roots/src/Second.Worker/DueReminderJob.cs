namespace Library.Second.Lending;

/// <implements>job/lending.due-reminder</implements>
public sealed class DueReminderJob : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
