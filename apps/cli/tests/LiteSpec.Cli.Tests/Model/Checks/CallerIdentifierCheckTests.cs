using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every required caller-supplied identifier is returned by a query the caller reaches.</summary>
public sealed class CallerIdentifierCheckTests
{
    /// <summary>Command Taking Unread Identifier Through Entry Point Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingUnreadIdentifierThroughEntryPointReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to renew.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}]}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        var finding = Assert.Single(findings, found => found.Code == "caller-identifier-unreachable");
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
    }

    /// <summary>Command Taking Identifier Returned Through Same Entry Point Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingIdentifierReturnedThroughSameEntryPointPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to renew.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", UseCaseFile(temp.Path, "list-loans.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-app"], "output": {"include": {"rows": {"fields": [{"name": "loans", "meaning": "The loans.", "type": "list", "rowShape": {"fields": [{"name": "loanId", "meaning": "The listed loan.", "type": "id", "identifies": "aggregate/lending.loan", "required": true}]}}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.DoesNotContain(findings, finding => finding.Code == "caller-identifier-unreachable");
    }

    /// <summary>Command Taking Identifier Returned As Direct Field Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingIdentifierReturnedAsDirectFieldPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to renew.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-app"], "output": {"include": {"loan": {"fields": [{"name": "loanId", "meaning": "The read loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.DoesNotContain(findings, finding => finding.Code == "caller-identifier-unreachable");
    }

    /// <summary>Command Taking Identifier Returned Through Another Entry Point Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingIdentifierReturnedThroughAnotherEntryPointReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("entry-point", "entry-point/library-api", Path.Combine(temp.Path, "api.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to renew.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", UseCaseFile(temp.Path, "list-loans.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-api"], "output": {"include": {"rows": {"fields": [{"name": "loanId", "meaning": "The listed loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        var finding = Assert.Single(findings, found => found.Code == "caller-identifier-unreachable");
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
    }

    /// <summary>Query Taking Its Own Returned Identifier With No Other Query Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void QueryTakingItsOwnReturnedIdentifierWithNoOtherQueryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to read.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}], "output": {"include": {"loan": {"fields": [{"name": "loanId", "meaning": "The read loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        var finding = Assert.Single(findings, found => found.Code == "caller-identifier-unreachable");
        Assert.Equal("use-case/lending.read-loan", finding.Subject);
    }

    /// <summary>Use Case Taking Unread Identifier With No Entry Point Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void UseCaseTakingUnreadIdentifierWithNoEntryPointReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["module/catalog"], "input": [{"name": "loan", "meaning": "The loan to read.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}], "output": {"include": {"loan": {"fields": [{"name": "loanId", "meaning": "The read loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        var finding = Assert.Single(findings, found => found.Code == "module-identifier-unreachable");
        Assert.Equal("use-case/lending.read-loan", finding.Subject);
    }

    /// <summary>Use Case Taking Identifier Returned By Module Query Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void UseCaseTakingIdentifierReturnedByModuleQueryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["module/catalog"], "input": [{"name": "loan", "meaning": "The loan to read.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}], "output": {"include": {"loan": {"fields": [{"name": "loanId", "meaning": "The read loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", UseCaseFile(temp.Path, "list-loans.yaml"), """{"kind": "query", "reachedThrough": ["module/catalog"], "output": {"include": {"rows": {"fields": [{"name": "loanId", "meaning": "The listed loan.", "type": "id", "identifies": "aggregate/lending.loan"}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.DoesNotContain(findings, finding => finding.Code == "module-identifier-unreachable");
    }

    /// <summary>Optional Identifier With No Read Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void OptionalIdentifierWithNoReadPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "loan", "meaning": "The loan to renew.", "type": "id", "identifies": "aggregate/lending.loan", "required": false, "origin": "caller", "classification": "internal"}]}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.Empty(findings);
    }

    /// <summary>Idempotency Key Identifier With No Read Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void IdempotencyKeyIdentifierWithNoReadPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.create-loan", UseCaseFile(temp.Path, "create-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "requestId", "meaning": "The identifier the app mints for one creation attempt.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "caller", "classification": "internal"}], "idempotency": {"mode": "key", "key": "{requestId}", "keyScope": "account", "replay": "returns-first-result"}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.Empty(findings);
    }

    /// <summary>Claim Identifier With No Read Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void ClaimIdentifierWithNoReadPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", UseCaseFile(temp.Path, "list-loans.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "borrower", "meaning": "The borrower listing their loans.", "type": "id", "identifies": "aggregate/lending.loan", "required": true, "origin": "claim", "classification": "internal"}]}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.Empty(findings);
    }

    /// <summary>Command Taking Object Row Part Identifier Returned Through Same Entry Point Passes.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingObjectRowPartIdentifierReturnedThroughSameEntryPointPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}], "parts": [{"name": "renewal", "identity": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "renewal", "meaning": "The renewal to extend.", "type": "object", "required": true, "origin": "caller", "classification": "internal", "rowType": "RenewalForm", "rowShape": {"fields": [{"name": "renewalId", "meaning": "The renewal.", "type": "id", "identifies": "aggregate/lending.loan#renewal", "required": true}]}}]}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", UseCaseFile(temp.Path, "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["entry-point/library-app"], "output": {"include": {"loan": {"fields": [{"name": "renewal", "meaning": "The latest renewal.", "type": "object", "rowType": "RenewalForm", "rowShape": {"fields": [{"name": "renewalId", "meaning": "The renewal.", "type": "id", "identifies": "aggregate/lending.loan#renewal", "required": true}]}}]}}}}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        Assert.DoesNotContain(findings, finding => finding.Code == "caller-identifier-unreachable");
    }

    /// <summary>Command Taking Object Row Identifier With No Read Reports.</summary>
    /// <proves>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</proves>
    [Fact]
    public void CommandTakingObjectRowIdentifierWithNoReadReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "audience": ["actor/reader"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan runs."}]}"""),
            TestModel.Record("use-case", "use-case/lending.renew-loan", UseCaseFile(temp.Path, "renew-loan.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/library-app"], "input": [{"name": "renewal", "meaning": "The renewal to extend.", "type": "object", "required": true, "origin": "caller", "classification": "internal", "rowType": "RenewalForm", "rowShape": {"fields": [{"name": "loanId", "meaning": "The loan.", "type": "id", "identifies": "aggregate/lending.loan", "required": true}]}}]}"""));

        var findings = new List<Finding>();
        CallerIdentifierChecks.Run(model, findings);

        var finding = Assert.Single(findings, found => found.Code == "caller-identifier-unreachable");
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
    }

    /// <summary>One use case file in the lending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string UseCaseFile(string root, string file) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "lending", file);
}
