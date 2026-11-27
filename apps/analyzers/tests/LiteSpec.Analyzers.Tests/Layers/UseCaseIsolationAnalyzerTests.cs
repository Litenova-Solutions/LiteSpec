using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2019, use case isolation between use case folders.</summary>
public sealed class UseCaseIsolationAnalyzerTests
{
    /// <summary>LS2019 reports a use case folder naming another use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a use case folder naming another use case folder")]
    public Task ReportsAUseCaseNamingAnotherUseCase() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Organizations.Application.Teams.RenameTeam;

                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2019:RenameTeamPolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamPolicy.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamPolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports a reaction filed in another use case folder that sends a use case command and reads its result.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a reaction filed in another use case folder that sends a use case command")]
    public Task ReportsAReactionFiledOutsideTheUseCaseItRuns() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/RenameTeamOnTeamCreatedHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    using Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamOnTeamCreatedHandler
                    {
                        internal void Handle()
                        {
                            var command = new {|LS2019:RenameTeamCommand|}("Finance");
                            {|LS2019:RenameTeamCommandResult|} result = new {|LS2019:RenameTeamCommandResult|}();
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamCommand
                    {
                        internal RenameTeamCommand(string name)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommandResult.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamCommandResult
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a reaction in the folder of the use case it runs, which sends that use case's command and reads its result.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a reaction in the folder of the use case it runs")]
    public Task AcceptsAReactionInTheFolderOfTheUseCaseItRuns() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamOnTeamCreatedHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamOnTeamCreatedHandler
                    {
                        internal void Handle()
                        {
                            var command = new RenameTeamCommand("Finance");
                            RenameTeamCommandResult result = new RenameTeamCommandResult();
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamCommand
                    {
                        internal RenameTeamCommand(string name)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommandResult.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamCommandResult
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a use case folder naming its own types and aggregate types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case folder naming its own types and aggregate types")]
    public Task AcceptsAUseCaseNamingItsOwnUseCase() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Organizations.Domain.Teams;

                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal Team Handle(CreateTeamCommand command) => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    internal sealed class CreateTeamCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Domain/Teams/Team.cs", """
                    namespace Claimwick.Organizations.Domain.Teams;

                    internal sealed class Team
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a use case naming layer folder types whose files carry role suffixes.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case naming layer folder types whose files carry role suffixes")]
    public Task AcceptsLayerFolderTypesWithRoleSuffixes() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    internal sealed class CreateTeamCommand : IActingAccountCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/IActingAccountCommand.cs", """
                    namespace Claimwick.Organizations.Application;

                    internal interface IActingAccountCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 stays quiet in a project laid out without product layer folders, such as a tool.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet in a project laid out without product layer folders")]
    public Task StaysQuietWithoutProductLayerFolders() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
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

    /// <summary>LS2019 stays quiet when a shared Application file names a use case type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet when a shared Application file names a use case type")]
    public Task StaysQuietOutsideUseCaseFolders() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/TeamQueries.cs", """
                    using Claimwick.Organizations.Application.Teams.RenameTeam;

                    namespace Claimwick.Organizations.Application;

                    internal sealed class TeamQueries
                    {
                        internal RenameTeamCommand? Current() => null;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 stays quiet for test files mirroring use case folders.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet for test files mirroring use case folders")]
    public Task StaysQuietInTestFiles() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending.Tests/Application/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Spending.Tests.Application.RenameTeam;

                    namespace Claimwick.Spending.Tests.Application.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle(RenameTeamCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending.Tests/Application/RenameTeam/RenameTeamCommand.cs", """
                    namespace Claimwick.Spending.Tests.Application.RenameTeam;

                    internal sealed class RenameTeamCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending.Tests");

    /// <summary>LS2019 isolates two use cases in one module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 isolates two use cases in one module")]
    public Task ReportsTwoUseCasesInOneModule() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Application/Invoices/CreateInvoice/CreateInvoiceHandler.cs", """
                    using Claimwick.Billing.Application.Invoices.CancelInvoice;

                    namespace Claimwick.Billing.Application.Invoices.CreateInvoice;

                    internal sealed class CreateInvoiceHandler
                    {
                        internal void Handle({|LS2019:CancelInvoicePolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Billing/Application/Invoices/CancelInvoice/CancelInvoicePolicy.cs", """
                    namespace Claimwick.Billing.Application.Invoices.CancelInvoice;

                    internal sealed class CancelInvoicePolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports a reaction naming another use case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a reaction naming another use case")]
    public Task ReportsAReactionNamingAnotherUseCase() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/RemoveTeamMember/SendNoticeOnTeamMemberRemovedHandler.cs", """
                    using Claimwick.Organizations.Application.Teams.CreateTeam;

                    namespace Claimwick.Organizations.Application.Teams.RemoveTeamMember;

                    internal sealed class SendNoticeOnTeamMemberRemovedHandler
                    {
                        internal void Handle({|LS2019:CreateTeamPolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamPolicy.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    internal sealed class CreateTeamPolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a use case naming a port.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a use case naming a port")]
    public Task AcceptsAUseCaseNamingAPort() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseHandler.cs", """
                    using Claimwick.Spending.Ports.ReceiptPdf;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseHandler
                    {
                        internal void Handle(IReceiptPdf pdf)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Ports/ReceiptPdf/IReceiptPdf.cs", """
                    namespace Claimwick.Spending.Ports.ReceiptPdf;

                    internal interface IReceiptPdf
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2019 stays quiet when an adapter names a use case type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 stays quiet when an adapter names a use case type")]
    public Task StaysQuietWhenAnAdapterNamesAUseCaseType() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/Infrastructure/ReceiptPdf/ReceiptPdfRenderer.cs", """
                    using Claimwick.Spending.Application.Expenses.SubmitExpense;

                    namespace Claimwick.Spending.Infrastructure.ReceiptPdf;

                    internal sealed class ReceiptPdfRenderer
                    {
                        internal void Render(SubmitExpenseCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2019 reports a WebApi use case folder naming another use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a WebApi use case folder naming another use case folder")]
    public Task ReportsAWebApiUseCaseNamingAnotherUseCase() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/SubmitExpenseEndpoint.cs", """
                    using Claimwick.Spending.Application.Expenses.ApproveExpense;

                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseEndpoint
                    {
                        internal void Describe({|LS2019:ApproveExpenseCommand|} command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/ApproveExpense/ApproveExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

                    internal sealed class ApproveExpenseCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2019 accepts a WebApi use case folder naming its own Application use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a WebApi use case folder naming its own Application use case folder")]
    public Task AcceptsAWebApiUseCaseNamingItsOwnApplicationUseCase() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/SubmitExpenseEndpoint.cs", """
                    using Claimwick.Spending.Application.Expenses.SubmitExpense;

                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseEndpoint
                    {
                        internal void Describe(SubmitExpenseCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2019 reports a use case naming another use case of its aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a use case naming another use case of its aggregate")]
    public Task ReportsAUseCaseNamingAnotherUseCaseOfItsAggregate() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseHandler.cs", """
                    using Claimwick.Spending.Application.Expenses.ApproveExpense;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseHandler
                    {
                        internal void Handle({|LS2019:ApproveExpensePolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/ApproveExpense/ApproveExpensePolicy.cs", """
                    namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

                    internal sealed class ApproveExpensePolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2019 reports a module-level use case naming another module-level use case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a module-level use case naming another module-level use case")]
    public Task ReportsAModuleLevelUseCaseNamingAnother() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Spending/Application/ChangeReceiptThreshold/ChangeReceiptThresholdHandler.cs", """
                    using Claimwick.Spending.Application.CloseExpensePeriod;

                    namespace Claimwick.Spending.Application.ChangeReceiptThreshold;

                    internal sealed class ChangeReceiptThresholdHandler
                    {
                        internal void Handle({|LS2019:CloseExpensePeriodPolicy|} policy)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/CloseExpensePeriod/CloseExpensePeriodPolicy.cs", """
                    namespace Claimwick.Spending.Application.CloseExpensePeriod;

                    internal sealed class CloseExpensePeriodPolicy
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    [Fact(DisplayName = "LS2019 accepts a use case reading the module options class in Application/Settings")]
    public Task AcceptsAUseCaseReadingTheModuleOptions() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/RenameTeam/RenameTeamCommandHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RenameTeam;

                    using Claimwick.Organizations.Application.Settings;

                    internal sealed class RenameTeamCommandHandler
                    {
                        internal int Limit(OrganizationsOptions options) => options.MaxTeamNameLength;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Settings/OrganizationsOptions.cs", """
                    namespace Claimwick.Organizations.Application.Settings;

                    internal sealed class OrganizationsOptions
                    {
                        internal int MaxTeamNameLength { get; init; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 accepts a reaction that sends the command of the use case it runs on refusal from the catch of the refused run.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 accepts a reaction sending its refusal use case command from a catch")]
    public Task AcceptsARefusalCommandSentFromACatch() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamOnTeamRequestedHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    using Claimwick.Organizations.Application.Teams.RecordTeamRefused;
                    using LiteBus.Events.Abstractions;

                    internal sealed class CreateTeamOnTeamRequestedHandler : IEventHandler<string>
                    {
                        internal void Handle()
                        {
                            try
                            {
                            }
                            catch (System.InvalidOperationException)
                            {
                                var command = new RecordTeamRefusedCommand();
                            }
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommandResult.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommandResult
                    {
                    }
                    """),
                ("Claimwick.Modules/EventHandler.cs", """
                    namespace LiteBus.Events.Abstractions;

                    public interface IEventHandler<TEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports a reaction that names another use case command outside a catch.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports a reaction naming another use case command outside a catch")]
    public Task ReportsAnotherUseCaseCommandOutsideACatch() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamOnTeamRequestedHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    using Claimwick.Organizations.Application.Teams.RecordTeamRefused;
                    using LiteBus.Events.Abstractions;

                    internal sealed class CreateTeamOnTeamRequestedHandler : IEventHandler<string>
                    {
                        internal void Handle()
                        {
                            var command = new {|LS2019:RecordTeamRefusedCommand|}();
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommandResult.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommandResult
                    {
                    }
                    """),
                ("Claimwick.Modules/EventHandler.cs", """
                    namespace LiteBus.Events.Abstractions;

                    public interface IEventHandler<TEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2019 reports another use case result even inside a catch: only the command message is the refusal run.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2019 reports another use case result inside a catch")]
    public Task ReportsAnotherUseCaseResultInsideACatch() =>
        Verify.AnalyzerAsync<UseCaseIsolationAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Application/Teams/CreateTeam/CreateTeamOnTeamRequestedHandler.cs", """
                    namespace Claimwick.Organizations.Application.Teams.CreateTeam;

                    using Claimwick.Organizations.Application.Teams.RecordTeamRefused;
                    using LiteBus.Events.Abstractions;

                    internal sealed class CreateTeamOnTeamRequestedHandler : IEventHandler<string>
                    {
                        internal void Handle()
                        {
                            try
                            {
                            }
                            catch (System.InvalidOperationException)
                            {
                                {|LS2019:RecordTeamRefusedCommandResult|} result = new {|LS2019:RecordTeamRefusedCommandResult|}();
                            }
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommand.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/Teams/RecordTeamRefused/RecordTeamRefusedCommandResult.cs", """
                    namespace Claimwick.Organizations.Application.Teams.RecordTeamRefused;

                    internal sealed class RecordTeamRefusedCommandResult
                    {
                    }
                    """),
                ("Claimwick.Modules/EventHandler.cs", """
                    namespace LiteBus.Events.Abstractions;

                    public interface IEventHandler<TEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");
}
