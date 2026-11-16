using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the served content checks: served content is a servedContent list on a browser entry point, and each entry fulfills requirements.</summary>
public sealed class ServedContentCheckTests
{
    /// <summary>An entry point of kind served-content reports record-invalid, because served content is a field of a browser entry point.</summary>
    /// <proves>litespec/core/access#describe-every-served-content-entry-with-its-serving-fields</proves>
    [Fact]
    public void ServedContentKindReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "served-content", "auth": ["session"]}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A browser entry point with servedContent passes the shape rule.</summary>
    /// <proves>litespec/core/access#describe-every-served-content-entry-with-its-serving-fields</proves>
    [Fact]
    public void BrowserWithServedContentPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/web.terms-page", Path.Combine(temp.Path, "terms-requirement.yaml"), """{"kind": "functional", "statement": "The product shows its terms of service.", "rationale": "Readers accept the terms before they sign in."}"""),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "browser", "auth": ["session"], "servedContent": [{"path": "/terms", "contentType": "text/html", "cacheControl": "public, max-age=3600", "versioning": "content-hash", "fulfills": ["requirement/web.terms-page"]}]}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A non-browser entry point with servedContent reports record-invalid.</summary>
    /// <proves>litespec/core/access#describe-every-served-content-entry-with-its-serving-fields</proves>
    [Fact]
    public void NonBrowserWithServedContentReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/web.terms-page", Path.Combine(temp.Path, "terms-requirement.yaml"), """{"kind": "functional", "statement": "The product shows its terms of service.", "rationale": "Readers accept the terms before they sign in."}"""),
            TestModel.Record("entry-point", "entry-point/api", Path.Combine(temp.Path, "api.yaml"), """{"kind": "api", "auth": ["token"], "servedContent": [{"path": "/terms", "contentType": "text/html", "cacheControl": "public, max-age=3600", "versioning": "content-hash", "fulfills": ["requirement/web.terms-page"]}]}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>An entry point with top-level fulfills reports record-invalid, because only a served-content entry carries fulfills.</summary>
    /// <proves>litespec/core/access#describe-every-served-content-entry-with-its-serving-fields</proves>
    [Fact]
    public void EntryPointWithTopLevelFulfillsReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/web.terms-page", Path.Combine(temp.Path, "terms-requirement.yaml"), """{"kind": "functional", "statement": "The product shows its terms of service.", "rationale": "Readers accept the terms before they sign in."}"""),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "browser", "auth": ["session"], "fulfills": ["requirement/web.terms-page"]}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A served-content entry fulfilling a goal reports record-invalid, because an entry fulfills requirements.</summary>
    /// <proves>litespec/core/access#describe-every-served-content-entry-with-its-serving-fields</proves>
    [Fact]
    public void ServedContentEntryFulfillingAGoalReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/grow-revenue", Path.Combine(temp.Path, "goal.yaml"), """{"statement": "Grow revenue.", "measure": "Monthly revenue.", "target": "One million."}"""),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "browser", "auth": ["session"], "servedContent": [{"path": "/terms", "contentType": "text/html", "cacheControl": "public, max-age=3600", "versioning": "content-hash", "fulfills": ["goal/grow-revenue"]}]}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }
}
