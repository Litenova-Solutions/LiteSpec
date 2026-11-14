namespace Claimwick.Spending.Expenses.UseCases.ApproveExpense.Application;

public sealed record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverId);
