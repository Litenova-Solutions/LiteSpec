using System.Security.Claims;
using Claimwick.Spending.Application.Expenses.ExportExpenses;
using Claimwick.Spending.Application.Expenses.ListExpenses;
using Claimwick.Spending.Application.Expenses.SubmitExpense;
using Claimwick.Spending.WebApi.Expenses.ExportExpenses;
using Claimwick.Spending.WebApi.Expenses.ListExpenses;
using Claimwick.Spending.WebApi.Expenses.SubmitExpense;

namespace Claimwick.Spending.WebApi;

/// <summary>The hand-written half of the generated spending endpoints: response shaping and derived values.</summary>
/// <follows>litespec/backend/application#keep-mappings-at-the-owning-boundary</follows>
internal static partial class SpendingEndpoints
{
    /// <summary>Shapes the Submit Expense response.</summary>
    static partial SubmitExpenseResponseModel ShapeSubmitExpenseResponse(SubmitExpenseCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Shapes the List Expenses response.</summary>
    static partial ListExpensesResponseModel ShapeListExpensesResponse(ListExpensesQueryResult result) =>
        new(result.Expenses
            .Select(expense => new ExpenseResponseItemModel(
                expense.ExpenseId.Value,
                expense.Total.Amount,
                expense.Total.Currency,
                expense.State,
                expense.SubmittedAt))
            .ToList());

    /// <summary>Shapes the Export Expenses response.</summary>
    static partial ExportExpensesResponseModel ShapeExportExpensesResponse(ExportExpensesQueryResult result) =>
        new(result.Rows
            .Select(row => new ExportedExpenseResponseItemModel(
                row.ExpenseId.Value,
                row.Amount,
                row.Currency,
                row.ApproverId,
                row.ApprovedAt))
            .ToList(),
            result.CountByCurrency);

    /// <summary>Derives ChangerIsFinance for Change Reminder Preference.</summary>
    static partial bool DeriveChangeReminderPreferenceChangerIsFinance(ClaimsPrincipal user) =>
        user.IsInRole("finance");
}
