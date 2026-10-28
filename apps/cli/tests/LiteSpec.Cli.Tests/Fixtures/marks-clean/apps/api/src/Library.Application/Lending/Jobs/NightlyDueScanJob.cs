namespace Library.Application.Lending.Jobs;

/// <implements>job/lending.nightly-due-scan</implements>
public sealed class NightlyDueScanJob : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
