using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the atomicity checks: writes equal to the bound group, and groups above four aggregates.</summary>
public sealed class AtomicityCheckTests
{
    /// <summary>Atomic Writes Differing From Binds Reports Drift.</summary>
    /// <proves>litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands</proves>
    [Fact]
    public void AtomicWritesDifferingFromBindsReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.yaml"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "writes": ["aggregate/booking.booking", "aggregate/booking.invoice"], "consistency": {"mode": "atomic", "boundBy": ["rule/booking.room-still-free"]}}"""));

        Assert.Contains("atomicity-drift", TestModel.Codes(model));
    }

    /// <summary>Atomic Writes Equal To Binds Passes.</summary>
    /// <proves>litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands</proves>
    [Fact]
    public void AtomicWritesEqualToBindsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.yaml"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "writes": ["aggregate/booking.booking", "aggregate/inventory.room-night"], "consistency": {"mode": "atomic", "boundBy": ["rule/booking.room-still-free"]}}"""));

        Assert.DoesNotContain("atomicity-drift", TestModel.Codes(model));
    }

    /// <summary>Rule Binding Five Aggregates Reports Invalid.</summary>
    /// <proves>litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule</proves>
    [Fact]
    public void RuleBindingFiveAggregatesReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/sales.big-order", Path.Combine(temp.Path, "sales.yaml"), """{"kind": "atomicity", "statement": "A big order ships together.", "governs": ["aggregate/sales.order"], "binds": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment", "aggregate/sales.invoice"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "atomicity-invalid");
        Assert.Equal("Rule 'rule/sales.big-order' binds 5 aggregates.", finding.Message);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Rule Binding Four Aggregates Passes.</summary>
    /// <proves>litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule</proves>
    [Fact]
    public void RuleBindingFourAggregatesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/sales.big-order", Path.Combine(temp.Path, "sales.yaml"), """{"kind": "atomicity", "statement": "A big order ships together.", "governs": ["aggregate/sales.order"], "binds": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment"]}"""));

        Assert.DoesNotContain("atomicity-invalid", TestModel.Codes(model));
    }

    /// <summary>Atomic Use Case Writing Five Aggregates Without A Rule Reports Invalid.</summary>
    /// <proves>litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands</proves>
    [Fact]
    public void AtomicUseCaseWritingFiveAggregatesWithoutARuleReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/sales.place-order", Path.Combine(temp.Path, "order.yaml"), """{"kind": "command", "writes": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment", "aggregate/sales.invoice"], "consistency": {"mode": "atomic"}}"""));

        Assert.Contains("atomicity-invalid", TestModel.Codes(model));
    }
}
