using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;

/// <summary>One reimbursed expense as reporting keeps it: who was paid, how much and when.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
internal sealed record ReimbursedExpense(
    ExpenseId Id,
    Guid? EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt);
