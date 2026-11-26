namespace Claimwick.Spending.WebApi.Expenses.RefuseExpense;

/// <summary>The answer to a refuse request: the refused expense's identity.</summary>
internal sealed record RefuseExpenseResponseModel(Guid ExpenseId);
