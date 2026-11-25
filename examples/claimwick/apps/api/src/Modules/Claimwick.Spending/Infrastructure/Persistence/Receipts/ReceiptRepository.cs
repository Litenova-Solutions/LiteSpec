using Claimwick.Spending.Domain.Receipts;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence.Receipts;

/// <summary>Loads and stages receipts through the scoped document session.</summary>
/// <implements>aggregate/spending.receipt</implements>
internal sealed class ReceiptRepository : IReceiptRepository
{
    private readonly IDocumentSession _session;

    public ReceiptRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Receipt> GetAsync(ReceiptId id, CancellationToken cancellationToken)
    {
        var receipt = await _session.LoadAsync<Receipt>(id, cancellationToken);
        return receipt ?? throw new InvalidOperationException($"No receipt with id '{id}'.");
    }

    public void Store(Receipt receipt)
    {
        _session.Store(receipt);
    }
}
