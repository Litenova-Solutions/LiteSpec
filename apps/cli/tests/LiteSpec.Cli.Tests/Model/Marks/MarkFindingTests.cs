using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the mark presence and link findings against the marks fixtures, and the frontend count finding against an inline model.</summary>
public sealed class MarkFindingTests
{
    /// <summary>Handler Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void HandlerWithoutAMarkReportsMarkMissing()
    {
        using var fixture = MarksFixture.Prepare("missing");
        var missing = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Handler", finding.Message);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Mark Naming No Record Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingNoRecordReportsMarkUnresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("matches no record"));
        Assert.Contains("use-case/lending.ghost", finding.Message);
        Assert.Equal("use-case/lending.ghost", finding.Subject);
    }

    /// <summary>Mark Naming A Record Of Another Type Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingARecordOfAnotherTypeReportsMarkUnresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("not a use-case record"));
        Assert.Contains("rule/loan.copy-available", finding.Message);
        Assert.Contains("rule record", finding.Message);
        Assert.Equal("rule/loan.copy-available", finding.Subject);
    }

    /// <summary>Route With Two Marks Reports Mark Count.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void RouteWithTwoMarksReportsMarkCount()
    {
        using var temp = TestCli.Temp("marks");
        var model = RouteModel(temp.Path);
        var webFile = Path.Combine(temp.Path, "apps", "web", "src", "routes", "loans", "+page.svelte");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendRoute, "/loans", null, webFile, 3,
            [
                new CodeMark("screen/app.loans", MarkStyle.Xml, MarkVerb.Implements, webFile, 1),
                new CodeMark("screen/app.holds", MarkStyle.Xml, MarkVerb.Implements, webFile, 2),
            ],
            InTests: false));

        var counts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-count-invalid");

        var finding = Assert.Single(counts);
        Assert.Contains("carries 2 marks", finding.Message);
        Assert.Contains("takes one", finding.Message);
    }

    /// <summary>A partial root marked on one part reports no mark missing, while an unmarked root still reports.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void PartialRootMarkedOnOnePartReportsNothing()
    {
        using var temp = WritePartialConsumer();
        var missing = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Hold", finding.Message);
    }

    /// <summary>Writes a consumer in the module layout where one root splits into a marked and an unmarked part, and one root carries no mark.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WritePartialConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            WriteFile(temp.Path, ".litespec/project.yaml", """
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
                  "reviewedLiteSpecVersion": "7.0.0",
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
                      "modules": "apps/api/src/Modules/*",
                      "kernel": "apps/api/src/Library.Kernel",
                      "platform": "apps/api/src/Library.Platform"
                    }
                  }
                }
                """);
            WriteFile(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            foreach (var (folder, id, title) in new[] { ("loans", "aggregate/lending.loan", "Loan"), ("holds", "aggregate/lending.hold", "Hold") })
            {
                TestRecords.Write(temp.Path, $".litespec/behavior/modules/lending/aggregates/{folder}/aggregate.yaml", """
                    {
                      "type": "aggregate",
                      "id": "ID",
                      "title": "TITLE",
                      "states": [
                        {
                          "name": "Active",
                          "meaning": "The record is open."
                        }
                      ]
                    }
                    """.Replace("ID", id).Replace("TITLE", title));
                TestRecords.Explain(temp.Path, $".litespec/behavior/modules/lending/aggregates/{folder}/aggregate.md", $"""
                    # {title}

                    A record stays open.
                    """);
            }

            WriteFile(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loan/Loan.cs", """
                namespace Library.Lending.Domain.Loan;

                /// <implements>aggregate/lending.loan</implements>
                public sealed partial class Loan : AggregateRoot<LoanId>
                {
                    public string State { get; private set; } = "active";
                }
                """);
            WriteFile(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Loan/Loan.Renew.cs", """
                namespace Library.Lending.Domain.Loan;

                public sealed partial class Loan
                {
                    private string RenewalNote() => "renewed";
                }
                """);
            WriteFile(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Hold/Hold.cs", """
                namespace Library.Lending.Domain.Hold;

                public sealed class Hold : AggregateRoot<HoldId>
                {
                    public string State { get; private set; } = "active";
                }
                """);
            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void WriteFile(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }

    /// <summary>A model with the screen record the route marks name.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel RouteModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\"}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("screen", "screen/app.loans", root, """{"status": "reviewed"}"""));
        model.Records.Add(Rec("screen", "screen/app.holds", root, """{"status": "reviewed"}"""));

        return model;
    }

    static Record Rec(string type, string id, string root, string json)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = Path.Combine(root, ".litespec", "record.md"),
            Line = 1,
            Raw = raw,
        };
    }
}
