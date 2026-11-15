using System;

namespace LiteSpec.Model;

/// <summary>
///     The refusal code of a rule: the refusal code its <c>refusal</c> names, such as
///     <c>spending.receipt-missing</c>. Every result of a use case that carries that code is the rule's
///     refusal there. The CLI, the analyzer and the generator read it here, so all three agree.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RefusalCode
{
    /// <summary>
    ///     Reads the rule's refusal code: the string its <c>refusal</c> names, or null when it names none.
    /// </summary>
    /// <param name="rule">The rule record.</param>
    /// <returns>The refusal code, or null when the rule names none.</returns>
    /// <exception cref="ArgumentNullException">The rule is null.</exception>
    public static string? Of(IRecordFields rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return rule.Field("refusal") is string refusal && refusal.Length > 0 ? refusal : null;
    }
}
