namespace Claimwick.Spending.Expenses.UseCases.RefuseExpense.Application;

public sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason);
