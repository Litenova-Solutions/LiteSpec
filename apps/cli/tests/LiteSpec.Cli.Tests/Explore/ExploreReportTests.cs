using System.Text.Json.Nodes;
using LiteSpec.Cli.Explore;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for the explore report: fault lines, the JSON report, and the Markdown report.</summary>
public sealed class ExploreReportTests
{
    /// <summary>A fault prints as its step, request and status, with the problem detail when it has one.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a fault prints as its step, request and status, with the problem detail when it has one")]
    public void FormatsFaultLine()
    {
        var fault = new ExploreFault(
            ExploreFault.ServerError,
            "read-expense",
            "GET",
            "/expenses/{expenseId}",
            "/expenses/e-42",
            null,
            500,
            """{"title": "Failed."}""",
            "The list failed.",
            12);

        Assert.Equal(
            "Step 12: GET /expenses/e-42 answered 500. Detail: The list failed.",
            ExploreReport.Line(fault));
    }

    /// <summary>A timeout prints as its step and request with no status.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a timeout prints as its step and request with no status")]
    public void FormatsTimeoutLine()
    {
        var fault = new ExploreFault(
            ExploreFault.Timeout,
            "list-expenses",
            "GET",
            "/expenses",
            "/expenses",
            null,
            0,
            string.Empty,
            null,
            2);

        Assert.Equal("Step 2: GET /expenses timed out.", ExploreReport.Line(fault));
    }

    /// <summary>A fault without a detail prints no detail.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a fault without a detail prints no detail")]
    public void OmitsMissingDetail()
    {
        var fault = new ExploreFault(
            ExploreFault.ClientFormat,
            "create-expense",
            "POST",
            "/expenses",
            "/expenses",
            """{"title": "Lunch"}""",
            400,
            "bad request",
            null,
            5);

        Assert.Equal("Step 5: POST /expenses answered 400.", ExploreReport.Line(fault));
    }

    /// <summary>The JSON report carries the run, the status counts, and each fault's request and detail.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the JSON report carries the run, the status counts, and each fault's request and detail")]
    public void ShapesJsonReport()
    {
        var result = new ExploreWalk.Result(
            10,
            [new ExploreFault(ExploreFault.ServerError, "read-expense", "GET", "/expenses/{expenseId}", "/expenses/e-42", null, 500, """{"title": "Failed."}""", "The list failed.", 4)],
            new Dictionary<string, int>(StringComparer.Ordinal) { ["200"] = 9, ["500"] = 1 },
            3,
            [("/session/sign-out", "Sign-out ends the walker's session.")],
            null);

        var json = ExploreReport.AsJson("http://localhost:5000", "/openapi/v1.json", 7, result);

        Assert.Equal("http://localhost:5000", json["baseUrl"]!.GetValue<string>());
        Assert.Equal("/openapi/v1.json", json["openapi"]!.GetValue<string>());
        Assert.Equal(7, json["seed"]!.GetValue<int>());
        Assert.Equal(10, json["calls"]!.GetValue<int>());
        Assert.Equal(3, json["operationsListed"]!.GetValue<int>());
        var skipped = (JsonArray)json["skipped"]!;
        Assert.Equal("/session/sign-out", skipped[0]!["template"]!.GetValue<string>());
        var counts = (JsonObject)json["statusCounts"]!;
        Assert.Equal(9, counts["200"]!.GetValue<int>());
        Assert.Equal(1, counts["500"]!.GetValue<int>());
        var fault = ((JsonArray)json["faults"]!)[0]!;
        Assert.Equal("server-error", fault["kind"]!.GetValue<string>());
        Assert.Equal("read-expense", fault["operation"]!.GetValue<string>());
        Assert.Equal("GET", fault["method"]!.GetValue<string>());
        Assert.Equal("/expenses/{expenseId}", fault["pathTemplate"]!.GetValue<string>());
        Assert.Equal("/expenses/e-42", fault["path"]!.GetValue<string>());
        Assert.Equal(500, fault["statusCode"]!.GetValue<int>());
        Assert.Equal("The list failed.", fault["Detail"]!.GetValue<string>());
        Assert.Equal(4, fault["step"]!.GetValue<int>());
        Assert.True(json.ContainsKey("stoppedEarly"));
        Assert.Null(json["stoppedEarly"]);
    }

    /// <summary>The Markdown report names the run, each fault, and each skipped route.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the Markdown report names the run, each fault, and each skipped route")]
    public void ShapesMarkdownReport()
    {
        var result = new ExploreWalk.Result(
            10,
            [new ExploreFault(ExploreFault.ServerError, "read-expense", "GET", "/expenses/{expenseId}", "/expenses/e-42", null, 500, """{"title": "Failed."}""", "The list failed.", 4)],
            new Dictionary<string, int>(StringComparer.Ordinal) { ["200"] = 9, ["500"] = 1 },
            3,
            [("/session/sign-out", "Sign-out ends the walker's session.")],
            null);

        var markdown = ExploreReport.AsMarkdown(
            "http://localhost:5000",
            "/openapi/v1.json",
            7,
            new DateTime(2026, 11, 23, 10, 30, 0, DateTimeKind.Utc),
            TimeSpan.FromSeconds(12.5),
            200,
            result);

        Assert.Contains("http://localhost:5000", markdown, StringComparison.Ordinal);
        Assert.Contains("Seed: 7", markdown, StringComparison.Ordinal);
        Assert.Contains("Calls made: 10 of up to 200 steps", markdown, StringComparison.Ordinal);
        Assert.Contains("| 500 | 1 |", markdown, StringComparison.Ordinal);
        Assert.Contains("GET /expenses/{expenseId}", markdown, StringComparison.Ordinal);
        Assert.Contains("Step 4", markdown, StringComparison.Ordinal);
        Assert.Contains("/expenses/e-42", markdown, StringComparison.Ordinal);
        Assert.Contains("The list failed.", markdown, StringComparison.Ordinal);
        Assert.Contains("/session/sign-out", markdown, StringComparison.Ordinal);
        Assert.Contains("new Random(7)", markdown, StringComparison.Ordinal);
    }

    /// <summary>The Markdown report states an empty run plainly.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the Markdown report states an empty run plainly")]
    public void ShapesEmptyMarkdownReport()
    {
        var result = new ExploreWalk.Result(0, [], new Dictionary<string, int>(StringComparer.Ordinal), 0, [], null);

        var markdown = ExploreReport.AsMarkdown(
            "http://localhost:5000",
            "/openapi/v1.json",
            1,
            new DateTime(2026, 11, 23, 10, 30, 0, DateTimeKind.Utc),
            TimeSpan.Zero,
            200,
            result);

        Assert.Contains("No calls were made.", markdown, StringComparison.Ordinal);
        Assert.Contains("No faults found.", markdown, StringComparison.Ordinal);
        Assert.Contains("No listed operation matched the deny list.", markdown, StringComparison.Ordinal);
    }
}
