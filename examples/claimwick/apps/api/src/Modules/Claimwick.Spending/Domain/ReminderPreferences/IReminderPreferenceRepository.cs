namespace Claimwick.Spending.Domain.ReminderPreferences;

/// <summary>Loads and stages reminder preferences.</summary>
internal interface IReminderPreferenceRepository
{
    /// <summary>Loads the preference of one manager, when the manager holds one.</summary>
    Task<ReminderPreference?> GetByManagerAsync(Guid managerId, CancellationToken cancellationToken);

    /// <summary>Stages a new or changed preference for the pipeline's one commit.</summary>
    void Store(ReminderPreference preference);

    /// <summary>Deletes the preference of one manager with the personal data it holds.</summary>
    void Delete(Guid managerId);
}
