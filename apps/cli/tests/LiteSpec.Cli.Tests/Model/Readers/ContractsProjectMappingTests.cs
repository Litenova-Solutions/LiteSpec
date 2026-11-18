using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves a file in a sibling Contracts project maps to its owner module with the Application layer, while another project stays its own module.</summary>
public sealed class ContractsProjectMappingTests
{
    /// <summary>A file in a sibling Contracts project maps to its owner module with the Application layer.</summary>
    /// <proves>litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project</proves>
    [Fact]
    public void SiblingContractsFileMapsToItsOwnerModule()
    {
        using var temp = TestCli.Temp("readers");
        WriteProject(temp.Path);
        var file = WriteSource(temp.Path, "Library.Lending.Contracts", "ModuleEvents", "LoanBorrowed.cs");
        var config = ConsumerConfig.Load(temp.Path);
        var contractsRoot = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending.Contracts");

        Assert.True(SourceRoots.IsContractsProject(contractsRoot));
        Assert.Equal("Lending", SourceRoots.ModuleNameOf(contractsRoot));
        Assert.Equal("Application", SourceRoots.LayerOf(config, file));
        var segments = SourceRoots.ModulePath(config, file);
        Assert.NotNull(segments);
        Assert.Equal("Lending", segments[0]);
    }

    /// <summary>A project that is not named Contracts stays its own module, and a module named Contracts stays a module.</summary>
    /// <proves>litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project</proves>
    [Fact]
    public void OtherProjectStaysItsOwnModule()
    {
        using var temp = TestCli.Temp("readers");
        WriteProject(temp.Path);
        var other = WriteSource(temp.Path, "Library.Other", "Application", "DoThing", "DoThingCommand.cs");
        var named = WriteSource(temp.Path, "Library.Contracts", "Application", "DoThing", "DoThingCommand.cs");
        var config = ConsumerConfig.Load(temp.Path);
        var otherRoot = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Other");
        var namedRoot = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Contracts");

        Assert.False(SourceRoots.IsContractsProject(otherRoot));
        Assert.Equal("Other", SourceRoots.ModuleNameOf(otherRoot));
        var otherSegments = SourceRoots.ModulePath(config, other);
        Assert.NotNull(otherSegments);
        Assert.Equal("Other", otherSegments[0]);
        Assert.False(SourceRoots.IsContractsProject(namedRoot));
        Assert.Equal("Contracts", SourceRoots.ModuleNameOf(namedRoot));
        var namedSegments = SourceRoots.ModulePath(config, named);
        Assert.NotNull(namedSegments);
        Assert.Equal("Contracts", namedSegments[0]);
    }

    /// <summary>Writes the fixture project with one project per module under the modules folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Modules/*\"}}}");

    /// <summary>Writes one source file under a module project, creating its folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The project folder name under the modules folder.</param>
    /// <param name="segments">The path segments under the project folder, with the file name last.</param>
    /// <returns>The full path of the written file.</returns>
    static string WriteSource(string root, string project, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Modules", project };
        parts.AddRange(segments);
        var file = Path.Combine([.. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "namespace Library.Fixture;\n");

        return file;
    }
}
