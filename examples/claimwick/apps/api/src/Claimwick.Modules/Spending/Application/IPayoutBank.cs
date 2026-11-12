namespace Claimwick.Spending.Application;

/// <summary>The bank that receives payout batches and reports their settlement.</summary>
/// <implements>external-system/payout-bank</implements>
/// <follows>claimwick/conventions#confirm-large-payouts</follows>
public interface IPayoutBank
{
    Task SendPayoutBatchAsync(Guid batchId, CancellationToken cancellationToken);

    Task<string> ReadSettlementReportAsync(Guid batchId, CancellationToken cancellationToken);
}
