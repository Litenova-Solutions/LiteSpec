using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the feature goal check against hand-built models.</summary>
public sealed class FeatureGoalCheckTests
{
    /// <summary>Planned Feature With Advances Passes.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Planned_Feature_With_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "advances": ["goal/faster-checkout"]}"""));

        Assert.DoesNotContain("feature-goal-missing", TestModel.Codes(model));
    }

    /// <summary>Planned Feature Without Advances Reports At Tier A.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Planned_Feature_Without_Advances_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "feature-goal-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Feature 'feature/hold-requests' advances no goal.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }

    /// <summary>Tier1 Feature Without Advances Reports At Tier G.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Tier1_Feature_Without_Advances_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "risk": ["money"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "feature-goal-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Feature 'feature/hold-requests' advances no goal.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }

    /// <summary>Refused Feature Without Advances Passes.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Refused_Feature_Without_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "refused", "solves": ["problem/hold-delays"], "refusedBecause": "The desk handles holds by hand."}"""));

        Assert.DoesNotContain("feature-goal-missing", TestModel.Codes(model));
    }

    /// <summary>Retired Feature Without Advances Passes.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Retired_Feature_Without_Advances_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "retired", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"]}"""));

        Assert.DoesNotContain("feature-goal-missing", TestModel.Codes(model));
    }

    /// <summary>Feature Carrying Serves Reports Invalid Field.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact]
    public void Feature_Carrying_Serves_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "advances": ["goal/faster-checkout"], "serves": ["goal/faster-checkout"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "field-invalid" && found.Message.Contains("names serves", StringComparison.Ordinal));
        Assert.Equal("Feature 'feature/hold-requests' names serves and advances holds its goal.", finding.Message);
        Assert.Equal("feature/hold-requests", finding.Subject);
    }
}
