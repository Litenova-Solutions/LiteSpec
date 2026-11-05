namespace Claimwick.Spending.Expenses.UseCases.RefuseExpense;

public sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason);
