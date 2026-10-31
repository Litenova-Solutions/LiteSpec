using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the advisory checks and the open-questions shape rule against hand-built models.</summary>
public sealed class AdvisoryCheckTests
{
    /// <proves>litespec/core/use-cases#write-prose-on-significant-use-cases</proves>
    [Fact]
    public void Significant_Use_Case_Without_Goal_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "changes": ["aggregate/lending.loan", "aggregate/lending.copy"]}"""));

        Assert.Contains("prose-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#write-prose-on-significant-use-cases</proves>
    [Fact]
    public void Trivial_Use_Case_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));

        Assert.DoesNotContain("prose-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void Atomic_With_Two_Cites_Rules_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "consistency": {"mode": "atomic", "cites": ["rule/loan.one-open-loan", "rule/copy.one-home-shelf"]}}"""));

        Assert.Contains("atomic-group-duplicate", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#state-the-consistency-of-every-use-case</proves>
    [Fact]
    public void Atomic_With_One_Cites_Rule_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command", "consistency": {"mode": "atomic", "cites": ["rule/loan.one-open-loan"]}}"""));

        Assert.DoesNotContain("atomic-group-duplicate", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Tier3_Question_Without_Recommendation_Past_DecideBy_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2020-01-01", "ballot": "junior-holds"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "question-expired");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Record 'actor/reader' holds a question past its decide-by date.", finding.Message);
        Assert.Equal("actor/reader", finding.Subject);
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Tier3_Question_With_Recommendation_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2020-01-01", "ballot": "junior-holds", "recommendation": "Yes, with a guardian."}]}"""));

        Assert.DoesNotContain("question-expired", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Tier1_Question_Past_DecideBy_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "risk": ["money"], "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2020-01-01", "ballot": "junior-holds"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "question-expired");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Record 'actor/reader' holds a question past its decide-by date.", finding.Message);
        Assert.Equal("actor/reader", finding.Subject);
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Answered_Question_Past_DecideBy_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2020-01-01", "ballot": "junior-holds", "answer": "Yes, with a guardian.", "answeredBy": "lending-team", "answeredOn": "2020-01-02"}]}"""));

        Assert.DoesNotContain("question-expired", TestModel.Codes(model));
        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code == "record-invalid" && finding.Message.Contains("open questions", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Question_Without_Decider_Reports_Draft_Without_Decider()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decideBy": "2035-02-01", "ballot": "junior-holds", "recommendation": "Yes, with a guardian."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "decider-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'actor/reader' holds a question that names no decider.", finding.Message);
        Assert.Equal("actor/reader", finding.Subject);
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Question_Without_DecideBy_Reports_Draft_Without_Decider()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "ballot": "junior-holds", "recommendation": "Yes, with a guardian."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "decider-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Record 'actor/reader' holds a question that names no decide-by date.", finding.Message);
        Assert.Equal("actor/reader", finding.Subject);
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Tier3_Question_Without_Recommendation_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2035-02-01", "ballot": "junior-holds"}]}"""));

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "record-invalid" && found.Message.Contains("names no recommendation", StringComparison.Ordinal));
        Assert.Equal("Record 'actor/reader' holds a question that names no recommendation.", finding.Message);
        Assert.Equal("actor/reader", finding.Subject);
    }

    /// <proves>litespec/core/model#answer-each-question-on-its-record</proves>
    [Fact]
    public void Tier1_Question_Without_Recommendation_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "risk": ["money"], "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2035-02-01", "ballot": "junior-holds"}]}"""));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code == "record-invalid" && finding.Message.Contains("names no recommendation", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/model#hold-approval-behind-open-questions</proves>
    [Fact]
    public void Approved_With_Open_Questions_Reports_Record_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2035-02-01", "ballot": "junior-holds", "recommendation": "Yes, with a guardian."}]}"""));

        Assert.Contains(
            Checker.Check(model).Findings,
            finding => finding.Code == "record-invalid" && finding.Message.Contains("open questions", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/model#hold-approval-behind-open-questions</proves>
    [Fact]
    public void Approved_With_Auto_Approved_Questions_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decider": "lending-team", "decideBy": "2020-01-01", "ballot": "junior-holds", "recommendation": "Yes, with a guardian."}]}"""));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            finding => finding.Code == "record-invalid" && finding.Message.Contains("open questions", StringComparison.Ordinal));
    }
}
