using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

internal sealed record AttachReceiptCommand(Guid ReceiptId, ExpenseId ExpenseId, DateTimeOffset AttachedAt);
