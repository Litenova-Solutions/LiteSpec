namespace Claimwick.Spending.Expenses.UseCases.SubmitExpense.WebApi;

/// <summary>The answer to a submit request: the new expense's identity.</summary>
public sealed record SubmitExpenseResponseModel(Guid ExpenseId);
