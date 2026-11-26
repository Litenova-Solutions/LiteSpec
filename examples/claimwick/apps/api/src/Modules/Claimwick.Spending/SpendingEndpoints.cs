using System.Security.Claims;
using Claimwick.Spending.Application.Expenses.ApproveExpense;
using Claimwick.Spending.Application.Expenses.ErasePersonalData;
using Claimwick.Spending.Application.Expenses.ExportExpenses;
using Claimwick.Spending.Application.Expenses.ListExpenses;
using Claimwick.Spending.Application.Expenses.RefuseExpense;
using Claimwick.Spending.Application.Expenses.ReimburseExpense;
using Claimwick.Spending.Application.Expenses.SubmitExpense;
using Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;
using Claimwick.Spending.Application.Receipts.AttachReceipt;
using Claimwick.Spending.Application.ReminderPreferences.ChangeReminderPreference;
using Claimwick.Spending.WebApi.Expenses.ApproveExpense;
using Claimwick.Spending.WebApi.Expenses.ErasePersonalData;
using Claimwick.Spending.WebApi.Expenses.ExportExpenses;
using Claimwick.Spending.WebApi.Expenses.ListExpenses;
using Claimwick.Spending.WebApi.Expenses.RefuseExpense;
using Claimwick.Spending.WebApi.Expenses.ReimburseExpense;
using Claimwick.Spending.WebApi.Expenses.SubmitExpense;
using Claimwick.Spending.WebApi.Policies.ChangeReceiptThreshold;
using Claimwick.Spending.WebApi.Receipts.AttachReceipt;
using Claimwick.Spending.WebApi.ReminderPreferences.ChangeReminderPreference;

namespace Claimwick.Spending.WebApi;

/// <summary>The hand-written half of the generated spending endpoints: response shaping and derived values.</summary>
/// <follows>litespec/backend/application#keep-mappings-at-the-owning-boundary</follows>
internal static partial class SpendingEndpoints
{
    /// <summary>Shapes the Submit Expense response.</summary>
    private static partial SubmitExpenseResponseModel ShapeSubmitExpenseResponse(SubmitExpenseCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Shapes the List Expenses response.</summary>
    private static partial ListExpensesResponseModel ShapeListExpensesResponse(ListExpensesQueryResult result) =>
        new(result.Expenses
            .Select(expense => new ExpenseResponseItemModel(
                expense.ExpenseId.Value,
                expense.Total.Amount,
                expense.Total.Currency,
                expense.State,
                expense.SubmittedAt,
                expense.ReceiptUris.Select(uri => uri.ToString()).ToList()))
            .ToList());

    /// <summary>Shapes the Export Expenses response.</summary>
    private static partial ExportExpensesResponseModel ShapeExportExpensesResponse(ExportExpensesQueryResult result) =>
        new(result.Rows
            .Select(row => new ExportedExpenseResponseItemModel(
                row.ExpenseId.Value,
                row.Amount,
                row.Currency,
                row.ApproverId,
                row.ApprovedAt))
            .ToList(),
            result.CountByCurrency);

    /// <summary>Shapes the Approve Expense response.</summary>
    private static partial ApproveExpenseResponseModel ShapeApproveExpenseResponse(ApproveExpenseCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Shapes the Attach Receipt response.</summary>
    private static partial AttachReceiptResponseModel ShapeAttachReceiptResponse(AttachReceiptCommandResult result) =>
        new(result.ExpenseId.Value, result.ReceiptId.Value, result.SizeBytes);

    /// <summary>Shapes the Change Receipt Threshold response.</summary>
    private static partial ChangeReceiptThresholdResponseModel ShapeChangeReceiptThresholdResponse(ChangeReceiptThresholdCommandResult result) =>
        new(result.Threshold.Amount, result.Currency);

    /// <summary>Shapes the Change Reminder Preference response.</summary>
    private static partial ChangeReminderPreferenceResponseModel ShapeChangeReminderPreferenceResponse(ChangeReminderPreferenceCommandResult result) =>
        new(result.ManagerId, result.ReceivesReminders);

    /// <summary>Shapes the Erase Personal Data response.</summary>
    private static partial ErasePersonalDataResponseModel ShapeErasePersonalDataResponse(ErasePersonalDataCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Shapes the Refuse Expense response.</summary>
    private static partial RefuseExpenseResponseModel ShapeRefuseExpenseResponse(RefuseExpenseCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Shapes the Reimburse Expense response.</summary>
    private static partial ReimburseExpenseResponseModel ShapeReimburseExpenseResponse(ReimburseExpenseCommandResult result) =>
        new(result.ExpenseId.Value);

    /// <summary>Derives ChangerIsFinance for Change Reminder Preference.</summary>
    private static partial bool DeriveChangeReminderPreferenceChangerIsFinance(ClaimsPrincipal user) =>
        user.IsInRole("finance");
}
