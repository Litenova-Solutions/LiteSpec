namespace Claimwick.Spending.WebApi.Expenses.ApproveExpense;

/// <summary>The answer to an approve request: the decided expense's identity.</summary>
internal sealed record ApproveExpenseResponseModel(Guid ExpenseId);
