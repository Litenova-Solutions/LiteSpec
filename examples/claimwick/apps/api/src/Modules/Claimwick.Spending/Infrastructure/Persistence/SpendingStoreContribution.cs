using Claimwick.Platform.Persistence;
using Claimwick.Spending.Domain.Expense;
using Claimwick.Spending.Domain.Policy;
using Claimwick.Spending.Domain.Receipt;
using Claimwick.Spending.Domain.ReminderPreference;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence;

/// <summary>The spending aggregates as the shared store keeps them: each root as its own document.</summary>
/// <follows>litespec/backend/dotnet/persistence#store-each-aggregate-root-as-its-own-document</follows>
internal sealed class SpendingStoreContribution : IStoreContribution
{
    public void Configure(StoreOptions options)
    {
        options.Schema.For<Expense>().Identity(x => x.Id);
        options.Schema.For<Expense>().UseOptimisticConcurrency(true);
        options.Schema.For<Policy>().UseOptimisticConcurrency(true);
        options.Schema.For<Receipt>().Identity(x => x.Id);
        options.Schema.For<Receipt>().UseOptimisticConcurrency(true);
        options.Schema.For<ReminderPreference>().UseOptimisticConcurrency(true);
    }
}
