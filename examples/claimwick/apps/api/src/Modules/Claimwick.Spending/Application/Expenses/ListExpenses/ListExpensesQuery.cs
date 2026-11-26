using LiteBus.Queries.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ListExpenses;

internal sealed record ListExpensesQuery(Guid EmployeeId) : IQuery<ListExpensesQueryResult>;
