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
                ("NightHall/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2019:RenameTeamCommand|} command)
                        {
                        }
                    }
                    """),
                ("NightHall/Organizations/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall.Domain;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2019 accepts a use case folder naming its own types and aggregate types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case folder naming its own types and aggregate types")]
    public Task AcceptsAUseCaseNamingItsOwnSlice() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
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
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall.Domain;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2019 accepts a use case naming product plumbing whose file carries a role suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case naming product plumbing whose file carries a role suffix")]
    public Task AcceptsProductPlumbingWithARoleSuffix() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("NightHall/Organizations/Teams/CreateTeam/CreateTeamCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class CreateTeamCommand : IActingAccountCommand
                    {
                    }
                    """),
                ("NightHall/Application/Authorization/IActingAccountCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal interface IActingAccountCommand
                    {
                    }
                    """),
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall.Domain;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2019 stays quiet in a project laid out without product layer folders, such as a tool.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet in a project laid out without product layer folders")]
    public Task StaysQuietWithoutProductLayerFolders() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("NightHall.Cli/Exercises/SignInCommand.cs", """
                    namespace NightHall.Cli.Exercises;

                    internal sealed class SignInCommand
                    {
                        internal void Run(NightHall.Cli.Stack.StartStackCommand start)
                        {
                        }
                    }
                    """),
                ("NightHall.Cli/Stack/StartStackCommand.cs", """
                    namespace NightHall.Cli.Stack;

                    internal sealed class StartStackCommand
                    {
                    }
                    """),
            ],
            assemblyName: "NightHall.Cli");

    /// <summary>LS2019 stays quiet when an aggregate file names a use case type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet when an aggregate file names a use case type")]
    public Task StaysQuietOutsideUseCaseFolders() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("NightHall/Organizations/Teams/Application/TeamQueries.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class TeamQueries
                    {
                        internal RenameTeamCommand? Current() => null;
                    }
                    """),
                ("NightHall/Organizations/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
                ("NightHall/Domain/Money.cs", """
                    namespace NightHall.Domain;

                    internal readonly record struct Money(decimal Amount);
                    """),
            ],
            assemblyName: "NightHall");

    /// <summary>LS2019 stays quiet for test files mirroring use case folders.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet for test files mirroring use case folders")]
    public Task StaysQuietInTestFiles() =>
        Verify.AnalyzerAsync<SliceIsolationAnalyzer>(
            [
                ("NightHall.Tests/Organizations/Teams/CreateTeam/CreateTeamHandler.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle(RenameTeamCommand command)
                        {
                        }
                    }
                    """),
                ("NightHall.Tests/Organizations/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace NightHall.Organizations.Teams;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
            ],
            assemblyName: "NightHall.Tests");

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
                        internal void Handle({|LS2019:CancelDraftCommand|} command)
                        {
                        }
                    }
                    """),
                ("Shop.Application/Orders/CancelDraft/CancelDraftCommand.cs", """
                    namespace Shop.Orders;

                    internal sealed class CancelDraftCommand
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
