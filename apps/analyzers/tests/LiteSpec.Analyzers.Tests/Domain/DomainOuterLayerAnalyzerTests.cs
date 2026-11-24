using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1001, outer-layer references in Domain.</summary>
public sealed class DomainOuterLayerAnalyzerTests
{
    /// <summary>LS1001 reports a using directive that imports an outer-layer namespace.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a using directive that imports an outer-layer namespace")]
    public Task ReportsAUsingOfAnOuterLayerNamespace() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    using {|LS1001:Microsoft.Extensions.Logging|};

                    namespace Shop.Orders;

                    public sealed class Order
                    {
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Microsoft.Extensions.Logging
                    {
                        public interface ILogger
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports a base type from an outer-layer namespace.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a base type from an outer-layer namespace")]
    public Task ReportsABaseTypeFromAnOuterLayerNamespace() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    namespace Shop.Orders;

                    public sealed class Order : Microsoft.EntityFrameworkCore.{|LS1001:DbContext|}
                    {
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Microsoft.EntityFrameworkCore
                    {
                        public class DbContext
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports an attribute from an outer-layer namespace.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports an attribute from an outer-layer namespace")]
    public Task ReportsAnAttributeFromAnOuterLayerNamespace() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Order
            {
                [System.Text.Json.Serialization.{|LS1001:JsonPropertyName|}("id")]
                public string Id { get; }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1001 reports a generic argument and a nested outer-layer namespace.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a generic argument and a nested outer-layer namespace")]
    public Task ReportsAGenericArgumentAndANestedNamespace() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    using System.Collections.Generic;

                    namespace Shop.Orders;

                    public sealed class OrderLog
                    {
                        public List<Microsoft.Extensions.Logging.{|LS1001:ILogger|}> Loggers { get; } = new();

                        public Microsoft.Extensions.Logging.Abstractions.{|LS1001:NullLogger|}? Logger { get; set; }
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Microsoft.Extensions.Logging
                    {
                        public interface ILogger
                        {
                        }
                    }

                    namespace Microsoft.Extensions.Logging.Abstractions
                    {
                        public sealed class NullLogger
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports a global using of an outer-layer namespace.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a global using of an outer-layer namespace")]
    public Task ReportsAGlobalUsingOfAnOuterLayerNamespace() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    global using {|LS1001:Marten|};

                    namespace Shop.Orders;

                    public sealed class Order
                    {
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Marten
                    {
                        public interface IDocumentStore
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 accepts base class library and project types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 accepts base class library and project types")]
    public Task AcceptsBaseClassLibraryAndProjectTypes() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            namespace Shop.Orders;

            public sealed class Order
            {
                public List<string> Tags { get; } = new();

                public DateTime PlacedAt { get; init; }

                public Task<int> CountAsync()
                {
                    return Task.FromResult(Tags.Count);
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1001 ignores a namespace that only shares a prefix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 ignores a namespace that only shares a prefix")]
    public Task IgnoresANamespaceThatOnlySharesAPrefix() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    namespace Shop.Orders;

                    public sealed class OrderService
                    {
                        public MartenStore.Store? Store { get; set; }
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace MartenStore
                    {
                        public sealed class Store
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports a parameter and a member access on an outer-layer type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a parameter and a member access on an outer-layer type")]
    public Task ReportsAParameterAndAMemberAccess() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    namespace Shop.Orders;

                    public sealed class OrderService
                    {
                        public void Save(Marten.{|LS1001:IDocumentSession|} session)
                        {
                            session.{|LS1001:Store|}(new object());
                        }
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Marten
                    {
                        public interface IDocumentSession
                        {
                            void Store(object entity);
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports object creation and typeof of an outer-layer type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports object creation and typeof of an outer-layer type")]
    public Task ReportsObjectCreationAndTypeof() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.Spending/Domain/Source.cs", """
                    namespace Shop.Orders;

                    public sealed class OrderService
                    {
                        public string Describe()
                        {
                            var factory = new Microsoft.Extensions.Logging.{|LS1001:LoggerFactory|}();
                            return factory.ToString() + typeof(Microsoft.Extensions.Logging.{|LS1001:LoggerFactory|}).FullName;
                        }
                    }
                    """),
                ("Shop.Spending/Domain/Stubs.cs", """
                    namespace Microsoft.Extensions.Logging
                    {
                        public sealed class LoggerFactory
                        {
                        }
                    }
                    """),
            ]);

    /// <summary>LS1001 reports a using static and an unqualified member call.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a using static and an unqualified member call")]
    public Task ReportsAUsingStaticAndAnUnqualifiedCall() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>("""
            using static {|LS1001:System.Text.Json.JsonSerializer|};

            namespace Shop.Orders;

            public sealed class OrderService
            {
                public string Describe(object value)
                {
                    return {|LS1001:Serialize|}(value);
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1001 ignores outer-layer types outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 ignores outer-layer types outside Domain")]
    public Task IgnoresOuterLayerTypesOutsideDomain() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Shop.WebApi/WebApi/OrderService.cs", """
                    using Microsoft.Extensions.Logging;

                    namespace Shop.Orders;

                    public sealed class OrderService
                    {
                        public void Record(ILogger logger)
                        {
                        }
                    }
                    """),
                ("Shop.WebApi/WebApi/Stubs.cs", """
                    namespace Microsoft.Extensions.Logging
                    {
                        public interface ILogger
                        {
                        }
                    }
                    """),
            ], assemblyName: "Shop.WebApi");


    /// <summary>LS1001 reports a Domain file naming an Application type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a Domain file naming an Application type")]
    public Task ReportsADomainFileNamingAnApplicationType() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    using Claimwick.Organizations.Application.CreateTeam;

                    namespace Claimwick.Organizations.Domain.Team;

                    internal sealed class Team
                    {
                        internal Team Rename({|LS1001:CreateTeamCommand|} command) => this;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Application/CreateTeam/CreateTeamCommand.cs", """
                    namespace Claimwick.Organizations.Application.CreateTeam;

                    internal sealed class CreateTeamCommand
                    {
                    }
                    """),
            ], assemblyName: "Claimwick.Modules");

    /// <summary>LS1001 reports a Domain file naming Infrastructure and WebApi types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a Domain file naming Infrastructure and WebApi types")]
    public Task ReportsADomainFileNamingInfrastructureAndWebApiTypes() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    using Claimwick.Organizations.Infrastructure.Persistence;
                    using Claimwick.Organizations.WebApi.CreateTeam;

                    namespace Claimwick.Organizations.Domain.Team;

                    internal sealed class Team
                    {
                        internal void Link({|LS1001:TeamStore|} store, {|LS1001:CreateTeamEndpoint|} endpoint)
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
                ("Claimwick.Modules/Organizations/WebApi/CreateTeam/CreateTeamEndpoint.cs", """
                    namespace Claimwick.Organizations.WebApi.CreateTeam;

                    internal sealed class CreateTeamEndpoint
                    {
                    }
                    """),
            ], assemblyName: "Claimwick.Modules");

    /// <summary>LS1001 reports a Domain file naming a port.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 reports a Domain file naming a port")]
    public Task ReportsADomainFileNamingAPort() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Ports.ReceiptPdf;

                    namespace Claimwick.Spending.Domain.Expenses;

                    internal sealed class Expense
                    {
                        internal void Print({|LS1001:IReceiptPdf|} pdf)
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
            ], assemblyName: "Claimwick.Spending");

    /// <summary>LS1001 accepts a typed identifier from another module's Contracts and reports a port there.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 accepts a typed identifier from Contracts and reports a port there")]
    public Task AcceptsAContractsIdentifierAndReportsAContractsPort() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Domain/Invoice/Invoice.cs", """
                    namespace Claimwick.Billing.Domain.Invoice;

                    internal sealed class Invoice
                    {
                        internal Invoice Bill(Claimwick.Organizations.Contracts.Identifiers.OrganizerId organizer, Claimwick.Organizations.Contracts.Ports.{|LS1001:IOrganizerReader|} reader) => this;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Identifiers/OrganizerId.cs", """
                    namespace Claimwick.Organizations.Contracts.Identifiers;

                    internal readonly record struct OrganizerId(System.Guid Value);
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Ports/IOrganizerReader.cs", """
                    namespace Claimwick.Organizations.Contracts.Ports;

                    internal interface IOrganizerReader
                    {
                    }
                    """),
            ], assemblyName: "Claimwick.Modules");

    /// <summary>LS1001 accepts a rule refusal another module publishes in its Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 accepts a rule refusal from Contracts")]
    public Task AcceptsAContractsRuleRefusal() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Modules/Billing/Domain/Invoice/Invoice.cs", """
                    namespace Claimwick.Billing.Domain.Invoice;

                    internal sealed class Invoice
                    {
                        internal static void Refuse() => throw new Claimwick.Organizations.Contracts.Exceptions.OrganizerUnknownException();
                    }
                    """),
                ("Claimwick.Modules/Organizations/Contracts/Exceptions/OrganizerUnknownException.cs", """
                    namespace Claimwick.Organizations.Contracts.Exceptions;

                    internal sealed class OrganizerUnknownException : System.Exception
                    {
                    }
                    """),
            ], assemblyName: "Claimwick.Modules");

    /// <summary>LS1001 accepts a Domain file naming only Domain types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</proves>
    [Fact(DisplayName = "LS1001 accepts a Domain file naming only Domain types")]
    public Task AcceptsADomainFileNamingOnlyDomainTypes() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("Claimwick.Modules/Organizations/Domain/Team/Team.cs", """
                    namespace Claimwick.Organizations.Domain.Team;

                    internal sealed class Team
                    {
                        internal Team Rename(TeamName name) => this;
                    }
                    """),
                ("Claimwick.Modules/Organizations/Domain/Team/TeamName.cs", """
                    namespace Claimwick.Organizations.Domain.Team;

                    internal sealed record TeamName(string Value);
                    """),
            ], assemblyName: "Claimwick.Modules");

    /// <summary>LS1001 accepts composition, logging and options in a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS1001 accepts composition, logging and options in a knowledge model")]
    public Task AcceptsCompositionLoggingAndOptionsInAKnowledgeModel() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel/TravelComposition.cs", """
                    using Microsoft.Extensions.DependencyInjection;
                    using Microsoft.Extensions.Logging;
                    using Microsoft.Extensions.Options;

                    namespace Claimwick.Travel;

                    public static class TravelComposition
                    {
                        public static IServiceCollection AddTravel(this IServiceCollection services, ILogger<TravelRegistry> logger, IOptions<TravelSettings> settings)
                        {
                            logger.LogInformation("Registering travel.");
                            services.AddSingleton(new TravelRegistry());
                            return services;
                        }
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/TravelRegistry.cs", """
                    namespace Claimwick.Travel;

                    public sealed class TravelRegistry
                    {
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/TravelSettings.cs", """
                    namespace Claimwick.Travel;

                    public sealed class TravelSettings
                    {
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/Stubs.cs", """
                    namespace Microsoft.Extensions.DependencyInjection
                    {
                        public interface IServiceCollection
                        {
                        }

                        public static class ServiceCollectionExtensions
                        {
                            public static IServiceCollection AddSingleton(this IServiceCollection services, object instance) => services;
                        }
                    }

                    namespace Microsoft.Extensions.Logging
                    {
                        public interface ILogger<T>
                        {
                            void LogInformation(string message);
                        }
                    }

                    namespace Microsoft.Extensions.Options
                    {
                        public interface IOptions<T>
                        {
                            T Value { get; }
                        }
                    }
                    """),
            ], assemblyName: "Claimwick.Travel");

    /// <summary>LS1001 reports any other outer-layer reference in a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS1001 reports any other outer-layer reference in a knowledge model")]
    public Task ReportsAnyOtherOuterLayerReferenceInAKnowledgeModel() =>
        Verify.AnalyzerAsync<DomainOuterLayerAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel/TravelStore.cs", """
                    using {|LS1001:Microsoft.EntityFrameworkCore|};

                    namespace Claimwick.Travel;

                    public sealed class TravelStore : {|LS1001:DbContext|}
                    {
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/Stubs.cs", """
                    namespace Microsoft.EntityFrameworkCore
                    {
                        public class DbContext
                        {
                        }
                    }
                    """),
            ], assemblyName: "Claimwick.Travel");
}
