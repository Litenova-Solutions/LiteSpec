using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the writing checks: long titles and sentences reported as advice, modal words in statements blocked, and defects the language check already reports left to it.</summary>
public sealed class WritingChecksTests
{
    /// <summary>A sentence of exactly the counted words, built token by token so the count is exact.</summary>
    /// <param name="count">The number of word tokens the sentence holds.</param>
    /// <returns>The sentence text ending in a period.</returns>
    static string Words(int count) =>
        string.Join(" ", Enumerable.Range(1, count).Select(index => $"w{index}")) + ".";

    /// <summary>Title Over Eight Words Reported.</summary>
    /// <proves>litespec/core/writing#title-every-record-in-title-case</proves>
    [Fact]
    public void Title_Over_Eight_Words_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/expenses-wait-weeks",
                Path.Combine(temp.Path, "problem.md"),
                """{"title": "One Two Three Four Five Six Seven Eight Nine", "statement": "Employees wait weeks for reimbursement."}"""));

        var findings = Checker.Check(model).Findings
            .Where(finding => finding.Code == "value-invalid" && finding.Message.Contains("title of", StringComparison.Ordinal))
            .ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("title of 9 words", finding.Message, StringComparison.Ordinal);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Title Of Eight Words Passes.</summary>
    /// <proves>litespec/core/writing#title-every-record-in-title-case</proves>
    [Fact]
    public void Title_Of_Eight_Words_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "goal",
                "goal/faster-reimbursement",
                Path.Combine(temp.Path, "goal.md"),
                """{"title": "One Two Three Four Five Six Seven Eight", "statement": "Employees receive reimbursement within two days of approval."}"""));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Message.Contains("title of", StringComparison.Ordinal));
    }

    /// <summary>Modal In Goal Statement Blocks.</summary>
    /// <param name="modal">The modal word the case plants in the statement.</param>
    /// <param name="statement">The goal statement text that carries the modal word.</param>
    /// <proves>litespec/core/writing#state-each-statement-without-modal-words</proves>
    [Theory]
    [InlineData("will", "Employees will receive reimbursement within two days.")]
    [InlineData("must", "Employees must receive reimbursement within two days.")]
    [InlineData("can", "Employees can receive reimbursement within two days.")]
    // The InlineData values pair each modal word with its statement.
    // Each statement holds its modal word outside backticks, so the check blocks it.
    public void Modal_In_Goal_Statement_Blocks(string modal, string statement)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "goal",
                "goal/faster-reimbursement",
                Path.Combine(temp.Path, "goal.md"),
                """{"statement": "__STATEMENT__"}""".Replace("__STATEMENT__", statement)));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "field-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains($"modal word '{modal}'", finding.Message, StringComparison.Ordinal);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Modal In Problem Statement Blocks.</summary>
    /// <proves>litespec/core/writing#state-each-statement-without-modal-words</proves>
    [Fact]
    public void Modal_In_Problem_Statement_Blocks()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/expenses-wait-weeks",
                Path.Combine(temp.Path, "problem.md"),
                """{"statement": "Employees should wait weeks for reimbursement."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "field-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("modal word 'should'", finding.Message, StringComparison.Ordinal);
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>Modal In Requirement Statement Reuses Value Invalid.</summary>
    /// <proves>litespec/core/writing#state-each-statement-without-modal-words</proves>
    [Fact]
    public void Modal_In_Requirement_Statement_Reuses_Value_Invalid()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/approval.decided-within-five-days",
                Path.Combine(temp.Path, "approval.md"),
                """{"kind": "functional", "statement": "Each expense should be decided within five days.", "rationale": "approvers batch decisions monthly"}"""));

        var findings = Checker.Check(model).Findings;

        Assert.Contains(findings, finding => finding.Code == "value-invalid" && finding.Message.Contains("modal word 'should'", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "field-invalid");
    }

    /// <summary>Modal Inside Backticks Skipped.</summary>
    /// <proves>litespec/core/writing#state-each-statement-without-modal-words</proves>
    [Fact]
    public void Modal_Inside_Backticks_Skipped()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "goal",
                "goal/faster-reimbursement",
                Path.Combine(temp.Path, "goal.md"),
                """{"statement": "The `will` flag stays stored on the expense."}"""));

        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <summary>Sentence Over Limit Reported.</summary>
    /// <param name="type">The record type carrying the long sentence.</param>
    /// <param name="id">The record identifier of the case.</param>
    /// <param name="field">The prose field holding the long sentence.</param>
    /// <proves>litespec/core/writing#keep-each-record-sentence-within-twenty-five-words</proves>
    [Theory]
    [InlineData("problem", "problem/expenses-wait-weeks", "statement")]
    [InlineData("actor", "actor/employee", "summary")]
    [InlineData("problem", "problem/expenses-wait-weeks", "situation")]
    [InlineData("journey", "journey/submit-and-reimburse", "description")]
    // The InlineData values name each prose field the length check reads.
    // Each case plants one 26-word sentence in that field, so the check reports it.
    public void Sentence_Over_Limit_Reported(string type, string id, string field)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var json = $$"""{"{{field}}": "{{Words(26)}}"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(type, id, Path.Combine(temp.Path, "record.md"), json));

        var findings = Checker.Check(model).Findings
            .Where(finding => finding.Code == "value-invalid" && finding.Message.Contains("sentence of", StringComparison.Ordinal))
            .ToList();
        var finding = Assert.Single(findings);

        Assert.Contains($"{field} sentence of 26 words", finding.Message, StringComparison.Ordinal);
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>Sentence At Limit Passes.</summary>
    /// <proves>litespec/core/writing#keep-each-record-sentence-within-twenty-five-words</proves>
    [Fact]
    public void Sentence_At_Limit_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var json = $$"""{"statement": "{{Words(25)}}"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "problem",
                "problem/expenses-wait-weeks",
                Path.Combine(temp.Path, "problem.md"),
                json));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Message.Contains("sentence of", StringComparison.Ordinal));
    }

    /// <summary>Title Case Defect Reuses Value Invalid Without Duplicate.</summary>
    /// <proves>litespec/core/writing#title-every-record-in-title-case</proves>
    [Fact]
    public void Title_Case_Defect_Reuses_Value_Invalid_Without_Duplicate()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"title": "room held during checkout", "kind": "functional", "statement": "A room is held for ten minutes.", "rationale": "partners share the risk"}"""));

        var findings = Checker.Check(model).Findings;

        Assert.Contains(findings, finding => finding.Code == "value-invalid" && finding.Message.Contains("is not Title Case", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Code == "field-invalid");
    }

    /// <summary>Clean Record Passes.</summary>
    /// <proves>litespec/core/writing#title-every-record-in-title-case</proves>
    [Fact]
    public void Clean_Record_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "goal",
                "goal/faster-reimbursement",
                Path.Combine(temp.Path, "goal.md"),
                """{"title": "Faster Reimbursement", "statement": "Employees receive reimbursement within two days of approval."}"""));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "field-invalid");
        Assert.DoesNotContain(findings, finding =>
            finding.Message.Contains("modal word", StringComparison.Ordinal)
            || finding.Message.Contains("sentence of", StringComparison.Ordinal)
            || finding.Message.Contains("title of", StringComparison.Ordinal));
    }
}
