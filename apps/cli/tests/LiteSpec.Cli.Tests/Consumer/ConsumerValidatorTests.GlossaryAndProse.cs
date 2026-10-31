using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for the glossary file and prose measures.</summary>
public sealed partial class ConsumerValidatorTests
{
    const string ModuleReadme = "docs/behavior/modules/orders/README.md";
    const string GlossaryFile = "docs/glossary.json";

    /// <summary>Glossary without its version.</summary>
    /// <proves>litespec/core/authoring#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "glossary without its version")]
    public void GlossaryFileWithoutItsVersion()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateJson(folder.Path, GlossaryFile, record => record.Remove("schemaVersion"));
        AssertProblem(RunConsumer(folder.Path), "schemaVersion");
    }

    /// <summary>Glossary with the old version.</summary>
    /// <proves>litespec/core/authoring#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "glossary with the old version")]
    public void GlossaryFileWithTheOldVersion()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateJson(folder.Path, GlossaryFile, record => record["schemaVersion"] = 1);
        AssertProblem(RunConsumer(folder.Path), "/schemaVersion");
    }

    /// <summary>Term scope that is not a regular expression.</summary>
    /// <proves>litespec/core/authoring#record-the-project-vocabulary-as-data</proves>
    [Fact(DisplayName = "term scope that is not a regular expression")]
    public void TermScopeThatIsNotARegularExpression()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateJson(
            folder.Path,
            GlossaryFile,
            record =>
            {
                var term = (JsonObject)((JsonArray)record["terms"]!)[0]!;
                var rejected = (JsonObject)((JsonArray)term["rejected"]!)[0]!;
                rejected["scope"] = "([";
            });
        AssertProblem(RunConsumer(folder.Path), "is not a regular expression");
    }

    /// <summary>Rejected synonym in page prose.</summary>
    /// <proves>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</proves>
    [Fact(DisplayName = "rejected synonym in page prose")]
    public void RejectedSynonymInPageProse()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ModuleReadme, raw => $"{raw}\nThe orderclaim form is wrong.\n");
        AssertProblem(RunConsumer(folder.Path), "GLOSSARY_REJECTED_SYNONYM");
    }

    /// <summary>Mannered term in page prose.</summary>
    /// <proves>litespec/core/authoring#state-mannered-terms-literally</proves>
    [Fact(DisplayName = "mannered term in page prose")]
    public void ManneredTermInPageProse()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ModuleReadme, raw => $"{raw}\nThis rule is load-bearing for orders.\n");
        AssertProblem(RunConsumer(folder.Path), "GLOSSARY_MANNERED_TERM");
    }

    /// <summary>Generated region skips language and prose.</summary>
    /// <proves>litespec/core/authoring#exclude-generated-regions-from-prose-checks</proves>
    [Fact(DisplayName = "generated region skips language and prose")]
    public void GeneratedRegionSkipsLanguageAndProse()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ModuleReadme,
            raw => $"{raw}\n<!-- generated:counts -->\nThe orderclaim form is load-bearing for word counts here and keeps growing past every bound a page would state for one sentence alone today without doubt or pause at all.\n<!-- /generated:counts -->\n");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Glossary scan pattern that matches nothing.</summary>
    /// <proves>litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets</proves>
    [Fact(DisplayName = "glossary scan pattern that matches nothing")]
    public void GlossaryScanPatternThatMatchesNothing()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(
            folder.Path,
            project => PathsOf(project)["glossaryScan"] = new JsonArray("src/**/*.cs"));
        AssertProblem(RunConsumer(folder.Path), "matches no file");
    }

    /// <summary>Rejected synonym in a scanned surface.</summary>
    /// <proves>litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets</proves>
    /// <proves>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</proves>
    [Fact(DisplayName = "rejected synonym in a scanned surface")]
    public void RejectedSynonymInAScannedSurface()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(
            folder.Path,
            project => PathsOf(project)["glossaryScan"] = new JsonArray("src/**/*.cs"));
        ConsumerFixture.WriteFile(folder.Path, "src/order-claims.cs", "// the orderclaim handling lives here\n");
        AssertProblem(RunConsumer(folder.Path), "GLOSSARY_REJECTED_SYNONYM");
    }
}
