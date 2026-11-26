using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

internal sealed record SubmitExpenseCommand(ExpenseId ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId) : ICommand<SubmitExpenseCommandResult>;
