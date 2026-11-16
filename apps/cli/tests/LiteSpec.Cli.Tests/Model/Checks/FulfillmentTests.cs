using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves only Behavior records fulfil a requirement.</summary>
public sealed class FulfillmentTests
{
    /// <summary>Behavior Fulfiller Passes.</summary>
    /// <param name="type">The Behavior record type the case uses as the requirement fulfiller.</param>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Theory]
    [InlineData("rule")]
    [InlineData("use-case")]
    [InlineData("reaction")]
    [InlineData("job")]
    [InlineData("notification")]
    [InlineData("setting")]
    [InlineData("screen")]
    // The InlineData values name each Behavior record type case.
    public void Behavior_Fulfiller_Passes(string type)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record(type, FulfillerId(type), Path.Combine(temp.Path, "fulfiller.md"), """{"fulfills": ["requirement/borrowing.max-active-loans"]}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Served Content Entry Fulfills Requirement.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void ServedContentEntryFulfillsRequirement()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "library-app.yaml"), """{"kind": "browser", "auth": ["session"], "servedContent": [{"path": "/loans", "contentType": "text/html", "cacheControl": "public, max-age=3600", "versioning": "content-hash", "fulfills": ["requirement/borrowing.max-active-loans"]}]}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Flow Alone Reports Unfulfilled.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void FlowAloneReportsUnfulfilled()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""),
            TestModel.Record("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "steps": [{"id": "s1", "statement": "The reader borrows the copy.", "cites": ["requirement/borrowing.max-active-loans"]}]}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>No Fulfiller Reports Unfulfilled.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void NoFulfillerReportsUnfulfilled()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement With A Check Is Fulfilled.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void QualityRequirementWithACheckIsFulfilled()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans", "check": "route-suite"}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement Proved By A Test Is Fulfilled.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void QualityRequirementProvedByATestIsFulfilled()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans"}"""));
        model.Proves.Add(new ProvesLink("BorrowingStaysFast", "requirement/borrowing.list-loans-latency", true, true, false));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement With No Check And No Proving Test Reports Unfulfilled.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void QualityRequirementWithNoCheckAndNoProvingTestReportsUnfulfilled()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans"}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>Message Names The Seven Types.</summary>
    /// <proves>litespec/core/intent#restrict-requirement-fulfillment-to-behavior</proves>
    [Fact]
    public void MessageNamesTheSevenTypes()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-fulfillment-missing");
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' is fulfilled by no rule, use case, reaction, job, notification, setting or screen.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <summary>A fulfiller id of the type, in identifier form.</summary>
    /// <param name="type">The fulfiller record type.</param>
    /// <returns>The record id for a fulfiller of the type.</returns>
    static string FulfillerId(string type) => type switch
    {
        "rule" => "rule/borrowing.max-active-loans",
        "use-case" => "use-case/lending.borrow-copy",
        "reaction" => "reaction/lending.send-due-notice-on-loan-borrowed",
        "job" => "job/lending.scan-due",
        "notification" => "notification/lending.loan-due",
        "setting" => "setting/lending.max-active-loans",
        "screen" => "screen/library-app.loans",
        _ => $"{type}/lending.fulfiller",
    };
}
