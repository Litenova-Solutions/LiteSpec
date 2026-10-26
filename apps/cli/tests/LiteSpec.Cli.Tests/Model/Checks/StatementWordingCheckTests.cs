using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the statement wording check: each vague word reported, and code in backticks skipped.</summary>
public sealed class StatementWordingCheckTests
{
    [Theory]
    [InlineData("handle", "The clerk will handle the request by hand.")]
    [InlineData("handles", "The clerk handles the request by hand.")]
    [InlineData("handled", "The clerk handled the request by hand.")]
    [InlineData("process", "The clerk will process the request today.")]
    [InlineData("processes", "The clerk processes the request today.")]
    [InlineData("manage", "The clerk will manage the queue fairly.")]
    [InlineData("manages", "The clerk manages the queue fairly.")]
    [InlineData("various", "The clerk checks various shelves daily.")]
    [InlineData("etc.", "The clerk lists copies, holds, etc.")]
    [InlineData("appropriate", "The clerk takes appropriate action daily.")]
    [InlineData("appropriately", "The clerk files the copy appropriately.")]
    [InlineData("properly", "The clerk scans the copy properly.")]
    [InlineData("as needed", "The clerk reorders shelves as needed.")]
    [InlineData("and so on", "The clerk counts copies, holds, and so on.")]
    public void Each_Vague_Word_Reported_In_A_Statement(string word, string statement)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/test.desk-kept-tidy",
                Path.Combine(temp.Path, "tidy.md"),
                """{"kind": "invariant", "statement": "__STATEMENT__", "governs": []}""".Replace("__STATEMENT__", statement)));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "statement-vague-word").ToList();

        Assert.Contains(findings, finding => finding.Message.Contains($"'{word}'", StringComparison.Ordinal));
    }

    [Fact]
    public void Vague_Word_Inside_Backticks_Skipped()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/test.desk-kept-tidy",
                Path.Combine(temp.Path, "tidy.md"),
                """{"kind": "invariant", "statement": "The `handle` value stays stored.", "governs": []}"""));

        Assert.DoesNotContain("statement-vague-word", TestModel.Codes(model));
    }

    [Fact]
    public void Vague_Word_Inside_A_Hyphenated_Identifier_Skipped()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/test.grant-kept",
                Path.Combine(temp.Path, "grant.md"),
                """{"kind": "invariant", "statement": "A role never holds manage-member-access.", "governs": []}"""));

        Assert.DoesNotContain("statement-vague-word", TestModel.Codes(model));
    }

    [Fact]
    public void Scenario_Steps_And_Result_Meaning_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "scenario",
                "scenario/borrow-copy.borrowing-handled-pile-succeeds",
                Path.Combine(temp.Path, "borrowing.md"),
                """{"given": "The clerk handles the request.", "when": "The clerk processes the request.", "then": "The request is handled."}"""),
            TestModel.Record(
                "result",
                "result/borrow-copy.borrowed",
                Path.Combine(temp.Path, "borrowed.md"),
                """{"outcome": "success", "meaning": "The clerk manages various queues."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "statement-vague-word").ToList();

        Assert.Equal(5, findings.Count);
    }
}
