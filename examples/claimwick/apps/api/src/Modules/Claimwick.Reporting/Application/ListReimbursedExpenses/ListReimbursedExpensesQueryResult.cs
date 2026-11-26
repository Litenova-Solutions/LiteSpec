using Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

internal sealed record ListReimbursedExpensesQueryResult(IReadOnlyList<ReimbursedExpense> Expenses);
