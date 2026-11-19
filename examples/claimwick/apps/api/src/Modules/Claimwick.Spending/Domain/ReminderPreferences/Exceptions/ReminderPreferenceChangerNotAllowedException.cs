using Claimwick.Kernel.Domain;

namespace Claimwick.Spending.Domain.ReminderPreferences.Exceptions;

/// <summary>The changer is neither the manager the preference belongs to nor finance.</summary>
/// <implements>rule/reminder-preference.changed-by-owner-or-finance</implements>
internal sealed class ReminderPreferenceChangerNotAllowedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ReminderPreferenceChangerNotAllowedException()
        : base("The changer is neither the manager the preference belongs to nor finance.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.not-preference-owner";
}
