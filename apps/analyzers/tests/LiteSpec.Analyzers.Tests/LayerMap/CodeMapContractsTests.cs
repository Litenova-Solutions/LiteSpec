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
        const string path = "Modules/Claimwick.Spending.Contracts/Expenses/OnExpenseReimbursed/ExpenseReimbursedModuleEvent.cs";
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

    /// <summary>A file in a module Ports folder maps to its module with the Ports layer and no use case.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void PortsFileMapsToThePortsLayer()
    {
        const string path = "Modules/Claimwick.Spending/Ports/ReceiptPdf/IReceiptPdf.cs";
        var map = MapFor("Claimwick.Spending", path);

        Assert.Equal(ArchitectureLayer.Ports, map.LayerOf(path));
        Assert.Equal("Modules/Claimwick.Spending", map.ModuleOf(path));
        Assert.Null(map.UseCaseOf(path));
    }

    /// <summary>A file in an Infrastructure adapter folder maps to Infrastructure with no use case.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void AdapterFileHasNoUseCase()
    {
        const string path = "Modules/Claimwick.Spending/Infrastructure/ReceiptPdf/ReceiptPdfRenderer.cs";
        var map = MapFor("Claimwick.Spending", path);

        Assert.Equal(ArchitectureLayer.Infrastructure, map.LayerOf(path));
        Assert.True(CodeMap.IsAdapter(path));
        Assert.Null(map.UseCaseOf(path));
    }

    /// <summary>A file in Persistence is no adapter and sits in no use case.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void PersistenceFileIsNoAdapter()
    {
        const string path = "Modules/Claimwick.Spending/Infrastructure/Persistence/ExpenseRepository.cs";
        const string nested = "Modules/Claimwick.Spending/Infrastructure/Persistence/Expenses/ExpenseRepository.cs";

        Assert.False(CodeMap.IsAdapter(path));
        Assert.Null(MapFor("Claimwick.Spending", path).UseCaseOf(path));
        Assert.False(CodeMap.IsAdapter(nested));
        Assert.Null(MapFor("Claimwick.Spending", nested).UseCaseOf(nested));
    }

    /// <summary>A file in Messaging is no adapter and sits in no use case.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void MessagingFileIsNoAdapter()
    {
        const string path = "Modules/Claimwick.Spending/Infrastructure/Messaging/DomainToModuleEventMapping.cs";
        var map = MapFor("Claimwick.Spending", path);

        Assert.Equal(ArchitectureLayer.Infrastructure, map.LayerOf(path));
        Assert.False(CodeMap.IsAdapter(path));
        Assert.Null(map.UseCaseOf(path));
    }

    /// <summary>A file in the Infrastructure Pipeline is no adapter and sits in no use case, at the root or in a protocol folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void PipelineFileSitsInNoUseCase()
    {
        const string path = "Modules/Claimwick.Spending/Infrastructure/Pipeline/RequestLoggingMiddleware.cs";
        const string nested = "Modules/Claimwick.Spending/Infrastructure/Pipeline/OAuth/OAuthHandler.cs";

        Assert.Equal(ArchitectureLayer.Infrastructure, MapFor("Claimwick.Spending", path).LayerOf(path));
        Assert.False(CodeMap.IsAdapter(path));
        Assert.Null(MapFor("Claimwick.Spending", path).UseCaseOf(path));
        Assert.False(CodeMap.IsAdapter(nested));
        Assert.Null(MapFor("Claimwick.Spending", nested).UseCaseOf(nested));
    }

    /// <summary>A type in a use case folder below an aggregate folder is a public query, public command or result.</summary>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void ContractsUseCaseFileIsAPublicQueryOrCommand()
    {
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Modules/Claimwick.Books.Contracts/Books/ReadBook/ReadBookQuery.cs", "ReadBookQuery"));
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookResult.cs", "ReadBookResult"));
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommand.cs", "LendBookCommand"));
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommandResult.cs", "LendBookCommandResult"));
    }

    /// <summary>A type directly in an aggregate folder is an identifier, never a public query or public command.</summary>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void ContractsAggregateFileIsNoPublicQueryOrCommand()
    {
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Modules/Claimwick.Books.Contracts/Books/BookId.cs", "BookId"));
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/BookId.cs", "BookId"));
    }

    /// <summary>A type in an On{Fact} folder is a fact, never a public query or public command.</summary>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void ContractsFactFolderFileIsNoPublicQueryOrCommand()
    {
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Modules/Claimwick.Books.Contracts/Books/OnBookLent/BookLentModuleEvent.cs", "BookLentModuleEvent"));
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/OnBookLent/BookLentModuleEvent.cs", "BookLentModuleEvent"));
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/OnBookLent/BookLentLine.cs", "BookLentLine"));
        Assert.False(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/Books/OnBookLent/BookQuery.cs", "BookQuery"));
    }

    /// <summary>A query, command or result directly in a Contracts top folder is a module-level public query or public command.</summary>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void ContractsModuleLevelQueryOrCommandFileIsAPublicQueryOrCommand()
    {
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Modules/Claimwick.Books.Contracts/SearchCatalog/SearchCatalogQuery.cs", "SearchCatalogQuery"));
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/SearchCatalog/SearchCatalogResult.cs", "SearchCatalogResult"));
        Assert.True(Contracts.IsPublicQueryOrCommand(
            "Claimwick.Modules/Books/Contracts/RegisterMember/RegisterMemberCommand.cs", "RegisterMemberCommand"));
    }

    /// <summary>A file in the Platform project reads as one by assembly or by path segment.</summary>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact]
    public void PlatformFileIsDetected()
    {
        Assert.True(CodeMap.IsPlatformFile("Claimwick.Platform/Access/ActorScope.cs", "Claimwick.Modules"));
        Assert.True(CodeMap.IsPlatformFile("Access/ActorScope.cs", "Claimwick.Platform"));
        Assert.False(CodeMap.IsPlatformFile("Claimwick.Modules/Books/Application/ReadBook/ReadBookQueryHandler.cs", "Claimwick.Modules"));
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
