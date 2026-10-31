using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the naming checks: the baseline and project verbs, one kind per verb, and the reaction and job name forms.</summary>
public sealed class UseCaseVerbCheckTests
{
    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Declared_Verb_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Single_Word_Name_Equal_To_Verb_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.list", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "list.md"), """{"kind": "query"}"""));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Declared_Phrasal_Verb_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.check-in-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "check-in-copy.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Unknown_Verb_Reports_Use_Case_Verb_Unknown()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.fetch-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "fetch-copy.md"), """{"kind": "command"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-verb-unknown").Message;
        Assert.Contains("starts with 'fetch'", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Verb_Prefix_Without_Dash_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.listing-loans", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "listing-loans.md"), """{"kind": "query"}"""));

        Assert.Contains("use-case-verb-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Retired_Synonym_Names_Its_Replacement()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.update-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "update-copy.md"), """{"kind": "command"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-verb-unknown").Message;
        Assert.Contains("starts with 'update' and 'change' replaces it", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/behavior#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Missing_Vocabulary_Checks_The_Baseline()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.fetch-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "fetch-copy.md"), """{"kind": "query"}"""),
            TestModel.Record("use-case", "use-case/lending.read-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "read-copy.md"), """{"kind": "query"}"""));

        var unknown = Checker.Check(model).Findings.Where(finding => finding.Code == "use-case-verb-unknown").ToList();

        Assert.Single(unknown);
        Assert.Contains("'read' replaces it", unknown[0].Message);
    }

    /// <proves>litespec/core/behavior#keep-one-kind-per-verb</proves>
    [Fact]
    public void Query_Opening_With_A_Changing_Verb_Reports_The_Kind()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.change-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "change-copy.md"), """{"kind": "query"}"""),
            TestModel.Record("use-case", "use-case/lending.list-copies", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "list-copies.md"), """{"kind": "command"}"""));

        Assert.Equal(2, TestModel.Codes(model).Count(code => code == "use-case-verb-invalid"));
    }

    /// <proves>litespec/core/behavior#keep-one-kind-per-verb</proves>
    [Fact]
    public void Project_Query_Verb_Opens_Only_A_Query()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), """{"vocabulary": {"queryVerbs": ["trace"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.trace-loan", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "trace-loan.md"), """{"kind": "command"}"""));

        Assert.Contains("use-case-verb-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/behavior#name-each-reaction-for-its-act-and-its-trigger</proves>
    [Fact]
    public void Reaction_Named_For_Its_Act_And_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "reactions", "send-due-notice-on-loan-borrowed.md"), """{"on": ["event/loan.loan-borrowed"]}"""),
            TestModel.Record("reaction", "reaction/lending.notify-borrower", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "reactions", "notify-borrower.md"), """{"on": ["event/loan.loan-borrowed"]}"""));

        var form = Checker.Check(model).Findings.Where(finding => finding.Code == "reaction-name-invalid").ToList();

        Assert.Single(form);
        Assert.Contains("notify-borrower", form[0].Message);
    }

    /// <proves>litespec/core/behavior#name-each-job-for-the-command-it-issues</proves>
    [Fact]
    public void Job_Carries_The_Verb_Of_The_Command_It_Issues()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.expire-holds", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "holds", "expire-holds.md"), """{"kind": "command"}"""),
            TestModel.Record("job", "job/lending.expire-lapsed-holds", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "jobs", "expire-lapsed-holds.md"), """{"effects": [{"issues": "use-case/lending.expire-holds"}]}"""),
            TestModel.Record("job", "job/lending.sweep-holds", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "jobs", "sweep-holds.md"), """{"effects": [{"issues": "use-case/lending.expire-holds"}]}"""));

        var form = Checker.Check(model).Findings.Where(finding => finding.Code == "job-name-invalid").ToList();

        Assert.Single(form);
        Assert.Contains("sweep-holds", form[0].Message);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """{"vocabulary": {"verbs": ["borrow", "list", "change"], "phrasal": ["check-in"], "retired": {"update": "change"}}}""");
}
