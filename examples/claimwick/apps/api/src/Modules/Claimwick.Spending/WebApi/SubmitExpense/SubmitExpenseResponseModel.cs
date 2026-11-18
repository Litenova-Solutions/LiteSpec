namespace Claimwick.Spending.WebApi.SubmitExpense;

/// <summary>The answer to a submit request: the new expense's identity.</summary>
internal sealed record SubmitExpenseResponseModel(Guid ExpenseId);
