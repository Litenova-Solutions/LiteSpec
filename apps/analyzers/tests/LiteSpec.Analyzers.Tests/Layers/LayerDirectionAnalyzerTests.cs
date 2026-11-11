using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2018, layer direction in Application and WebApi files.</summary>
public sealed class LayerDirectionAnalyzerTests
{
    /// <summary>LS2018 reports an Application file naming an Infrastructure type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports an Application file naming an Infrastructure type")]
    public Task ReportsAnApplicationFileNamingAnInfrastructureType() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:TeamStore|} store)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Infrastructure/TeamStore.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class TeamStore
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports an Application file naming a WebApi type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports an Application file naming a WebApi type")]
    public Task ReportsAnApplicationFileNamingAWebApiType() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:CreateTeamEndpoint|} endpoint)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamEndpoint
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports an Application file naming ASP.NET Core and HttpContext.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports an Application file naming ASP.NET Core and HttpContext")]
    public Task ReportsAnApplicationFileNamingAspNetCore() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    using Microsoft.AspNetCore.Http;
                    using Microsoft.AspNetCore.Mvc;

                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:ControllerBase|} controller, {|LS2018:HttpContext|} context)
                        {
                        }
                    }
                    """),
                ("Stubs.cs", """
                    namespace Microsoft.AspNetCore.Mvc;

                    public abstract class ControllerBase
                    {
                    }
                    """),
                ("HttpStubs.cs", """
                    namespace Microsoft.AspNetCore.Http;

                    public sealed class HttpContext
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports a WebApi file naming a Domain aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a WebApi file naming a Domain aggregate")]
    public Task ReportsAWebApiFileNamingADomainAggregate() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamEndpoint
                    {
                        internal {|LS2018:Team|} Load() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Domain/Team.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports a WebApi file naming a repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a WebApi file naming a repository")]
    public Task ReportsAWebApiFileNamingARepository() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamEndpoint
                    {
                        internal {|LS2018:ITeamRepository|} Store() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Domain/ITeamRepository.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal interface ITeamRepository
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 accepts a WebApi file naming an identifier and a shared-kernel value.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 accepts a WebApi file naming an identifier and a shared-kernel value")]
    public Task AcceptsAWebApiFileNamingAnIdentifierAndASharedValue() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamEndpoint
                    {
                        internal TeamId Load(Claimwick.Money budget) => default;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Composition/Domain/Money.cs", """
                    namespace Claimwick;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 accepts an Application file naming Domain and Application types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 accepts an Application file naming Domain and Application types")]
    public Task AcceptsAnApplicationFileNamingInnerTypes() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal Team Handle(CreateTeamCommand command) => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Domain/Team.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 stays quiet in a Domain file of the same compilation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 stays quiet in a Domain file of the same compilation")]
    public Task StaysQuietOutsideApplicationAndWebApi() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/Domain/Team.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class Team
                    {
                        internal TeamStore Store() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Infrastructure/TeamStore.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class TeamStore
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Source.cs", """
                    using Microsoft.AspNetCore.Mvc;

                    namespace Shop.Core;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:ControllerBase|} controller)
                        {
                        }
                    }
                    """),
                ("Stubs.cs", """
                    namespace Microsoft.AspNetCore.Mvc;

                    public abstract class ControllerBase
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Core",
            layer: "Application");

    /// <summary>LS2018 reports ASP.NET Core in an Application project under layer-projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports ASP.NET Core in an Application project under layer-projects")]
    public Task ReportsAspNetCoreUnderLayerProjects() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Source.cs", """
                    using Microsoft.AspNetCore.Mvc;

                    namespace Shop.Orders;

                    internal sealed class CreateOrderHandler
                    {
                        internal void Handle({|LS2018:ControllerBase|} controller)
                        {
                        }
                    }
                    """),
                ("Stubs.cs", """
                    namespace Microsoft.AspNetCore.Mvc;

                    public abstract class ControllerBase
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application",
            layout: "layer-projects");

    /// <summary>LS2018 reports a repository in a WebApi project under layer-projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a repository in a WebApi project under layer-projects")]
    public Task ReportsARepositoryUnderLayerProjects() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Source.cs", """
                    namespace Shop.Orders;

                    internal sealed class CreateOrderEndpoint
                    {
                        internal {|LS2018:IOrderRepository|} Store() => null!;
                    }
                    """),
                ("Stubs.cs", """
                    namespace Shop.Orders;

                    internal interface IOrderRepository
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.WebApi",
            layout: "layer-projects");
}
