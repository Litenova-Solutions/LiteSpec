using Claimwick.Spending.Ports.PayoutBank;

namespace Claimwick.Spending.Infrastructure.PayoutBank;

/// <summary>Sends payout batches to the payout bank and reads back its settlement reports.</summary>
/// <implements>external-system/payout-bank</implements>
internal sealed class PayoutBankClient : IPayoutBank
{
    public Task SendPayoutBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<string> ReadSettlementReportAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
