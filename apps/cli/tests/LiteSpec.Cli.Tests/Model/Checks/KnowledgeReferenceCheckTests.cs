using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the knowledge project-reference checks: a module project references no knowledge set, a knowledge set references only its model and the Kernel, and a knowledge model references no module.</summary>
public sealed class KnowledgeReferenceCheckTests
{
    /// <summary>A module project referencing a knowledge set reports knowledge-reference-invalid.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void ModuleReferencingKnowledgeSetReports()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, ModuleProject, ProjectWithReferences(SetProjectFile));

        var finding = Assert.Single(Run(model));
        Assert.Equal("knowledge-reference-invalid", finding.Code);
        Assert.Contains("references the knowledge set 'Library.Travel.Germany'", finding.Message);
        Assert.Contains("through its model", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Library.Lending.csproj"), finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>A module project referencing a knowledge model reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void ModuleReferencingKnowledgeModelPasses()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, ModuleProject, ProjectWithReferences(ModelProjectFile));

        Assert.Empty(Run(model));
    }

    /// <summary>A knowledge set referencing the Platform reports knowledge-reference-invalid.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void KnowledgeSetReferencingPlatformReports()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, SetProject, ProjectWithReferences(ModelProjectFile, KernelProjectFile, PlatformProjectFile));

        var finding = Assert.Single(Run(model));
        Assert.Equal("knowledge-reference-invalid", finding.Code);
        Assert.Contains("Knowledge set 'Library.Travel.Germany' references 'Library.Platform'", finding.Message);
        Assert.Contains("only its model and the Kernel", finding.Message);
    }

    /// <summary>A knowledge set referencing its model and the Kernel reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void KnowledgeSetReferencingModelAndKernelPasses()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, SetProject, ProjectWithReferences(ModelProjectFile, KernelProjectFile));

        Assert.Empty(Run(model));
    }

    /// <summary>A knowledge model referencing a module project reports knowledge-reference-invalid.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void KnowledgeModelReferencingModuleReports()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, ModelProject, ProjectWithReferences(KernelProjectFile, ModuleProjectFile));

        var finding = Assert.Single(Run(model));
        Assert.Equal("knowledge-reference-invalid", finding.Code);
        Assert.Contains("Knowledge model 'Library.Travel' references the module project 'Library.Lending'", finding.Message);
        Assert.Contains("references no module", finding.Message);
    }

    /// <summary>A knowledge model referencing the Kernel reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void KnowledgeModelReferencingKernelPasses()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = KnowledgeModel(temp.Path);
        WriteProject(temp.Path, ModelProject, ProjectWithReferences(KernelProjectFile));

        Assert.Empty(Run(model));
    }

    /// <summary>A workspace with no Knowledge folder reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</proves>
    [Fact]
    public void WorkspaceWithoutKnowledgePasses()
    {
        using var temp = TestCli.Temp("knowledge");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            "{\"stacks\": [\"dotnet\"], \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Modules/*\", \"kernel\": \"apps/api/src/Library.Kernel\"}}}");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending"));
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the dependency check and keeps its knowledge findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The knowledge reference findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        DependencyChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "knowledge-reference-invalid").ToList();
    }

    /// <summary>A model with the Kernel, the Platform, one module, one knowledge model and one set, each holding an empty project file.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the configuration.</returns>
    static ProjectModel KnowledgeModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"stacks\": [\"dotnet\"], \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Modules/*\", \"kernel\": \"apps/api/src/Library.Kernel\"}}}");

        foreach (var project in new[] { KernelProject, PlatformProject, ModuleProject, ModelProject, SetProject })
        {
            WriteProject(root, project, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        }

        return new ProjectModel { Config = ConsumerConfig.Load(root) };
    }

    /// <summary>Writes the project file of one fixture project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The project folder segments under apps/api/src.</param>
    /// <param name="text">The project file text.</param>
    static void WriteProject(string root, string[] project, string text)
    {
        var parts = new List<string> { root, "apps", "api", "src" };
        parts.AddRange(project);
        var folder = Path.Combine([.. parts]);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, project[^1] + ".csproj"), text);
    }

    /// <summary>A project file referencing the named project files, resolved from the referencing folder.</summary>
    /// <param name="references">The referenced project files, as paths under apps/api/src.</param>
    /// <returns>The project file text.</returns>
    static string ProjectWithReferences(params string[][] references) =>
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n"
        + string.Concat(references.Select(reference =>
            $"    <ProjectReference Include=\"{string.Join("/", reference[..^1])}/{reference[^1]}.csproj\" />\n"))
        + "  </ItemGroup>\n</Project>";

    static readonly string[] KernelProject = ["Library.Kernel"];
    static readonly string[] PlatformProject = ["Library.Platform"];
    static readonly string[] ModuleProject = ["Modules", "Library.Lending"];
    static readonly string[] ModelProject = ["Knowledge", "Library.Travel"];
    static readonly string[] SetProject = ["Knowledge", "Library.Travel.Germany"];

    static readonly string[] KernelProjectFile = ["..", "..", "Library.Kernel", "Library.Kernel"];
    static readonly string[] PlatformProjectFile = ["..", "..", "Library.Platform", "Library.Platform"];
    static readonly string[] ModuleProjectFile = ["..", "..", "Modules", "Library.Lending", "Library.Lending"];
    static readonly string[] ModelProjectFile = ["..", "..", "Knowledge", "Library.Travel", "Library.Travel"];
    static readonly string[] SetProjectFile = ["..", "..", "Knowledge", "Library.Travel.Germany", "Library.Travel.Germany"];
}
