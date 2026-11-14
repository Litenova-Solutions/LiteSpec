using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the access checks: a role scope needs a glossary term titled with the level and Scope, and a permission
///     name opens with a verb of the use-case verb table or with manage.
/// </summary>
public sealed class AccessCheckTests
{
    /// <summary>A role with a scope that has its glossary term passes.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A role scope with its glossary term passes")]
    public void ScopeWithItsTermPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "finance", "platform"),
            TestModel.Record("term", "term/platform-scope", Path.Combine(temp.Path, ".litespec", "glossary.yaml"), "{\"title\": \"platform scope\", \"term\": \"platform scope\"}"));

        Assert.DoesNotContain("scope-term-missing", TestModel.Codes(model));
    }

    /// <summary>A role scope with no glossary term titled with the level and Scope is reported with the title it needs.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A role scope with no glossary term is reported with its title")]
    public void ScopeWithoutTermIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "finance", "platform"),
            TestModel.Record("term", "term/self-scope", Path.Combine(temp.Path, ".litespec", "glossary.yaml"), "{\"title\": \"self scope\", \"term\": \"self scope\"}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "scope-term-missing");
        Assert.Equal("Role 'role/finance' names scope 'platform', and no glossary term is titled 'Platform Scope'.", finding.Message);
    }

    /// <summary>A multi-word scope level takes each kebab word capitalized in its title.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A multi-word scope level takes its title from each word")]
    public void MultiWordScopeTakesEachWordInItsTitle()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "desk", "lending-desk"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "scope-term-missing");
        Assert.Contains("'Lending Desk Scope'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A permission opening with a verb of the table, or with manage, passes.</summary>
    /// <param name="id">The permission identifier under test.</param>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Theory(DisplayName = "A permission opening with a table verb or manage passes")]
    [InlineData("permission/approve-expense")]
    [InlineData("permission/list-expenses")]
    [InlineData("permission/manage-policy")]
    public void VerbFromTheTablePasses(string id)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Permission(temp.Path, id));

        Assert.DoesNotContain("permission-verb-unknown", TestModel.Codes(model));
    }

    /// <summary>A permission opening with no verb of the table is reported with the verb it opens with.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "A permission opening with no table verb is reported")]
    public void VerbOutsideTheTableIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Permission(temp.Path, "permission/handle-documents"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "permission-verb-unknown");
        Assert.Equal("Permission 'permission/handle-documents' opens with 'handle', which is no verb of the use-case verb table.", finding.Message);
    }

    /// <summary>The baseline verbs the code reads are the verbs the use-cases page's verb table states.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "The baseline verbs match the use-cases page verb table")]
    public void BaselineVerbsMatchTheVerbTable()
    {
        var page = File.ReadAllText(Path.Combine(TestCli.Repository, "docs", "core", "use-cases.md"));
        var table = Regex.Match(page, @"### Use-Case Verbs\s*\n(?<body>.*?)\n\nThe baseline vocabulary retires", RegexOptions.Singleline).Groups["body"].Value;
        var verbs = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match row in Regex.Matches(table, @"^\| (?<cells>[^|]+)\|", RegexOptions.Multiline))
        {
            foreach (Match verb in Regex.Matches(row.Groups["cells"].Value, "`(?<verb>[a-z-]+)`"))
            {
                verbs.Add(verb.Groups["verb"].Value);
            }
        }

        Assert.NotEmpty(verbs);
        Assert.Equal(verbs.OrderBy(verb => verb, StringComparer.Ordinal), UseCaseVerbs.Baseline.OrderBy(verb => verb, StringComparer.Ordinal));
    }

    /// <summary>Builds a role record with its scope and one allowed permission.</summary>
    /// <param name="root">The consumer root the role file sits under.</param>
    /// <param name="name">The role name, which names its folder and file.</param>
    /// <param name="scope">The scope level the role names.</param>
    /// <returns>The role record.</returns>
    private static Record Role(string root, string name, string scope) =>
        TestModel.Record(
            "role",
            "role/" + name,
            Path.Combine(root, ".litespec", "intent", "roles", name, name + ".yaml"),
            "{\"scope\": \"" + scope + "\", \"allows\": [\"permission/approve-expense\"], \"summary\": \"A role.\"}");

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
