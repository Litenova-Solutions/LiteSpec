using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the use-case verb checks: a use-case name opens with a baseline verb or a glossary verb of its own kind,
///     a glossary verb never restates a baseline verb, and a permission name opens with a glossary verb of kind command or either.
/// </summary>
public sealed class UseCaseVerbCheckTests
{
    /// <summary>A use case opening with a baseline verb of its own kind passes.</summary>
    /// <param name="id">The use-case identifier under test.</param>
    /// <param name="kind">The use-case kind under test.</param>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Theory(DisplayName = "A use case opening with a baseline verb of its own kind passes")]
    [InlineData("use-case/lending.list-loans", "query")]
    [InlineData("use-case/lending.change-copy", "command")]
    [InlineData("use-case/lending.answer-inquiry", "query")]
    [InlineData("use-case/lending.answer-inquiry", "command")]
    public void BaselineVerbOfItsOwnKindPasses(string id, string kind)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), UseCase(temp.Path, id, kind));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("use-case-verb-invalid", TestModel.Codes(model));
    }

    /// <summary>A use case opening with a glossary verb of its own kind passes.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A use case opening with a glossary verb of its own kind passes")]
    public void GlossaryVerbOfItsOwnKindPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "use-case/lending.borrow-copy", "command"),
            VerbTerm(temp.Path, "borrow", "command"));

        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("use-case-verb-invalid", TestModel.Codes(model));
    }

    /// <summary>A use case opening with a verb neither list declares is reported with the verb it opens with.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A use case opening with an undeclared verb is reported")]
    public void UndeclaredVerbIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "use-case/lending.update-copy", "command"),
            VerbTerm(temp.Path, "borrow", "command"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-verb-unknown");
        Assert.Equal("Use case 'use-case/lending.update-copy' opens with 'update', which is no verb of the use-case verb table or the project glossary.", finding.Message);
    }

    /// <summary>A use case opening with a verb of the other kind is reported with both kinds.</summary>
    /// <param name="id">The use-case identifier under test.</param>
    /// <param name="kind">The use-case kind under test.</param>
    /// <param name="verb">The opening verb the message names.</param>
    /// <param name="verbKind">The verb kind the message names.</param>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Theory(DisplayName = "A use case opening with a verb of the other kind is reported")]
    [InlineData("use-case/lending.change-copy", "query", "change", "command")]
    [InlineData("use-case/lending.read-overdue-count", "command", "read", "query")]
    public void VerbOfTheOtherKindIsReported(string id, string kind, string verb, string verbKind)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), UseCase(temp.Path, id, kind));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-verb-invalid");
        Assert.Equal($"Use case '{id}' is a {kind} opening with the {verbKind} verb '{verb}'.", finding.Message);
    }

    /// <summary>A use case opening with a glossary verb of the other kind is reported.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact(DisplayName = "A use case opening with a glossary verb of the other kind is reported")]
    public void GlossaryVerbOfTheOtherKindIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "use-case/lending.borrow-copy", "query"),
            VerbTerm(temp.Path, "borrow", "command"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-verb-invalid");
        Assert.Equal("Use case 'use-case/lending.borrow-copy' is a query opening with the command verb 'borrow'.", finding.Message);
    }

    /// <summary>A glossary verb term restating a baseline verb is reported with the verb it restates.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A glossary verb restating a baseline verb is reported")]
    public void BaselineVerbRedefinedIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            VerbTerm(temp.Path, "read", "query"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "term-verb-duplicate");
        Assert.Equal("Term 'term/read' declares the verb 'read', which the use-case verb table already declares.", finding.Message);
    }

    /// <summary>A permission opening with a glossary verb of kind command or either passes.</summary>
    /// <param name="id">The permission identifier under test.</param>
    /// <param name="verb">The glossary verb under test.</param>
    /// <param name="verbKind">The glossary verb kind under test.</param>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Theory(DisplayName = "A permission opening with a changing glossary verb passes")]
    [InlineData("permission/submit-expense", "submit", "command")]
    [InlineData("permission/triage-inbox", "triage", "either")]
    public void PermissionWithChangingGlossaryVerbPasses(string id, string verb, string verbKind)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Permission(temp.Path, id),
            VerbTerm(temp.Path, verb, verbKind));

        Assert.DoesNotContain("permission-verb-unknown", TestModel.Codes(model));
    }

    /// <summary>A permission opening with a glossary verb of kind query is reported with the verb it opens with.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "A permission opening with a query glossary verb is reported")]
    public void PermissionWithQueryGlossaryVerbIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Permission(temp.Path, "permission/lookup-cache"),
            VerbTerm(temp.Path, "lookup", "query"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "permission-verb-unknown");
        Assert.Equal("Permission 'permission/lookup-cache' opens with 'lookup', which is a glossary verb of kind query.", finding.Message);
    }

    /// <summary>The baseline verb kinds the code reads are the kinds the use-cases page's verb table states.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact(DisplayName = "The baseline verb kinds match the use-cases page verb table")]
    public void BaselineVerbKindsMatchTheVerbTable()
    {
        var page = File.ReadAllText(Path.Combine(TestCli.Repository, "docs", "core", "use-cases.md"));
        var table = Regex.Match(page, @"### Use-Case Verbs\s*\n(?<body>.*?)\n\nThe baseline vocabulary retires", RegexOptions.Singleline).Groups["body"].Value;
        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match row in Regex.Matches(table, @"^\| (?<verbs>[^|]+)\| (?<kind>[a-z]+) \|", RegexOptions.Multiline))
        {
            foreach (Match verb in Regex.Matches(row.Groups["verbs"].Value, "`(?<verb>[a-z-]+)`"))
            {
                kinds[verb.Groups["verb"].Value] = row.Groups["kind"].Value;
            }
        }

        Assert.NotEmpty(kinds);
        Assert.Equal(kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal), UseCaseVerbs.Kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    /// <summary>Builds a use-case record with its kind.</summary>
    /// <param name="root">The consumer root the use-case file sits under.</param>
    /// <param name="id">The use-case identifier.</param>
    /// <param name="kind">The use-case kind.</param>
    /// <returns>The use-case record.</returns>
    private static Record UseCase(string root, string id, string kind) =>
        TestModel.Record(
            "use-case",
            id,
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "borrow-copy.yaml"),
            "{\"kind\": \"" + kind + "\"}");

    /// <summary>Builds a glossary verb term with its kind.</summary>
    /// <param name="root">The consumer root the glossary file sits under.</param>
    /// <param name="verb">The verb the term declares.</param>
    /// <param name="verbKind">The use-case kind the verb opens.</param>
    /// <returns>The term record.</returns>
    private static Record VerbTerm(string root, string verb, string verbKind) =>
        TestModel.Record(
            "term",
            "term/" + verb,
            Path.Combine(root, ".litespec", "glossary.yaml"),
            "{\"term\": \"" + verb + "\", \"meaning\": \"The act under test.\", \"verbKind\": \"" + verbKind + "\"}");

    /// <summary>Builds a permission record with its meaning.</summary>
    /// <param name="root">The consumer root the permission file sits under.</param>
    /// <param name="id">The permission identifier.</param>
    /// <returns>The permission record.</returns>
    private static Record Permission(string root, string id) =>
        TestModel.Record(
            "permission",
            id,
            Path.Combine(root, ".litespec", "behavior", "permissions.yaml"),
            "{\"meaning\": \"The right to act.\"}");
}
