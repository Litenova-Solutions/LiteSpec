using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the atomic group checks: changes equal to the bound group, and groups above four aggregates.</summary>
public sealed class AtomicGroupCheckTests
{
    /// <summary>Atomic Changes Differing From Binds Reports Drift.</summary>
    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void AtomicChangesDifferingFromBindsReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.yaml"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "changes": ["aggregate/booking.booking", "aggregate/booking.invoice"], "consistency": {"mode": "atomic", "cites": ["rule/booking.room-still-free"]}}"""));

        Assert.Contains("atomic-group-drift", TestModel.Codes(model));
    }

    /// <summary>Atomic Changes Equal To Binds Passes.</summary>
    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void AtomicChangesEqualToBindsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.yaml"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "changes": ["aggregate/booking.booking", "aggregate/inventory.room-night"], "consistency": {"mode": "atomic", "cites": ["rule/booking.room-still-free"]}}"""));

        Assert.DoesNotContain("atomic-group-drift", TestModel.Codes(model));
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

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "atomic-group-invalid");
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

        Assert.DoesNotContain("atomic-group-invalid", TestModel.Codes(model));
    }

    /// <summary>Atomic Use Case Changing Five Aggregates Without A Rule Reports Invalid.</summary>
    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void AtomicUseCaseChangingFiveAggregatesWithoutARuleReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/sales.place-order", Path.Combine(temp.Path, "order.yaml"), """{"kind": "command", "changes": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment", "aggregate/sales.invoice"], "consistency": {"mode": "atomic"}}"""));

        Assert.Contains("atomic-group-invalid", TestModel.Codes(model));
    }
}
