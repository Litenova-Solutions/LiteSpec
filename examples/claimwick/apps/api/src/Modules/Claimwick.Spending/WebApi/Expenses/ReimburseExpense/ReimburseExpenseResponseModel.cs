namespace Claimwick.Spending.WebApi.Expenses.ReimburseExpense;

/// <summary>The answer to a reimburse request: the reimbursed expense's identity.</summary>
internal sealed record ReimburseExpenseResponseModel(Guid ExpenseId);
