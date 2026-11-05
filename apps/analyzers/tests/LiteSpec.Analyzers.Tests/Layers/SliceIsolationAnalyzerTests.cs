using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2019, slice isolation between use case folders.</summary>
public sealed class SliceIsolationAnalyzerTests
{
    /// <summary>LS2019 reports a use case folder naming another use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a use case folder naming another use case folder")]
    public Task ReportsAUseCaseNamingAnotherUseCase() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2019:RenameTeamPolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/RenameTeam/RenameTeamPolicy.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class RenameTeamPolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a reaction that sends a use case command and reads its result.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a reaction that sends a use case command and reads its result")]
    public Task AcceptsARunUseCaseMessageAndResult() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/UseCases/OnTeamCreated/RenameTeamOnTeamCreatedHandler.cs", """
                    namespace Claimwick.Organizations.UseCases.OnTeamCreated;

                    using Claimwick.Organizations.Teams.UseCases.RenameTeam;

                    internal sealed class RenameTeamOnTeamCreatedHandler
                    {
                        internal bool Handle()
                        {
                            var command = new RenameTeamCommand { Name = "Finance" };
                            RenameTeamCommandResult result = new RenameTeamCommandResult.Renamed();
                            return command.Name.Length > 0 && result is RenameTeamCommandResult.Renamed;
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Teams.UseCases.RenameTeam;

                    internal sealed class RenameTeamCommand
                    {
                        internal string Name { get; init; } = "";
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/RenameTeam/RenameTeamCommandResult.cs", """
                    namespace Claimwick.Organizations.Teams.UseCases.RenameTeam;

                    internal abstract record RenameTeamCommandResult
                    {
                        internal sealed record Renamed : RenameTeamCommandResult;
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a use case folder naming its own types and aggregate types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case folder naming its own types and aggregate types")]
    public Task AcceptsAUseCaseNamingItsOwnSlice() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
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

    /// <summary>LS2019 accepts a use case naming layer folder types whose files carry role suffixes.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case naming layer folder types whose files carry role suffixes")]
    public Task AcceptsLayerFolderTypesWithRoleSuffixes() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamCommand : IActingAccountCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/Application/IActingAccountCommand.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal interface IActingAccountCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 stays quiet in a project laid out without product layer folders, such as a tool.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet in a project laid out without product layer folders")]
    public Task StaysQuietWithoutProductLayerFolders() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Cli/Exercises/SignInCommand.cs", """
                    namespace Claimwick.Cli.Exercises;

                    internal sealed class SignInCommand
                    {
                        internal void Run(Claimwick.Cli.Stack.StartStackCommand start)
                        {
                        }
                    }
                    """),
                ("Claimwick.Cli/Stack/StartStackCommand.cs", """
                    namespace Claimwick.Cli.Stack;

                    internal sealed class StartStackCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Cli");

    /// <summary>LS2019 stays quiet when an aggregate file names a use case type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet when an aggregate file names a use case type")]
    public Task StaysQuietOutsideUseCaseFolders() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Teams/Application/TeamQueries.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class TeamQueries
                    {
                        internal RenameTeamCommand? Current() => null;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 stays quiet for test files mirroring use case folders.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet for test files mirroring use case folders")]
    public Task StaysQuietInTestFiles() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Tests/Organizations/Teams/UseCases/CreateTeam/CreateTeamHandler.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle(RenameTeamCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Tests/Organizations/Teams/UseCases/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Tests");

    /// <summary>LS2019 isolates two slices under one aggregate UseCases folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 isolates two slices under one aggregate UseCases folder")]
    public Task ReportsTwoSlicesUnderOneAggregateUseCases() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Invoices/UseCases/CreateInvoice/CreateInvoiceHandler.cs", """
                    namespace Claimwick.Billing.Invoices;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2019:CancelInvoicePolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Billing/Invoices/UseCases/CancelInvoice/CancelInvoicePolicy.cs", """
                    namespace Claimwick.Billing.Invoices;

                    internal sealed class CancelInvoicePolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports a reaction folder naming another slice.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a reaction folder naming another slice")]
    public Task ReportsAReactionFolderNamingAnotherSlice() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/UseCases/OnTeamMemberRemoved/SendNoticeOnTeamMemberRemovedHandler.cs", """
                    using Claimwick.Organizations.Teams;

                    namespace Claimwick.Organizations;

                    internal sealed class SendNoticeOnTeamMemberRemovedHandler
                    {
                        internal void Handle({|LS2019:CreateTeamPolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Teams/UseCases/CreateTeam/CreateTeamPolicy.cs", """
                    namespace Claimwick.Organizations.Teams;

                    internal sealed class CreateTeamPolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports a use case folder naming another under layer-projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a use case folder naming another under layer-projects")]
    public Task ReportsAUseCaseNamingAnotherUnderLayerProjects() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Shop.Application/Orders/CreateDraft/CreateDraftHandler.cs", """
                    namespace Shop.Orders;

                    internal sealed class CreateDraftHandler
                    {
                        internal void Handle({|LS2019:CancelDraftValidator|} validator)
                        {
                        }
                    }
                    """),
                ("Shop.Application/Orders/CancelDraft/CancelDraftValidator.cs", """
                    namespace Shop.Orders;

                    internal sealed class CancelDraftValidator
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application",
            layout: "layer-projects");

    /// <summary>LS2019 accepts a use case folder naming its own slice under layer-projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case folder naming its own slice under layer-projects")]
    public Task AcceptsAUseCaseNamingItsOwnSliceUnderLayerProjects() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("Shop.Application/Orders/CreateDraft/CreateDraftHandler.cs", """
                    namespace Shop.Orders;

                    internal sealed class CreateDraftHandler
                    {
                        internal void Handle(CreateDraftCommand command)
                        {
                        }
                    }
                    """),
                ("Shop.Application/Orders/CreateDraft/CreateDraftCommand.cs", """
                    namespace Shop.Orders;

                    internal sealed class CreateDraftCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application",
            layout: "layer-projects");
}
