using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.LayerMap;

/// <summary>Proves a file in a sibling Contracts project maps to its owner module with the Application layer, while another project stays its own module.</summary>
public sealed class CodeMapContractsTests
{
    /// <summary>A file in a sibling Contracts project maps to its owner module with the Application layer.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact]
    public void SiblingContractsFileMapsToItsOwnerModule()
    {
        const string path = "Modules/Claimwick.Spending.Contracts/ModuleEvents/ExpenseReimbursed.cs";
        var map = MapFor("Claimwick.Spending.Contracts", path);

        Assert.True(CodeMap.IsContracts(path));
        Assert.Equal(ArchitectureLayer.Application, map.LayerOf(path));
        Assert.Equal("Modules/Claimwick.Spending", map.ModuleOf(path));
    }

    /// <summary>A project that is not named Contracts stays its own module, and a module named Contracts stays a module.</summary>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact]
    public void OtherProjectStaysItsOwnModule()
    {
        const string other = "Modules/Claimwick.Other/Application/DoThing/DoThingCommand.cs";
        const string named = "Modules/Claimwick.Contracts/Application/DoThing/DoThingCommand.cs";

        Assert.False(CodeMap.IsContracts(other));
        Assert.Equal("Modules/Claimwick.Other", MapFor("Claimwick.Other", other).ModuleOf(other));
        Assert.False(CodeMap.IsContracts(named));
        Assert.Equal("Modules/Claimwick.Contracts", MapFor("Claimwick.Contracts", named).ModuleOf(named));
    }

    /// <summary>Builds the code map for one compilation holding one file.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="path">The source file path.</param>
    /// <returns>The code map.</returns>
    static CodeMap MapFor(string assemblyName, string path)
    {
        var tree = CSharpSyntaxTree.ParseText(SourceText.From("namespace Fixture;\n", Encoding.UTF8), path: path);
        var compilation = CSharpCompilation.Create(assemblyName, [tree]);

        return CodeMap.For(compilation, new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));
    }
}
