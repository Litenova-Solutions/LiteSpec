using Claimwick.Reporting.Views.ReimbursedExpenses;

namespace Claimwick.Reporting.UseCases.ListReimbursedExpenses.Application;

public sealed record ListReimbursedExpensesQueryResult(IReadOnlyList<ReimbursedExpense> Expenses);
