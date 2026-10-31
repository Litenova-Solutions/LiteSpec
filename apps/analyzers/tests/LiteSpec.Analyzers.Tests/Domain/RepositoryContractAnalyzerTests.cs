using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1010, repository contracts in Domain.</summary>
public sealed class RepositoryContractAnalyzerTests
{
    /// <summary>LS1010 accepts a find returning a nullable aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 accepts a find returning a nullable aggregate")]
    public Task AcceptsFindReturningNullableAggregate() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public readonly record struct OrderId
            {
            }

            public interface IOrderRepository
            {
                Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken);
            }
            """);

    /// <summary>LS1010 reports a queryable return.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports a queryable return")]
    public Task ReportsQueryableReturn() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Linq;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public interface IOrderRepository
            {
                {|LS1010:IQueryable<Order>|} Query();
            }
            """);

    /// <summary>LS1010 reports an expression filter parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports an expression filter parameter")]
    public Task ReportsExpressionFilterParameter() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public interface IOrderRepository
            {
                Task<IReadOnlyList<Order>> ListAsync({|LS1010:Expression<Func<Order, bool>>|} filter);
            }
            """);

    /// <summary>LS1010 reports a session property.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports a session property")]
    public Task ReportsSessionProperty() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            namespace Shop.Orders;

            public interface IDocumentSession
            {
            }

            public sealed class Order
            {
            }

            public interface IOrderRepository
            {
                {|LS1010:IDocumentSession|} Session { get; }
            }
            """);

    /// <summary>LS1010 reports a set property.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports a set property")]
    public Task ReportsSetProperty() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public sealed class DbSet<T>
            {
            }

            public interface IOrderRepository
            {
                {|LS1010:DbSet<Order>|} Orders { get; }
            }
            """);

    /// <summary>LS1010 reports a derived queryable return.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports a derived queryable return")]
    public Task ReportsDerivedQueryableReturn() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Linq;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public interface ISorted<T> : IQueryable<T>
            {
            }

            public interface IOrderRepository
            {
                {|LS1010:ISorted<Order>|} Sorted();
            }
            """);

    /// <summary>LS1010 reports a generic repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 reports a generic repository")]
    public Task ReportsGenericRepository() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public interface {|LS1010:IRepository|}<T>
            {
                Task<T> GetAsync(Guid id, CancellationToken cancellationToken);
            }
            """);

    /// <summary>LS1010 ignores an interface not named Repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 ignores an interface not named Repository")]
    public Task IgnoresInterfaceNotNamedRepository() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Linq;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public interface IOrderQueries
            {
                IQueryable<Order> Query();
            }
            """);

    /// <summary>LS1010 ignores a repository outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 ignores a repository outside Domain")]
    public Task IgnoresRepositoryOutsideDomain() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Linq;

            namespace Shop.Orders;

            public sealed class Order
            {
            }

            public interface IOrderRepository
            {
                IQueryable<Order> Query();
            }
            """, assemblyName: "Shop.Infrastructure");

    /// <summary>LS1010 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Linq;

            namespace Shop.Core;

            public sealed class Order
            {
            }

            public interface IOrderRepository
            {
                {|LS1010:IQueryable<Order>|} Query();
            }
            """, assemblyName: "Shop.Core", layer: "Domain");

    /// <summary>LS1010 accepts a repository over an aggregate named Session.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain</proves>
    [Fact(DisplayName = "LS1010 accepts a repository over an aggregate named Session")]
    public Task AcceptsRepositoryOverAggregateNamedSession() =>
        Verify.AnalyzerAsync<RepositoryContractAnalyzer>("""
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Sessions;

            public abstract class AggregateRoot<TId>
            {
            }

            public readonly record struct SupportSessionId
            {
            }

            public sealed class SupportSession : AggregateRoot<SupportSessionId>
            {
            }

            public interface ISupportSessionRepository
            {
                Task<SupportSession> GetByIdAsync(SupportSessionId id, CancellationToken cancellationToken);

                Task<IReadOnlyList<SupportSession>> ListAsync(CancellationToken cancellationToken);

                Task<SupportSession?> FindAsync(SupportSessionId id, CancellationToken cancellationToken);

                void Store(SupportSession session);
            }
            """);
}
