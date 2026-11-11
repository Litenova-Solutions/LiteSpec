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
    [InlineData("Shop.Domain", Layer.Domain)]
    [InlineData("Shop.Application.Abstractions", Layer.ApplicationAbstractions)]
    [InlineData("Shop.Application", Layer.Application)]
    [InlineData("Shop.Infrastructure", Layer.Infrastructure)]
    [InlineData("Shop.WebApi", Layer.WebApi)]
    [InlineData("Domain", Layer.Domain)]
    [InlineData("Shop.DomainTests", Layer.None)]
    [InlineData("Shop.Domain.Tests", Layer.Test)]
    [InlineData("Shop.Integration.Tests", Layer.Test)]
    [InlineData("Shop.Architecture.Test", Layer.Test)]
    [InlineData("Shop.Worker", Layer.Worker)]
    public void ReadsTheAssemblyName(string assemblyName, Layer expected) =>
        Assert.Equal(expected, LayerResolver.FromAssemblyName(assemblyName));

    /// <summary>A declared layer name parses with or without its dot.</summary>
    /// <param name="value">The declared layer name to parse.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [InlineData("Domain", Layer.Domain)]
    [InlineData("Application.Abstractions", Layer.ApplicationAbstractions)]
    [InlineData("applicationabstractions", Layer.ApplicationAbstractions)]
    [InlineData("WebApi", Layer.WebApi)]
    [InlineData("Test", Layer.Test)]
    [InlineData("Tests", Layer.Test)]
    [InlineData("None", Layer.None)]
    [InlineData("Worker", Layer.Worker)]
    [InlineData("Staging", Layer.None)]
    public void ParsesADeclaredLayer(string value, Layer expected) =>
        Assert.Equal(expected, LayerResolver.Parse(value));

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
        Assert.Equal(expected, LayerResolver.ParseLayout(value));

    /// <summary>The nearest layer folder names the layer.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick/Organizations/Teams/Domain/Team.cs", Layer.Domain)]
    [InlineData("Claimwick/Organizations/Teams/Application/CreateTeam.cs", Layer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Infrastructure/TeamRepository.cs", Layer.Infrastructure)]
    [InlineData("Claimwick/Organizations/Teams/WebApi/TeamLinks.cs", Layer.WebApi)]
    [InlineData("Claimwick/Organizations/Contracts/TeamId.cs", Layer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Domain/Events/TeamRenamedEvent.cs", Layer.Domain)]
    [InlineData("Claimwick/Application/Pipeline/Transaction.cs", Layer.Application)]
    [InlineData("Claimwick\\Organizations\\Teams\\Domain\\Team.cs", Layer.Domain)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", null)]
    [InlineData("Source.cs", null)]
    [InlineData(null, null)]
    public void ReadsTheNearestLayerFolder(string? filePath, Layer? expected) =>
        Assert.Equal(expected, LayerResolver.FolderLayer(filePath));

    /// <summary>A role suffix names the layer of a use case, reaction or job file.</summary>
    /// <param name="fileName">The file name under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("CreateTeamEndpoint.cs", Layer.WebApi)]
    [InlineData("CreateTeamRequestModel.cs", Layer.WebApi)]
    [InlineData("CreateTeamResponseModel.cs", Layer.WebApi)]
    [InlineData("CreateTeamCommand.cs", Layer.Application)]
    [InlineData("ListTeamsQuery.cs", Layer.Application)]
    [InlineData("CreateTeamResult.cs", Layer.Application)]
    [InlineData("CreateTeamHandler.cs", Layer.Application)]
    [InlineData("CreateTeamValidator.cs", Layer.Application)]
    [InlineData("NotifyOnTeamRenamedHandler.cs", Layer.Application)]
    [InlineData("SendDigestJob.cs", Layer.Application)]
    [InlineData("SendDigestWorker.cs", Layer.Application)]
    [InlineData("Team.cs", null)]
    [InlineData("TeamLinks.cs", null)]
    [InlineData("CreateTeamEndpoints.cs", null)]
    [InlineData(null, null)]
    public void ReadsTheRoleSuffix(string? fileName, Layer? expected) =>
        Assert.Equal(expected, LayerResolver.RoleLayer(fileName));

    /// <summary>A module file reads its layer from its folder or its role suffix.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick/Organizations/Teams/Domain/Team.cs", Layer.Domain)]
    [InlineData("Claimwick/Organizations/Contracts/TeamId.cs", Layer.Application)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", Layer.Application)]
    [InlineData("Claimwick/Organizations/Teams/CreateTeam/CreateTeamEndpoint.cs", Layer.WebApi)]
    [InlineData("Claimwick/Organizations/Jobs/SendDigest/SendDigestJob.cs", Layer.Application)]
    [InlineData("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/TeamNameRules.cs", Layer.Application)]
    [InlineData("Claimwick.Modules/Organizations/UseCases/OnTeamCreated/WelcomeNoticeText.cs", Layer.Application)]
    [InlineData("Claimwick/Organizations/Teams/Team.cs", Layer.None)]
    public void ResolvesAModuleFile(string filePath, Layer expected) =>
        Assert.Equal(expected, LayerResolver.FileLayer(filePath, "Claimwick", BackendLayout.Modules, null));

    /// <summary>A host file reads its layer from its assembly.</summary>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("Claimwick.WebApi", Layer.WebApi)]
    [InlineData("Claimwick.Worker", Layer.Worker)]
    public void ResolvesAHostFileFromItsAssembly(string assemblyName, Layer expected) =>
        Assert.Equal(expected, LayerResolver.FileLayer("Claimwick.WebApi/Program.cs", assemblyName, BackendLayout.Modules, null));

    /// <summary>A test file stays in the test layer whatever folders it mirrors.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void ResolvesATestFileFromItsAssembly()
    {
        var layer = LayerResolver.FileLayer(
            "Claimwick.Tests/Organizations/Teams/Domain/TeamTests.cs", "Claimwick.Tests", BackendLayout.Modules, null);

        Assert.Equal(Layer.Test, layer);
    }

    /// <summary>Under layer-projects the assembly names the layer of every file.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void ResolvesEveryFileFromItsAssemblyUnderLayerProjects()
    {
        var layer = LayerResolver.FileLayer(
            "Orders/Order.cs", "Shop.Domain", BackendLayout.LayerProjects, null);

        Assert.Equal(Layer.Domain, layer);
    }

    /// <summary>A declared layer wins over folders and assemblies in both layouts.</summary>
    /// <param name="layout">The backend layout under test.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData(BackendLayout.Modules)]
    [InlineData(BackendLayout.LayerProjects)]
    public void DeclaredLayerWins(BackendLayout layout)
    {
        var layer = LayerResolver.FileLayer(
            "Claimwick/Organizations/Teams/Domain/Team.cs", "Claimwick", layout, "Application");

        Assert.Equal(Layer.Application, layer);
    }
}
