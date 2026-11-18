namespace Claimwick.Spending.Ports.PayoutBank;

/// <summary>The bank that receives payout batches and reports their settlement.</summary>
/// <implements>external-system/payout-bank</implements>
/// <follows>claimwick/conventions#confirm-large-payouts</follows>
internal interface IPayoutBank
{
    Task SendPayoutBatchAsync(Guid batchId, CancellationToken cancellationToken);

    Task<string> ReadSettlementReportAsync(Guid batchId, CancellationToken cancellationToken);
}
