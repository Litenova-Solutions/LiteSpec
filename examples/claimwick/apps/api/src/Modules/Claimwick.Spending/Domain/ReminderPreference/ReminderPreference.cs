using Claimwick.Kernel;
using Claimwick.Spending.Domain.ReminderPreference.Exceptions;

namespace Claimwick.Spending.Domain.ReminderPreference;

/// <summary>One manager's choice to skip approval reminders.</summary>
/// <implements>aggregate/spending.reminder-preference</implements>
internal sealed class ReminderPreference : AggregateRoot<Guid>
{
    private ReminderPreference(ReminderPreferenceActiveState state)
    {
        State = state;
    }

    /// <summary>The current lifecycle state.</summary>
    public ReminderPreferenceState State { get; private set; }

    /// <summary>The manager the preference belongs to.</summary>
    public Guid ManagerId => ((ReminderPreferenceActiveState)State).ManagerId;

    /// <summary>Why the manager skips reminders, when one was given.</summary>
    public string? Reason => ((ReminderPreferenceActiveState)State).Reason;

    /// <summary>The time the preference was last set.</summary>
    public DateTimeOffset UpdatedAt => ((ReminderPreferenceActiveState)State).UpdatedAt;

    /// <summary>Records that the manager skips approval reminders.</summary>
    /// <implements>use-case/spending.change-reminder-preference</implements>
    public static ReminderPreference Suppress(Guid managerId, Guid changerId, bool changerIsFinance, string? reason, DateTimeOffset suppressedAt)
    {
        EnsureChangerAllowed(managerId, changerId, changerIsFinance);

        return new ReminderPreference(new ReminderPreferenceActiveState(managerId, reason, suppressedAt));
    }

    /// <summary>Sets a new reason and time on the recorded preference.</summary>
    /// <implements>use-case/spending.change-reminder-preference</implements>
    public void Change(Guid changerId, bool changerIsFinance, string? reason, DateTimeOffset changedAt)
    {
        EnsureChangerAllowed(ManagerId, changerId, changerIsFinance);

        State = new ReminderPreferenceActiveState(ManagerId, reason, changedAt);
    }

    /// <summary>Refuses a change by anyone but the manager the preference belongs to or finance.</summary>
    public static void EnsureChangerAllowed(Guid managerId, Guid changerId, bool changerIsFinance)
    {
        if (changerId != managerId && !changerIsFinance)
        {
            throw new ReminderPreferenceChangerNotAllowedException();
        }
    }
}
