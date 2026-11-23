using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2034, the mediation settings rule.</summary>
public sealed class HandlerMediationSettingsAnalyzerTests
{
    /// <summary>The mediators and handler contracts the test sources name, declared in one file of each project.</summary>
    private const string Stubs = """
        using System.Threading;
        using System.Threading.Tasks;
        using LiteBus.Mediation.Abstractions;

        namespace LiteBus.Mediation.Abstractions
        {
            public sealed class MediationSettings
            {
                public static MediationSettings System { get; } = new MediationSettings();
            }
        }

        namespace LiteBus.Commands.Abstractions
        {
            public interface ICommandMediator
            {
                Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken);

                Task SendAsync<TCommand>(TCommand command, MediationSettings settings, CancellationToken cancellationToken);
            }
        }

        namespace LiteBus.Queries.Abstractions
        {
            public interface IQueryMediator
            {
                Task<TAnswer> QueryAsync<TQuestion, TAnswer>(TQuestion question, CancellationToken cancellationToken);

                Task<TAnswer> QueryAsync<TQuestion, TAnswer>(TQuestion question, MediationSettings settings, CancellationToken cancellationToken);
            }
        }

        namespace LiteBus.Events.Abstractions
        {
            public interface IEventHandler<TEvent>
            {
                Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
            }
        }
        """;

    /// <summary>LS2034 reports a command dispatch without settings in an event handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</proves>
    [Fact(DisplayName = "LS2034 reports a command dispatch without settings in an event handler")]
    public Task ReportsACommandDispatchWithoutSettings() =>
        Verify.AnalyzerAsync<HandlerMediationSettingsAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SendReminder/SendReminderOnExpenseSubmittedHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Events.Abstractions;

                    namespace Claimwick.Spending.Application.SendReminder;

                    public sealed record ExpenseSubmittedDomainEvent(int ExpenseId);

                    public sealed record SendReminderCommand(int ExpenseId);

                    internal sealed class SendReminderOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedDomainEvent>
                    {
                        private readonly ICommandMediator _commands;

                        public SendReminderOnExpenseSubmittedHandler(ICommandMediator commands)
                        {
                            _commands = commands;
                        }

                        public Task HandleAsync(ExpenseSubmittedDomainEvent @event, CancellationToken cancellationToken) =>
                            _commands.{|LS2034:SendAsync|}(new SendReminderCommand(@event.ExpenseId), cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2034 reports a query dispatch without settings in an event handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</proves>
    [Fact(DisplayName = "LS2034 reports a query dispatch without settings in an event handler")]
    public Task ReportsAQueryDispatchWithoutSettings() =>
        Verify.AnalyzerAsync<HandlerMediationSettingsAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseOnExpenseSubmittedHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using LiteBus.Events.Abstractions;
                    using LiteBus.Queries.Abstractions;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public sealed record ExpenseSubmittedDomainEvent(int ExpenseId);

                    public sealed record ReadExpenseQuery(int ExpenseId);

                    public sealed class Expense
                    {
                    }

                    internal sealed class ReadExpenseOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedDomainEvent>
                    {
                        private readonly IQueryMediator _queries;

                        public ReadExpenseOnExpenseSubmittedHandler(IQueryMediator queries)
                        {
                            _queries = queries;
                        }

                        public Task HandleAsync(ExpenseSubmittedDomainEvent @event, CancellationToken cancellationToken) =>
                            Task.CompletedTask;

                        public Task<Expense?> ReadAsync(int expenseId, CancellationToken cancellationToken) =>
                            _queries.{|LS2034:QueryAsync|}<ReadExpenseQuery, Expense?>(new ReadExpenseQuery(expenseId), cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2034 accepts a dispatch that names its mediation settings.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</proves>
    [Fact(DisplayName = "LS2034 accepts a dispatch that names its mediation settings")]
    public Task AcceptsADispatchWithSettings() =>
        Verify.AnalyzerAsync<HandlerMediationSettingsAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SendReminder/SendReminderOnExpenseSubmittedHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Events.Abstractions;
                    using LiteBus.Mediation.Abstractions;

                    namespace Claimwick.Spending.Application.SendReminder;

                    public sealed record ExpenseSubmittedDomainEvent(int ExpenseId);

                    public sealed record SendReminderCommand(int ExpenseId);

                    internal sealed class SendReminderOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedDomainEvent>
                    {
                        private readonly ICommandMediator _commands;

                        public SendReminderOnExpenseSubmittedHandler(ICommandMediator commands)
                        {
                            _commands = commands;
                        }

                        public Task HandleAsync(ExpenseSubmittedDomainEvent @event, CancellationToken cancellationToken) =>
                            _commands.SendAsync(new SendReminderCommand(@event.ExpenseId), MediationSettings.System, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2034 stays quiet for a dispatch outside an event handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</proves>
    [Fact(DisplayName = "LS2034 stays quiet for a dispatch outside an event handler")]
    public Task StaysQuietOutsideAnEventHandler() =>
        Verify.AnalyzerAsync<HandlerMediationSettingsAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SendReminder/SendReminderCommandHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using LiteBus.Commands.Abstractions;

                    namespace Claimwick.Spending.Application.SendReminder;

                    public sealed record SendReminderCommand(int ExpenseId);

                    internal sealed class SendReminderCommandHandler
                    {
                        private readonly ICommandMediator _commands;

                        public SendReminderCommandHandler(ICommandMediator commands)
                        {
                            _commands = commands;
                        }

                        public Task HandleAsync(SendReminderCommand command, CancellationToken cancellationToken) =>
                            _commands.SendAsync(command, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2034 stays quiet outside Application, Infrastructure and Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases</proves>
    [Fact(DisplayName = "LS2034 stays quiet outside Application, Infrastructure and Worker")]
    public Task StaysQuietOutsideItsLayers() =>
        Verify.AnalyzerAsync<HandlerMediationSettingsAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/SendReminderOnExpenseSubmittedHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Events.Abstractions;

                    namespace Claimwick.Spending.Domain;

                    public sealed record ExpenseSubmittedDomainEvent(int ExpenseId);

                    public sealed record SendReminderCommand(int ExpenseId);

                    internal sealed class SendReminderOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedDomainEvent>
                    {
                        private readonly ICommandMediator _commands;

                        public SendReminderOnExpenseSubmittedHandler(ICommandMediator commands)
                        {
                            _commands = commands;
                        }

                        public Task HandleAsync(ExpenseSubmittedDomainEvent @event, CancellationToken cancellationToken) =>
                            _commands.SendAsync(new SendReminderCommand(@event.ExpenseId), cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
