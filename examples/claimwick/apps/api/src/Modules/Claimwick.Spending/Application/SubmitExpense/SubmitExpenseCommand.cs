namespace Claimwick.Spending.Application.SubmitExpense;

internal sealed record SubmitExpenseCommand(Guid ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId);
