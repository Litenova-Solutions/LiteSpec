using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2007, event handler and job names.</summary>
public sealed class ReactionNamingAnalyzerTests
{
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 reports an event handler without an event handler name")]
    public Task ReportsAnEventHandlerWithoutAnEventHandlerName() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IEventHandler<TEvent> { }

            public sealed record OrderPlacedEvent(int OrderId);

            internal sealed class {|LS2007:SubscriberNotifier|} : IEventHandler<OrderPlacedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 accepts the On fact handler shape")]
    public Task AcceptsTheOnFactHandlerShape() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IEventHandler<TEvent> { }

            public sealed record OrderPlacedEvent(int OrderId);

            internal sealed class NotifySubscribersOnOrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 reports a Reaction suffix")]
    public Task ReportsAReactionSuffix() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEventReaction { }

            internal sealed class {|LS2007:OrderPlacedReaction|} : IDomainEventReaction
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 accepts the On fact handler shape on either contract")]
    public Task AcceptsTheOnFactHandlerShapeOnEitherContract() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEventReaction { }

            internal sealed class NotifySubscribersOnOrderPlacedHandler : IDomainEventReaction
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 reports a job without a job name")]
    public Task ReportsAJobWithoutAJobName() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public abstract class BackgroundService { }

            internal sealed class {|LS2007:ReminderLoop|} : BackgroundService
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 reports a job without a job name in Worker")]
    public Task ReportsAJobWithoutAJobNameInWorker() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public abstract class BackgroundService { }

            internal sealed class {|LS2007:ReminderLoop|} : BackgroundService
            {
            }
            """, assemblyName: "Shop.Worker");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 ignores a bare hosted service without a job name")]
    public Task IgnoresABareHostedServiceWithoutAJobName() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IHostedService { }

            internal sealed class ReminderLoop : IHostedService
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 ignores a one-shot startup hook")]
    public Task IgnoresAOneShotStartupHook() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public interface IHostedService
            {
                Task StartAsync(CancellationToken cancellationToken);
                Task StopAsync(CancellationToken cancellationToken);
            }

            internal sealed class DemoTopology : IHostedService
            {
                public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

                public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """, assemblyName: "Shop.Infrastructure");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 accepts Job, Worker, and Recurring names")]
    public Task AcceptsJobWorkerAndRecurringNames() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public abstract class BackgroundService { }

            internal sealed class ReminderJob : BackgroundService
            {
            }

            internal sealed class ReminderWorker : BackgroundService
            {
            }

            internal sealed class RecurringReminderSender : BackgroundService
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 ignores a class that is neither event handler nor job")]
    public Task IgnoresAnOrdinaryClass() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            internal sealed class ReminderLoop
            {
            }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 ignores an event handler outside Application and Infrastructure")]
    public Task IgnoresEventHandlerOutsideItsLayers() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IEventHandler<TEvent> { }

            public sealed record OrderPlacedEvent(int OrderId);

            public sealed class SubscriberNotifier : IEventHandler<OrderPlacedEvent>
            {
            }
            """, assemblyName: "Shop.Domain");

    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2007 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<ReactionNamingAnalyzer>("""
            namespace Shop.Core;

            public interface IEventHandler<TEvent> { }

            public sealed record OrderPlacedEvent(int OrderId);

            internal sealed class {|LS2007:SubscriberNotifier|} : IEventHandler<OrderPlacedEvent>
            {
            }
            """, assemblyName: "Shop.Core", layer: "Application");
}
