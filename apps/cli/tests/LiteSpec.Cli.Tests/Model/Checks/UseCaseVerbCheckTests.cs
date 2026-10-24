using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the use-case verb check: a declared vocabulary holds every use-case opening verb.</summary>
public sealed class UseCaseVerbCheckTests
{
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

    [Fact]
    public void Retired_Synonym_Names_Its_Replacement()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.update-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "update-copy.md"), """{"kind": "command"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-verb-unknown").Message;
        Assert.Contains("starts with 'update'; use 'change'", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_Vocabulary_Reports_Nothing()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.fetch-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "fetch-copy.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """{"vocabulary": {"verbs": ["borrow", "list", "change"], "phrasal": ["check-in"], "retired": {"update": "change"}}}""");
}
