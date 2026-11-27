using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema refusal rules: every named refusal is a refusal identifier the module refusal set declares, and no result states a generic refusal by hand.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
internal static class SchemaRefusalRules
{
    /// <summary>The refusal names a product derives for every use case, which no result states by hand.</summary>
    private static readonly string[] GenericRefusals = ["forbidden", "invalid-input", "not-found", "conflict", "unauthenticated", "sign-in-too-old", "rule-not-stated"];

    /// <summary>Whether a refusal code names one of the generic refusals the product derives.</summary>
    /// <param name="code">The refusal code under test, bare or with its module prefix.</param>
    /// <returns>True when the name after the module prefix is a generic refusal; otherwise false.</returns>
    internal static bool IsGenericRefusal(string code)
    {
        var name = code.Contains('.', StringComparison.Ordinal) ? code[(code.IndexOf('.', StringComparison.Ordinal) + 1)..] : code;
        return GenericRefusals.Contains(name, StringComparer.Ordinal);
    }

    /// <summary>Reports a result that writes a generic refusal by hand, which the product derives.</summary>
    /// <param name="record">The result record that holds the code.</param>
    /// <param name="code">The refusal code the result writes.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckGenericRefusal(Record record, string code, List<Finding> findings)
    {
        if (IsGenericRefusal(code))
        {
            findings.Add(new Finding("refusal-generic-written", $"Result '{record.Id}' writes refusal '{code}' by hand; the product derives it and no result states it.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Whether the value is a refusal identifier: refusal with a module home and a name.</summary>
    /// <param name="value">The value under test.</param>
    /// <returns>True for a refusal identifier; otherwise false.</returns>
    internal static bool IsRefusalId(string value) =>
        Identifiers.TrySplit(value, out var prefix, out var home, out var name)
        && prefix == "refusal"
        && home is { Length: > 0 }
        && name.Length > 0;

    /// <summary>Reports a refusal that is neither derived nor named in the refusal set of its module.</summary>
    /// <param name="model">The loaded project model that holds each module's refusal set.</param>
    /// <param name="record">The record that names the refusal.</param>
    /// <param name="refusal">The refusal identifier under test, or null when the record names none.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRefusalCode(ProjectModel model, Record record, string? refusal, List<Finding> findings)
    {
        var code = RefusalCode.CodeOf(refusal);

        if (code is not { Length: > 0 } || IsGenericRefusal(code) || record.Module is not { Length: > 0 } module)
        {
            return;
        }

        if (!model.RefusalSets.Any(entry => entry.Module == module && entry.Code == code))
        {
            findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' names refusal '{refusal}', which the refusal set of module '{module}' does not declare.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
