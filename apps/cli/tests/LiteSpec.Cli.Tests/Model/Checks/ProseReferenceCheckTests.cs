using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves prose names every record by a resolving identifier and every provision by one the carried release ships: unknown identifiers in explanations, record fields and the agent context report, while fenced examples and resolving step links pass.</summary>
public sealed class ProseReferenceCheckTests
{
    /// <summary>An explanation file naming an unknown record identifier reports it with its file and line.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void UnknownRecordIdentifierInExplanationFileReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\nThe limit follows `use-case/lending.no-such-use-case`.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Prose names 'use-case/lending.no-such-use-case', which no record holds.", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, "borrowing.md"), finding.Path);
        Assert.Equal(3, finding.Line);
    }

    /// <summary>A waiver exit naming an unknown record identifier reports it on the waiver.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void UnknownIdentifierInWaiverExitReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "waiver",
                "waiver/ghost-route",
                Path.Combine(temp.Path, "ghost-route.yaml"),
                """{"kind": "gap", "covers": ["mark-missing"], "paths": ["src/Ghost.cs"], "reason": "Infrastructure.", "closedBy": ["waiver/ghost-route"], "exitWhen": "The gap closes when `decision/0084-no-such-decision` lands.", "expiresOn": "2999-01-01"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'waiver/ghost-route' names 'decision/0084-no-such-decision' in prose, which no record holds.", finding.Message);
        Assert.Equal("waiver/ghost-route", finding.Subject);
    }

    /// <summary>A record field naming a provision the carried release does not ship reports it.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void UnknownProvisionIdReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans, as `litespec/core/model#no-such-provision` requires."}"""));
        model.Provisions = new HashSet<string>(StringComparer.Ordinal) { "litespec/core/model#give-every-record-one-parent" };

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'requirement/borrowing.max-active-loans' names 'litespec/core/model#no-such-provision' in prose, which the carried release does not ship.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <summary>An unknown record identifier in the root agent context reports it with its file and line.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void UnknownRecordIdentifierInAgentContextReports()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "AGENTS.md"), "# Notes\n\nSee `use-case/lending.no-such-use-case`.\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown");
        Assert.Equal("Prose names 'use-case/lending.no-such-use-case', which no record holds.", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, "AGENTS.md"), finding.Path);
        Assert.Equal(3, finding.Line);
    }

    /// <summary>An unknown identifier inside a fenced code block passes.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void IdentifierInsideFencedCodeBlockPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Max Active Loans\n\n```markdown\nSee `use-case/lending.no-such-use-case`.\n```\n"));

        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));
    }

    /// <summary>A known step identifier passes; the old flow-step suffix is reported.</summary>
    /// <proves>litespec/core/model#name-records-in-prose-by-their-identifiers</proves>
    [Fact]
    public void KnownStepIdentifierPassesAndOldFormReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"title": "Borrow and return", "actor": ["actor/reader"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"title": "Reader", "kind": "person"}""")]);
        model.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Borrowing\n\nStart at `step/borrow-and-return.borrow-copy`.\n"));

        Assert.DoesNotContain("reference-unknown", TestModel.Codes(model));

        var old = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"title": "Borrow and return", "actor": ["actor/reader"], "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"title": "Reader", "kind": "person"}""")]);
        old.Bodies.Add(TestModel.Body(
            Path.Combine(temp.Path, "borrowing.md"),
            "# Borrowing\n\nStart at `flow/borrow-and-return#s1`.\n"));

        Assert.Contains("reference-unknown", TestModel.Codes(old));
    }
}
