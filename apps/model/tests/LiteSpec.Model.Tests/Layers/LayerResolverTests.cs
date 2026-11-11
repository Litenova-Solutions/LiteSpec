using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Tests for reading a file's layer.</summary>
public sealed class LayerResolverTests
{
    /// <summary>The assembly name suffix names the layer.</summary>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [InlineData("Shop.Domain", ArchitectureLayer.Domain)]
    [InlineData("Shop.Application.Abstractions", ArchitectureLayer.ApplicationAbstractions)]
    [InlineData("Shop.Application", ArchitectureLayer.Application)]
    [InlineData("Shop.Infrastructure", ArchitectureLayer.Infrastructure)]
    [InlineData("Shop.WebApi", ArchitectureLayer.WebApi)]
    [InlineData("Domain", ArchitectureLayer.Domain)]
    [InlineData("Shop.DomainTests", ArchitectureLayer.None)]
    [InlineData("Shop.Domain.Tests", ArchitectureLayer.Test)]
    [InlineData("Shop.Integration.Tests", ArchitectureLayer.Test)]
    [InlineData("Shop.Architecture.Test", ArchitectureLayer.Test)]
    [InlineData("Shop.Worker", ArchitectureLayer.Worker)]
    public void ReadsTheAssemblyName(string assemblyName, ArchitectureLayer expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.FromAssemblyName(assemblyName));

    /// <summary>A declared layer name parses with or without its dot.</summary>
    /// <param name="value">The declared layer name to parse.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [InlineData("Domain", ArchitectureLayer.Domain)]
    [InlineData("Application.Abstractions", ArchitectureLayer.ApplicationAbstractions)]
    [InlineData("applicationabstractions", ArchitectureLayer.ApplicationAbstractions)]
    [InlineData("WebApi", ArchitectureLayer.WebApi)]
    [InlineData("Test", ArchitectureLayer.Test)]
    [InlineData("Tests", ArchitectureLayer.Test)]
    [InlineData("None", ArchitectureLayer.None)]
    [InlineData("Worker", ArchitectureLayer.Worker)]
    [InlineData("Staging", ArchitectureLayer.None)]
    public void ParsesADeclaredLayer(string value, ArchitectureLayer expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.Parse(value));

    /// <summary>A declared layout name parses, and anything else reads as the module layout.</summary>
    /// <param name="value">The declared layout name to parse.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [InlineData(null, BackendLayout.Modules)]
    [InlineData("", BackendLayout.Modules)]
    [InlineData("modules", BackendLayout.Modules)]
    [InlineData("Modules", BackendLayout.Modules)]
    [InlineData("layer-projects", BackendLayout.LayerProjects)]
    [InlineData("Layer-Projects", BackendLayout.LayerProjects)]
    [InlineData("layerprojects", BackendLayout.LayerProjects)]
    [InlineData("slices", BackendLayout.Modules)]
    public void ParsesADeclaredLayout(string? value, BackendLayout expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.ParseLayout(value));

    /// <summary>The nearest layer folder names the layer.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick/Organizations/Teams/Domain/Team.cs", ArchitectureLayer.Domain)]
    [InlineData("Claimwick/Organizations/Teams/Application/CreateTeam.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Infrastructure/TeamRepository.cs", ArchitectureLayer.Infrastructure)]
    [InlineData("Claimwick/Organizations/Teams/WebApi/TeamLinks.cs", ArchitectureLayer.WebApi)]
    [InlineData("Claimwick/Organizations/Contracts/TeamId.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Domain/Events/TeamRenamedEvent.cs", ArchitectureLayer.Domain)]
    [InlineData("Claimwick/Application/Pipeline/Transaction.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick\\Organizations\\Teams\\Domain\\Team.cs", ArchitectureLayer.Domain)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", null)]
    [InlineData("Source.cs", null)]
    [InlineData(null, null)]
    public void ReadsTheNearestLayerFolder(string? filePath, ArchitectureLayer? expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.FolderLayer(filePath));

    /// <summary>A role suffix names the layer of a use case, reaction or job file.</summary>
    /// <param name="fileName">The file name under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("CreateTeamEndpoint.cs", ArchitectureLayer.WebApi)]
    [InlineData("CreateTeamRequestModel.cs", ArchitectureLayer.WebApi)]
    [InlineData("CreateTeamResponseModel.cs", ArchitectureLayer.WebApi)]
    [InlineData("CreateTeamCommand.cs", ArchitectureLayer.Application)]
    [InlineData("ListTeamsQuery.cs", ArchitectureLayer.Application)]
    [InlineData("CreateTeamResult.cs", ArchitectureLayer.Application)]
    [InlineData("CreateTeamHandler.cs", ArchitectureLayer.Application)]
    [InlineData("CreateTeamValidator.cs", ArchitectureLayer.Application)]
    [InlineData("NotifyOnTeamRenamedHandler.cs", ArchitectureLayer.Application)]
    [InlineData("SendDigestJob.cs", ArchitectureLayer.Application)]
    [InlineData("SendDigestWorker.cs", ArchitectureLayer.Application)]
    [InlineData("Team.cs", null)]
    [InlineData("TeamLinks.cs", null)]
    [InlineData("CreateTeamEndpoints.cs", null)]
    [InlineData(null, null)]
    public void ReadsTheRoleSuffix(string? fileName, ArchitectureLayer? expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.RoleLayer(fileName));

    /// <summary>A module file reads its layer from its folder or its role suffix.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick/Organizations/Teams/Domain/Team.cs", ArchitectureLayer.Domain)]
    [InlineData("Claimwick/Organizations/Contracts/TeamId.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamEndpoint.cs", ArchitectureLayer.WebApi)]
    [InlineData("Claimwick/Organizations/Jobs/SendDigest/SendDigestJob.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/TeamNameRules.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick.Modules/Organizations/UseCases/OnTeamCreated/WelcomeNoticeText.cs", ArchitectureLayer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Team.cs", ArchitectureLayer.None)]
    public void ResolvesAModuleFile(string filePath, ArchitectureLayer expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.FileLayer(filePath, "Claimwick", BackendLayout.Modules, null));

    /// <summary>A host file reads its layer from its assembly.</summary>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick.WebApi", ArchitectureLayer.WebApi)]
    [InlineData("Claimwick.Worker", ArchitectureLayer.Worker)]
    public void ResolvesAHostFileFromItsAssembly(string assemblyName, ArchitectureLayer expected) =>
        Assert.Equal(expected, ArchitectureLayerResolver.FileLayer("Claimwick.WebApi/Program.cs", assemblyName, BackendLayout.Modules, null));

    /// <summary>A test file stays in the test layer whatever folders it mirrors.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void ResolvesATestFileFromItsAssembly()
    {
        var layer = ArchitectureLayerResolver.FileLayer(
            "Claimwick.Tests/Organizations/Teams/Domain/TeamTests.cs", "Claimwick.Tests", BackendLayout.Modules, null);

        Assert.Equal(ArchitectureLayer.Test, layer);
    }

    /// <summary>Under layer-projects the assembly names the layer of every file.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void ResolvesEveryFileFromItsAssemblyUnderLayerProjects()
    {
        var layer = ArchitectureLayerResolver.FileLayer(
            "Orders/Order.cs", "Shop.Domain", BackendLayout.LayerProjects, null);

        Assert.Equal(ArchitectureLayer.Domain, layer);
    }

    /// <summary>A declared layer wins over folders and assemblies in both layouts.</summary>
    /// <param name="layout">The backend layout under test.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData(BackendLayout.Modules)]
    [InlineData(BackendLayout.LayerProjects)]
    public void DeclaredLayerWins(BackendLayout layout)
    {
        var layer = ArchitectureLayerResolver.FileLayer(
            "Claimwick/Organizations/Teams/Domain/Team.cs", "Claimwick", layout, "Application");

        Assert.Equal(ArchitectureLayer.Application, layer);
    }
}
