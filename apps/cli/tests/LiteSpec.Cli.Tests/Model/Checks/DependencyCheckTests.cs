using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the dependency checks against the bill of materials and the admitting decisions.</summary>
public sealed class DependencyCheckTests
{
    /// <summary>A direct package with no pin and no admission reports dependency-unadmitted.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Unadmitted_Direct_Package_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("UnknownLib", "1.0.0", "MIT", "pkg:nuget/UnknownLib@1.0.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-unadmitted"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", finding);
        Assert.Contains("nuget:UnknownLib", finding);
        Assert.Contains("1.0.0", finding);
        Assert.Contains("MIT", finding);
        Assert.DoesNotContain("dependency-version-drift", check.Output);
        Assert.DoesNotContain("dependency-licence-unapproved", check.Output);
    }

    /// <summary>A direct package a technical decision admits reports no dependency-unadmitted.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Admitted_Direct_Package_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("UnknownLib", "1.0.0", "MIT", "pkg:nuget/UnknownLib@1.0.0")));
        WriteDecision(temp.Path, CompleteAdmits);
        WriteFollower(temp.Path);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>A pinned package at its pin with an accepted licence reports nothing.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void Pinned_Package_At_Pin_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "9.50.1", "MIT", "pkg:nuget/Marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>A pinned package shipped at another version reports dependency-version-drift.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void Version_Drift_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "1.0.0", "MIT", "pkg:nuget/Marten@1.0.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-version-drift"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", finding);
        Assert.Contains("nuget:Marten", finding);
        Assert.Contains("1.0.0", finding);
        Assert.Contains("MIT", finding);
        Assert.Contains("9.50.1", finding);
        Assert.DoesNotContain("dependency-unadmitted", check.Output);
        Assert.DoesNotContain("dependency-licence-unapproved", check.Output);
    }

    /// <summary>A direct package under a rejected licence reports dependency-licence-unapproved.</summary>
    /// <proves>litespec/workspace/dependencies#accept-every-shipped-package-licence-explicitly</proves>
    [Fact]
    public void Licence_Not_Accepted_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "9.50.1", "AGPL-3.0-only", "pkg:nuget/Marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-licence-unapproved"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", finding);
        Assert.Contains("nuget:Marten", finding);
        Assert.Contains("9.50.1", finding);
        Assert.Contains("AGPL-3.0-only", finding);
        Assert.DoesNotContain("dependency-unadmitted", check.Output);
        Assert.DoesNotContain("dependency-version-drift", check.Output);
    }

    /// <summary>An expression with every identifier accepted reports no licence finding.</summary>
    /// <proves>litespec/workspace/dependencies#accept-every-shipped-package-licence-explicitly</proves>
    [Fact]
    public void Expression_With_All_Licences_Accepted_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "9.50.1", "MIT OR Apache-2.0", "pkg:nuget/Marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>An expression with one identifier rejected reports dependency-licence-unapproved.</summary>
    /// <proves>litespec/workspace/dependencies#accept-every-shipped-package-licence-explicitly</proves>
    [Fact]
    public void Expression_With_One_Licence_Rejected_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "9.50.1", "MIT OR GPL-3.0-only", "pkg:nuget/Marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-licence-unapproved"));

        Assert.Equal(1, check.Status);
        Assert.Contains("MIT OR GPL-3.0-only", finding);
    }

    /// <summary>A licence the admitting decision names reports no licence finding.</summary>
    /// <proves>litespec/workspace/dependencies#accept-every-shipped-package-licence-explicitly</proves>
    [Fact]
    public void Licence_Named_On_Admission_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("UnknownLib", "2.0.0", "GPL-3.0-only", "pkg:nuget/UnknownLib@2.0.0")));
        WriteDecision(temp.Path, GplAdmits);
        WriteFollower(temp.Path);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>An admits entry missing a fact reports admission-incomplete.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Admission_Missing_Fact_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("Marten", "9.50.1", "MIT", "pkg:nuget/Marten@9.50.1")));
        WriteDecision(temp.Path, IncompleteAdmits);
        WriteFollower(temp.Path);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("admission-incomplete"));

        Assert.Equal(1, check.Status);
        Assert.Contains("[G]", finding);
        Assert.Contains("decision/0099-admit-extra-package", finding);
        Assert.Contains("nuget:UnknownLib", finding);
        Assert.Contains("operatingCost", finding);
        Assert.DoesNotContain("dependency-unadmitted", check.Output);
        Assert.DoesNotContain("dependency-version-drift", check.Output);
        Assert.DoesNotContain("dependency-licence-unapproved", check.Output);
    }

    /// <summary>An admits entry naming every fact reports no admission-incomplete.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Complete_Admission_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "docs", "proof", "sbom"));
        WriteDecision(temp.Path, CompleteAdmits);
        WriteFollower(temp.Path);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>Without paths.sbom the bill checks stay silent over a violating bill.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void Without_Sbom_Path_Bill_Checks_Silent()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(
            ("UnknownLib", "9.9.9", "GPL-3.0-only", "pkg:nuget/UnknownLib@9.9.9"),
            ("Marten", "1.0.0", "AGPL-3.0-only", "pkg:nuget/Marten@1.0.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>admission-incomplete runs even when the project names no bill of materials.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Admission_Incomplete_Without_Sbom_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteDecision(temp.Path, IncompleteAdmits);
        WriteFollower(temp.Path);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("admission-incomplete"));

        Assert.Equal(1, check.Status);
        Assert.Contains("operatingCost", finding);
    }

    /// <summary>A CycloneDX direct package with no pin and no admission reports dependency-unadmitted.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void CycloneDx_Unadmitted_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.cdx.json", CycloneDxDoc(("UnknownCdx", "1.0.0", "MIT", false, "pkg:nuget/UnknownCdx@1.0.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-unadmitted"));

        Assert.Equal(1, check.Status);
        Assert.Contains("nuget:UnknownCdx", finding);
        Assert.Contains("1.0.0", finding);
        Assert.Contains("MIT", finding);
    }

    /// <summary>A CycloneDX pinned package at its pin with an accepted licence reports nothing.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void CycloneDx_Pinned_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.cdx.json", CycloneDxDoc(("Marten", "9.50.1", "MIT", false, "pkg:nuget/Marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>A NuGet pin matches whatever case the bill spells the name in.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void NuGet_Pin_Match_Ignores_Case()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("marten", "9.50.1", "MIT", "pkg:nuget/marten@9.50.1")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    /// <summary>An npm name in another case than its pin reports dependency-unadmitted.</summary>
    /// <proves>litespec/workspace/dependencies#approve-new-packages-explicitly</proves>
    [Fact]
    public void Npm_Pin_Match_Respects_Case_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("React", "19.3.0", "MIT", "pkg:npm/React@19.3.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var finding = check.Output.Split('\n').Single(candidate => candidate.Contains("dependency-unadmitted"));

        Assert.Equal(1, check.Status);
        Assert.Contains("npm:React", finding);
    }

    /// <summary>An npm package at its pin with an accepted licence reports nothing.</summary>
    /// <proves>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</proves>
    [Fact]
    public void Npm_Pinned_Package_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        ConfigureSbom(temp.Path);
        Write(temp.Path, "docs/proof/sbom/api.spdx.json", SpdxDoc(("react", "19.3.0", "MIT", "pkg:npm/react@19.3.0")));

        var check = Cli.Run("model", "check", "--root", temp.Path);

        AssertNoDependencyFindings(check);
    }

    static void AssertNoDependencyFindings(CliResult check)
    {
        Assert.DoesNotContain("dependency-unadmitted", check.Output);
        Assert.DoesNotContain("dependency-version-drift", check.Output);
        Assert.DoesNotContain("dependency-licence-unapproved", check.Output);
        Assert.DoesNotContain("admission-incomplete", check.Output);
    }

    static void ConfigureSbom(string root)
    {
        var file = Path.Combine(root, "litespec.project.json");
        var node = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        node["paths"]!.AsObject()["sbom"] = new JsonArray("docs/proof/sbom");
        node["dependencies"] = new JsonObject { ["licences"] = new JsonArray("MIT", "Apache-2.0") };
        File.WriteAllText(file, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    static void WriteDecision(string root, string admitsJson) =>
        Write(root, "docs/implementation/decisions/0099-admit-extra-package.md", DecisionPage("decision/0099-admit-extra-package", admitsJson));

    static void WriteFollower(string root) =>
        Write(root, "apps/api/src/Library.Application/Lending/Loans/ExtraPackageAdapter.cs", FollowerFile("decision/0099-admit-extra-package", "ExtraPackageAdapter"));

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static string DecisionPage(string id, string admitsJson)
    {
        var frontmatter =
            "{\n"
            + "  \"type\": \"decision\",\n"
            + $"  \"id\": \"{id}\",\n"
            + "  \"title\": \"Admit extra package\",\n"
            + "  \"owner\": \"lending-team\",\n"
            + "  \"status\": \"approved\",\n"
            + "  \"decided\": \"2026-09-01\",\n"
            + "  \"options\": [\n"
            + "    {\"name\": \"Library\", \"for\": \"It fits.\", \"against\": \"It costs.\"},\n"
            + "    {\"name\": \"Hand written\", \"for\": \"No package.\", \"against\": \"More code.\"}\n"
            + "  ],\n"
            + "  \"chosen\": \"Library\",\n"
            + "  \"changeWhen\": \"The library stops fitting.\",\n"
            + "  \"basedOn\": [\"requirement/borrowing.loan-period-days\"],\n"
            + $"  \"admits\": {admitsJson}\n"
            + "}\n";

        using var document = JsonDocument.Parse(frontmatter);
        var record = new LiteSpec.Model.Record
        {
            Type = "decision",
            Id = id,
            Raw = document.RootElement.Clone(),
        };
        var hash = Checker.SemanticHash(record);
        var stamped = frontmatter.Replace(
            "\n}",
            ",\n  \"approvedBy\": \"lending-team\",\n  \"approvedOn\": \"2026-10-01\",\n  \"approvedHash\": \"" + hash + "\"\n}");

        return "---\n" + stamped + "---\n" + "\n" + "# Admit extra package\n" + "\n" + "A technical choice with its admission.\n";
    }

    static string FollowerFile(string decisionId, string className) =>
        "namespace Library.Application.Lending.Loans;\n"
        + "\n"
        + "/// <summary>Follows its admitting decision.</summary>\n"
        + $"/// <follows>{decisionId}</follows>\n"
        + $"internal sealed class {className}\n"
        + "{\n"
        + "    public string Describe(string input) => \"Admitted: \" + input;\n"
        + "}\n";

    static string SpdxDoc(params (string Name, string Version, string Licence, string Purl)[] direct)
    {
        var packages = string.Join(",\n", direct.Select((entry, index) =>
            "    {\n"
            + $"      \"SPDXID\": \"SPDXRef-Package-{index}\",\n"
            + $"      \"name\": \"{entry.Name}\",\n"
            + $"      \"versionInfo\": \"{entry.Version}\",\n"
            + "      \"supplier\": \"Organization: Test\",\n"
            + "      \"downloadLocation\": \"NOASSERTION\",\n"
            + "      \"filesAnalyzed\": false,\n"
            + $"      \"licenseConcluded\": \"{entry.Licence}\",\n"
            + $"      \"licenseDeclared\": \"{entry.Licence}\",\n"
            + "      \"copyrightText\": \"NOASSERTION\",\n"
            + "      \"externalRefs\": [\n"
            + "        {\n"
            + "          \"referenceCategory\": \"PACKAGE-MANAGER\",\n"
            + "          \"referenceType\": \"purl\",\n"
            + $"          \"referenceLocator\": \"{entry.Purl}\"\n"
            + "        }\n"
            + "      ]\n"
            + "    }"));

        var depends = string.Join(",\n", direct.Select((_, index) =>
            "    {\n"
            + "      \"spdxElementId\": \"SPDXRef-Package-Root\",\n"
            + "      \"relationshipType\": \"DEPENDS_ON\",\n"
            + $"      \"relatedSpdxElement\": \"SPDXRef-Package-{index}\"\n"
            + "    }"));

        return
            "{\n"
            + "  \"spdxVersion\": \"SPDX-2.3\",\n"
            + "  \"dataLicense\": \"CC0-1.0\",\n"
            + "  \"SPDXID\": \"SPDXRef-DOCUMENT\",\n"
            + "  \"name\": \"library-api\",\n"
            + "  \"documentNamespace\": \"https://library.example/sbom/api\",\n"
            + "  \"creationInfo\": {\"created\": \"2026-10-01T00:00:00Z\", \"creators\": [\"Tool: test\"]},\n"
            + "  \"packages\": [\n"
            + "    {\n"
            + "      \"SPDXID\": \"SPDXRef-Package-Root\",\n"
            + "      \"name\": \"Library.WebApi\",\n"
            + "      \"versionInfo\": \"1.0.0\",\n"
            + "      \"supplier\": \"Organization: Library\",\n"
            + "      \"downloadLocation\": \"NOASSERTION\",\n"
            + "      \"filesAnalyzed\": false,\n"
            + "      \"licenseConcluded\": \"NOASSERTION\",\n"
            + "      \"licenseDeclared\": \"NOASSERTION\",\n"
            + "      \"copyrightText\": \"NOASSERTION\"\n"
            + (packages.Length > 0 ? "    },\n" + packages + "\n" : "    }\n")
            + "  ],\n"
            + "  \"relationships\": [\n"
            + "    {\n"
            + "      \"spdxElementId\": \"SPDXRef-DOCUMENT\",\n"
            + "      \"relationshipType\": \"DESCRIBES\",\n"
            + "      \"relatedSpdxElement\": \"SPDXRef-Package-Root\"\n"
            + (depends.Length > 0 ? "    },\n" + depends + "\n" : "    }\n")
            + "  ]\n"
            + "}\n";
    }

    static string CycloneDxDoc(params (string Name, string Version, string Licence, bool Expression, string Purl)[] direct)
    {
        var components = string.Join(",\n", direct.Select((entry, index) =>
            "    {\n"
            + $"      \"bom-ref\": \"pkg-{index}\",\n"
            + "      \"type\": \"library\",\n"
            + $"      \"name\": \"{entry.Name}\",\n"
            + $"      \"version\": \"{entry.Version}\",\n"
            + $"      \"purl\": \"{entry.Purl}\",\n"
            + "      \"licenses\": [\n"
            + (entry.Expression ? $"        {{\"expression\": \"{entry.Licence}\"}}\n" : $"        {{\"license\": {{\"id\": \"{entry.Licence}\"}}}}\n")
            + "      ]\n"
            + "    }"));

        var depends = string.Join(", ", direct.Select((_, index) => $"\"pkg-{index}\""));

        return
            "{\n"
            + "  \"bomFormat\": \"CycloneDX\",\n"
            + "  \"specVersion\": \"1.6\",\n"
            + "  \"version\": 1,\n"
            + "  \"metadata\": {\"component\": {\"bom-ref\": \"root\", \"type\": \"application\", \"name\": \"Library.WebApi\"}},\n"
            + "  \"components\": [\n"
            + components + "\n"
            + "  ],\n"
            + "  \"dependencies\": [\n"
            + $"    {{\"ref\": \"root\", \"dependsOn\": [{depends}]}}\n"
            + "  ]\n"
            + "}\n";
    }

    const string CompleteAdmits = """[{"package": "nuget:UnknownLib", "layer": "Infrastructure", "licence": "MIT", "location": "in-process", "accountOwner": "none", "failureBehavior": "The download fails and the page stays readable.", "operatingCost": "none"}]""";

    const string IncompleteAdmits = """[{"package": "nuget:UnknownLib", "layer": "Infrastructure", "licence": "MIT", "location": "in-process", "accountOwner": "none", "failureBehavior": "The download fails and the page stays readable."}]""";

    const string GplAdmits = """[{"package": "nuget:UnknownLib", "layer": "Infrastructure", "licence": "GPL-3.0-only", "location": "in-process", "accountOwner": "none", "failureBehavior": "The download fails and the page stays readable.", "operatingCost": "none"}]""";
}
