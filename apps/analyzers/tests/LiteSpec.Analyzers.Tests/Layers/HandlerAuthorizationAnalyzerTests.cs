using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2012, the handler authorization rule.</summary>
public sealed class HandlerAuthorizationAnalyzerTests
{
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
