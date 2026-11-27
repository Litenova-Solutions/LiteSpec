using LiteBus.Commands;
using LiteBus.Extensions.AspNetCore;
using LiteBus.Messaging.Abstractions;
using LiteBus.Outbox.Abstractions;
using Marten;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace __NAMESPACE__.Platform.Modules;

/// <summary>
/// One business capability of the product: its handlers, endpoints, stored types
/// and refusal codes. The host registers every module it runs, and the platform
/// builds one store, one mediator and one endpoint map from them.
/// </summary>
public interface IModule
{
    /// <summary>Registers the module services beyond the handlers and endpoints the platform scans.</summary>
    /// <param name="services">The service collection.</param>
    void AddServices(IServiceCollection services);

    /// <summary>Registers the module documents and indexes on the shared store.</summary>
    /// <param name="options">The store options.</param>
    void ConfigureStore(StoreOptions options);

    /// <summary>Registers the module message contracts: the durable names of its commands.</summary>
    /// <param name="commands">The command module builder.</param>
    void ContributeCommands(CommandModuleBuilder commands);

    /// <summary>Maps the module refusal codes to their transport-neutral kinds.</summary>
    /// <param name="refusals">The refusal registry.</param>
    void ContributeRefusals(IRefusalWriter refusals);

    /// <summary>Maps the module refusal codes to their HTTP statuses.</summary>
    /// <param name="catalog">The refusal problem catalog.</param>
    void ContributeRefusalProblems(IRefusalProblemCatalog catalog);

    /// <summary>Registers the module domain events on the outbox: harvest, translation and ignore decisions.</summary>
    /// <param name="outbox">The outbox builder.</param>
    void ContributeOutbox(OutboxModuleBuilder outbox);

    /// <summary>Maps the module endpoints.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
