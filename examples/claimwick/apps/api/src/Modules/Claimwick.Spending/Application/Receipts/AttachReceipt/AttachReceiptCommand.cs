using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

internal sealed record AttachReceiptCommand(Guid ReceiptId, ExpenseId ExpenseId, DateTimeOffset AttachedAt) : ICommand<AttachReceiptCommandResult>;
