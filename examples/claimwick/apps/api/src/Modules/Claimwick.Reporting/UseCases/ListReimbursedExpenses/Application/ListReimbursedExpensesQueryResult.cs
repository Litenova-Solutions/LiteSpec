using Claimwick.Reporting.ReadModels.ReimbursedExpenses;

namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.Application;

internal sealed record ListReimbursedExpensesQueryResult(IReadOnlyList<ReimbursedExpense> Expenses);
