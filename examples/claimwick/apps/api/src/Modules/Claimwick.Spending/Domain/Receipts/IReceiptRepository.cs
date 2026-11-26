namespace Claimwick.Spending.Domain.Receipts;

/// <summary>Loads and stages receipts; queries read through the read session instead.</summary>
internal interface IReceiptRepository
{
    /// <summary>Loads one receipt by its identity.</summary>
    Task<Receipt> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stages a new or changed receipt for the pipeline's one commit.</summary>
    void Store(Receipt receipt);
}
