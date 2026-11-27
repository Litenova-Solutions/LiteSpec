using Claimwick.Platform.Persistence;
using Claimwick.Reporting.Application.ReadModels.ReimbursedExpenses;
using Marten;

namespace Claimwick.Reporting.Infrastructure.Persistence;

/// <summary>The reporting read documents as the shared store keeps them.</summary>
/// <follows>litespec/core/backend-persistence#add-read-documents-for-query-shaped-data</follows>
internal sealed class ReportingStoreContribution : IStoreContribution
{
    public void Configure(StoreOptions options)
    {
        options.Schema.For<ReimbursedExpense>().Identity(x => x.Id);
    }
}
