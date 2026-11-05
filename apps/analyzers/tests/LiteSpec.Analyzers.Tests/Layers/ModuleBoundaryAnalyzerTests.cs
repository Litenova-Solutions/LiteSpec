using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2020, cross-module access through Contracts folders.</summary>
public sealed class ModuleBoundaryAnalyzerTests
{
    /// <summary>LS2020 reports a module naming another module outside its Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 reports a module naming another module outside its Contracts")]
    public Task ReportsAModuleNamingAnotherModuleOutsideContracts() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    using Claimwick.Organizations;
                    using Claimwick.Organizations.Teams;

                    namespace Claimwick.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle(TeamId id, {|LS2020:Team|} team)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Organizations/Teams/Domain/Team.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("Claimwick.Modules/Billing/Contracts/Identifiers/InvoiceId.cs", """
                    namespace Claimwick.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2020 reports a module that publishes no Contracts folder yet.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 reports a module that publishes no Contracts folder yet")]
    public Task ReportsAModuleWithoutContracts() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    using Claimwick.Organizations.Teams;

                    namespace Claimwick.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2020:Team|} team)
                        {
                        }
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

    /// <summary>LS2020 stays quiet in a project laid out without product layer folders, such as a tool.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 stays quiet in a project laid out without product layer folders")]
    public Task StaysQuietWithoutProductLayerFolders() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Cli/Exercises/SignInCommand.cs", """
                    using Claimwick.Cli.Stack;

                    namespace Claimwick.Cli.Exercises;

                    internal sealed class SignInCommand
                    {
                        internal void Run(IdentityIssuer issuer)
                        {
                        }
                    }
                    """),
                ("Claimwick.Cli/Stack/IdentityIssuer.cs", """
                    namespace Claimwick.Cli.Stack;

                    internal sealed class IdentityIssuer
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Cli");

    /// <summary>LS2020 accepts a module naming another module through its Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 accepts a module naming another module through its Contracts")]
    public Task AcceptsAModuleNamingContracts() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    using Claimwick.Organizations;

                    namespace Claimwick.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal TeamId Handle(Money budget) => default;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Billing/Contracts/Identifiers/InvoiceId.cs", """
                    namespace Claimwick.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Billing/Invoices/Domain/Money.cs", """
                    namespace Claimwick.Billing;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2020 accepts a module naming its own types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 accepts a module naming its own types")]
    public Task AcceptsAModuleNamingItsOwnTypes() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
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
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2020 still reports a reference resolved through a global using.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 still reports a reference resolved through a global using")]
    public Task ReportsThroughAGlobalUsing() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    namespace Claimwick.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2020:Team|} team)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/Globals.cs", """
                    global using Claimwick.Organizations.Teams;
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
                ("Claimwick.Modules/Billing/Contracts/Identifiers/InvoiceId.cs", """
                    namespace Claimwick.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2020 stays quiet for shared files outside every module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 stays quiet for shared files outside every module")]
    public Task StaysQuietOutsideModules() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Composition/Application/ModuleComposition.cs", """
                    using Claimwick.Organizations.Teams;

                    namespace Claimwick;

                    internal sealed class ModuleComposition
                    {
                        internal Team? Current() => null;
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

    /// <summary>LS2020 accepts another module naming a Contracts kind subfolder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 accepts another module naming a Contracts kind subfolder")]
    public Task AcceptsAContractsKindSubfolder() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    using Claimwick.Organizations;

                    namespace Claimwick.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal TeamId Handle(IOrganizerReader reader) => default;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/TeamId.cs", """
                    namespace Claimwick.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Ports/IOrganizerReader.cs", """
                    namespace Claimwick.Organizations;

                    internal interface IOrganizerReader
                    {
                    }
                    """),
                ("Claimwick.Modules/Billing/Contracts/Identifiers/InvoiceId.cs", """
                    namespace Claimwick.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2020 stays quiet under layer-projects, which has no Contracts folders.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 stays quiet under layer-projects, which has no Contracts folders")]
    public Task StaysQuietUnderLayerProjects() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("Source.cs", """
                    namespace Shop.Orders;

                    internal sealed class CreateOrderHandler
                    {
                        internal void Handle(OrderStore store)
                        {
                        }
                    }
                    """),
                ("Stubs.cs", """
                    namespace Shop.Orders;

                    internal sealed class OrderStore
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application",
            layout: "layer-projects");
}
