using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2008, event handler fact folders.</summary>
public sealed class EventHandlerFolderAnalyzerTests
{
    [Fact(DisplayName = "LS2008 accepts a handler in its fact folder")]
    public Task AcceptsAHandlerInItsFactFolder() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans.OnLoanBorrowed;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            internal sealed class SendDueNoticeOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 reports a handler outside its fact folder")]
    public Task ReportsAHandlerOutsideItsFactFolder() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            internal sealed class {|LS2008:SendDueNoticeOnLoanBorrowedHandler|} : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 reports a handler in another fact folder")]
    public Task ReportsAHandlerInAnotherFactFolder() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans.OnLoanReturned;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            internal sealed class {|LS2008:SendDueNoticeOnLoanBorrowedHandler|} : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 ignores a handler of two facts")]
    public Task IgnoresAHandlerOfTwoFacts() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            public sealed record LoanReturnedEvent(int LoanId);

            internal sealed class LoanNoticeHandler
                : IEventHandler<LoanBorrowedEvent>, IEventHandler<LoanReturnedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 ignores a handler of an interface fact")]
    public Task IgnoresAHandlerOfAnInterfaceFact() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public interface IIntegrationEvent { }

            internal sealed class PublishToTransportOnIntegrationEvent : IEventHandler<IIntegrationEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 ignores an interface-fact handler in a sensibly named folder")]
    public Task IgnoresAnInterfaceFactHandlerInASensiblyNamedFolder() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Webhooks.OnIntegrationEvent;

            public interface IEventHandler<TEvent> { }

            public interface IIntegrationEvent { }

            internal sealed class FanOutWebhooksOnIntegrationEventHandler : IEventHandler<IIntegrationEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 ignores a class that handles no event")]
    public Task IgnoresAClassThatHandlesNoEvent() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans;

            internal sealed class SendDueNoticeOnLoanBorrowedHandler
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2008 ignores a handler outside Application and Infrastructure")]
    public Task IgnoresAHandlerOutsideItsLayers() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            public sealed class SendDueNoticeOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS2008 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<EventHandlerFolderAnalyzer>("""
            namespace Shop.Core;

            public interface IEventHandler<TEvent> { }

            public sealed record LoanBorrowedEvent(int LoanId);

            internal sealed class {|LS2008:SendDueNoticeOnLoanBorrowedHandler|} : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Core", layer: "Application");
}
