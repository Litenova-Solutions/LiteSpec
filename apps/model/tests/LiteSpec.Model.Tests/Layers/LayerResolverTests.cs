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
    public void Reads_The_Assembly_Name(string assemblyName, Layer expected) =>
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
    public void Parses_A_Declared_Layer(string value, Layer expected) =>
        Assert.Equal(expected, LayerResolver.Parse(value));

    /// <summary>A declared layout name parses, and anything else reads as the module layout.</summary>
    /// <param name="value">The declared layout name to parse.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [InlineData(null, Layout.Modules)]
    [InlineData("", Layout.Modules)]
    [InlineData("modules", Layout.Modules)]
    [InlineData("Modules", Layout.Modules)]
    [InlineData("layer-projects", Layout.LayerProjects)]
    [InlineData("Layer-Projects", Layout.LayerProjects)]
    [InlineData("layerprojects", Layout.LayerProjects)]
    [InlineData("slices", Layout.Modules)]
    public void Parses_A_Declared_Layout(string? value, Layout expected) =>
        Assert.Equal(expected, LayerResolver.ParseLayout(value));

    /// <summary>The nearest layer folder names the layer.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("NightHall/Organizations/Teams/Domain/Team.cs", Layer.Domain)]
    [InlineData("NightHall/Organizations/Teams/Application/CreateTeam.cs", Layer.Application)]
    [InlineData("NightHall/Organizations/Teams/Infrastructure/TeamRepository.cs", Layer.Infrastructure)]
    [InlineData("NightHall/Organizations/Teams/WebApi/TeamLinks.cs", Layer.WebApi)]
    [InlineData("NightHall/Organizations/Contracts/TeamId.cs", Layer.Application)]
    [InlineData("NightHall/Organizations/Teams/Domain/Events/TeamRenamedEvent.cs", Layer.Domain)]
    [InlineData("NightHall/Application/Pipeline/Transaction.cs", Layer.Application)]
    [InlineData("NightHall\\Organizations\\Teams\\Domain\\Team.cs", Layer.Domain)]
    [InlineData("NightHall/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", null)]
    [InlineData("Source.cs", null)]
    [InlineData(null, null)]
    public void Reads_The_Nearest_Layer_Folder(string? filePath, Layer? expected) =>
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
    public void Reads_The_Role_Suffix(string? fileName, Layer? expected) =>
        Assert.Equal(expected, LayerResolver.RoleLayer(fileName));

    /// <summary>A module file reads its layer from its folder or its role suffix.</summary>
    /// <param name="filePath">The file path under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("NightHall/Organizations/Teams/Domain/Team.cs", Layer.Domain)]
    [InlineData("NightHall/Organizations/Contracts/TeamId.cs", Layer.Application)]
    [InlineData("NightHall/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", Layer.Application)]
    [InlineData("NightHall/Organizations/Teams/CreateTeam/CreateTeamEndpoint.cs", Layer.WebApi)]
    [InlineData("NightHall/Organizations/Jobs/SendDigest/SendDigestJob.cs", Layer.Application)]
    [InlineData("NightHall/Organizations/Teams/Team.cs", Layer.None)]
    public void Resolves_A_Module_File(string filePath, Layer expected) =>
        Assert.Equal(expected, LayerResolver.FileLayer(filePath, "NightHall", Layout.Modules, null));

    /// <summary>A host file reads its layer from its assembly.</summary>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData("NightHall.WebApi", Layer.WebApi)]
    [InlineData("NightHall.Worker", Layer.Worker)]
    public void Resolves_A_Host_File_From_Its_Assembly(string assemblyName, Layer expected) =>
        Assert.Equal(expected, LayerResolver.FileLayer("NightHall.WebApi/Program.cs", assemblyName, Layout.Modules, null));

    /// <summary>A test file stays in the test layer whatever folders it mirrors.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void Resolves_A_Test_File_From_Its_Assembly()
    {
        var layer = LayerResolver.FileLayer(
            "NightHall.Tests/Organizations/Teams/Domain/TeamTests.cs", "NightHall.Tests", Layout.Modules, null);

        Assert.Equal(Layer.Test, layer);
    }

    /// <summary>Under layer-projects the assembly names the layer of every file.</summary>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Fact]
    public void Resolves_Every_File_From_Its_Assembly_Under_Layer_Projects()
    {
        var layer = LayerResolver.FileLayer(
            "Orders/Order.cs", "Shop.Domain", Layout.LayerProjects, null);

        Assert.Equal(Layer.Domain, layer);
    }

    /// <summary>A declared layer wins over folders and assemblies in both layouts.</summary>
    /// <param name="layout">The backend layout under test.</param>
    /// <proves>litespec/backend/architecture#point-dependencies-inward</proves>
    [Theory]
    [InlineData(Layout.Modules)]
    [InlineData(Layout.LayerProjects)]
    public void Declared_Layer_Wins(Layout layout)
    {
        var layer = LayerResolver.FileLayer(
            "NightHall/Organizations/Teams/Domain/Team.cs", "NightHall", layout, "Application");

        Assert.Equal(Layer.Application, layer);
    }
}
