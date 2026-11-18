namespace Claimwick.Spending.Domain.Expenses;

/// <summary>Loads and stages expenses; queries read through the read session instead.</summary>
internal interface IExpenseRepository
{
    /// <summary>Loads one expense by its identity.</summary>
    Task<Expense> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stages a new or changed expense for the pipeline's one commit.</summary>
    void Store(Expense expense);

    /// <summary>Deletes one expense with the personal data it holds.</summary>
    void Delete(Guid id);

    /// <summary>Selects the identities of the decided expenses past their retention.</summary>
    Task<IReadOnlyList<Guid>> SelectExpiredAsync(CancellationToken cancellationToken);
}
