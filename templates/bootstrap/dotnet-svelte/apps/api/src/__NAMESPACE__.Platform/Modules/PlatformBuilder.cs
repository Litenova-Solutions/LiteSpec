using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace __NAMESPACE__.Platform.Modules;

/// <summary>
/// The platform services one host builds its modules on: the service collection,
/// the configuration, the host choices, the product connection string and its
/// shared data source.
/// </summary>
public sealed class PlatformBuilder
{
    /// <summary>Creates the builder over the platform services of one host.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="hostOptions">The host choices.</param>
    /// <param name="connectionString">The product connection string.</param>
    /// <param name="dataSource">The shared data source.</param>
    public PlatformBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        BackendHostOptions hostOptions,
        string connectionString,
        NpgsqlDataSource dataSource)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        HostOptions = hostOptions ?? throw new ArgumentNullException(nameof(hostOptions));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    /// <summary>The service collection.</summary>
    public IServiceCollection Services { get; }

    /// <summary>The host configuration.</summary>
    public IConfiguration Configuration { get; }

    /// <summary>The host choices.</summary>
    public BackendHostOptions HostOptions { get; }

    /// <summary>The product connection string.</summary>
    public string ConnectionString { get; }

    /// <summary>The shared data source.</summary>
    public NpgsqlDataSource DataSource { get; }
}
