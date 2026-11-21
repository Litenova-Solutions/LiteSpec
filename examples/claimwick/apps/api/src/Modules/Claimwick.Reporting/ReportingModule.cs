using Claimwick.Platform.Modules;
using Claimwick.Platform.Persistence;
using Claimwick.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Reporting;

/// <summary>The Reporting module: the reimbursed expenses read model with its query.</summary>
public sealed class ReportingModule : IModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IStoreContribution, ReportingStoreContribution>();
    }
}
