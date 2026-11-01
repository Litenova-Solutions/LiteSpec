using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2016, the pipeline-owned collaborator rule.</summary>
public sealed class PipelineOwnedDependencyAnalyzerTests
{
    /// <summary>LS2016 reports a command handler taking the unit of work.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 reports a command handler taking the unit of work")]
    public Task ReportsACommandHandlerTakingTheUnitOfWork() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            using LiteBus.Messaging.Abstractions;

            namespace LiteBus.Messaging.Abstractions
            {
                public interface IUnitOfWork { }
            }

            namespace Shop.Loans
            {
                internal sealed class RenewLoanCommandHandler({|LS2016:IUnitOfWork|} unitOfWork)
                {
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2016 reports a query handler taking the outbox.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 reports a query handler taking the outbox")]
    public Task ReportsAQueryHandlerTakingTheOutbox() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            using LiteBus.Outbox.Abstractions;

            namespace LiteBus.Outbox.Abstractions
            {
                public interface IOutbox { }
            }

            namespace Shop.Loans
            {
                internal sealed class ListLoansQueryHandler({|LS2016:IOutbox|} outbox)
                {
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2016 reports a handler detected through its contract.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 reports a handler detected through its contract")]
    public Task ReportsAHandlerDetectedThroughItsContract() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            using LiteBus.Messaging.Abstractions;

            namespace LiteBus.Messaging.Abstractions
            {
                public interface IIdempotencyStore { }
            }

            namespace Shop.Loans
            {
                public interface ICommandHandler<TCommand, TResult> { }

                public sealed record RenewLoanCommand(int LoanId);

                internal sealed class RenewLoanUseCase : ICommandHandler<RenewLoanCommand, int>
                {
                    public RenewLoanUseCase({|LS2016:IIdempotencyStore|} idempotencyStore) { }
                }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2016 accepts a handler taking a repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 accepts a handler taking a repository")]
    public Task AcceptsAHandlerTakingARepository() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                void Store(object loan);
            }

            internal sealed class RenewLoanCommandHandler(ILoanRepository loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2016 accepts a handler taking an unlisted collaborator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 accepts a handler taking an unlisted collaborator")]
    public Task AcceptsAHandlerTakingAnUnlistedCollaborator() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            namespace Shop.Loans;

            public interface IClock
            {
                object Now();
            }

            internal sealed class RenewLoanCommandHandler(IClock clock)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2016 stays silent outside Application and Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/persistence#keep-uniqueness-out-of-handlers</proves>
    [Fact(DisplayName = "LS2016 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<PipelineOwnedDependencyAnalyzer>("""
            using LiteBus.Messaging.Abstractions;

            namespace LiteBus.Messaging.Abstractions
            {
                public interface IUnitOfWork { }
            }

            namespace Shop.Loans
            {
                internal sealed class RenewLoanCommandHandler(IUnitOfWork unitOfWork)
                {
                }
            }
            """, assemblyName: "Shop.Domain");
}
