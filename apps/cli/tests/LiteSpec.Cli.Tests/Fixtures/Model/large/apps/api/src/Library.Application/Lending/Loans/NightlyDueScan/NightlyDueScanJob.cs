namespace Library.Application.Lending.Loans.NightlyDueScan;

/// <implements>job/lending.nightly-due-scan</implements>
public sealed class NightlyDueScanJob(IMailMessenger messenger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messenger.SendAsync("loan-due-reader", Guid.Empty, stoppingToken);
    }
}
