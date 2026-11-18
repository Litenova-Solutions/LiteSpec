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
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Organizations.Infrastructure.Persistence;

                    namespace Claimwick.Organizations.Application.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:TeamStore|} store)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/Infrastructure/Persistence/TeamStore.cs", """
                    namespace Claimwick.Organizations.Infrastructure.Persistence;

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
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Organizations.WebApi.CreateTeam;

                    namespace Claimwick.Organizations.Application.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal void Handle({|LS2018:CreateTeamEndpoint|} endpoint)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Organizations/WebApi/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.WebApi.CreateTeam;

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
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamHandler.cs", """
                    using Microsoft.AspNetCore.Http;
                    using Microsoft.AspNetCore.Mvc;

                    namespace Claimwick.Organizations.Application.CreateTeam;

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
                ("Claimwick.Modules/Organizations/WebApi/CreateTeam/CreateTeamEndpoint.cs", """
                    using Claimwick.Organizations.Domain.Team;

                    namespace Claimwick.Organizations.WebApi.CreateTeam;

                    internal sealed class CreateTeamEndpoint
                    {
                        internal {|LS2018:Team|} Load() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    namespace Claimwick.Organizations.Domain.Team;

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
                ("Claimwick.Modules/Organizations/WebApi/CreateTeam/CreateTeamEndpoint.cs", """
                    using Claimwick.Organizations.Domain.Team;

                    namespace Claimwick.Organizations.WebApi.CreateTeam;

                    internal sealed class CreateTeamEndpoint
                    {
                        internal {|LS2018:ITeamRepository|} Store() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Domain/Team/ITeamRepository.cs", """
                    namespace Claimwick.Organizations.Domain.Team;

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
                ("Claimwick.Modules/Organizations/WebApi/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.WebApi.CreateTeam;

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
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamHandler.cs", """
                    using Claimwick.Organizations.Domain.Team;

                    namespace Claimwick.Organizations.Application.CreateTeam;

                    internal sealed class CreateTeamHandler
                    {
                        internal Team Handle(CreateTeamCommand command) => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.CreateTeam;

                    internal sealed class CreateTeamCommand
                    {
                    }
                    """),
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    namespace Claimwick.Organizations.Domain.Team;

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
    public Task StaysQuietOutsideApplicationPortsAndWebApi() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    using Claimwick.Organizations.Infrastructure.Persistence;

                    namespace Claimwick.Organizations.Domain.Team;

                    internal sealed class Team
                    {
                        internal TeamStore Store() => null!;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Infrastructure/Persistence/TeamStore.cs", """
                    namespace Claimwick.Organizations.Infrastructure.Persistence;

                    internal sealed class TeamStore
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");


    /// <summary>LS2018 reports a repository named from a WebApi host.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a repository named from a WebApi host")]
    public Task ReportsARepositoryFromAWebApiHost() =>
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
            assemblyName: "Shop.WebApi");

    /// <summary>LS2018 reports an Application file naming a stored document of an aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports an Application file naming a stored document of an aggregate")]
    public Task ReportsAnApplicationFileNamingAnAggregateDocument() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Infrastructure.Persistence;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle({|LS2018:ExpenseDocument|} document)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Spending/Infrastructure/Persistence/ExpenseDocument.cs", """
                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseDocument
                    {
                    }
                    """),
                ("Claimwick.Modules/Spending/Domain/Expense/Expense.cs", """
                    namespace Claimwick.Spending.Domain.Expense;

                    public abstract class AggregateRoot<TId>;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                    }

                    public readonly record struct ExpenseId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports a WebApi file naming a stored document of an aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a WebApi file naming a stored document of an aggregate")]
    public Task ReportsAWebApiFileNamingAnAggregateDocument() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Spending/WebApi/ReadExpense/ReadExpenseEndpoint.cs", """
                    using Claimwick.Spending.Infrastructure.Persistence;

                    namespace Claimwick.Spending.WebApi.ReadExpense;

                    internal sealed class ReadExpenseEndpoint
                    {
                        internal void Handle({|LS2018:ExpenseDocument|} document)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Spending/Infrastructure/Persistence/ExpenseDocument.cs", """
                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseDocument
                    {
                    }
                    """),
                ("Claimwick.Modules/Spending/Domain/Expense/Expense.cs", """
                    namespace Claimwick.Spending.Domain.Expense;

                    public abstract class AggregateRoot<TId>;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                    }

                    public readonly record struct ExpenseId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 reports an Application file naming a document with no aggregate behind it.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports an Application file naming a document with no aggregate behind it")]
    public Task ReportsAnApplicationFileNamingADocumentWithNoAggregate() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Modules/Spending/Application/ReadTotals/ReadTotalsQueryHandler.cs", """
                    using Claimwick.Spending.Infrastructure.Persistence;

                    namespace Claimwick.Spending.Application.ReadTotals;

                    internal sealed class ReadTotalsQueryHandler
                    {
                        internal void Handle({|LS2018:ExpenseTotalsDocument|} document)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Spending/Infrastructure/Persistence/ExpenseTotalsDocument.cs", """
                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseTotalsDocument
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2018 accepts an Application file naming a port.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 accepts an Application file naming a port")]
    public Task AcceptsAnApplicationFileNamingAPort() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Ports.ReceiptPdf;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
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

    /// <summary>LS2018 reports a WebApi file naming a port.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a WebApi file naming a port")]
    public Task ReportsAWebApiFileNamingAPort() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/SubmitExpenseEndpoint.cs", """
                    using Claimwick.Spending.Ports.ReceiptPdf;

                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseEndpoint
                    {
                        internal void Describe({|LS2018:IReceiptPdf|} pdf)
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

    /// <summary>LS2018 reports a Ports file naming an Application type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 reports a Ports file naming an Application type")]
    public Task ReportsAPortsFileNamingAnApplicationType() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Spending/Ports/ReceiptPdf/IReceiptPdf.cs", """
                    using Claimwick.Spending.Application.Expenses.SubmitExpense;

                    namespace Claimwick.Spending.Ports.ReceiptPdf;

                    internal interface IReceiptPdf
                    {
                        void Render({|LS2018:SubmitExpenseCommand|} command);
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

    /// <summary>LS2018 accepts a Ports file naming Domain types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2018 accepts a Ports file naming Domain types")]
    public Task AcceptsAPortsFileNamingDomainTypes() =>
        Verify.AnalyzerAsync<LayerDirectionAnalyzer>(
            [
                ("Claimwick.Spending/Ports/ReceiptPdf/IReceiptPdf.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Ports.ReceiptPdf;

                    internal interface IReceiptPdf
                    {
                        void Render(Expense expense);
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    internal sealed class Expense
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
