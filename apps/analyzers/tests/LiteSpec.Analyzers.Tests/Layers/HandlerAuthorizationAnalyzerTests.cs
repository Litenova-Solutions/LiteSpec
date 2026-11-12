using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2012, the handler authorization rule.</summary>
public sealed class HandlerAuthorizationAnalyzerTests
{
    /// <summary>LS2012 reports a command handler throwing a forbidden exception.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a command handler throwing a forbidden exception")]
    public Task ReportsACommandHandlerThrowingForbidden() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class LoanForbiddenException : Exception { }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command)
                {
                    throw new {|LS2012:LoanForbiddenException|}();
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a query handler throwing an unauthorized exception.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a query handler throwing an unauthorized exception")]
    public Task ReportsAQueryHandlerThrowingUnauthorized() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IQueryHandler<TQuery, TResult> { }

            public sealed class ReaderUnauthorizedException : Exception
            {
                public ReaderUnauthorizedException(string message) : base(message) { }
            }

            public sealed record ListLoansQuery(Guid ReaderId);

            internal sealed class ListLoansReader : IQueryHandler<ListLoansQuery, int>
            {
                public int Handle(ListLoansQuery query)
                {
                    throw new {|LS2012:ReaderUnauthorizedException|}("Denied.");
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a handler throwing an access denied exception.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a handler throwing an access denied exception")]
    public Task ReportsAHandlerThrowingAccessDenied() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class HoldAccessDeniedException : Exception { }

            public sealed record PlaceHoldCommand(Guid BookId);

            internal sealed class PlaceHoldCommandHandler
            {
                public void Handle(PlaceHoldCommand command)
                {
                    throw new {|LS2012:HoldAccessDeniedException|}();
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a forbidden exception thrown through a local.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a forbidden exception thrown through a local")]
    public Task ReportsAForbiddenExceptionThrownThroughALocal() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class LoanForbiddenException : Exception { }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command)
                {
                    var refusal = new LoanForbiddenException();
                    throw {|LS2012:refusal|};
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a command handler taking the authorization guard.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a command handler taking the authorization guard")]
    public Task ReportsACommandHandlerTakingTheAuthorizationGuard() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            internal sealed class RenewLoanCommandHandler({|LS2012:IAuthorizationGuard|} authorizationGuard)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a query handler taking the authorization guard.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a query handler taking the authorization guard")]
    public Task ReportsAQueryHandlerTakingTheAuthorizationGuard() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IQueryHandler<TQuery, TResult> { }

            public sealed class AuthorizationGuard
            {
            }

            public sealed record ListLoansQuery(int ReaderId);

            internal sealed class ListLoansReader : IQueryHandler<ListLoansQuery, int>
            {
                public ListLoansReader({|LS2012:AuthorizationGuard|} authorizationGuard) { }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a handler taking the authorization guard through its contract.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a handler taking the authorization guard through its contract")]
    public Task ReportsAHandlerTakingTheAuthorizationGuardThroughItsContract() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface ICommandHandler<TCommand, TResult> { }

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            public sealed record RenewLoanCommand(int LoanId);

            internal sealed class RenewLoanUseCase : ICommandHandler<RenewLoanCommand, int>
            {
                public RenewLoanUseCase({|LS2012:IAuthorizationGuard|} authorizationGuard) { }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a nullable authorization guard parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a nullable authorization guard parameter")]
    public Task ReportsANullableAuthorizationGuardParameter() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            internal sealed class RenewLoanCommandHandler({|LS2012:IAuthorizationGuard?|} authorizationGuard)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 reports a handler taking a type that implements the authorization guard.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 reports a handler taking a type that implements the authorization guard")]
    public Task ReportsAHandlerTakingATypeThatImplementsTheAuthorizationGuard() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            public sealed class LoanGuard : IAuthorizationGuard
            {
                public bool Allows(object message) => true;
            }

            internal sealed class RenewLoanCommandHandler({|LS2012:LoanGuard|} authorizationGuard)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts a handler throwing a domain refusal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts a handler throwing a domain refusal")]
    public Task AcceptsAHandlerThrowingADomainRefusal() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class LoanNotFoundException : Exception { }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command)
                {
                    throw new LoanNotFoundException();
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts a throw in an event handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts a throw in an event handler")]
    public Task AcceptsAThrowInAnEventHandler() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public sealed class LoanForbiddenException : Exception { }

            public sealed record LoanBorrowedEvent(Guid LoanId);

            internal sealed class SendDueNoticeOnLoanBorrowedHandler : IEventHandler<LoanBorrowedEvent>
            {
                public void Handle(LoanBorrowedEvent fact)
                {
                    throw new LoanForbiddenException();
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts a rethrow.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts a rethrow")]
    public Task AcceptsARethrow() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command)
                {
                    try
                    {
                        command.ToString();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts a handler taking a repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts a handler taking a repository")]
    public Task AcceptsAHandlerTakingARepository() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                void Store(object loan);
            }

            internal sealed class RenewLoanCommandHandler(ILoanRepository loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts a handler taking an unrelated service.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts a handler taking an unrelated service")]
    public Task AcceptsAHandlerTakingAnUnrelatedService() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IClock
            {
                object Now();
            }

            internal sealed class RenewLoanCommandHandler(IClock clock)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts the authorization guard in an event handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts the authorization guard in an event handler")]
    public Task AcceptsTheAuthorizationGuardInAnEventHandler() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            public sealed record LoanBorrowedEvent(int LoanId);

            internal sealed class SendDueNoticeOnLoanBorrowedHandler(IAuthorizationGuard authorizationGuard) : IEventHandler<LoanBorrowedEvent>
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 accepts the authorization guard outside a constructor.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 accepts the authorization guard outside a constructor")]
    public Task AcceptsTheAuthorizationGuardOutsideAConstructor() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            internal sealed class RenewLoanCommandHandler
            {
                public bool Allows(IAuthorizationGuard authorizationGuard) => true;
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2012 stays silent for the authorization guard outside Application and Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 stays silent for the authorization guard outside Application and Infrastructure")]
    public Task StaysSilentForTheAuthorizationGuardOutsideItsLayers() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            namespace Shop.Loans;

            public interface IAuthorizationGuard
            {
                bool Allows(object message);
            }

            internal sealed class RenewLoanCommandHandler(IAuthorizationGuard authorizationGuard)
            {
            }
            """, assemblyName: "Shop.Domain");

    /// <summary>LS2012 stays silent outside Application and Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#enforce-target-authorization-in-the-pipeline</proves>
    [Fact(DisplayName = "LS2012 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<HandlerAuthorizationAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class LoanForbiddenException : Exception { }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command)
                {
                    throw new LoanForbiddenException();
                }
            }
            """, assemblyName: "Shop.Domain");
}
