namespace Library.Application.Lending.Loans.NightlyDueScan;

/// <implements>job/lending.nightly-due-scan</implements>
public sealed class NightlyDueScanJob : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
