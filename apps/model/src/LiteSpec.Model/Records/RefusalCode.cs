using System;

namespace LiteSpec.Model;

/// <summary>
///     The refusal code of a rule or result: the wire code its <c>refusal</c> names, derived from
///     the refusal identifier as its home and name, such as <c>spending.receipt-missing</c> from
///     <c>refusal/spending.receipt-missing</c>. The refusal entry holds the identifier; the code is
///     the API's problem code and the audit trail's stored value. The CLI, the analyzer and the
///     generator read it here, so all three agree.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class RefusalCode
{
    /// <summary>
    ///     Reads the record's refusal code: the wire code derived from the refusal identifier its
    ///     <c>refusal</c> names, or the code a derived generic result carries, or null when it
    ///     names neither.
    /// </summary>
    /// <param name="record">The rule or result record.</param>
    /// <returns>The refusal code, or null when the record names none.</returns>
    /// <exception cref="ArgumentNullException">The record is null.</exception>
    public static string? Of(IRecordFields record)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        return CodeOf(record.Field("refusal")) ?? record.Field("code");
    }

    /// <summary>
    ///     Derives the wire code from a refusal reference: the home and name of a refusal
    ///     identifier, such as <c>spending.receipt-missing</c> from
    ///     <c>refusal/spending.receipt-missing</c>. A value that is no refusal identifier
    ///     is already a code, such as a derived generic refusal, and is returned unchanged.
    /// </summary>
    /// <param name="refusal">The refusal reference, or null when the record names none.</param>
    /// <returns>The wire code, or null when the record names none.</returns>
    public static string? CodeOf(string? refusal)
    {
        if (refusal is not { Length: > 0 })
        {
            return null;
        }

        return Identifiers.TrySplit(refusal, out var prefix, out var home, out var name)
            && prefix == "refusal"
            && home is { Length: > 0 }
            && name.Length > 0
                ? home + "." + name
                : refusal;
    }
}
