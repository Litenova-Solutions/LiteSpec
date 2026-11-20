using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a module event is published only when a reaction or read model consumes it.</summary>
public sealed class ModuleEventSubscriberCheckTests
{
    /// <summary>A module event no consumer names reports.</summary>
    [Fact]
    public void ModuleEventWithNoConsumerReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Published(temp.Path));

        Assert.Contains("module-event-unused", TestModel.Codes(model));
    }

    /// <summary>A module event named by a reaction trigger passes.</summary>
    [Fact]
    public void ModuleEventWithReactionConsumerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Published(temp.Path),
            TestModel.Record("reaction", "reaction/paying.pay-on-expense-approved", Path.Combine(temp.Path, "pay.yaml"),
                """{"triggeredBy": ["module-event/spending.expense-approved"]}"""));

        Assert.DoesNotContain("module-event-unused", TestModel.Codes(model));
    }

    /// <summary>A module event named by a read model source passes.</summary>
    [Fact]
    public void ModuleEventWithReadModelConsumerPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Published(temp.Path),
            TestModel.Record("read-model", "read-model/reporting.approved-expense", Path.Combine(temp.Path, "approved.yaml"),
                """{"summary": "An approved expense.", "keyedBy": "aggregate/spending.expense", "fields": [{"name": "expenseId", "meaning": "The expense.", "type": "text", "from": "module-event/spending.expense-approved#expenseId"}]}"""));

        Assert.DoesNotContain("module-event-unused", TestModel.Codes(model));
    }

    /// <summary>A module event named only by a source of another event still reports.</summary>
    [Fact]
    public void ModuleEventNamedByOtherEventReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Published(temp.Path),
            TestModel.Record("integration-event", "integration-event/expense-approved", Path.Combine(temp.Path, "external.yaml"),
                """{"action": "send", "translates": "domain-event/expense.approved", "payload": [{"name": "expenseId", "meaning": "The expense.", "type": "text", "required": true}]}"""));

        Assert.Contains("module-event-unused", TestModel.Codes(model));
    }

    private static LiteSpec.Cli.Model.Records.Record Published(string root) => TestModel.Record(
        "module-event",
        "module-event/spending.expense-approved",
        Path.Combine(root, "approved.yaml"),
        """{"summary": "An expense was approved.", "translates": "domain-event/expense.approved", "fields": ["expenseId"], "compatibility": "Additive fields only."}""");
}
