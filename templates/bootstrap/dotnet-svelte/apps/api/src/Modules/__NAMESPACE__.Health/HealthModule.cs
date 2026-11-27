using LiteBus.Commands;
using LiteBus.Extensions.AspNetCore;
using LiteBus.Messaging.Abstractions;
using LiteBus.Outbox.Abstractions;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;
using __NAMESPACE__.Health.Domain.HealthChecks;
using __NAMESPACE__.Health.Infrastructure.Persistence.HealthChecks;
using __NAMESPACE__.Health.Domain.HealthChecks.Events;
using __NAMESPACE__.Health.WebApi.HealthChecks.ListCheckIns;
using __NAMESPACE__.Health.WebApi.HealthChecks.ReadCheckIn;
using __NAMESPACE__.Health.WebApi.HealthChecks.RecordCheckIn;
using __NAMESPACE__.Platform.Modules;

namespace __NAMESPACE__.Health;

/// <summary>
/// The Health module: one sample aggregate with one command and two queries,
/// its stored types, its endpoints, its refusal codes and its outbox decisions.
/// </summary>
public sealed class HealthModule : IModule
{
    /// <inheritdoc />
    public void AddServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IHealthCheckRepository, MartenHealthCheckRepository>();
    }

    /// <inheritdoc />
    public void ConfigureStore(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var checks = options.Schema.For<HealthCheck>();
        checks.DatabaseSchemaName("health");
        checks.Identity(check => check.Id);
        checks.UseOptimisticConcurrency(true);
    }

    /// <inheritdoc />
    public void ContributeCommands(CommandModuleBuilder commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        commands.Contracts.Register<RecordCheckInCommand>("health.record-check-in", 1);
    }

    /// <inheritdoc />
    public void ContributeRefusals(IRefusalWriter refusals)
    {
        ArgumentNullException.ThrowIfNull(refusals);
        refusals.Map(HealthRefusalCodes.CheckInUnknown, RefusalKind.NotFound, "The check-in is unknown.", retryable: false, owner: "health");
    }

    /// <inheritdoc />
    public void ContributeRefusalProblems(IRefusalProblemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        catalog.Replace(HealthRefusalCodes.CheckInUnknown, StatusCodes.Status404NotFound, "The check-in is unknown.", "health");
    }

    /// <inheritdoc />
    public void ContributeOutbox(OutboxModuleBuilder outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        outbox.HarvestDomainEvents<HealthCheck>(
            check => check.StagedDomainEvents.Select(domainEvent => (object)domainEvent),
            check => check.DrainDomainEvents());
        outbox.IgnoreDomainEvent<CheckInRecordedDomainEvent>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var group = endpoints.MapGroup("/api/health").WithTags("Health");
        RecordCheckInEndpoint.Map(group);
        ReadCheckInEndpoint.Map(group);
        ListCheckInsEndpoint.Map(group);
    }
}
