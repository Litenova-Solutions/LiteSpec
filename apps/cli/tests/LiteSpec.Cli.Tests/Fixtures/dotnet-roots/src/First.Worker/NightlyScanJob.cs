namespace Library.First.Lending;

/// <implements>job/lending.nightly-scan</implements>
public sealed class NightlyScanJob : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
