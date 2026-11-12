using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Tests the follows and proves checks against a consumer with a project convention.</summary>
public sealed class ConventionMarkTests
{
    /// <summary>A technical class following a known convention reports nothing.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class following a known convention reports nothing")]
    public void TechnicalClassFollowingKnownConventionReportsNothing()
    {
        using var temp = WriteConsumer();

        var findings = MarksFixture.Check(temp.Path);

        Assert.DoesNotContain(findings, finding => FileName(finding) == "FollowsConventionStep.cs");
    }

    /// <summary>A technical class following an unknown convention reports mark-unknown.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class following an unknown convention reports mark-unknown")]
    public void TechnicalClassFollowingUnknownConventionReportsMarkUnknown()
    {
        using var temp = WriteConsumer();

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown"),
            finding => FileName(finding) == "MissesConventionStep.cs");

        Assert.Contains("which matches no convention.", finding.Message);
        Assert.Equal("demo/conventions#no-such-rule", finding.Subject);
    }

    /// <summary>A technical class implementing a convention reports mark-unknown.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class implementing a convention reports mark-unknown")]
    public void TechnicalClassImplementingConventionReportsMarkUnknown()
    {
        using var temp = WriteConsumer();

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown"),
            finding => FileName(finding) == "ImplementsConventionStep.cs");

        Assert.Contains("which is a convention, not a", finding.Message);
        Assert.Equal("demo/conventions#confirm-large-payouts", finding.Subject);
    }

    /// <summary>A test proving a known convention reports no mark finding.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact(DisplayName = "A test proving a known convention reports no mark finding")]
    public void TestProvingKnownConventionReportsNothing()
    {
        using var temp = WriteConsumer();

        var findings = MarksFixture.Check(temp.Path);

        foreach (var code in new[] { "mark-missing", "mark-unknown", "mark-count-invalid", "mark-style-invalid" })
        {
            Assert.DoesNotContain(
                MarksFixture.WithCode(findings, code),
                finding => FileName(finding) == "ProvesConventionScenarios.cs");
        }
    }

    /// <summary>A test proving an unknown convention reports mark-unknown.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact(DisplayName = "A test proving an unknown convention reports mark-unknown")]
    public void TestProvingUnknownConventionReportsMarkUnknown()
    {
        using var temp = WriteConsumer();

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown"),
            finding => FileName(finding) == "ProvesUnknownConventionScenarios.cs");

        Assert.Contains("which matches no convention.", finding.Message);
        Assert.Equal("demo/conventions#no-such-rule", finding.Subject);
    }

    /// <summary>No other mark-unknown fires in the convention consumer.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "No other mark-unknown fires in the convention consumer")]
    public void NoOtherMarkUnknownFires()
    {
        using var temp = WriteConsumer();

        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unknown");

        Assert.Equal(3, unresolved.Count);
    }

    static TempFolder WriteConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Demo"
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
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow"
                    ]
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/borrow-copy.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.borrow-copy",
                  "title": "Borrow copy",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "records": [
                    {
                      "type": "result",
                      "id": "result/borrow-copy.borrowed",
                      "title": "Borrowed",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The copy is lent."
                    },
                    {
                      "type": "scenario",
                      "id": "scenario/borrow-copy.borrowing-succeeds",
                      "title": "Borrowing succeeds",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "given": "An available copy.",
                      "when": "The reader borrows the copy.",
                      "then": "The loan is active.",
                      "result": "result/borrow-copy.borrowed"
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/loans/borrow-copy.md", """
                # Borrow copy

                A reader borrows an available copy.
                """);
            Write(temp.Path, ".litespec/governance/conventions/confirm-large-payouts.md", """
                # Confirm Large Payouts

                **ID:** `demo/conventions#confirm-large-payouts`

                **Strength:** default

                **Applies to:** `apps/api/src/**`

                **Default:** A payout above 1,000 euros asks for one confirmation step before it runs.

                **Rationale:** Large payouts are hard to reverse, and one confirmation stops most slips.

                **Reviewed for:** Which payouts above 1,000 euros run without a confirmation step? Look at the payout screens.
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Technical/FollowsConventionStep.cs", """
                namespace Library.Application.Lending.Technical;

                /// <follows>demo/conventions#confirm-large-payouts</follows>
                internal sealed class FollowsConventionStep
                {
                    public Task ConfirmAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Technical/MissesConventionStep.cs", """
                namespace Library.Application.Lending.Technical;

                /// <follows>demo/conventions#no-such-rule</follows>
                internal sealed class MissesConventionStep
                {
                    public Task ConfirmAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Technical/ImplementsConventionStep.cs", """
                namespace Library.Application.Lending.Technical;

                /// <implements>demo/conventions#confirm-large-payouts</implements>
                internal sealed class ImplementsConventionStep
                {
                    public Task ConfirmAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """);
            Write(temp.Path, "apps/api/tests/Library.Tests/Lending/Loans/BorrowCopy/ProvesConventionScenarios.cs", """
                namespace Library.Tests.Lending.Loans.BorrowCopy;

                public sealed class ProvesConventionScenarios
                {
                    /// <proves>demo/conventions#confirm-large-payouts</proves>
                    [Fact]
                    public void ConfirmingLargePayouts()
                    {
                        Assert.True(true);
                    }
                }
                """);
            Write(temp.Path, "apps/api/tests/Library.Tests/Lending/Loans/BorrowCopy/ProvesUnknownConventionScenarios.cs", """
                namespace Library.Tests.Lending.Loans.BorrowCopy;

                public sealed class ProvesUnknownConventionScenarios
                {
                    /// <proves>demo/conventions#no-such-rule</proves>
                    [Fact]
                    public void ConfirmingLargePayouts()
                    {
                        Assert.True(true);
                    }
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

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }

    static string FileName(Finding finding) =>
        Path.GetFileName(finding.Path);
}
