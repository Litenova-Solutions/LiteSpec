using Claimwick.Domain;

namespace Claimwick.Spending.ReminderPreferences.Domain.Exceptions;

/// <summary>The changer is neither the manager the preference belongs to nor finance.</summary>
/// <implements>rule/reminder-preference.changed-by-owner-or-finance</implements>
public sealed class ReminderPreferenceChangerNotAllowedException()
    : DomainException("The changer is neither the manager the preference belongs to nor finance.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.not-preference-owner";
}
