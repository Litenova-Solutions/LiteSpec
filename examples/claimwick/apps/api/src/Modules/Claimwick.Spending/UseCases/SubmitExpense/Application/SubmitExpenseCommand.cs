namespace Claimwick.Spending.UseCases.SubmitExpense.Application;

internal sealed record SubmitExpenseCommand(Guid ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId);
