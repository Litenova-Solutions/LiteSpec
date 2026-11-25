using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the module-event checks: selected fields carried by the translated domain event, reaction triggers, locality, publishers, read-model sources and effect loops.</summary>
public sealed class ModuleEventCheckTests
{
    /// <summary>A module event selecting a field its domain event does not carry reports event-field-uncarried.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void ModuleEventFieldNotCarriedReportsUncarried()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId", "approver"]}"""));

        Assert.Contains("event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>A module event selecting only carried fields passes the uncarried rule.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void ModuleEventFieldsCarriedPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}, {"name": "amount", "type": "money"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId", "amount"]}"""));

        Assert.DoesNotContain("event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>An integration event publishing a field its domain event does not carry reports event-field-uncarried.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void IntegrationEventPayloadFieldNotCarriedReportsUncarried()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.reimbursed", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was reimbursed.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("integration-event", "integration-event/expense-reimbursed", Path.Combine(temp.Path, "reimbursed.yaml"), """{"action": "send", "translates": "domain-event/expense.reimbursed", "payload": [{"name": "expenseId", "meaning": "The reimbursed expense.", "type": "text", "required": true}, {"name": "paidAt", "meaning": "When the expense was paid.", "type": "instant", "required": true}]}"""));

        Assert.Contains("event-field-uncarried", TestModel.Codes(model));
        Assert.DoesNotContain("integration-event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>A module event field stating its own untyped identifier passes where the domain carries the identifier typed.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventFieldOverrideUntypedIdPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "loanId", "type": "id", "identifies": "aggregate/lending.loan"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": [{"name": "loanId", "type": "id"}]}"""));

        Assert.DoesNotContain("event-field-uncarried", TestModel.Codes(model));
        Assert.False(Reports(model, "record-invalid", "carries as"));
    }

    /// <summary>A module event field stating its own type differing from the translated carry reports.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventFieldOverrideMismatchReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "shelf", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": [{"name": "shelf", "type": "id"}]}"""));

        Assert.True(Reports(model, "record-invalid", "publishes field 'shelf' with type 'id' that its translated domain event 'domain-event/loan.loan-borrowed' carries as 'text'"));
    }

    /// <summary>A module event field identifying another record than the translated carry reports.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventFieldOverrideIdentifiesMismatchReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "copy.yaml"), """{"states": [{"name": "Open", "meaning": "The copy lends."}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "loanId", "type": "id", "identifies": "aggregate/lending.loan"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": [{"name": "loanId", "type": "id", "identifies": "aggregate/lending.copy"}]}"""));

        Assert.True(Reports(model, "record-invalid", "identifying 'aggregate/lending.copy' that its translated domain event 'domain-event/loan.loan-borrowed' carries as 'aggregate/lending.loan'"));
    }

    /// <summary>A module event field identifying no record reports.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventFieldOverrideDanglingIdentifiesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "loanId", "type": "id"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": [{"name": "loanId", "type": "id", "identifies": "aggregate/lending.no-such-loan"}]}"""));

        Assert.True(Reports(model, "reference-unknown", "identifying 'aggregate/lending.no-such-loan', which no record holds"));
    }

    /// <summary>A module event publishing one field twice, as a name and as an entry, reports.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventFieldPublishedTwiceReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "loanId", "type": "id"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": ["loanId", {"name": "loanId", "type": "id"}]}"""));

        Assert.True(Reports(model, "record-invalid", "publishes field 'loanId' more than once"));
    }

    /// <summary>A module event entry naming a field its domain event does not carry reports event-field-uncarried.</summary>
    /// <proves>litespec/core/automation#publish-every-module-event-from-the-event-it-translates</proves>
    [Fact]
    public void ModuleEventObjectFieldNotCarriedReportsUncarried()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"), """{"meaning": "A copy was lent.", "carries": [{"name": "loanId", "type": "id"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"), """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": [{"name": "shelf", "type": "text"}]}"""));

        Assert.Contains("event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>A module event with empty fields passes the uncarried rule, because no field is published.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void ModuleEventWithEmptyFieldsPassesUncarried()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": []}"""));

        Assert.DoesNotContain("event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>A module event with no fields passes the uncarried rule, because the shape check judges the missing list.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void ModuleEventWithoutFieldsPassesUncarried()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved"}"""));

        Assert.DoesNotContain("event-field-uncarried", TestModel.Codes(model));
    }

    /// <summary>An integration event payload field without a type reports record-invalid.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void IntegrationEventPayloadFieldWithoutTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.reimbursed", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was reimbursed.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("integration-event", "integration-event/expense-reimbursed", Path.Combine(temp.Path, "reimbursed.yaml"), """{"action": "send", "translates": "domain-event/expense.reimbursed", "payload": [{"name": "expenseId", "meaning": "The reimbursed expense.", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "holds payload field 'expenseId' with no type"));
    }

    /// <summary>A reaction triggering on a module event of a module it depends on passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void ReactionWithModuleEventTriggerAndDependencyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, "spending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, "payouts.yaml"), """{"dependsOn": ["module/spending"]}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("use-case", "use-case/payouts.pay-expense", Path.Combine(temp.Path, "pay-expense.yaml"), """{"kind": "command"}"""),
            TestModel.Record("reaction", "reaction/payouts.pay-on-expense-approved", Path.Combine(temp.Path, "pay.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"], "effects": [{"runs": "use-case/payouts.pay-expense", "timing": "async"}]}"""));

        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("reaction-trigger-foreign", TestModel.Codes(model));
    }

    /// <summary>A reaction triggering on a module event of a module it does not depend on passes: the module event is the declared crossing.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void ReactionWithModuleEventTriggerWithoutDependencyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, "spending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, "payouts.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("reaction", "reaction/payouts.pay-on-expense-approved", Path.Combine(temp.Path, "pay.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"], "effects": [{"runs": "use-case/payouts.pay-expense", "timing": "async"}]}"""));

        Assert.DoesNotContain("reaction-trigger-foreign", TestModel.Codes(model));
    }

    /// <summary>A reaction triggering on a module event of its own module passes without a declared dependency.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void ReactionWithOwnModuleEventTriggerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, "spending.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("reaction", "reaction/spending.remind-on-expense-approved", Path.Combine(temp.Path, "remind.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"], "effects": [{"runs": "use-case/spending.send-reminder", "timing": "async"}]}"""));

        Assert.DoesNotContain("reaction-trigger-foreign", TestModel.Codes(model));
    }

    /// <summary>A reaction triggering on an outbound integration event reports reaction-trigger-foreign.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void ReactionWithOutboundIntegrationTriggerReportsForeign()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/expense-exported", Path.Combine(temp.Path, "exported.yaml"), """{"action": "send"}"""),
            TestModel.Record("reaction", "reaction/payouts.pay-on-expense-exported", Path.Combine(temp.Path, "pay.yaml"), """{"triggeredBy": ["integration-event/expense-exported"], "effects": [{"runs": "use-case/payouts.pay-expense", "timing": "async"}]}"""));

        Assert.Contains("reaction-trigger-foreign", TestModel.Codes(model));
    }

    /// <summary>A reaction triggering on an inbound integration event passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void ReactionWithInboundIntegrationTriggerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/bank-statement-received", Path.Combine(temp.Path, "statement.yaml"), """{"action": "receive"}"""),
            TestModel.Record("reaction", "reaction/payouts.reconcile-on-statement-received", Path.Combine(temp.Path, "reconcile.yaml"), """{"triggeredBy": ["integration-event/bank-statement-received"], "effects": [{"runs": "use-case/payouts.reconcile-payout", "timing": "async"}]}"""));

        Assert.DoesNotContain("reaction-trigger-foreign", TestModel.Codes(model));
    }

    /// <summary>A reaction in one module triggering on another module's module event passes locality without a declared dependency.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkThroughModuleEventPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("reaction", "reaction/payouts.pay-on-expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "reactions", "pay-on-expense-approved", "pay-on-expense-approved.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>A module event translated from an event two modules raise reports event-publisher-invalid.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void ModuleEventWithTwoPublishersReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "domain-events", "approved", "approved.yaml"), """{"carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "approve-expense", "approve-expense.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""),
            TestModel.Record("use-case", "use-case/payouts.settle-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "use-cases", "settle-expense", "settle-expense.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""));

        Assert.Contains("event-publisher-invalid", TestModel.Codes(model));
    }

    /// <summary>A module event raised only by use cases of its own module passes the publisher rule.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void ModuleEventPublishedFromOwnModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "domain-events", "approved", "approved.yaml"), """{"carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "use-cases", "approve-expense", "approve-expense.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""));

        Assert.DoesNotContain("event-publisher-invalid", TestModel.Codes(model));
    }

    /// <summary>A module event raised only by a use case of another module reports event-publisher-invalid.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void ModuleEventPublishedFromForeignModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/payouts", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "domain-events", "approved", "approved.yaml"), """{"carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("use-case", "use-case/payouts.settle-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "payouts", "use-cases", "settle-expense", "settle-expense.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""));

        Assert.Contains("event-publisher-invalid", TestModel.Codes(model));
    }

    /// <summary>A module event no use case raises passes the publisher rule, because another check judges the unraised event.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void ModuleEventWithNoPublisherPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/spending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "domain-events", "approved", "approved.yaml"), """{"carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""));

        Assert.DoesNotContain("event-publisher-invalid", TestModel.Codes(model));
    }

    /// <summary>A read model field taken from a module event field passes the source rule.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void ReadModelFromModuleEventPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ApprovedModuleEvent(temp.Path),
            ApprovedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "module-event/spending.expense-approved#employeeId", "classification": "internal"}"""),
            Reader(temp.Path));

        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>A read model field naming a field the module event does not select reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void ReadModelFromModuleEventMissingFieldReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ApprovedModuleEvent(temp.Path),
            ReadModel(temp.Path, """{"name": "accountId", "meaning": "The account.", "type": "id", "from": "module-event/spending.expense-approved#accountId", "classification": "internal"}"""),
            Reader(temp.Path));

        Assert.Contains("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>An effect loop through a module event reports effect-loop-invalid.</summary>
    /// <proves>litespec/core/principles#end-every-effect-cycle-in-a-domain-event</proves>
    [Fact]
    public void EffectLoopThroughModuleEventReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("reaction", "reaction/spending.approve-again-on-expense-approved", Path.Combine(temp.Path, "again.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"], "effects": [{"runs": "use-case/spending.approve-expense", "timing": "async"}]}"""));

        Assert.Contains("effect-loop-invalid", TestModel.Codes(model));
    }

    /// <summary>An effect chain through a module event without a loop passes the effect-loop rule.</summary>
    /// <proves>litespec/core/principles#end-every-effect-cycle-in-a-domain-event</proves>
    [Fact]
    public void EffectChainThroughModuleEventWithoutLoopPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "raises": ["domain-event/expense.approved"]}"""),
            TestModel.Record("domain-event", "domain-event/expense.approved", Path.Combine(temp.Path, "expense.yaml"), """{"meaning": "An expense was approved.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("module-event", "module-event/spending.expense-approved", Path.Combine(temp.Path, "approved.yaml"), """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"]}"""),
            TestModel.Record("reaction", "reaction/payouts.pay-on-expense-approved", Path.Combine(temp.Path, "pay.yaml"), """{"triggeredBy": ["module-event/spending.expense-approved"], "effects": [{"runs": "use-case/payouts.pay-expense", "timing": "async"}]}"""),
            TestModel.Record("use-case", "use-case/payouts.pay-expense", Path.Combine(temp.Path, "pay-expense.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("effect-loop-invalid", TestModel.Codes(model));
    }

    /// <summary>The module event of the spending module that a read model reads, with its selected fields.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The module event record.</returns>
    static Record ApprovedModuleEvent(string root) => TestModel.Record(
        "module-event",
        "module-event/spending.expense-approved",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "module-events", "expense-approved", "expense-approved.yaml"),
        """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["employeeId", "amount"]}""");

    /// <summary>The domain event the approved module event translates, carrying the selected fields.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The domain event record.</returns>
    static Record ApprovedDomainEvent(string root) => TestModel.Record(
        "domain-event",
        "domain-event/expense.approved",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "domain-events", "approved", "approved.yaml"),
        """{"meaning": "An expense was approved.", "carries": [{"name": "employeeId", "type": "id"}, {"name": "amount", "type": "money"}]}""");

    /// <summary>The spending aggregate the read model is keyed by.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The aggregate record.</returns>
    static Record ExpenseAggregate(string root) => TestModel.Record(
        "aggregate",
        "aggregate/spending.expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expense", "aggregate.yaml"),
        """{"states": [{"name": "Active", "meaning": "The expense is open."}]}""");

    /// <summary>The read model of approved expenses, keyed by an aggregate of the spending module.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="field">One field entry as JSON.</param>
    /// <returns>The read-model record.</returns>
    static Record ReadModel(string root, string field) => TestModel.Record(
        "read-model",
        "read-model/reporting.approved-expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "read-models", "approved-expense", "approved-expense.yaml"),
        "{\"summary\": \"What Reporting keeps about each approved expense.\", \"keyedBy\": \"aggregate/spending.expense\", \"fields\": [" + field + "], \"freshness\": \"PT5S\"}");

    /// <summary>The query that reads the read model, so the read model is not unread.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The use case record.</returns>
    static Record Reader(string root) => TestModel.Record(
        "use-case",
        "use-case/reporting.total-spending",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "use-cases", "total-spending", "total-spending.yaml"),
        """{"kind": "query", "reads": ["read-model/reporting.approved-expense"]}""");

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <param name="code">The finding code to look for.</param>
    /// <param name="text">The text the finding message must contain.</param>
    /// <returns>True when a finding carries the code and the text.</returns>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
