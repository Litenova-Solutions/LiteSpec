using Record = LiteSpec.Model.Record;
using LiteSpec.Model.Diff;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Diff;

/// <summary>Proves the behavior diff names saga step, exit, driver and purpose changes.</summary>
public sealed class AggregateDiffTests
{
    /// <summary>Added, removed and changed saga steps print one line each.</summary>
    /// <proves>litespec/core/release#review-behavior-changes-through-the-behavior-diff</proves>
    [Fact]
    public void Saga_Step_Changes_Print_Step_Lines()
    {
        using var temp = Cli.Temp("diff-saga");
        var before = TestModel.Record(
            "aggregate", "aggregate/lending.refund", Path.Combine(temp.Path, "refund.md"),
            """
            {"kind": "saga",
             "steps": [
               {"id": "release", "runs": "use-case/lending.release-hold", "compensation": {"mode": "none", "reason": "the hold never moved"}, "guard": "the hold is expired"},
               {"id": "refund", "runs": "use-case/lending.refund-card"}
             ]}
            """);
        var after = TestModel.Record(
            "aggregate", "aggregate/lending.refund", Path.Combine(temp.Path, "refund.md"),
            """
            {"kind": "saga",
             "steps": [
               {"id": "release", "runs": "use-case/lending.release-hold", "compensation": {"mode": "use-case", "useCase": "use-case/lending.rehold"}, "guard": "the hold is released"},
               {"id": "notify", "runs": "use-case/lending.notify-reader"}
             ]}
            """);

        var details = DetailsOf(temp.Path, before, after);

        Assert.Contains("Step removed: refund.", details);
        Assert.Contains("Step added: notify running use-case/lending.notify-reader.", details);
        Assert.Contains("Step release guard changed: \"the hold is released\" (was: \"the hold is expired\").", details);
        Assert.Contains("Step release compensation changed.", details);
    }

    /// <summary>Added and removed exits and a new driver print one line each.</summary>
    /// <proves>litespec/core/release#review-behavior-changes-through-the-behavior-diff</proves>
    [Fact]
    public void Saga_Exit_And_Driver_Changes_Print_Lines()
    {
        using var temp = Cli.Temp("diff-saga");
        var before = TestModel.Record(
            "aggregate", "aggregate/lending.refund", Path.Combine(temp.Path, "refund.md"),
            """
            {"kind": "saga",
             "exits": ["refunded", "abandoned"],
             "drivenBy": [{"automation": "automation/lending.refund-expired-holds"}]}
            """);
        var after = TestModel.Record(
            "aggregate", "aggregate/lending.refund", Path.Combine(temp.Path, "refund.md"),
            """
            {"kind": "saga",
             "exits": ["refunded", "escalated"],
             "drivenBy": [{"automation": "automation/lending.refund-on-request"}]}
            """);

        var details = DetailsOf(temp.Path, before, after);

        Assert.Contains("Exit removed: abandoned.", details);
        Assert.Contains("Exit added: escalated.", details);
        Assert.Contains("Driven by changed from automation/lending.refund-expired-holds to automation/lending.refund-on-request.", details);
    }

    /// <summary>A data class purpose change prints the purpose line beside the other class lines.</summary>
    /// <proves>litespec/core/release#review-behavior-changes-through-the-behavior-diff</proves>
    [Fact]
    public void Data_Class_Purpose_Change_Prints_Purpose_Line()
    {
        using var temp = Cli.Temp("diff-saga");
        var before = TestModel.Record(
            "aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.md"),
            """
            {"kind": "entity",
             "dataClasses": [{"name": "borrower-contact", "classification": "personal", "purpose": "lending notices", "onErasure": "erase"}]}
            """);
        var after = TestModel.Record(
            "aggregate", "aggregate/lending.hold", Path.Combine(temp.Path, "hold.md"),
            """
            {"kind": "entity",
             "dataClasses": [{"name": "borrower-contact", "classification": "personal", "purpose": "overdue notices", "onErasure": "erase"}]}
            """);

        var details = DetailsOf(temp.Path, before, after);

        Assert.Contains("Data class borrower-contact purpose changed: \"overdue notices\" (was: \"lending notices\").", details);
        Assert.DoesNotContain(details, line => line.Contains("classification changed", StringComparison.Ordinal));
        Assert.DoesNotContain(details, line => line.Contains("erasure changed", StringComparison.Ordinal));
    }

    static IReadOnlyList<string> DetailsOf(string root, Record before, Record after)
    {
        var config = TestModel.Config(root);
        var diff = Differ.Diff(
            TestModel.Model(config, before),
            TestModel.Model(config, after),
            "base", "head");

        return diff.Changes.Single().Details;
    }
}
