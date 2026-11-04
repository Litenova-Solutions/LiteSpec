namespace Claimwick.Spending.Expenses.RefuseExpense;

public sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason);
