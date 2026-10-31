using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the builder reads decider and decideBy on glossary terms, so a draft term carrying both raises nothing.</summary>
public sealed class TermDeciderTests
{
    /// <proves>litespec/core/model#keep-one-owner-and-status-on-every-record</proves>
    [Fact]
    public void Draft_Term_With_Decider_And_DecideBy_Raises_Nothing()
    {
        using var temp = WriteConsumer(Term(decider: true));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var findings = Checker.Check(model).Findings;

        var term = Assert.Single(model.Records, record => record.Id == "term/loan");
        Assert.Equal("lending-team", term.Decider);
        Assert.Equal("2030-01-31", term.DecideBy);
        Assert.DoesNotContain(findings, finding => finding.Code == "decider-missing");
    }

    /// <proves>litespec/core/model#keep-one-owner-and-status-on-every-record</proves>
    [Fact]
    public void Draft_Term_Without_Decider_Reports()
    {
        using var temp = WriteConsumer(Term(decider: false));

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var findings = Checker.Check(model).Findings;

        var missing = findings.Where(finding => finding.Code == "decider-missing").ToList();

        Assert.Equal(2, missing.Count);
        Assert.Contains("Draft 'term/loan' names no decider.", missing.Select(finding => finding.Message));
        Assert.Contains("Draft 'term/loan' names no decide-by date.", missing.Select(finding => finding.Message));
    }

    static string Term(bool decider) =>
        decider
            ? """
              {
                "id": "term/loan",
                "term": "loan",
                "meaning": "A copy lent to one borrower until its due date.",
                "layer": "intent",
                "owner": "lending-team",
                "status": "draft",
                "decider": "lending-team",
                "decideBy": "2030-01-31"
              }
              """
            : """
              {
                "id": "term/loan",
                "term": "loan",
                "meaning": "A copy lent to one borrower until its due date.",
                "layer": "intent",
                "owner": "lending-team",
                "status": "draft"
              }
              """;

    static TempFolder WriteConsumer(string term)
    {
        var temp = Cli.Temp("term-decider");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {"project": {"name": "Term Decider Fixture"}, "size": "small", "shape": "server",
                  "stacks": ["dotnet"], "reviewedLiteSpecVersion": "3.0.0", "owners": ["lending-team"],
                  "paths": {"docs": "docs", "glossary": "docs/glossary.json", "testRoots": [], "frontends": []}}
                """);
            Write(temp.Path, "docs/glossary.json", $$"""
                {"schemaVersion": 2, "terms": [{{term}}], "mannered": []}
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
        var file = Path.Combine(root, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }
}
