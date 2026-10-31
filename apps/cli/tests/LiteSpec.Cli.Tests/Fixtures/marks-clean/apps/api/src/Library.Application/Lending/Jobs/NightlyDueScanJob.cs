namespace Library.Application.Lending.Jobs;

/// <implements>automation/lending.nightly-due-scan</implements>
public sealed class NightlyDueScanJob(IMailMessenger messenger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return messenger.SendAsync("loan-due-reader", stoppingToken);
    }
}
