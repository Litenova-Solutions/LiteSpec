namespace Claimwick.Spending.Expenses.UseCases.ApproveExpense;

public sealed record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverId);
