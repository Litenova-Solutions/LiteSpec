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
}
