using Claimwick.Reporting.Application.ListReimbursedExpenses;
using Claimwick.Reporting.WebApi.ListReimbursedExpenses;

namespace Claimwick.Reporting.WebApi;

/// <summary>The hand-written half of the generated reporting endpoints: response shaping.</summary>
/// <follows>litespec/backend/application#keep-mappings-at-the-owning-boundary</follows>
internal static partial class ReportingEndpoints
{
    /// <summary>Shapes the List Reimbursed Expenses response.</summary>
    static partial ListReimbursedExpensesResponseModel ShapeListReimbursedExpensesResponse(ListReimbursedExpensesQueryResult result) =>
        new(result.Expenses
            .Select(expense => new ReimbursedExpenseResponseItemModel(
                expense.ExpenseId.Value,
                expense.EmployeeId,
                expense.Amount,
                expense.Currency,
                expense.ReimbursedAt))
            .ToList());
}
