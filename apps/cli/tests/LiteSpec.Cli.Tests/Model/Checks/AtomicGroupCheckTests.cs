using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the atomic group checks: changes equal to the bound group, and groups above four aggregates.</summary>
public sealed class AtomicGroupCheckTests
{
    [Fact]
    public void Atomic_Changes_Differing_From_Binds_Reports_Drift()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.md"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "changes": ["aggregate/booking.booking", "aggregate/booking.invoice"], "consistency": {"mode": "atomic", "cites": ["rule/booking.room-still-free"]}}"""));

        Assert.Contains("atomic-group-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Atomic_Changes_Equal_To_Binds_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/booking.room-still-free", Path.Combine(temp.Path, "booking.md"), """{"kind": "atomicity", "statement": "A booking holds its room night.", "governs": ["aggregate/booking.booking"], "binds": ["aggregate/booking.booking", "aggregate/inventory.room-night"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "changes": ["aggregate/booking.booking", "aggregate/inventory.room-night"], "consistency": {"mode": "atomic", "cites": ["rule/booking.room-still-free"]}}"""));

        Assert.DoesNotContain("atomic-group-drift", TestModel.Codes(model));
    }

    [Fact]
    public void Rule_Binding_Five_Aggregates_Reports_Invalid()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/sales.big-order", Path.Combine(temp.Path, "sales.md"), """{"kind": "atomicity", "statement": "A big order ships together.", "governs": ["aggregate/sales.order"], "binds": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment", "aggregate/sales.invoice"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "atomic-group-invalid");
        Assert.Equal("Rule 'rule/sales.big-order' binds 5 aggregates.", finding.Message);
        Assert.Equal("A", finding.Tier);
    }

    [Fact]
    public void Rule_Binding_Four_Aggregates_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/sales.big-order", Path.Combine(temp.Path, "sales.md"), """{"kind": "atomicity", "statement": "A big order ships together.", "governs": ["aggregate/sales.order"], "binds": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment"]}"""));

        Assert.DoesNotContain("atomic-group-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void Atomic_Use_Case_Changing_Five_Aggregates_Without_A_Rule_Reports_Invalid()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/sales.place-order", Path.Combine(temp.Path, "order.md"), """{"kind": "command", "changes": ["aggregate/sales.order", "aggregate/sales.line", "aggregate/sales.charge", "aggregate/sales.shipment", "aggregate/sales.invoice"], "consistency": {"mode": "atomic"}}"""));

        Assert.Contains("atomic-group-invalid", TestModel.Codes(model));
    }
}
