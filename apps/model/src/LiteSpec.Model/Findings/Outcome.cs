namespace LiteSpec.Model;

/// <summary>
///     What a check host does with a finding once exceptions are set aside.
/// </summary>
public enum Outcome
{
    /// <summary>The finding is printed and fails the check.</summary>
    Blocking = 0,

    /// <summary>The finding is printed and the check passes.</summary>
    Reported = 1,

    /// <summary>The finding is covered by a departure: printed in its own list, and the check passes.</summary>
    Covered = 2,
}
