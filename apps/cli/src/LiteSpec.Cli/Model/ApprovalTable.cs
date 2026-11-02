namespace LiteSpec.Model;

/// <summary>
///     The risk classes a record carries and the approval the project table gives
///     them. A record with no risk class carries no approval from the table; its
///     questions and batches follow the table's <c>default</c> rule instead.
/// </summary>
/// <follows>litespec/core/model#approve-each-risk-class-by-the-named-approver</follows>
static class ApprovalTable
{
    /// <summary>The risk classes a record may carry, in the order the model states them.</summary>
    public static readonly IReadOnlyList<string> RiskClasses =
    [
        "money", "personal-data", "access", "physical-access",
    ];

    /// <summary>The risk classes the record carries, in written order. A result or a scenario takes its use case's classes.</summary>
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record whose risk classes are wanted.</param>
    /// <returns>The risk classes of the record or of the use case holding it, or an empty list when it carries none.</returns>
    public static IReadOnlyList<string> ClassesOf(ProjectModel model, Record record)
    {
        if (record.Type is RecordTypes.Result or RecordTypes.Scenario
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is { } holder)
        {
            return ClassesOf(holder);
        }

        return ClassesOf(record);
    }

    /// <summary>The risk classes one record carries, in written order, reading a single string as one class.</summary>
    /// <param name="record">The record whose risk classes are wanted.</param>
    /// <returns>The risk classes the record carries, or an empty list when it carries none.</returns>
    public static IReadOnlyList<string> ClassesOf(Record record)
    {
        if (record.Raw.ValueKind != System.Text.Json.JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Risk, out var risk))
        {
            return [];
        }

        if (risk.ValueKind == System.Text.Json.JsonValueKind.String
            && risk.GetString() is { Length: > 0 } single)
        {
            return [single];
        }

        return risk.ValueKind == System.Text.Json.JsonValueKind.Array
            ? [.. risk.EnumerateArray()
                      .Where(entry => entry.ValueKind == System.Text.Json.JsonValueKind.String)
                      .Select(entry => entry.GetString()!)
                      .Where(entry => entry.Length > 0)]
            : [];
    }

    /// <summary>Whether the record carries any risk class. A result or a scenario takes its use case's classes.</summary>
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record to test.</param>
    /// <returns>True when the record or the use case holding it carries a risk class.</returns>
    public static bool HasRisk(ProjectModel model, Record record) =>
        ClassesOf(model, record).Count > 0;

    /// <summary>Whether the record carries any risk class.</summary>
    /// <param name="record">The record to test.</param>
    /// <returns>True when the record carries a risk class.</returns>
    public static bool HasRisk(Record record) =>
        ClassesOf(record).Count > 0;

    /// <summary>
    ///     The approval the project table gives the record: the approver for each of
    ///     its risk classes and the rule naming them. A class the table does not name
    ///     falls back to the table's <c>default</c> entry when that entry names an
    ///     owner, then to the record owner. Null when the record carries no risk class.
    /// </summary>
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record whose approval is wanted.</param>
    /// <returns>The approval for the record, or null when it carries no risk class.</returns>
    public static RecordApproval? ApprovalOf(ProjectModel model, Record record)
    {
        var classes = ClassesOf(model, record);

        return classes.Count == 0 ? null : ApprovalFor(model.Config, record, classes);
    }

    /// <summary>The approval the table gives a record carrying the named risk classes.</summary>
    /// <param name="config">The consumer configuration holding the approval table and the owners.</param>
    /// <param name="record">The record carrying the risk classes.</param>
    /// <param name="classes">The risk classes the record carries.</param>
    /// <returns>The approval naming the approver and the rule for each class.</returns>
    public static RecordApproval ApprovalFor(
        ConsumerConfig config, Record record, IReadOnlyList<string> classes)
    {
        var approvers = new List<string>();
        var clauses = new List<string>();

        foreach (var riskClass in classes)
        {
            var approver = ApproverFor(config, record, riskClass);

            if (!approvers.Contains(approver, StringComparer.Ordinal))
            {
                approvers.Add(approver);
            }

            clauses.Add($"{riskClass} risk is approved by {approver}");
        }

        return new RecordApproval(
            string.Join(", ", approvers),
            string.Join("; ", clauses) + ".");
    }

    /// <summary>The approver the table names for one risk class of the record.</summary>
    /// <param name="config">The consumer configuration holding the approval table and the owners.</param>
    /// <param name="record">The record carrying the risk class.</param>
    /// <param name="riskClass">The risk class whose approver is wanted.</param>
    /// <returns>The approver for the class: the table entry, the default owner, or the record owner.</returns>
    static string ApproverFor(ConsumerConfig config, Record record, string riskClass)
    {
        if (config.Approvals.TryGetValue(riskClass, out var named) && named.Length > 0)
        {
            return named;
        }

        if (config.Approvals.TryGetValue("default", out var fallback)
            && config.Owners.Contains(fallback, StringComparer.Ordinal))
        {
            return fallback;
        }

        return record.Owner ?? record.Decider ?? (config.Owners.Count > 0 ? config.Owners[0] : null) ?? "unknown";
    }
}
