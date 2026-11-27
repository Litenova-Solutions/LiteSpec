using System.Text.Json;
using LiteBus.Auditing;
using LiteBus.Auditing.Abstractions;
using LiteBus.Auditing.Storage.Marten;
using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Commands.Storage.Marten;
using LiteBus.Events;
using LiteBus.Queries.Abstractions;
using LiteBus.Extensions.AspNetCore;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Abstractions.Authorization;
using LiteBus.Messaging.Abstractions.Tenancy;
using LiteBus.Messaging.Authorization;
using LiteBus.Outbox;
using LiteBus.Outbox.Dispatch.InProcess;
using LiteBus.Outbox.Storage.Marten;
using LiteBus.Outbox.Storage.PostgreSql;
using LiteBus.Queries;
using LiteBus.Storage.PostgreSql;
using __NAMESPACE__.Platform.Access;
using __NAMESPACE__.Platform.Http;
using __NAMESPACE__.Platform.Messaging;
using __NAMESPACE__.Platform.Persistence;
using JasperFx;
using Marten;
using Marten.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace __NAMESPACE__.Platform.Modules;

/// <summary>
/// Composes the platform once for each host: Marten documents on PostgreSQL
/// and LiteBus mediation with the in-process outbox, each pipeline position
/// bound to its pinned LiteBus API. Hosts call <see cref="AddPlatform"/>
/// and then <see cref="AddModules"/>.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#plug-modules-into-the-platform-through-contributions</follows>
public static class PlatformComposition
{
    /// <summary>The connection string name of the product database.</summary>
    public const string ConnectionStringName = "__SCOPE__";

    /// <summary>The PostgreSQL schema of the platform documents: the command idempotency records.</summary>
    private const string PlatformSchemaName = "sample";

    /// <summary>The connection limit of each host pool when the connection string names none.</summary>
    private const int DefaultMaxPoolSize = 30;

    /// <summary>Adds the platform services every module uses.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="hostOptions">The host choices: schema creation.</param>
    /// <returns>The builder that adds the modules.</returns>
    public static PlatformBuilder AddPlatform(
        this IServiceCollection services,
        IConfiguration configuration,
        BackendHostOptions hostOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostOptions);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new SampleCompositionException(
                $"The {ConnectionStringName} connection string is missing. Set ConnectionStrings:{ConnectionStringName}.");
        }

        var pooled = new NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("Max Pool Size", StringComparison.OrdinalIgnoreCase))
        {
            pooled.MaxPoolSize = DefaultMaxPoolSize;
        }

        var dataSource = NpgsqlDataSource.Create(pooled.ConnectionString);
        services.AddSingleton(dataSource);
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddLiteBusExecutionIdentity(options =>
        {
            options.TenantClaimTypes = ["tenant"];
        });
        return new PlatformBuilder(services, configuration, hostOptions, connectionString, dataSource);
    }

    /// <summary>Adds the modules that run in this host, in the order given, and builds the store and the mediator from them.</summary>
    /// <param name="platform">The platform builder.</param>
    /// <param name="modules">The modules of this host.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddModules(this PlatformBuilder platform, params IModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(modules);

        var services = platform.Services;
        foreach (var module in modules)
        {
            module.AddServices(services);
        }

        AddMarten(services, platform, modules);
        AddRefusals(services, modules);
        AddLiteBus(services, platform, modules);
        return services;
    }

    private static void AddMarten(IServiceCollection services, PlatformBuilder platform, IReadOnlyList<IModule> modules)
    {
        services.AddMarten(options =>
        {
            options.Connection(platform.ConnectionString);
            options.AutoCreateSchemaObjects = platform.HostOptions.AutoCreateSchemas ? AutoCreate.All : AutoCreate.None;
            options.Schema.For<MartenIdempotencyRecord>().DatabaseSchemaName(PlatformSchemaName);
            if (options.Serializer() is SystemTextJsonSerializer json)
            {
                json.Configure(StoreJson.Configure);
            }

            foreach (var module in modules)
            {
                module.ConfigureStore(options);
            }
        });
    }

    private static void AddRefusals(IServiceCollection services, IReadOnlyList<IModule> modules)
    {
        services.AddRefusalProblemCatalog(catalog =>
        {
            catalog.MapInvalid(StatusCodes.Status400BadRequest, "The input is invalid.");
            catalog.MapDenied(StatusCodes.Status403Forbidden, "The request is refused.");
            catalog.Replace(AccessPolicies.SignInRequiredCode, StatusCodes.Status401Unauthorized, "Sign-in is required.", "sample");
            foreach (var module in modules)
            {
                module.ContributeRefusalProblems(catalog);
            }
        });
    }

    private static void AddLiteBus(IServiceCollection services, PlatformBuilder platform, IReadOnlyList<IModule> modules)
    {
        var dataSource = platform.DataSource;
        services.AddLiteBus(liteBus =>
        {
            liteBus.AddMessaging(messaging =>
            {
                messaging.AddAuthorizationPolicies(policies =>
                {
                    policies.Add(AccessPolicies.SignedIn, MessagePolicy.SignedIn()
                        .WithRefusal(AccessPolicies.SignInRequiredCode, "Sign-in is required."));
                });
                messaging.UseTenancy(tenancy => tenancy.ValidateTenant(tenantId => Guid.TryParse(tenantId, out _)));
                messaging.UseRefusalExceptionTranslator<DomainExceptionTranslator>();
                var messageJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                messageJson.Converters.Add(new MoneyJsonConverter());
                messageJson.Converters.Add(new TypedIdJsonConverterFactory());
                messageJson.Converters.Add(new CodedClosedSetJsonConverterFactory(new HashSet<Type>()));
                messaging.UseSerializerOptions(messageJson);
                messaging.UseTimeProvider(TimeProvider.System);
                messaging.UseFaultRefusalClassifierInstance(new PostgreSqlConstraintRefusalClassifier(new Dictionary<string, string>()));
                messaging.UseFaultRefusalClassifier<MartenConcurrencyRefusalClassifier>();
                messaging.RequireDeclaration<AuthorizationDeclaration, ICommand>(DeclarationEnforcement.Fail);
                messaging.RequireDeclaration<AuthorizationDeclaration, IQuery>(DeclarationEnforcement.Fail);
                messaging.RequireDeclaration<AuditDeclaration, ICommand>(DeclarationEnforcement.Fail);
                messaging.RequireDeclaration<AuditDeclaration, IQuery>(DeclarationEnforcement.Fail);
                messaging.RequireDeclaration<TenancyDeclaration, ICommand>(DeclarationEnforcement.Fail);
                messaging.RequireDeclaration<TenancyDeclaration, IQuery>(DeclarationEnforcement.Fail);
                messaging.Refusals
                    .Map(AccessPolicies.SignInRequiredCode, RefusalKind.Denied, "Sign-in is required.", retryable: false, owner: "sample");
                foreach (var module in modules)
                {
                    module.ContributeRefusals(messaging.Refusals);
                }
            });
            liteBus.AddAuditing(auditing =>
            {
                auditing.ForCommands();
                auditing.ForQueries();
                auditing.RequireUniqueActions();
                auditing.RequireRecord();
                auditing.UseMartenStore();
                auditing.UseActorResolverInstance(new SampleActorResolver());
            });
            liteBus.AddCommands(commands =>
            {
                foreach (var module in modules)
                {
                    commands.RegisterFromAssembly(module.GetType().Assembly);
                    module.ContributeCommands(commands);
                }

                commands.ForbidNestedCommands();
                commands.UseMartenUnitOfWork();
                commands.EnableIdempotency();
                commands.UseMartenIdempotencyStore();
            });
            liteBus.AddQueries(queries =>
            {
                foreach (var module in modules)
                {
                    queries.RegisterFromAssembly(module.GetType().Assembly);
                }
            });
            liteBus.AddEvents(events =>
            {
                foreach (var module in modules)
                {
                    events.RegisterFromAssembly(module.GetType().Assembly);
                }
            });
            liteBus.AddOutbox(outbox =>
            {
                outbox.UsePostgreSqlStorage(postgres =>
                {
                    postgres.UseDataSource(dataSource);
                    postgres.UseMartenTransactionalOutbox();
                    postgres.HarvestDomainEvents();
                });
                foreach (var module in modules)
                {
                    module.ContributeOutbox(outbox);
                }

                outbox.RequireDomainEventDecision();
                outbox.StageCollectedEvents();
                outbox.UseAmbientTrace();
                outbox.UseAmbientTenant();
                outbox.UseInProcessDispatch(dispatch => dispatch.AllowEnvelopesWithoutHandler("The sample stages no event with a local subscriber yet."));
                outbox.EnablePerHandlerDelivery();
                outbox.EnableImmediateDispatch(dispatch =>
                {
                    dispatch.Timeout = TimeSpan.FromSeconds(10);
                    dispatch.FallbackDelay = TimeSpan.FromSeconds(30);
                });
                outbox.EnableOutboxProcessor(_ => { });
                outbox.EnableCleanup(cleanup =>
                {
                    cleanup.Interval = TimeSpan.FromHours(6);
                    cleanup.Retention = TimeSpan.FromDays(7);
                });
            });
        });
    }
}
