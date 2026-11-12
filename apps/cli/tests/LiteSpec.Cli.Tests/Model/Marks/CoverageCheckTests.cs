using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the record-side traceability findings against the marks fixtures.</summary>
public sealed class CoverageCheckTests
{
    /// <summary>Aggregate Without A Marked Root Reports Aggregate Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void AggregateWithoutAMarkedRootReportsAggregateUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>Store Aggregate Without A Root Reports Nothing.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void StoreAggregateWithoutARootReportsNothing()
    {
        using var temp = WriteKindConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "aggregate-unimplemented");

        Assert.DoesNotContain(findings, finding => finding.Subject == "aggregate/lending.outbox");
    }

    /// <summary>Entity Aggregate Without A Root Still Reports.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void EntityAggregateWithoutARootStillReports()
    {
        using var temp = WriteKindConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>Reaction Without A Marked Handler Reports Reaction Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void ReactionWithoutAMarkedHandlerReportsReactionUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "reaction-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("reaction/lending.send-due-notice-on-loan-borrowed", finding.Subject);
    }

    /// <summary>Job Without A Marked Host Reports Job Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void JobWithoutAMarkedHostReportsJobUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "job-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("job/lending.nightly-due-scan", finding.Subject);
    }

    /// <summary>System Without A Marked Port Reports External System Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void SystemWithoutAMarkedPortReportsExternalSystemUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "external-system-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("external-system/text-gateway", finding.Subject);
    }

    /// <summary>Browser Use Case Without An Endpoint Reports Use Case Unexposed.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void BrowserUseCaseWithoutAnEndpointReportsUseCaseUnexposed()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-endpoint-missing");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <summary>Browser Use Case Without A Screen Reports Use Case Unreached.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void BrowserUseCaseWithoutAScreenReportsUseCaseUnreached()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-unreached");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <summary>Declared Edge No Code Takes Reports Effect Unimplemented.</summary>
    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void DeclaredEdgeNoCodeTakesReportsEffectUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("effects");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("runs use-case/lending.register-copy", finding.Message);
    }

    /// <summary>Writes a consumer where an entity and a store aggregate each have no root in code.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteKindConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "5.1.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "entity",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/loans/aggregate.md", """
                # Loan

                A loan lends one copy to one reader.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/outbox/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.outbox",
                  "title": "Outbox",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "store"
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/outbox/aggregate.md", """
                # Outbox

                The lending-owned table of scans awaiting dispatch.
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
