using System;

namespace LiteSpec.Model;

/// <summary>
///     The refusal code of a rule: the code of the refusal result its
///     <c>refusal</c> names, or the bare code its <c>refusal</c> carries. The
///     CLI, the analyzer and the generator read it here, so all three agree.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RefusalCode
{
    /// <summary>
    ///     Reads the rule's refusal code: when its <c>refusal</c> names a result
    ///     record whose outcome is <c>refusal</c> with a non-empty code, that
    ///     code; when its <c>refusal</c> is a bare code string, the string itself;
    ///     otherwise null.
    /// </summary>
    /// <param name="rule">The rule record.</param>
    /// <param name="byId">Reads a record by id, or null when none carries it.</param>
    /// <returns>The refusal code, or null when the rule names none.</returns>
    /// <exception cref="ArgumentNullException">The rule or lookup is null.</exception>
    public static string? Of(IRecordFacts rule, Func<string, IRecordFacts?> byId)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (byId is null)
        {
            throw new ArgumentNullException(nameof(byId));
        }

        if (rule.Field("refusal") is not string refusal || refusal.Length == 0)
        {
            return null;
        }

        if (!refusal.StartsWith("result/", StringComparison.Ordinal))
        {
            return refusal;
        }

        var result = byId(refusal);

        if (result is null || result.Type != "result" || result.Field("outcome") != "refusal")
        {
            return null;
        }

        var code = result.Field("code");

        return code is not null && code.Length > 0 ? code : null;
    }
}
