using System.Text.Json.Nodes;
using LiteSpec.Cli.Audit;

namespace LiteSpec.Cli.Tests.Audit;

/// <summary>Tests for audit report shaping: merging, lines, JSON, and screenshot names.</summary>
public sealed class AuditReportTests
{
    /// <summary>Merging folds one finding across widths into one entry with ascending widths.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "merging folds one finding across widths into one entry with ascending widths")]
    public void MergesIdenticalFindingsAcrossWidths()
    {
        var routes = new[] { new AuditRoute("/expenses", "screen/claimwick-app.expense-list") };
        var raw = new[]
        {
            new AuditRawFinding("surface-nesting-invalid", "section > div", "A surface inside another surface draws its own border.", "/expenses", "light", 1440),
            new AuditRawFinding("surface-nesting-invalid", "section > div", "A surface inside another surface draws its own border.", "/expenses", "light", 390),
            new AuditRawFinding("surface-nesting-invalid", "section > div", "A surface inside another surface draws its own border.", "/expenses", "light", 1440),
        };

        var merged = AuditReport.Merge(raw, routes);

        var only = Assert.Single(merged);
        Assert.Equal("surface-nesting-invalid", only.Code);
        Assert.Equal("screen/claimwick-app.expense-list", only.Subject);
        Assert.Equal("A surface inside another surface draws its own border.", only.Message);
        Assert.Equal("/expenses", only.Route);
        Assert.Equal("light", only.Scheme);
        Assert.Equal([390, 1440], only.Widths);
        Assert.Equal("section > div", only.Selector);
    }

    /// <summary>Merging keeps another scheme, selector, or message as its own finding.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "merging keeps another scheme, selector, or message as its own finding")]
    public void KeepsDistinctFindingsSeparate()
    {
        var routes = new[] { new AuditRoute("/expenses", "screen/claimwick-app.expense-list") };
        var raw = new[]
        {
            new AuditRawFinding("text-code-invalid", "main", "The page shows the code 'null'.", "/expenses", "dark", 390),
            new AuditRawFinding("text-code-invalid", "main", "The page shows the code 'null'.", "/expenses", "light", 390),
            new AuditRawFinding("text-code-invalid", "aside", "The page shows the code 'null'.", "/expenses", "light", 390),
        };

        var merged = AuditReport.Merge(raw, routes);

        Assert.Equal(3, merged.Count);
        Assert.Equal("dark", merged[0].Scheme);
        Assert.Equal("aside", merged[1].Selector);
        Assert.Equal("main", merged[2].Selector);
    }

    /// <summary>Merging orders findings by route, then scheme, then code.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "merging orders findings by route, then scheme, then code")]
    public void OrdersMergedFindings()
    {
        var routes = new[]
        {
            new AuditRoute("/expenses", "screen/claimwick-app.expense-list"),
            new AuditRoute("/expenses/new", "screen/claimwick-app.new-expense"),
        };
        var raw = new[]
        {
            new AuditRawFinding("text-code-invalid", "main", "The page shows the code 'null'.", "/expenses/new", "light", 390),
            new AuditRawFinding("field-surface-missing", "form > input", "A field sits on the page background.", "/expenses", "light", 390),
        };

        var merged = AuditReport.Merge(raw, routes);

        Assert.Equal("/expenses", merged[0].Route);
        Assert.Equal("field-surface-missing", merged[0].Code);
        Assert.Equal("/expenses/new", merged[1].Route);
    }

    /// <summary>A finding prints as its route, scheme, widths, code, message, and selector.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a finding prints as its route, scheme, widths, code, message, and selector")]
    public void FormatsFindingLine()
    {
        var finding = new AuditFinding(
            "surface-nesting-invalid",
            "screen/claimwick-app.expense-list",
            "A surface inside another surface draws its own border.",
            "/expenses",
            "light",
            [390, 1440],
            "section > div:nth-of-type(2)");

        Assert.Equal(
            "/expenses [light 390,1440] [surface-nesting-invalid] A surface inside another surface draws its own border. (section > div:nth-of-type(2))",
            AuditReport.Line(finding));
    }

    /// <summary>The JSON report carries the run and each finding's code, subject, and message.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the JSON report carries the run and each finding's code, subject, and message")]
    public void ShapesJsonReport()
    {
        var routes = new[] { new AuditRoute("/expenses", "screen/claimwick-app.expense-list") };
        var findings = new[]
        {
            new AuditFinding(
                "primary-action-duplicate",
                "screen/claimwick-app.expense-list",
                "The view shows 2 primary actions.",
                "/expenses",
                "dark",
                [390],
                "main"),
        };

        var json = AuditReport.AsJson(
            "http://localhost:4173", routes, [390], ["dark"], findings, ["expenses--dark--390.png"]);

        Assert.Equal("http://localhost:4173", json["baseUrl"]!.GetValue<string>());
        Assert.Equal("/expenses", ((JsonArray)json["routes"]!)[0]!["path"]!.GetValue<string>());
        Assert.Equal("screen/claimwick-app.expense-list", ((JsonArray)json["routes"]!)[0]!["subject"]!.GetValue<string>());
        Assert.Equal(390, ((JsonArray)json["widths"]!)[0]!.GetValue<int>());
        Assert.Equal("dark", ((JsonArray)json["schemes"]!)[0]!.GetValue<string>());
        var finding = ((JsonArray)json["findings"]!)[0]!;
        Assert.Equal("primary-action-duplicate", finding["code"]!.GetValue<string>());
        Assert.Equal("screen/claimwick-app.expense-list", finding["subject"]!.GetValue<string>());
        Assert.Equal("The view shows 2 primary actions.", finding["message"]!.GetValue<string>());
        Assert.Equal("/expenses", finding["route"]!.GetValue<string>());
        Assert.Equal("dark", finding["scheme"]!.GetValue<string>());
        Assert.Equal(390, ((JsonArray)finding["widths"]!)[0]!.GetValue<int>());
        Assert.Equal("main", finding["selector"]!.GetValue<string>());
        Assert.Equal("expenses--dark--390.png", ((JsonArray)json["screenshots"]!)[0]!.GetValue<string>());
    }

    /// <summary>A screenshot name follows the capture pattern of route, scheme, and width.</summary>
    /// <param name="route">The route path.</param>
    /// <param name="scheme">The color scheme.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="file">The expected file name.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a screenshot name follows the capture pattern of route, scheme, and width")]
    [InlineData("/expenses/new", "light", 390, "expenses-new--light--390.png")]
    [InlineData("/", "dark", 1440, "home--dark--1440.png")]
    [InlineData("/expenses", "light", 2560, "expenses--light--2560.png")]
    public void NamesScreenshotFiles(string route, string scheme, int width, string file)
    {
        Assert.Equal(file, AuditReport.FileName(route, scheme, width));
    }
}
