using Claimwick.Spending.Domain.ReminderPreferences;
using Marten;

namespace Claimwick.Spending.Infrastructure.Persistence;

/// <summary>Loads and stages reminder preferences through the scoped document session.</summary>
/// <implements>aggregate/spending.reminder-preference</implements>
internal sealed class ReminderPreferenceRepository : IReminderPreferenceRepository
{
    private readonly IDocumentSession _session;

    public ReminderPreferenceRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<ReminderPreference?> GetByManagerAsync(Guid managerId, CancellationToken cancellationToken) =>
        await _session.Query<ReminderPreference>()
            .Where(preference => preference.ManagerId == managerId)
            .FirstOrDefaultAsync(cancellationToken);

    public void Store(ReminderPreference preference)
    {
        _session.Store(preference);
    }

    public void Delete(Guid managerId)
    {
        _session.DeleteWhere<ReminderPreference>(preference => preference.ManagerId == managerId);
    }
}
