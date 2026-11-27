using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the verb distinction check: a glossary verb retaking a retired synonym names the baseline verb it differs from in its meaning.</summary>
public sealed class VerbDistinctionCheckTests
{
    /// <summary>A retired synonym whose meaning names its baseline verb passes, and a use case opening with it passes.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A retired synonym naming its baseline verb passes")]
    public void RetiredSynonymNamingBaselinePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.archive-copy", Path.Combine(temp.Path, "archive.yaml"), """{"kind": "command"}"""),
            VerbTerm(temp.Path, "archive", "command", "A copy kept readable and restorable, unlike a copy taken out of use by retire."));

        Assert.DoesNotContain("verb-distinction-missing", TestModel.Codes(model));
        Assert.DoesNotContain("use-case-verb-unknown", TestModel.Codes(model));
    }

    /// <summary>A retired synonym whose meaning names no baseline verb is reported with the verb and its baseline.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A retired synonym missing its baseline verb is reported")]
    public void RetiredSynonymMissingBaselineIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            VerbTerm(temp.Path, "archive", "command", "A copy kept readable and restorable."));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "verb-distinction-missing");
        Assert.Contains("term/archive", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'archive'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'retire'", finding.Message, StringComparison.Ordinal);
        Assert.Equal("term/archive", finding.Subject);
    }

    /// <summary>A retired synonym whose meaning holds its baseline verb only inside another word is reported.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A baseline verb inside another word does not pass")]
    public void BaselineInsideAnotherWordIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            VerbTerm(temp.Path, "get", "query", "Already fetched records."));

        Assert.Contains("verb-distinction-missing", TestModel.Codes(model));
    }

    /// <summary>A retired synonym whose meaning names its baseline verb with a capital passes.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A baseline verb with a capital passes")]
    public void BaselineWithCapitalPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            VerbTerm(temp.Path, "deny", "command", "Access stopped by a rule, unlike a request decided by Refuse."));

        Assert.DoesNotContain("verb-distinction-missing", TestModel.Codes(model));
    }

    /// <summary>A verb outside the retired synonyms passes without naming a baseline verb.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "A verb outside the retired synonyms passes")]
    public void VerbOutsideRetiredSynonymsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            VerbTerm(temp.Path, "borrow", "command", "The loan of one copy to one reader."));

        Assert.DoesNotContain("verb-distinction-missing", TestModel.Codes(model));
    }

    /// <summary>The retired synonyms the code reads are the synonyms the use-cases page retires.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact(DisplayName = "The retired synonyms match the use-cases page")]
    public void RetiredSynonymsMatchTheUseCasesPage()
    {
        var page = File.ReadAllText(Path.Combine(TestCli.Repository, "docs", "core", "use-cases.md"));
        var table = Regex.Match(page, @"\| Retired synonyms \| Become \|\s*\n(?<body>.*?)\n\n", RegexOptions.Singleline).Groups["body"].Value;
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match row in Regex.Matches(table, @"^\| (?<retired>[^|]+)\| (?<baseline>[^|]+)\|", RegexOptions.Multiline))
        {
            var baseline = Regex.Match(row.Groups["baseline"].Value, "`(?<verb>[a-z-]+)`").Groups["verb"].Value;

            foreach (Match synonym in Regex.Matches(row.Groups["retired"].Value, "`(?<verb>[a-z-]+)`"))
            {
                expected[synonym.Groups["verb"].Value] = baseline;
            }
        }

        Assert.NotEmpty(expected);
        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal), UseCaseVerbChecks.Retired.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    /// <summary>Builds a glossary verb term with its kind and meaning.</summary>
    /// <param name="root">The consumer root the glossary file sits under.</param>
    /// <param name="verb">The verb the term declares.</param>
    /// <param name="verbKind">The use-case kind the verb opens.</param>
    /// <param name="meaning">The term meaning.</param>
    /// <returns>The term record.</returns>
    private static Record VerbTerm(string root, string verb, string verbKind, string meaning) =>
        TestModel.Record(
            "term",
            "term/" + verb,
            Path.Combine(root, ".litespec", "glossary.yaml"),
            "{\"term\": \"" + verb + "\", \"meaning\": \"" + meaning + "\", \"verbKind\": \"" + verbKind + "\"}");
}
