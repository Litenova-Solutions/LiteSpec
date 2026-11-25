using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a key template names only fields the use case input, reaction trigger or job run carries.</summary>
public sealed class IdempotencyKeyCheckTests
{
    /// <summary>A use case key naming a placeholder outside its input reports.</summary>
    [Fact]
    public void UseCaseKeyOutsideInputReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "input": [{"name": "expenseId", "meaning": "The expense."}], "idempotency": {"mode": "key", "key": "submit-{attemptId}", "keyScope": "tenant", "replay": "returns-first-result"}}"""));

        Assert.Contains("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A use case key naming a placeholder from its input passes.</summary>
    [Fact]
    public void UseCaseKeyFromInputPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "input": [{"name": "expenseId", "meaning": "The expense."}], "idempotency": {"mode": "key", "key": "submit-{expenseId}", "keyScope": "tenant", "replay": "returns-first-result"}}"""));

        Assert.DoesNotContain("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A reaction key naming a placeholder the triggering event does not carry reports.</summary>
    [Fact]
    public void ReactionKeyOutsideTriggerReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.submitted", Path.Combine(temp.Path, "expense.yaml"),
                """{"meaning": "An expense was submitted.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("reaction", "reaction/spending.remind-on-expense-submitted", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/expense.submitted"], "idempotency": {"mode": "key", "key": "remind-{attemptId}", "keyScope": "tenant"}}"""));

        Assert.Contains("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A reaction key naming a placeholder the triggering event carries passes.</summary>
    [Fact]
    public void ReactionKeyFromTriggerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/expense.submitted", Path.Combine(temp.Path, "expense.yaml"),
                """{"meaning": "An expense was submitted.", "carries": [{"name": "expenseId", "type": "text"}]}"""),
            TestModel.Record("reaction", "reaction/spending.remind-on-expense-submitted", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/expense.submitted"], "idempotency": {"mode": "key", "key": "remind-{expenseId}", "keyScope": "tenant"}}"""));

        Assert.DoesNotContain("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A job key naming a placeholder the run use case does not take reports.</summary>
    [Fact]
    public void JobKeyOutsideRunInputReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.erase-expired-data", Path.Combine(temp.Path, "erase.yaml"),
                """{"kind": "command", "input": [{"name": "expenseId", "meaning": "The expense."}]}"""),
            TestModel.Record("job", "job/spending.erase-expired-data", Path.Combine(temp.Path, "job.yaml"),
                """{"kind": "scheduled", "effects": [{"runs": "use-case/spending.erase-expired-data", "timing": "async"}], "idempotency": {"mode": "key", "key": "erase-{attemptId}", "keyScope": "tenant"}}"""));

        Assert.Contains("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A reaction key reaching a member of a carry naming a declared part passes.</summary>
    /// <proves>litespec/core/use-cases#carry-every-key-placeholder</proves>
    [Fact]
    public void ReactionDottedKeyThroughPartPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"),
                """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "renewal"}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"),
                """{"meaning": "A copy was lent.", "carries": [{"name": "latestRenewal", "type": "aggregate/lending.loan#renewal"}]}"""),
            TestModel.Record("reaction", "reaction/lending.remind-on-loan-borrowed", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "idempotency": {"mode": "key", "key": "remind-{latestRenewal.renewedAt}", "keyScope": "tenant"}}"""));

        Assert.DoesNotContain("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A reaction key reaching into a carry of a C# type name reports with the part form as the fix.</summary>
    /// <proves>litespec/core/use-cases#carry-every-key-placeholder</proves>
    [Fact]
    public void ReactionDottedKeyThroughCodeTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"),
                """{"meaning": "A copy was lent.", "carries": [{"name": "posting", "type": "InvoicePostingFacts"}]}"""),
            TestModel.Record("reaction", "reaction/lending.remind-on-loan-borrowed", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "idempotency": {"mode": "key", "key": "remind-{posting.counterparty}", "keyScope": "tenant"}}"""));

        var findings = Checker.Check(model).Findings
            .Where(finding => finding.Code == "idempotency-key-uncarried")
            .ToList();

        Assert.Single(findings);
        Assert.Contains("which the records do not describe", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("name an aggregate part in type", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A reaction key reaching into a carry of scalar type reports.</summary>
    /// <proves>litespec/core/use-cases#carry-every-key-placeholder</proves>
    [Fact]
    public void ReactionDottedKeyThroughScalarReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"),
                """{"meaning": "A copy was lent.", "carries": [{"name": "shelf", "type": "text"}]}"""),
            TestModel.Record("reaction", "reaction/lending.remind-on-loan-borrowed", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "idempotency": {"mode": "key", "key": "remind-{shelf.mark}", "keyScope": "tenant"}}"""));

        var findings = Checker.Check(model).Findings
            .Where(finding => finding.Code == "idempotency-key-uncarried")
            .ToList();

        Assert.Single(findings);
        Assert.Contains("which holds no member", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A reaction key reaching deeper than one member reports.</summary>
    /// <proves>litespec/core/use-cases#carry-every-key-placeholder</proves>
    [Fact]
    public void ReactionDeepKeyReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"),
                """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "renewal"}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"),
                """{"meaning": "A copy was lent.", "carries": [{"name": "latestRenewal", "type": "aggregate/lending.loan#renewal"}]}"""),
            TestModel.Record("reaction", "reaction/lending.remind-on-loan-borrowed", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "idempotency": {"mode": "key", "key": "remind-{latestRenewal.note.mark}", "keyScope": "tenant"}}"""));

        Assert.Contains("idempotency-key-uncarried", TestModel.Codes(model));
    }

    /// <summary>A reaction key reaching a member through a module event field passes when the translated carry names a declared part.</summary>
    /// <proves>litespec/core/use-cases#carry-every-key-placeholder</proves>
    [Fact]
    public void ReactionDottedKeyThroughModuleEventPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"),
                """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "renewal"}]}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, "loan-borrowed.yaml"),
                """{"meaning": "A copy was lent.", "carries": [{"name": "latestRenewal", "type": "aggregate/lending.loan#renewal"}]}"""),
            TestModel.Record("module-event", "module-event/lending.loan-receipt", Path.Combine(temp.Path, "loan-receipt.yaml"),
                """{"summary": "A loan receipt.", "translates": "domain-event/loan.loan-borrowed", "fields": ["latestRenewal"]}"""),
            TestModel.Record("reaction", "reaction/lending.remind-on-loan-receipt", Path.Combine(temp.Path, "remind.yaml"),
                """{"triggeredBy": ["module-event/lending.loan-receipt"], "idempotency": {"mode": "key", "key": "remind-{latestRenewal.renewedAt}", "keyScope": "tenant"}}"""));

        Assert.DoesNotContain("idempotency-key-uncarried", TestModel.Codes(model));
    }
}
