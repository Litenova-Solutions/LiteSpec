namespace Claimwick.Application.Spending.Payouts;

/// <implements>external-system/payout-bank</implements>
public interface IPayoutBank
{
    Task SendPayoutBatchAsync(Guid batchId, CancellationToken cancellationToken);

    Task<string> ReadSettlementReportAsync(Guid batchId, CancellationToken cancellationToken);
}
