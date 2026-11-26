using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the Knowledge closed set: one model project per area with one set project per variant of a model the folder holds, and that marked knowledge files carry no module placement.</summary>
public sealed class KnowledgeFolderCheckTests
{
    /// <summary>A model with two sets reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void ModelWithSetsPasses()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel"));
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel.Germany"));
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel.Netherlands"));

        Assert.Empty(Run(model));
    }

    /// <summary>A model with no set, for an area that varies by nothing, reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void ModelWithoutSetsPasses()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel"));

        Assert.Empty(Run(model));
    }

    /// <summary>A folder under Knowledge that is neither a model nor a set reports drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void UnknownFolderReportsDrift()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel"));
        var stray = KnowledgeFolder(temp.Path, "TravelRates");
        Directory.CreateDirectory(stray);

        var finding = Assert.Single(Run(model));
        Assert.Contains("names no knowledge model or set of a known model", finding.Message);
        Assert.Equal(stray, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>A set whose model the folder does not hold reports drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void SetOfUnknownModelReportsDrift()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = FolderModel(temp.Path);
        var orphan = KnowledgeFolder(temp.Path, "Library.Travel.Germany");
        Directory.CreateDirectory(orphan);

        var finding = Assert.Single(Run(model));
        Assert.Contains("names no knowledge model or set of a known model", finding.Message);
        Assert.Equal(orphan, finding.Path);
        Assert.Null(finding.Subject);
    }

    /// <summary>A file directly in the Knowledge root reports drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void FileDirectlyInKnowledgeReportsDrift()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(KnowledgeFolder(temp.Path, "Library.Travel"));
        var file = Path.Combine(KnowledgeRoot(temp.Path), "notes.md");
        File.WriteAllText(file, "Travel rates.\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits directly in the Knowledge root", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Null(finding.Subject);
    }

    /// <summary>A marked file under Knowledge carries no module placement, so its namespace reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void MarkedKnowledgeFileSkipsModulePlacement()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = PlacementModel(temp.Path);
        var file = KnowledgeSource(temp.Path, "Library.Travel", "ReadLoanHandler.cs");
        File.WriteAllText(file, "namespace Library.Travel.PerDiem;\n\ninternal sealed class ReadLoanHandler;\n");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Handler, "ReadLoanHandler", null, file, 1,
            [new CodeMark("use-case/lending.read-loan", MarkStyle.Xml, MarkVerb.Implements, file, 1)], InTests: false));

        Assert.Empty(RunPlacement(model));
    }

    /// <summary>A marked file under Modules with a namespace naming another area reports drift, so the skip above is proved sensitive.</summary>
    /// <proves>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</proves>
    [Fact]
    public void MarkedModuleFileWithForeignNamespaceReportsDrift()
    {
        using var temp = TestCli.Temp("knowledge");
        var model = PlacementModel(temp.Path);
        var folder = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "ReadLoan");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "ReadLoanHandler.cs");
        File.WriteAllText(file, "namespace Library.Travel.PerDiem;\n\ninternal sealed class ReadLoanHandler;\n");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Handler, "ReadLoanHandler", null, file, 1,
            [new CodeMark("use-case/lending.read-loan", MarkStyle.Xml, MarkVerb.Implements, file, 1)], InTests: false));

        var finding = Assert.Single(RunPlacement(model));
        Assert.Contains("declares namespace 'Library.Travel.PerDiem'", finding.Message);
        Assert.Equal("use-case/lending.read-loan", finding.Subject);
    }

    /// <summary>Runs the closed folder check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        ClosedFolderChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "code-placement-drift").ToList();
    }

    /// <summary>Runs the backend placement check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> RunPlacement(ProjectModel model)
    {
        var findings = new List<Finding>();
        BackendPlacementChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "code-placement-drift").ToList();
    }

    /// <summary>A model with the Kernel root and an empty Knowledge root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the configuration.</returns>
    static ProjectModel FolderModel(string root)
    {
        WriteConfig(root);
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Kernel"));
        Directory.CreateDirectory(KnowledgeRoot(root));

        return new ProjectModel { Config = ConsumerConfig.Load(root) };
    }

    /// <summary>A model with the module and Knowledge roots and the declared lending loan aggregate with its read-loan use case.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel PlacementModel(string root)
    {
        WriteConfig(root);
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Kernel"));
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending"));
        Directory.CreateDirectory(KnowledgeRoot(root));

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };
        model.Records.Add(Rec("aggregate", "aggregate/lending.loan", "lending", "aggregates/loans", root));
        model.Records.Add(Rec("use-case", "use-case/lending.read-loan", "lending", "aggregates/loans/use-cases/read-loan", root));

        return model;
    }

    /// <summary>Writes the fixture project file with the modules and Kernel roots.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteConfig(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"stacks\": [\"dotnet\"], \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Modules/*\", \"kernel\": \"apps/api/src/Library.Kernel\"}}}");

    /// <summary>The absolute Knowledge root of the fixture.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The full path to the Knowledge folder.</returns>
    static string KnowledgeRoot(string root) =>
        Path.Combine(root, "apps", "api", "src", "Knowledge");

    /// <summary>One folder under the fixture Knowledge root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="name">The project folder name.</param>
    /// <returns>The full path to the folder.</returns>
    static string KnowledgeFolder(string root, string name) =>
        Path.Combine(KnowledgeRoot(root), name);

    /// <summary>One source file path under a fixture knowledge project, with its folder created.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The knowledge project folder name.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string KnowledgeSource(string root, string project, string file)
    {
        var folder = KnowledgeFolder(root, project);
        Directory.CreateDirectory(folder);

        return Path.Combine(folder, file);
    }

    /// <summary>One record with its module and folder.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="module">The module name.</param>
    /// <param name="under">The folder segments under the module folder.</param>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The record.</returns>
    static LiteSpec.Cli.Model.Records.Record Rec(string type, string id, string module, string under, string root)
    {
        using var document = JsonDocument.Parse("{}");

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = type,
            Id = id,
            Folder = $".litespec/behavior/modules/{module}/{under}",
            Module = module,
            Title = id,
            File = Path.Combine(root, ".litespec", "lending.yaml"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
