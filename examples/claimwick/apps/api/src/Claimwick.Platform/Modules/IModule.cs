using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Platform.Modules;

/// <summary>The entry point of one module: registers everything the module holds.</summary>
public interface IModule
{
    /// <summary>Registers the module's options, store contributions and adapters.</summary>
    void AddServices(IServiceCollection services, IConfiguration configuration);
}
