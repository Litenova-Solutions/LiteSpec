using System.Text.Json;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the HTTP surface checks: an HTTP-reached use case states its method and route with each input bound, each route parameter names a route input, a claim input takes a verified-claim origin, each cited refusal states its status, and each created location names an output field.</summary>
public sealed class HttpSurfaceCheckTests
{
    /// <summary>A use case reached through an api entry point with no http reports http-surface-missing.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void HttpReachedUseCaseWithoutHttpReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"]}"""));

        Assert.Contains("http-surface-missing", TestModel.Codes(model));
    }

    /// <summary>A use case reached through a browser entry point with its http passes the surface rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void HttpReachedUseCaseWithHttpPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}}"""));

        Assert.DoesNotContain("http-surface-missing", TestModel.Codes(model));
    }

    /// <summary>A use case reached only through a module passes the surface rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void ModuleOnlyUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"),
                """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain("http-surface-missing", TestModel.Codes(model));
    }

    /// <summary>A use case reached through a webhook entry point passes the surface rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void WebhookReachedUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/provider-hooks", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "webhook"}"""),
            TestModel.Record("use-case", "use-case/spending.record-payout", Path.Combine(temp.Path, "record.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/provider-hooks"]}"""));

        Assert.DoesNotContain("http-surface-missing", TestModel.Codes(model));
    }

    /// <summary>An input with no binding on an HTTP-reached use case reports input-binding-missing.</summary>
    /// <proves>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</proves>
    [Fact]
    public void InputWithoutBindingReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}, "input": [{"name": "copyId", "meaning": "The copy.", "type": "id", "identifies": "aggregate/lending.copy", "origin": "caller", "classification": "internal"}]}"""));

        Assert.Contains("input-binding-missing", TestModel.Codes(model));
    }

    /// <summary>A derived input with no binding passes the binding rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</proves>
    [Fact]
    public void DerivedInputWithoutBindingPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}, "input": [{"name": "borrowedAt", "meaning": "The time.", "type": "instant", "origin": "derived", "classification": "internal"}]}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("input-binding-missing", codes);
        Assert.DoesNotContain("input-binding-invalid", codes);
    }

    /// <summary>A claim binding with a caller origin reports input-binding-invalid.</summary>
    /// <proves>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</proves>
    [Fact]
    public void ClaimBindingWithCallerOriginReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}, "input": [{"name": "readerId", "meaning": "The reader.", "type": "id", "identifies": "actor/reader", "origin": "caller", "binding": "claim", "classification": "personal"}]}"""));

        Assert.Contains("input-binding-invalid", TestModel.Codes(model));
    }

    /// <summary>A claim binding with a verified-claim origin passes the binding rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</proves>
    [Fact]
    public void ClaimBindingWithVerifiedClaimPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}, "input": [{"name": "readerId", "meaning": "The reader.", "type": "id", "identifies": "actor/reader", "origin": "verified-claim", "verifiedBy": "entry-point/library-app", "binding": "claim", "classification": "personal"}]}"""));

        Assert.DoesNotContain("input-binding-invalid", TestModel.Codes(model));
    }

    /// <summary>A binding on a derived input reports input-binding-invalid.</summary>
    /// <proves>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</proves>
    [Fact]
    public void BindingOnDerivedInputReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}, "input": [{"name": "borrowedAt", "meaning": "The time.", "type": "instant", "origin": "derived", "binding": "query", "classification": "internal"}]}"""));

        Assert.Contains("input-binding-invalid", TestModel.Codes(model));
    }

    /// <summary>A route parameter naming no route input reports http-route-param-unknown.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamNamingNoInputReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans/{loanId}:extend"}, "input": [{"name": "copyId", "meaning": "The copy.", "type": "id", "identifies": "aggregate/lending.copy", "origin": "caller", "binding": "body", "classification": "internal"}]}"""));

        Assert.Contains("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A route parameter matching a route input by name passes the route rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamMatchingInputNamePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.cancel-hold", Path.Combine(temp.Path, "cancel.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/holds/{holdId}:cancel"}, "input": [{"name": "holdId", "meaning": "The hold.", "type": "id", "identifies": "aggregate/lending.hold", "origin": "caller", "binding": "route", "classification": "internal"}]}"""));

        Assert.DoesNotContain("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A route parameter matching an id input by identity passes the route rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamMatchingIdentityPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser"}"""),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "POST", "route": "/api/expenses/{expenseId}:approve"}, "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller", "binding": "route", "classification": "internal"}]}"""));

        Assert.DoesNotContain("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A route parameter matching a non-id input only by identity reports http-route-param-unknown.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamMatchingNonIdByIdentityReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans/{copyId}"}, "input": [{"name": "copy", "meaning": "The copy shelf.", "type": "text", "origin": "caller", "binding": "route", "classification": "internal"}]}"""));

        Assert.Contains("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A route parameter matching a stated route parameter name passes the route rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamMatchingCustomNamePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/spending.change-reminder-preference", Path.Combine(temp.Path, "change.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "PUT", "route": "/api/reminder-preferences/{id}"}, "input": [{"name": "manager", "meaning": "The manager the preference belongs to.", "type": "id", "identifies": "actor/manager", "origin": "caller", "binding": "route", "routeParameter": "id", "classification": "personal"}]}"""));

        Assert.DoesNotContain("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A route parameter using the input name when the input renames it reports http-route-param-unknown.</summary>
    /// <proves>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</proves>
    [Fact]
    public void RouteParamUsingInputNameWhenRenamedReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/spending.change-reminder-preference", Path.Combine(temp.Path, "change.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "PUT", "route": "/api/reminder-preferences/{managerId}"}, "input": [{"name": "manager", "meaning": "The manager the preference belongs to.", "type": "id", "identifies": "actor/manager", "origin": "caller", "binding": "route", "routeParameter": "id", "classification": "personal"}]}"""));

        Assert.Contains("http-route-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A refusal with no status cited by an HTTP-reached use case reports refusal-status-missing.</summary>
    /// <proves>litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites</proves>
    [Fact]
    public void RefusalWithoutStatusReportsMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}}"""),
            Entry("result", "result/borrow-copy.copy-unavailable", Path.Combine(temp.Path, "borrow.yaml"),
                """{"outcome": "refusal", "refusal": "refusal/lending.copy-not-available"}""", "use-case/lending.borrow-copy"));
        model.RefusalSets.Add(new RefusalEntry("lending", "lending.copy-not-available", "The copy is unavailable.", Path.Combine(temp.Path, "refusals.yaml"), "loans", 4));

        Assert.Contains("refusal-status-missing", TestModel.Codes(model));
    }

    /// <summary>A refusal with a status cited by an HTTP-reached use case passes the status rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites</proves>
    [Fact]
    public void RefusalWithStatusPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}}"""),
            Entry("result", "result/borrow-copy.copy-unavailable", Path.Combine(temp.Path, "borrow.yaml"),
                """{"outcome": "refusal", "refusal": "refusal/lending.copy-not-available"}""", "use-case/lending.borrow-copy"));
        model.RefusalSets.Add(new RefusalEntry("lending", "lending.copy-not-available", "The copy is unavailable.", Path.Combine(temp.Path, "refusals.yaml"), "loans", 4)
        {
            Status = 409,
        });

        Assert.DoesNotContain("refusal-status-missing", TestModel.Codes(model));
    }

    /// <summary>A refusal with no status cited only by a module-reached use case passes the status rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites</proves>
    [Fact]
    public void RefusalWithoutStatusOnModuleUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"),
                """{"kind": "query", "reachedThrough": ["module/reporting"]}"""),
            Entry("result", "result/read-expense.unknown-copy", Path.Combine(temp.Path, "read.yaml"),
                """{"outcome": "refusal", "refusal": "refusal/spending.unknown-copy"}""", "use-case/spending.read-expense"));
        model.RefusalSets.Add(new RefusalEntry("spending", "spending.unknown-copy", "The copy is unknown.", Path.Combine(temp.Path, "refusals.yaml"), "expenses", 4));

        Assert.DoesNotContain("refusal-status-missing", TestModel.Codes(model));
    }

    /// <summary>A result citing a refusal no set holds passes the status rule; the unknown reference owns it.</summary>
    /// <proves>litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites</proves>
    [Fact]
    public void UnknownRefusalPassesStatusRule()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "http": {"method": "POST", "route": "/api/loans"}}"""),
            Entry("result", "result/borrow-copy.copy-unavailable", Path.Combine(temp.Path, "borrow.yaml"),
                """{"outcome": "refusal", "refusal": "refusal/lending.copy-not-available"}""", "use-case/lending.borrow-copy"));

        Assert.DoesNotContain("refusal-status-missing", TestModel.Codes(model));
    }

    /// <summary>A created-location parameter naming no output field reports http-location-param-unknown.</summary>
    /// <proves>litespec/core/use-cases#declare-the-created-location-of-every-command-that-creates-a-resource</proves>
    [Fact]
    public void LocationParamNamingNoOutputReportsUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "POST", "route": "/api/expenses", "created": "/api/expenses/{receiptId}"}, "output": [{"name": "expenseId", "type": "id"}]}"""));

        Assert.Contains("http-location-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A created-location parameter naming an output field passes the location rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-created-location-of-every-command-that-creates-a-resource</proves>
    [Fact]
    public void LocationParamMatchingOutputPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "POST", "route": "/api/expenses", "created": "/api/expenses/{expenseId}"}, "output": [{"name": "expenseId", "type": "id"}]}"""));

        Assert.DoesNotContain("http-location-param-unknown", TestModel.Codes(model));
    }

    /// <summary>A created-location parameter naming an id output by identity passes the location rule.</summary>
    /// <proves>litespec/core/use-cases#declare-the-created-location-of-every-command-that-creates-a-resource</proves>
    [Fact]
    public void LocationParamMatchingIdentityPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/claimwick-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "api"}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"),
                """{"kind": "command", "reachedThrough": ["entry-point/claimwick-app"], "http": {"method": "POST", "route": "/api/expenses", "created": "/api/expenses/{expenseId}"}, "output": [{"name": "expense", "type": "id"}]}"""));

        Assert.DoesNotContain("http-location-param-unknown", TestModel.Codes(model));
    }

    /// <summary>An entry of the type and id on the page, with the JSON fields.</summary>
    /// <param name="type">The record type of the entry.</param>
    /// <param name="id">The record id of the entry.</param>
    /// <param name="file">The page file the entry sits on.</param>
    /// <param name="json">The entry fields as JSON.</param>
    /// <param name="pageId">The id of the page holding the entry.</param>
    /// <returns>The record for the entry.</returns>
    static Record Entry(string type, string id, string file, string json, string pageId)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = 1,
            IsEntry = true,
            PageId = pageId,
            Raw = raw,
        };
    }
}
