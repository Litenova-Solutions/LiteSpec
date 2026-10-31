using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves the record-side traceability findings against the marks fixtures.</summary>
public sealed class CoverageCheckTests
{
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Aggregate_Without_A_Marked_Root_Reports_Aggregate_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Store_Aggregate_Without_A_Root_Reports_Nothing()
    {
        using var temp = WriteKindConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "aggregate-unimplemented");

        Assert.DoesNotContain(findings, finding => finding.Subject == "aggregate/lending.outbox");
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Entity_Aggregate_Without_A_Root_Still_Reports()
    {
        using var temp = WriteKindConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Automations_Without_Marked_Hosts_Report_Automation_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "automation-unimplemented");

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, finding => finding.Subject == "automation/lending.send-due-notice-on-loan-borrowed");
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void System_Without_A_Marked_Port_Reports_External_System_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "external-system-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("external-system/text-gateway", finding.Subject);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Browser_Use_Case_Without_An_Endpoint_Reports_Use_Case_Unexposed()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-endpoint-missing");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Browser_Use_Case_Without_A_Screen_Reports_Use_Case_Unreached()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-unreached");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void Declared_Edge_No_Code_Takes_Reports_Effect_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("effects");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("runs use-case/lending.register-copy", finding.Message);
    }

    /// <summary>Writes a consumer where an entity and a store aggregate each have no root in code.</summary>
    static TempFolder WriteKindConsumer()
    {
        var temp = Cli.Temp("marks");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "reviewedLiteSpecVersion": "3.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": "docs",
                    "behaviorDocs": "docs/behavior",
                    "glossary": "docs/glossary.json",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "application": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  }
                }
                """);
            Write(temp.Path, "docs/glossary.json", """
                {
                  "schemaVersion": 2,
                  "terms": [],
                  "mannered": []
                }
                """);
            Write(temp.Path, "docs/behavior/modules/lending/loans/README.md", """
                ---
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
                ---

                # Loan

                A loan lends one copy to one reader.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/outbox/README.md", """
                ---
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.outbox",
                  "title": "Outbox",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "store"
                }
                ---

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
