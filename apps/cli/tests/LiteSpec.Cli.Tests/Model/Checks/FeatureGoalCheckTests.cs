using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the feature goal check against hand-built models.</summary>
public sealed class FeatureGoalCheckTests
{
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Planned_Feature_With_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "advances": ["goal/faster-checkout"]}"""));

        Assert.DoesNotContain("feature-without-goal", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Planned_Feature_Without_Advances_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "feature-without-goal");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Feature 'feature/hold-requests' advances no goal.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }

    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Tier1_Feature_Without_Advances_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "risk": ["money"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "feature-without-goal");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Feature 'feature/hold-requests' advances no goal.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }

    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Refused_Feature_Without_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "refused", "solves": ["problem/hold-delays"], "refusedBecause": "The desk handles holds by hand."}"""));

        Assert.DoesNotContain("feature-without-goal", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Retired_Feature_Without_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "retired", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"]}"""));

        Assert.DoesNotContain("feature-without-goal", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Feature_Carrying_Serves_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "advances": ["goal/faster-checkout"], "serves": ["goal/faster-checkout"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "invalid-field" && found.Message.Contains("names serves", StringComparison.Ordinal));
        Assert.Equal("Feature 'feature/hold-requests' names serves; a feature advances a goal in advances.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }
}
