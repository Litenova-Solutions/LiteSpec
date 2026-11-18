namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.WebApi;

/// <summary>One reimbursed expense row: its identity, the employee it was paid to, the amount and the reimbursement time.</summary>
internal sealed record ReimbursedExpenseResponseItemModel(
    Guid ExpenseId,
    Guid? EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt);
