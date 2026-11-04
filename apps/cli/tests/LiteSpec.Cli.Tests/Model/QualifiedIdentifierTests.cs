using LiteSpec.Model;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the qualified identifier grammar against the placement table.</summary>
public sealed class QualifiedIdentifierTests
{
    /// <summary>Each record type computes its qualified form from its identifier plus placement.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The short identifier.</param>
    /// <param name="file">The record file, relative to .litespec.</param>
    /// <param name="expected">The qualified form.</param>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Theory]
    [InlineData("product", "product/claimwick", "intent/product.yaml", "product/claimwick")]
    [InlineData("goal", "goal/faster-reimbursement", "intent/goals/faster-reimbursement.yaml", "goal/faster-reimbursement")]
    [InlineData("goal", "goal/faster-reimbursement", "intent/modules/finance/goals/faster-reimbursement.yaml", "finance/goal/faster-reimbursement")]
    [InlineData("problem", "problem/expenses-wait-weeks", "intent/problems/expenses-wait-weeks.yaml", "problem/expenses-wait-weeks")]
    [InlineData("problem", "problem/organizers-wait-for-ticket-money", "intent/modules/finance/problems/organizers-wait-for-ticket-money.yaml", "finance/problem/organizers-wait-for-ticket-money")]
    [InlineData("feature", "feature/payout", "intent/modules/finance/features/payout.yaml", "finance/feature/payout")]
    [InlineData("requirement", "requirement/payout.request-payout", "intent/modules/finance/features/payout.yaml", "finance/feature/payout/requirement/request-payout")]
    [InlineData("constraint", "constraint/tax-receipt-retention", "intent/constraints/tax-receipt-retention.yaml", "constraint/tax-receipt-retention")]
    [InlineData("constraint", "constraint/tax-receipt-retention", "intent/modules/finance/constraints/tax-receipt-retention.yaml", "finance/constraint/tax-receipt-retention")]
    [InlineData("decision", "decision/0001-receipt-threshold", "intent/decisions/0001-receipt-threshold.yaml", "decision/0001-receipt-threshold")]
    [InlineData("decision", "decision/0001-receipt-threshold", "intent/modules/finance/decisions/0001-receipt-threshold.yaml", "finance/decision/0001-receipt-threshold")]
    [InlineData("decision", "decision/0002-store-choice", "implementation/modules/finance/decisions/0002-store-choice.yaml", "finance/decision/0002-store-choice")]
    [InlineData("research", "research/approval-time-benchmarks", "intent/research/approval-time-benchmarks.yaml", "research/approval-time-benchmarks")]
    [InlineData("research", "research/approval-time-benchmarks", "intent/modules/finance/research/approval-time-benchmarks.yaml", "finance/research/approval-time-benchmarks")]
    [InlineData("journey", "journey/submit-and-reimburse", "intent/journeys/submit-and-reimburse.yaml", "journey/submit-and-reimburse")]
    [InlineData("actor", "actor/employee", "intent/actors/employee.yaml", "actor/employee")]
    [InlineData("role", "role/approver", "intent/roles/approver.yaml", "role/approver")]
    [InlineData("module", "module/finance", "intent/modules/finance/module.yaml", "finance")]
    [InlineData("term", "term/expense", "glossary.yaml", "term/expense")]
    [InlineData("aggregate", "aggregate/spending.expense", "behavior/modules/spending/expenses/aggregate.yaml", "spending/expenses")]
    [InlineData("use-case", "use-case/spending.submit-expense", "behavior/modules/spending/expenses/submit-expense.yaml", "spending/expenses/use-case/submit-expense")]
    [InlineData("use-case", "use-case/finance.request-payout", "behavior/modules/finance/use-cases/request-payout.yaml", "finance/use-case/request-payout")]
    [InlineData("result", "result/submit-expense.receipt-missing", "behavior/modules/spending/expenses/submit-expense.yaml", "spending/expenses/use-case/submit-expense/result/receipt-missing")]
    [InlineData("result", "result/request-payout.requested", "behavior/modules/finance/use-cases/request-payout.yaml", "finance/use-case/request-payout/result/requested")]
    [InlineData("result", "result/submit-expense.forbidden", "behavior/modules/spending/expenses/submit-expense.yaml", "spending/expenses/use-case/submit-expense/result/forbidden")]
    [InlineData("scenario", "scenario/submit-expense.submitting-with-a-receipt-succeeds", "behavior/modules/spending/expenses/submit-expense.yaml", "spending/expenses/use-case/submit-expense/scenario/submitting-with-a-receipt-succeeds")]
    [InlineData("event", "event/expense.submitted", "behavior/modules/spending/expenses/aggregate.yaml", "spending/expenses/event/submitted")]
    [InlineData("event", "event/expense.submitted", "behavior/modules/spending/expenses/events/submitted.yaml", "spending/expenses/event/submitted")]
    [InlineData("rule", "rule/expense.approver-is-not-submitter", "behavior/modules/spending/expenses/rules/approver.yaml", "spending/expenses/rule/approver-is-not-submitter")]
    [InlineData("rule", "rule/spending.max-open-expenses", "behavior/modules/spending/rules/limits.yaml", "spending/rule/max-open-expenses")]
    [InlineData("rule", "rule/loan.cross-aggregate-uniqueness", "behavior/rules/uniqueness.yaml", "rule/cross-aggregate-uniqueness")]
    [InlineData("reaction", "reaction/spending.send-approver-reminder", "behavior/modules/spending/reactions/send-approver-reminder.yaml", "spending/reaction/send-approver-reminder")]
    [InlineData("job", "job/spending.expire-lapsed-holds", "behavior/modules/spending/jobs/expire-lapsed-holds.yaml", "spending/job/expire-lapsed-holds")]
    [InlineData("notification", "notification/spending.approval-reminder", "behavior/modules/spending/notifications/approval-reminder.yaml", "spending/notification/approval-reminder")]
    [InlineData("setting", "setting/spending.max-open-expenses", "behavior/modules/spending/settings/limits.yaml", "spending/setting/max-open-expenses")]
    [InlineData("integration-event", "integration-event/expense-submitted", "behavior/modules/spending/integration-events/expense-submitted.yaml", "spending/integration-event/expense-submitted")]
    [InlineData("permission", "permission/approve-expense", "behavior/modules/spending/permissions.yaml", "spending/permission/approve-expense")]
    [InlineData("entry-point", "entry-point/claimwick-app", "behavior/entry-points/claimwick-app/entry-point.yaml", "claimwick-app")]
    [InlineData("screen", "screen/claimwick-app.expense-list", "behavior/entry-points/claimwick-app/screens/expense-list.yaml", "claimwick-app/screen/expense-list")]
    [InlineData("external-system", "external-system/payout-bank", "behavior/external-systems/payout-bank.yaml", "external-system/payout-bank")]
    [InlineData("external-system", "external-system/payout-bank", "behavior/modules/finance/external-systems/payout-bank.yaml", "finance/external-system/payout-bank")]
    [InlineData("runbook", "runbook/clear-stuck-reimbursements", "implementation/runbooks/clear-stuck-reimbursements.yaml", "runbook/clear-stuck-reimbursements")]
    [InlineData("runbook", "runbook/clear-stuck-reimbursements", "implementation/modules/finance/runbooks/clear-stuck-reimbursements.yaml", "finance/runbook/clear-stuck-reimbursements")]
    [InlineData("exception", "exception/payment-retries-later", "governance/exceptions/payment-retries-later.yaml", "exception/payment-retries-later")]
    [InlineData("exception", "exception/export-expenses-unimplemented", "governance/modules/spending/exceptions/export-expenses-unimplemented.yaml", "spending/exception/export-expenses-unimplemented")]
    public void Qualified_Form_Follows_The_Placement_Table(string type, string id, string file, string expected)
    {
        Assert.Equal(expected, QualifiedIdentifier.Of(type, id, file));
    }

    /// <summary>A screen outside its entry point folder falls back to its identifier home.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Screen_Outside_Its_Folder_Falls_Back_To_Its_Home()
    {
        Assert.Equal(
            "library-app/screen/loans",
            QualifiedIdentifier.Of("screen", "screen/library-app.loans", "behavior/screens/library-app.loans.yaml"));
    }

    /// <summary>A module-homed record outside its folder falls back to its identifier home.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Module_Homed_Record_Outside_Its_Folder_Falls_Back_To_Its_Home()
    {
        Assert.Equal(
            "lending/use-case/borrow-copy",
            QualifiedIdentifier.Of("use-case", "use-case/lending.borrow-copy", "elsewhere/borrow-copy.yaml"));
    }

    /// <summary>An unknown type keeps its identifier as its qualified form.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Unknown_Type_Keeps_Its_Identifier()
    {
        Assert.Equal(
            "topic/borrowing",
            QualifiedIdentifier.Of("topic", "topic/borrowing", "product/lending/borrowing.yaml"));
    }

    /// <summary>An empty identifier computes an empty qualified identifier.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Empty_Identifier_Computes_Empty_Qualified_Identifier()
    {
        Assert.Equal("", QualifiedIdentifier.Of("goal", "", "intent/goals/unnamed.yaml"));
    }
}
