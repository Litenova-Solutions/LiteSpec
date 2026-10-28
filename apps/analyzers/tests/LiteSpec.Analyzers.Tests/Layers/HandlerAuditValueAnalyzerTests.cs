using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2013, the handler audit value rule.</summary>
public sealed class HandlerAuditValueAnalyzerTests
{
    [Fact(DisplayName = "LS2013 reports a target read from the message")]
    public Task ReportsATargetReadFromTheMessage() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record RenewLoanCommand(Guid LoanId, string ReaderName);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command, IAuditScope audit) =>
                    audit.{|LS2013:WithTarget|}(command.LoanId);
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 reports a reason formatted from the message")]
    public Task ReportsAReasonFormattedFromTheMessage() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record RenewLoanCommand(Guid LoanId, string ReaderName);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command, IAuditScope audit) =>
                    audit.{|LS2013:WithReason|}($"Renewed by {command.ReaderName}");
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 accepts a target read from a loaded aggregate")]
    public Task AcceptsATargetReadFromALoadedAggregate() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed class Loan
            {
                public Guid Id { get; }
            }

            public interface ILoanRepository
            {
                Loan GetById(Guid id);
            }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(RenewLoanCommand command, IAuditScope audit)
                {
                    var loan = loanRepository.GetById(command.LoanId);
                    audit.WithTarget(loan.Id);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 accepts a target read from a generated value")]
    public Task AcceptsATargetReadFromAGeneratedValue() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record RegisterReaderCommand(string Name);

            internal sealed class RegisterReaderCommandHandler
            {
                public void Handle(RegisterReaderCommand command, IAuditScope audit)
                {
                    var newId = Guid.NewGuid();
                    audit.WithTarget(newId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 accepts a constant audit value")]
    public Task AcceptsAConstantAuditValue() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command, IAuditScope audit) =>
                    audit.WithTarget("loans");
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 accepts a message value in a query handler")]
    public Task AcceptsAMessageValueInAQueryHandler() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record ListLoansQuery(Guid ReaderId);

            internal sealed class ListLoansQueryHandler
            {
                public void Handle(ListLoansQuery query, IAuditScope audit) =>
                    audit.WithTarget(query.ReaderId);
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 accepts a target call on another receiver type")]
    public Task AcceptsATargetCallOnAnotherReceiverType() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface ITraceScope
            {
                ITraceScope WithTarget(object target);
            }

            public sealed record RenewLoanCommand(Guid LoanId);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command, ITraceScope trace) =>
                    trace.WithTarget(command.LoanId);
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2013 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<HandlerAuditValueAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public interface IAuditScope
            {
                IAuditScope WithTarget(object target);
                IAuditScope WithReason(string reason);
            }

            public sealed record RenewLoanCommand(Guid LoanId, string ReaderName);

            internal sealed class RenewLoanCommandHandler
            {
                public void Handle(RenewLoanCommand command, IAuditScope audit) =>
                    audit.WithTarget(command.LoanId);
            }
            """, assemblyName: "Shop.Domain");
}
