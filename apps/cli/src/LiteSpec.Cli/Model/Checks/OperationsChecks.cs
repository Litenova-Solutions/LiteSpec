using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Model.Readers.Operations;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The operations checks: every alert rule names its quality requirement and
///     runbook and carries its measure's numbers, every quality requirement is
///     watched, and every operations file points at its decision.
/// </summary>
/// <follows>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</follows>
/// <follows>litespec/quality/operations#keep-each-alert-threshold-to-its-requirements-measure</follows>
/// <follows>litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check</follows>
/// <follows>litespec/quality/operations#point-each-operations-file-at-its-decision</follows>
internal static class OperationsChecks
{
    /// <summary>Runs alert-link-missing, alert-threshold-drift, quality-alert-missing and the operations-file marks.</summary>
    /// <param name="model">The loaded project model that holds every record, each measure run and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        var alerts = AlertRuleReader.Read(model.Config);

        CheckAlertLinked(model, alerts, findings);
        CheckAlertThreshold(model, alerts, findings);
        CheckQualityAlerted(model, alerts, findings);
        CheckOperationsMarks(model, findings);
    }

    /// <summary>Reports each alert rule whose requirement or runbook label names no matching record of the required kind.</summary>
    /// <param name="model">The loaded project model that resolves each labelled record identifier.</param>
    /// <param name="alerts">The alert rules read from the operations files.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAlertLinked(ProjectModel model, List<AlertRule> alerts, List<Finding> findings)
    {
        foreach (var rule in alerts)
        {
            var problems = new List<string>();

            if (Label(rule, "requirement") is not { } requirement)
            {
                problems.Add("carries no requirement label");
            }
            else if (!model.ById.TryGetValue(requirement, out var required))
            {
                problems.Add($"names requirement '{requirement}', which matches no record");
            }
            else if (required.Type != RecordTypes.Requirement)
            {
                problems.Add($"names '{requirement}', which is a {required.Type} record, not a quality requirement");
            }
            else if (required.Field(RecordFields.Kind) != "quality")
            {
                problems.Add(required.Field(RecordFields.Kind) is { Length: > 0 } kind
                    ? $"names '{requirement}', which is a {kind} requirement, not a quality requirement"
                    : $"names '{requirement}', which is a requirement with no kind, not a quality requirement");
            }

            if (Label(rule, "runbook") is not { } runbook)
            {
                problems.Add("carries no runbook label");
            }
            else if (!model.ById.TryGetValue(runbook, out var answering))
            {
                problems.Add($"names runbook '{runbook}', which matches no record");
            }
            else if (answering.Type != RecordTypes.Runbook)
            {
                problems.Add($"names '{runbook}', which is a {answering.Type} record, not a runbook");
            }

            if (problems.Count > 0)
            {
                findings.Add(new Finding("alert-link-missing", $"Alert rule '{rule.Name}' {string.Join("; ", problems)}.", rule.File, rule.Line, null));
            }
        }
    }

    /// <summary>Reports each alert rule that carries none of the decimal numbers its quality requirement names in its measure.</summary>
    /// <param name="model">The loaded project model that resolves each labelled requirement.</param>
    /// <param name="alerts">The alert rules read from the operations files.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAlertThreshold(ProjectModel model, List<AlertRule> alerts, List<Finding> findings)
    {
        foreach (var rule in alerts)
        {
            if (Label(rule, "requirement") is not { } id
                || !model.ById.TryGetValue(id, out var requirement)
                || requirement.Type != RecordTypes.Requirement
                || requirement.Field(RecordFields.Kind) != "quality"
                || requirement.Field(RecordFields.Measure) is not { Length: > 0 } measure)
            {
                continue;
            }

            var wanted = Decimals.Matches(measure).Select(match => match.Value).ToList();

            if (wanted.Count == 0)
            {
                continue;
            }

            var carried = Decimals.Matches(rule.Text).Select(match => match.Value).ToList();

            if (!wanted.Any(number => carried.Any(held => NumberMatches(number, held))))
            {
                findings.Add(new Finding(
                    "alert-threshold-drift",
                    $"Alert rule '{rule.Name}' carries none of the decimal numbers its requirement '{id}' names in its measure ({string.Join(", ", wanted)}).",
                    rule.File,
                    rule.Line,
                    id));
            }
        }
    }

    /// <summary>Whether the carried number states the wanted one, as written or as a percent of it.</summary>
    /// <param name="wanted">The decimal number as written in the requirement measure.</param>
    /// <param name="carried">The decimal number as written in the alert rule text.</param>
    /// <returns>True when both numbers agree as written or one states the other as a percent; otherwise false.</returns>
    private static bool NumberMatches(string wanted, string carried)
    {
        if (string.Equals(wanted, carried, StringComparison.Ordinal))
        {
            return true;
        }

        if (!decimal.TryParse(wanted, NumberStyles.Number, CultureInfo.InvariantCulture, out var want)
            || !decimal.TryParse(carried, NumberStyles.Number, CultureInfo.InvariantCulture, out var have))
        {
            return false;
        }

        return have == want / 100m || want == have / 100m;
    }

    /// <summary>Reports each quality requirement that no alert rule names and no measured check result matches.</summary>
    /// <param name="model">The loaded project model that holds every requirement and each measure run.</param>
    /// <param name="alerts">The alert rules read from the operations files.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckQualityAlerted(ProjectModel model, List<AlertRule> alerts, List<Finding> findings)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in alerts)
        {
            if (Label(rule, "requirement") is { } id)
            {
                named.Add(id);
            }
        }

        foreach (var requirement in model.Records
                     .Where(record => record.Type == RecordTypes.Requirement && record.Field(RecordFields.Kind) == "quality" && record.Status != RecordStatuses.Retired))
        {
            if (named.Contains(requirement.Id))
            {
                continue;
            }

            if (requirement.Field(RecordFields.Check) is { Length: > 0 } check
                && model.Measures.Any(measure =>
                    measure.Check.Equals(check, StringComparison.Ordinal)
                    || $"{measure.Tool} {measure.Check}".Equals(check, StringComparison.Ordinal)))
            {
                continue;
            }

            findings.Add(new Finding(
                "quality-alert-missing",
                $"Quality requirement '{requirement.Id}' is named by no alert rule and matched by no measured-check or check result.",
                requirement.File,
                requirement.Line,
                requirement.Id.Length > 0 ? requirement.Id : null));
        }
    }

    /// <summary>Reports each operations file that carries no follows mark and validates each mark it carries.</summary>
    /// <param name="model">The loaded project model that holds the project configuration and every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOperationsMarks(ProjectModel model, List<Finding> findings)
    {
        var byFile = OperationsMarkReader.Read(model.Config)
            .GroupBy(mark => mark.File, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var file in OperationsMarkReader.Files(model.Config))
        {
            var relative = model.Config.Relative(file);

            if (!byFile.TryGetValue(file, out var held) || held.Count == 0)
            {
                findings.Add(new Finding("mark-missing", $"Operations file '{relative}' carries no follows mark.", file, 1, null));

                continue;
            }

            foreach (var mark in held)
            {
                CheckOperationsFollows(model, relative, mark, findings);
            }
        }
    }

    /// <summary>
    ///     Checks one follows mark in an operations file: it names a provision of
    ///     the carried release on an applied page, or a decision or exception that
    ///     is not retired.
    /// </summary>
    /// <param name="model">The loaded project model that holds the admitted provisions, the applied pages and every record.</param>
    /// <param name="relative">The operations file path relative to the consumer root, shown in each finding.</param>
    /// <param name="mark">The follows mark read from the operations file.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOperationsFollows(ProjectModel model, string relative, OperationsMark mark, List<Finding> findings)
    {
        var name = $"Operations file '{relative}'";
        var id = mark.Id;

        if (id.StartsWith("litespec/", StringComparison.Ordinal))
        {
            if (!Authoring.Provisions.IsProvisionId(id) || (model.Provisions.Count > 0 && !model.Provisions.Contains(id)))
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which matches no provision of the carried release.", mark.File, mark.Line, id));
            }
            else if (model.AppliedPages.Count > 0
                && Authoring.Provisions.Parse(id) is { } parsed
                && !model.AppliedPages.Contains("docs/" + parsed.Page + ".md"))
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which sits on a page the project's stacks and extensions do not apply.", mark.File, mark.Line, id));
            }

            return;
        }

        if (id.StartsWith("decision/", StringComparison.Ordinal) || id.StartsWith("exception/", StringComparison.Ordinal))
        {
            if (!model.ById.TryGetValue(id, out var record) || (record.Type != RecordTypes.Decision && record.Type != RecordTypes.Exception))
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which matches no decision or exception.", mark.File, mark.Line, id));
            }
            else if (record.Status == RecordStatuses.Retired)
            {
                findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which is retired.", mark.File, mark.Line, null));
            }

            return;
        }

        if (!model.ById.TryGetValue(id, out var other))
        {
            findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which matches no provision, decision, or exception.", mark.File, mark.Line, id));
        }
        else
        {
            findings.Add(new Finding("mark-unknown", $"{name} follows '{id}', which is a {other.Type} record, not a provision, decision, or exception.", mark.File, mark.Line, id));
        }
    }

    /// <summary>One rule label, or null when the rule carries no value for the key.</summary>
    /// <param name="rule">The alert rule whose labels are read.</param>
    /// <param name="key">The label name whose value is wanted.</param>
    /// <returns>The label value, or null when the rule carries no non-empty value for the key.</returns>
    private static string? Label(AlertRule rule, string key) =>
        rule.Labels.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

    /// <summary>Finds each decimal number in a measure or an alert rule text.</summary>
    private static readonly Regex Decimals = new(@"\d+(\.\d+)?", RegexOptions.Compiled);
}
