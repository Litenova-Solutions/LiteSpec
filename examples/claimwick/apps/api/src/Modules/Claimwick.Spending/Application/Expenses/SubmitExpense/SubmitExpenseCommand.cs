using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

internal sealed record SubmitExpenseCommand(ExpenseId ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId);
