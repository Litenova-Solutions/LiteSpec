using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves only Behavior records fulfil a requirement.</summary>
public sealed class FulfillmentTests
{
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Theory]
    [InlineData("rule")]
    [InlineData("use-case")]
    [InlineData("automation")]
    [InlineData("notification")]
    [InlineData("setting")]
    public void Behavior_Fulfiller_Passes(string type)
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record(type, FulfillerId(type), Path.Combine(temp.Path, "fulfiller.md"), """{"fulfills": ["requirement/borrowing.max-active-loans"]}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void Journey_Alone_Reports_Unfulfilled()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.md"), """{"actor": ["actor/reader"], "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}], "cites": ["requirement/borrowing.max-active-loans"]}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void No_Fulfiller_Reports_Unfulfilled()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void Message_Names_The_Five_Types()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-fulfillment-missing");
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' is fulfilled by no rule, use case, automation, notification or setting.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void Retired_Requirement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"status": "retired", "kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void Undelivered_Feature_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["feature/hold-requests"]}"""),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(temp.Path, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"]}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>A fulfiller id of the type, in identifier form.</summary>
    static string FulfillerId(string type) => type switch
    {
        "rule" => "rule/borrowing.max-active-loans",
        "use-case" => "use-case/lending.borrow-copy",
        "automation" => "automation/lending.send-due-notice-on-loan-borrowed",
        "notification" => "notification/lending.loan-due",
        "setting" => "setting/lending.max-active-loans",
        _ => $"{type}/lending.fulfiller",
    };
}
