namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

internal sealed record SubmitExpenseCommand(Guid ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId);
