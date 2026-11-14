namespace Claimwick.Spending.Expenses.UseCases.SubmitExpense.Application;

public sealed record SubmitExpenseCommand(Guid ExpenseId, Guid EmployeeId, decimal Amount, string Currency, Guid? ReceiptId);
