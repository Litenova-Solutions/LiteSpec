namespace Claimwick.Reporting.Views.ReimbursedExpenses;

/// <summary>One reimbursed expense as reporting keeps it: who was paid, how much and when.</summary>
/// <implements>view/reporting.reimbursed-expense</implements>
public sealed record ReimbursedExpense(
    Guid Id,
    Guid? EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt);
