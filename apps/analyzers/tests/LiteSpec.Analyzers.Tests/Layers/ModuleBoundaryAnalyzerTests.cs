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
                ("NightHall/Billing/Invoices/CreateInvoice/CreateInvoiceHandler.cs", """
                    using NightHall.Organizations;
                    using NightHall.Organizations.Teams;

                    namespace NightHall.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle(TeamId id, {|LS2020:Team|} team)
                        {
                        }
                    }
                    """),
                ("NightHall/Organizations/Contracts/TeamId.cs", """
                    namespace NightHall.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("NightHall/Organizations/Teams/Domain/Team.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("NightHall/Billing/Contracts/InvoiceId.cs", """
                    namespace NightHall.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2020 reports a module that publishes no Contracts folder yet.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 reports a module that publishes no Contracts folder yet")]
    public Task ReportsAModuleWithoutContracts() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall/Billing/Invoices/CreateInvoice/CreateInvoiceHandler.cs", """
                    using NightHall.Organizations.Teams;

                    namespace NightHall.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2020:Team|} team)
                        {
                        }
                    }
                    """),
                ("NightHall/Organizations/Teams/Domain/Team.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall.Domain;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2020 stays quiet in a project laid out without product layer folders, such as a tool.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 stays quiet in a project laid out without product layer folders")]
    public Task StaysQuietWithoutProductLayerFolders() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall.Cli/Exercises/SignInCommand.cs", """
                    using NightHall.Cli.Stack;

                    namespace NightHall.Cli.Exercises;

                    internal sealed class SignInCommand
                    {
                        internal void Run(IdentityIssuer issuer)
                        {
                        }
                    }
                    """),
                ("NightHall.Cli/Stack/IdentityIssuer.cs", """
                    namespace NightHall.Cli.Stack;

                    internal sealed class IdentityIssuer
                    {
                    }
                    """),
            ],
            assemblyName: "NightHall.Cli");

    /// <summary>LS2020 accepts a module naming another module through its Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 accepts a module naming another module through its Contracts")]
    public Task AcceptsAModuleNamingContracts() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall/Billing/Invoices/CreateInvoice/CreateInvoiceHandler.cs", """
                    using NightHall.Organizations;

                    namespace NightHall.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal TeamId Handle(Money budget) => default;
                    }
                    """),
                ("NightHall/Organizations/Contracts/TeamId.cs", """
                    namespace NightHall.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("NightHall/Billing/Contracts/InvoiceId.cs", """
                    namespace NightHall.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2020 accepts a module naming its own types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 accepts a module naming its own types")]
    public Task AcceptsAModuleNamingItsOwnTypes() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal Team Handle(CreateTeamCommand command) => null!;
                    }
                    """),
                ("NightHall/Organizations/Teams/CreateTeam/CreateTeamCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class CreateTeamCommand
                    {
                    }
                    """),
                ("NightHall/Organizations/Teams/Domain/Team.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("NightHall/Organizations/Contracts/TeamId.cs", """
                    namespace NightHall.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2020 still reports a reference resolved through a global using.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 still reports a reference resolved through a global using")]
    public Task ReportsThroughAGlobalUsing() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall/Billing/Invoices/CreateInvoice/CreateInvoiceHandler.cs", """
                    namespace NightHall.Billing;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2020:Team|} team)
                        {
                        }
                    }
                    """),
                ("NightHall/Billing/Invoices/CreateInvoice/Globals.cs", """
                    global using NightHall.Organizations.Teams;
                    """),
                ("NightHall/Organizations/Teams/Domain/Team.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("NightHall/Organizations/Contracts/TeamId.cs", """
                    namespace NightHall.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
                ("NightHall/Billing/Contracts/InvoiceId.cs", """
                    namespace NightHall.Billing;

                    internal readonly record struct InvoiceId(System.Guid Value);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2020 stays quiet for shared files outside every module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</proves>
    [Fact(DisplayName = "LS2020 stays quiet for shared files outside every module")]
    public Task StaysQuietOutsideModules() =>
        Verify.AnalyzerAsync<ModuleBoundaryAnalyzer>(
            [
                ("NightHall/Application/Pipeline/Transaction.cs", """
                    using NightHall.Organizations.Teams;

                    namespace NightHall;

                    internal sealed class Transaction
                    {
                        internal Team? Current() => null;
                    }
                    """),
                ("NightHall/Organizations/Teams/Domain/Team.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
                ("NightHall/Organizations/Contracts/TeamId.cs", """
                    namespace NightHall.Organizations;

                    internal readonly record struct TeamId(System.Guid Value);
                    """),
            ],
            assemblyName: "NightHall");

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
