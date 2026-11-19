using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

internal sealed record AttachReceiptCommandResult(ExpenseId ExpenseId, Guid ReceiptId);
