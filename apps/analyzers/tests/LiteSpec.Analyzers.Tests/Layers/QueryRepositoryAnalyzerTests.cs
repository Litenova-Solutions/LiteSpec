using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2011, the query handler repository rule.</summary>
public sealed class QueryRepositoryAnalyzerTests
{
    /// <summary>LS2011 reports a query handler constructor parameter typed as a repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 reports a query handler constructor parameter typed as a repository")]
    public Task ReportsARepositoryConstructorParameter() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                object GetById(object id);
            }

            internal sealed class ListLoansQueryHandler({|LS2011:ILoanRepository|} loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 reports a query handler detected through IQueryHandler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 reports a query handler detected through IQueryHandler")]
    public Task ReportsAnInterfaceDetectedQueryHandler() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface IQueryHandler<TQuery, TResult> { }

            public interface ILoanRepository
            {
                object GetById(object id);
            }

            public sealed record ListLoansQuery(int Page);

            internal sealed class ListLoansReader : IQueryHandler<ListLoansQuery, int>
            {
                public ListLoansReader({|LS2011:ILoanRepository|} loanRepository) { }
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 reports a nullable repository parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 reports a nullable repository parameter")]
    public Task ReportsANullableRepositoryParameter() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                object GetById(object id);
            }

            internal sealed class ListLoansQueryHandler({|LS2011:ILoanRepository?|} loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 accepts a command handler taking a repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 accepts a command handler taking a repository")]
    public Task AcceptsACommandHandlerTakingARepository() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                void Store(object loan);
            }

            internal sealed class RenewLoanCommandHandler(ILoanRepository loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 accepts a query handler projecting through the read session.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 accepts a query handler projecting through the read session")]
    public Task AcceptsAQueryHandlerProjectingThroughTheReadSession() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface IQuerySession
            {
                object Query(object query);
            }

            internal sealed class ListLoansQueryHandler(IQuerySession session)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 accepts a concrete repository class parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 accepts a concrete repository class parameter")]
    public Task AcceptsAConcreteRepositoryClass() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public sealed class LoanRepository
            {
            }

            internal sealed class ListLoansQueryHandler(LoanRepository loanRepository)
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2011 stays silent outside Application and Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#project-queries-directly</proves>
    [Fact(DisplayName = "LS2011 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<QueryRepositoryAnalyzer>("""
            namespace Shop.Loans;

            public interface ILoanRepository
            {
                object GetById(object id);
            }

            internal sealed class ListLoansQueryHandler(ILoanRepository loanRepository)
            {
            }
            """, assemblyName: "Shop.Domain");
}
