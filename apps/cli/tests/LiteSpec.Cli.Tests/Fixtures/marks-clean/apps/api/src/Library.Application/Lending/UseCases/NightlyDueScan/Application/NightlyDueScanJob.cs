namespace Library.Application.Lending.UseCases.NightlyDueScan.Application;

/// <implements>job/lending.nightly-due-scan</implements>
public sealed class NightlyDueScanJob(IMailMessenger messenger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return messenger.SendAsync("loan-due-reader", stoppingToken);
    }
}
