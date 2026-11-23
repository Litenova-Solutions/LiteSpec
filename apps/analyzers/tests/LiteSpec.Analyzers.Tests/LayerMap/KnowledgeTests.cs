using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.LayerMap;

/// <summary>Proves a knowledge model file reads as Domain and a knowledge set file reads as no layer.</summary>
public sealed class KnowledgeTests
{
    /// <summary>A file in a knowledge model project reads as Domain and sits in no module.</summary>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact]
    public void ModelFileReadsAsDomain()
    {
        const string path = "src/Knowledge/Claimwick.Travel/Rates/TravelRates.cs";
        var map = MapFor("Claimwick.Travel", path);

        Assert.True(Knowledge.IsModelFile(path));
        Assert.False(Knowledge.IsSetFile(path));
        Assert.Equal(ArchitectureLayer.Domain, map.LayerOf(path));
        Assert.Null(map.ModuleOf(path));
    }

    /// <summary>A file in a knowledge set project reads as no layer and sits in no module.</summary>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact]
    public void SetFileReadsAsNoLayer()
    {
        const string path = "src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs";
        var map = MapFor("Claimwick.Travel.France", path);

        Assert.True(Knowledge.IsSetFile(path));
        Assert.False(Knowledge.IsModelFile(path));
        Assert.Equal(ArchitectureLayer.None, map.LayerOf(path));
        Assert.Null(map.ModuleOf(path));
    }

    /// <summary>A module file is no knowledge file and keeps its folder layer.</summary>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact]
    public void ModuleFileIsNoKnowledgeFile()
    {
        const string path = "Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs";
        var map = MapFor("Claimwick.Spending", path);

        Assert.False(Knowledge.IsModelFile(path));
        Assert.False(Knowledge.IsSetFile(path));
        Assert.Null(Knowledge.ProjectOf(path));
        Assert.Equal(ArchitectureLayer.Application, map.LayerOf(path));
    }

    /// <summary>A model name holds a product and an area, a set name adds a variant, and a Contracts name is no set.</summary>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact]
    public void ProjectNamesDecideModelAndSet()
    {
        Assert.True(Knowledge.IsModelProject("Claimwick.Travel"));
        Assert.False(Knowledge.IsSetProject("Claimwick.Travel"));
        Assert.True(Knowledge.IsSetProject("Claimwick.Travel.France"));
        Assert.False(Knowledge.IsModelProject("Claimwick.Travel.France"));
        Assert.False(Knowledge.IsSetProject("Claimwick.Spending.Contracts"));
        Assert.Equal("Claimwick.Travel", Knowledge.ProjectOf("src/Knowledge/Claimwick.Travel/Rates/TravelRates.cs"));
    }

    /// <summary>A compilation is a model or a set when its assembly names its Knowledge project.</summary>
    /// <proves>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</proves>
    [Fact]
    public void CompilationNamesDecideModelAndSet()
    {
        var model = CompilationFor("Claimwick.Travel", "src/Knowledge/Claimwick.Travel/Rates/TravelRates.cs");
        var set = CompilationFor("Claimwick.Travel.France", "src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs");
        var module = CompilationFor("Claimwick.Spending", "Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.True(Knowledge.IsModelCompilation(model));
        Assert.False(Knowledge.IsSetCompilation(model));
        Assert.True(Knowledge.IsSetCompilation(set));
        Assert.False(Knowledge.IsModelCompilation(set));
        Assert.False(Knowledge.IsModelCompilation(module));
        Assert.False(Knowledge.IsSetCompilation(module));
    }

    /// <summary>Builds the code map for one compilation holding one file.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="path">The source file path.</param>
    /// <returns>The code map.</returns>
    static CodeMap MapFor(string assemblyName, string path) =>
        CodeMap.For(CompilationFor(assemblyName, path), new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));

    /// <summary>Builds the compilation holding one file.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="path">The source file path.</param>
    /// <returns>The compilation.</returns>
    static Compilation CompilationFor(string assemblyName, string path)
    {
        var tree = CSharpSyntaxTree.ParseText(SourceText.From("namespace Fixture;\n", Encoding.UTF8), path: path);
        return CSharpCompilation.Create(assemblyName, [tree]);
    }
}
