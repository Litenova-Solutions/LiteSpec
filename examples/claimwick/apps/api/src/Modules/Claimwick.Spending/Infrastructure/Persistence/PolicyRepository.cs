using Claimwick.Spending.Domain.Policies;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence;

/// <summary>Loads and stages the company spending policy through the scoped document session.</summary>
/// <implements>aggregate/spending.policy</implements>
internal sealed class PolicyRepository : IPolicyRepository
{
    private readonly IDocumentSession _session;

    public PolicyRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<Policy> GetActiveAsync(CancellationToken cancellationToken)
    {
        var policy = await _session.Query<Policy>().FirstOrDefaultAsync(cancellationToken);
        return policy ?? throw new InvalidOperationException("No active spending policy.");
    }

    public async Task<Policy> GetForGuardAsync(CancellationToken cancellationToken)
    {
        var policies = await _session.QueryAsync<Policy>(
            "select data from mt_doc_policy for share",
            cancellationToken);

        return policies.FirstOrDefault() ?? throw new InvalidOperationException("No active spending policy.");
    }

    public void Store(Policy policy)
    {
        _session.Store(policy);
    }
}
