using System.Text.Json.Nodes;
using LiteSpec.Cli.Audit;

namespace LiteSpec.Cli.Tests.Audit;

/// <summary>Tests for audit finding shaping: merging, lines, JSON, screenshot names, and failed requests.</summary>
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

    /// <summary>A failed-request finding prints its route, scheme, widths, code, message, and selector.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a failed-request finding prints its route, scheme, widths, code, message, and selector")]
    public void FormatsFailedRequestLine()
    {
        var finding = new AuditFinding(
            "page-request-failed",
            "screen/claimwick-app.expense-list",
            "A request the page made fails: GET /api/reports answers 500.",
            "/expenses",
            "light",
            [390],
            "page");

        Assert.Equal(
            "/expenses [light 390] [page-request-failed] A request the page made fails: GET /api/reports answers 500. (page)",
            AuditReport.Line(finding));
    }

    /// <summary>A 5xx same-origin fetch or XHR response names its method, path, and status, without the query.</summary>
    /// <param name="type">The network resource type.</param>
    /// <param name="method">The request method.</param>
    /// <param name="status">The response status.</param>
    /// <param name="statusText">The response status as text.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a 5xx same-origin fetch or XHR response names its method, path, and status, without the query")]
    [InlineData("Fetch", "GET", 500, "500")]
    [InlineData("XHR", "POST", 503, "503")]
    public void FailedResponseNamesMethodPathAndStatus(string type, string method, int status, string statusText)
    {
        var sent = new Dictionary<string, (string Method, string Url)>(StringComparer.Ordinal)
        {
            ["request-1"] = (method, "http://localhost:4173/api/reports?period=2026-10"),
        };
        var parameters = new JsonObject
        {
            ["type"] = type,
            ["requestId"] = "request-1",
            ["response"] = new JsonObject
            {
                ["url"] = "http://localhost:4173/api/reports?period=2026-10",
                ["status"] = status,
            },
        };

        var message = AuditBrowser.FailedResponse(parameters, sent, "http://localhost:4173");

        Assert.NotNull(message);
        Assert.Contains(method, message, StringComparison.Ordinal);
        Assert.Contains("/api/reports", message, StringComparison.Ordinal);
        Assert.Contains(statusText, message, StringComparison.Ordinal);
        Assert.DoesNotContain("period=2026-10", message, StringComparison.Ordinal);
    }

    /// <summary>A response that is not a failed same-origin page request reports nothing.</summary>
    /// <param name="type">The network resource type.</param>
    /// <param name="url">The response address.</param>
    /// <param name="status">The response status.</param>
    /// <param name="tracked">Whether the request was tracked from requestWillBeSent.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a response that is not a failed same-origin page request reports nothing")]
    [InlineData("XHR", "http://localhost:4173/api/reports", 200, true)]
    [InlineData("XHR", "http://localhost:4173/api/reports", 404, true)]
    [InlineData("XHR", "http://localhost:4173/api/reports", 499, true)]
    [InlineData("Document", "http://localhost:4173/expenses", 500, true)]
    [InlineData("XHR", "https://cdn.example.net/lib.js", 500, true)]
    [InlineData("Fetch", "http://localhost:4173/api/reports", 500, false)]
    public void FailedResponseIgnoresOtherResponses(string type, string url, int status, bool tracked)
    {
        var sent = new Dictionary<string, (string Method, string Url)>(StringComparer.Ordinal);

        if (tracked)
        {
            sent["request-1"] = ("GET", url);
        }

        var parameters = new JsonObject
        {
            ["type"] = type,
            ["requestId"] = "request-1",
            ["response"] = new JsonObject
            {
                ["url"] = url,
                ["status"] = status,
            },
        };

        Assert.Null(AuditBrowser.FailedResponse(parameters, sent, "http://localhost:4173"));
    }

    /// <summary>A failed same-origin fetch or XHR load names its method, path, and error, without the query.</summary>
    /// <param name="errorText">The network error text, or null when the event holds none.</param>
    /// <param name="reason">The reason the message names.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a failed same-origin fetch or XHR load names its method, path, and error, without the query")]
    [InlineData("net::ERR_CONNECTION_REFUSED", "net::ERR_CONNECTION_REFUSED")]
    [InlineData(null, "the network failed")]
    public void FailedLoadNamesMethodPathAndError(string? errorText, string reason)
    {
        var sent = new Dictionary<string, (string Method, string Url)>(StringComparer.Ordinal)
        {
            ["request-1"] = ("POST", "http://localhost:4173/api/expenses?draft=true"),
        };
        var parameters = new JsonObject
        {
            ["type"] = "Fetch",
            ["requestId"] = "request-1",
            ["canceled"] = false,
        };

        if (errorText is not null)
        {
            parameters["errorText"] = errorText;
        }

        var message = AuditBrowser.FailedLoad(parameters, sent, "http://localhost:4173");

        Assert.NotNull(message);
        Assert.Contains("POST", message, StringComparison.Ordinal);
        Assert.Contains("/api/expenses", message, StringComparison.Ordinal);
        Assert.Contains(reason, message, StringComparison.Ordinal);
        Assert.DoesNotContain("draft=true", message, StringComparison.Ordinal);
    }

    /// <summary>A load failure the page aborted, or that is not a same-origin page request, reports nothing.</summary>
    /// <param name="canceled">Whether the page aborted the request on navigation.</param>
    /// <param name="type">The network resource type.</param>
    /// <param name="url">The request address.</param>
    /// <param name="tracked">Whether the request was tracked from requestWillBeSent.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a load failure the page aborted, or that is not a same-origin page request, reports nothing")]
    [InlineData(true, "XHR", "http://localhost:4173/api/reports", true)]
    [InlineData(false, "XHR", "https://cdn.example.net/lib.js", true)]
    [InlineData(false, "Script", "http://localhost:4173/app.js", true)]
    [InlineData(false, "Fetch", "http://localhost:4173/api/reports", false)]
    public void FailedLoadIgnoresOtherFailures(bool canceled, string type, string url, bool tracked)
    {
        var sent = new Dictionary<string, (string Method, string Url)>(StringComparer.Ordinal);

        if (tracked)
        {
            sent["request-1"] = ("GET", url);
        }

        var parameters = new JsonObject
        {
            ["type"] = type,
            ["requestId"] = "request-1",
            ["canceled"] = canceled,
            ["errorText"] = "net::ERR_FAILED",
        };

        Assert.Null(AuditBrowser.FailedLoad(parameters, sent, "http://localhost:4173"));
    }

    /// <summary>A request path holds the address path without its query string or fragment.</summary>
    /// <param name="url">The request address.</param>
    /// <param name="path">The expected path.</param>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Theory(DisplayName = "a request path holds the address path without its query string or fragment")]
    [InlineData("http://localhost:4173/api/reports?period=2026-10#top", "/api/reports")]
    [InlineData("http://localhost:4173/", "/")]
    [InlineData("http://localhost:4173", "/")]
    [InlineData("/api/reports?period=2026-10", "/api/reports")]
    public void RequestPathStripsQueryAndFragment(string url, string path)
    {
        Assert.Equal(path, AuditBrowser.PathOf(url));
    }

    /// <summary>A sent request reads its id, method, and address from the event.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a sent request reads its id, method, and address from the event")]
    public void SentRequestReadsMethodAndAddress()
    {
        var parameters = new JsonObject
        {
            ["requestId"] = "request-1",
            ["request"] = new JsonObject
            {
                ["method"] = "POST",
                ["url"] = "http://localhost:4173/api/expenses",
            },
        };

        var line = AuditBrowser.SentRequest(parameters);

        Assert.NotNull(line);
        Assert.Equal("request-1", line.Value.RequestId);
        Assert.Equal("POST", line.Value.Method);
        Assert.Equal("http://localhost:4173/api/expenses", line.Value.Url);
    }

    /// <summary>A sent request without an id, method, or address reads nothing.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact(DisplayName = "a sent request without an id, method, or address reads nothing")]
    public void SentRequestIgnoresIncompleteEvents()
    {
        Assert.Null(AuditBrowser.SentRequest(new JsonObject
        {
            ["request"] = new JsonObject { ["method"] = "GET", ["url"] = "http://localhost:4173/api/reports" },
        }));
        Assert.Null(AuditBrowser.SentRequest(new JsonObject { ["requestId"] = "request-1" }));
    }
}
