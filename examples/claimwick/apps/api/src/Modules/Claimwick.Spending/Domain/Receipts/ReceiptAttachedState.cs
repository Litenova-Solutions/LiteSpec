using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Receipts;

/// <summary>The receipt is attached to an expense.</summary>
internal sealed record ReceiptAttachedState(ExpenseId ExpenseId, DateTimeOffset AttachedAt) : ReceiptState;
