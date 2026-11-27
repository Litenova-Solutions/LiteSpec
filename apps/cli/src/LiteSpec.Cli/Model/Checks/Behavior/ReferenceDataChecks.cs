using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The reference-data checks: every row value stays within its table's closed set, every cross-reference names a row key its table holds, every required role is played by a row, the rows leave no date uncovered, each overlap finding names the shared range, and the use cases computing one value agree on its sign.</summary>
/// <follows>litespec/core/domain-model#state-what-an-empty-table-will-hold</follows>
/// <follows>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</follows>
/// <follows>litespec/core/knowledge#name-a-role-not-a-row</follows>
/// <follows>litespec/core/knowledge#validate-reference-data-before-serving</follows>
/// <follows>litespec/core/domain-model#cover-every-date-in-use</follows>
/// <follows>litespec/core/domain-model#state-the-sign-of-every-amount</follows>
internal static class ReferenceDataChecks
{
    /// <summary>Gets the name of the `requiredRoles` field.</summary>
    const string RequiredRoles = "requiredRoles";

    /// <summary>Gets the name of the `refersTo` field.</summary>
    const string RefersTo = "refersTo";

    /// <summary>Gets the name of the `sign` field.</summary>
    const string Sign = "sign";

    /// <summary>The sign conventions an amount field states.</summary>
    private static readonly string[] AmountSigns = ["money-in-positive", "money-out-positive"];

    /// <summary>The field types that may state a sign convention.</summary>
    private static readonly string[] AmountTypes = ["money", "decimal"];

    /// <summary>A kebab-case row key or role: a lowercase letter or digit opening, then lowercase letters, digits or hyphens.</summary>
    private static readonly Regex KebabName = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>A reference-data table identifier: the base a row reference resolves through.</summary>
    private static readonly Regex TableId = new("^reference-data/[a-z0-9][a-z0-9-]*\\.[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Reports a reference-data row whose value is outside the closed set its table lists in choices.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunValueClosedSet(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RecordFields.Choices, out var choices))
            {
                continue;
            }

            if (choices.ValueKind != JsonValueKind.Array)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds choices that are not a list.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var allowed = choices.EnumerateArray().ToList();

            if (allowed.Count == 0)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds an empty closed set.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var index = 0;

            foreach (var row in RowsOf(table))
            {
                if (row.TryGetProperty(RecordFields.Value, out var value)
                    && !allowed.Any(candidate => ValueEquals(candidate, value)))
                {
                    findings.Add(new Finding(
                        "reference-data-value-invalid",
                        $"Reference data '{table.Id}' holds row {index} with value {Show(value)} outside its choices.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }

                index++;
            }
        }
    }

    /// <summary>Reports a reference-data row whose cross-reference names a key no row of the named table holds.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunKeyResolves(ProjectModel model, List<Finding> findings)
    {
        var keys = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var table in model.Records.Where(record => record.Type == RecordTypes.ReferenceData))
        {
            keys.TryAdd(
                table.Id,
                RowsOf(table)
                    .Select(row => Str(row, RecordFields.Key))
                    .OfType<string>()
                    .ToList());
        }

        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckRowKeys(table, findings);

            var index = 0;

            foreach (var row in RowsOf(table))
            {
                if (Str(row, RecordFields.Role) is { } role && !KebabName.IsMatch(role))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} with role '{role}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
                }

                if (Str(row, RefersTo) is { } target)
                {
                    CheckRefersTo(model, keys, table, target, index, findings);
                }

                index++;
            }
        }
    }

    /// <summary>Reports a reference-data table whose required role is played by no row of a variant. Each variant answers lookups on its own, so each plays every required role.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRolePlayed(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RequiredRoles, out var required))
            {
                continue;
            }

            if (required.ValueKind != JsonValueKind.Array)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds required roles that are not a list.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var wanted = new List<string>();

            foreach (var entry in required.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String || entry.GetString() is not { } role)
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds a required role that is not a name.", table.File, table.Line, SubjectOf(table)));
                }
                else if (!KebabName.IsMatch(role))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' requires role '{role}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
                }
                else
                {
                    wanted.Add(role);
                }
            }

            var rows = RowsOf(table);

            foreach (var variant in VariantsOf(rows))
            {
                var played = rows
                    .Where(row => string.Equals(Str(row, RecordFields.Variant), variant, StringComparison.Ordinal))
                    .Select(row => Str(row, RecordFields.Role))
                    .Where(role => role is not null)
                    .ToHashSet(StringComparer.Ordinal);

                var whose = variant is null ? "no row plays" : $"no row of variant '{variant}' plays";

                foreach (var role in wanted.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                {
                    if (!played.Contains(role))
                    {
                        findings.Add(new Finding(
                            "reference-data-role-missing",
                            $"Reference data '{table.Id}' requires role '{role}', which {whose}.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }
            }
        }
    }

    /// <summary>Reports a reference-data table with no rows yet that states no summary: the summary states what rows the table will hold.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunEmptySummary(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (RowsOf(table).Count == 0 && table.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Reference data '{table.Id}' holds no rows and states no summary; state in `summary` what rows the table will hold.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }
    }

    /// <summary>Reports a reference-data table whose rows of one variant leave dates with no row in force, or cover no row on the date the cast declares as today. Each variant covers its own dates.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record and the cast.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunCoverage(ProjectModel model, List<Finding> findings)
    {
        var today = model.Records
            .Where(record => record.Type == RecordTypes.Cast)
            .Select(record => record.Field(RecordFields.Today))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var spans = SpansOf(table);

            foreach (var group in GroupsOf(spans))
            {
                var whose = group.Variant is { } variant ? $" of variant '{variant}'" : "";
                var rows = group.Spans;

                for (var left = 0; left + 1 < rows.Count; left++)
                {
                    var current = rows[left];
                    var next = rows[left + 1];

                    // An inverted or overlapping pair already draws its period finding, so one defect draws one finding.
                    if (current.Inverted || next.Inverted || Overlap(current, next))
                    {
                        continue;
                    }

                    if (current.To is { } ends
                        && next.From is { } starts
                        && starts > ends.AddDays(1))
                    {
                        findings.Add(new Finding(
                            "reference-data-coverage-missing",
                            $"Reference data '{table.Id}' holds rows {current.Index} and {next.Index}{whose} leaving dates between '{current.ToText}' and '{next.FromText}' with no row in force.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }

                foreach (var declared in today)
                {
                    if (!IsoDate(declared, out var now))
                    {
                        continue;
                    }

                    var covered = rows.Any(span =>
                        span.From is { } starts && starts <= now && (span.To is null || now <= span.To));

                    if (!covered)
                    {
                        var held = group.Variant is { } heldBy ? $"no row of variant '{heldBy}' in force" : "no row in force";

                        findings.Add(new Finding(
                            "reference-data-coverage-missing",
                            $"Reference data '{table.Id}' holds {held} on '{declared}', the date the cast declares as today.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }
            }
        }

        RunOverlapRanges(model, findings);
    }

    /// <summary>Reports each overlapping reference-data pair with the shared range both rows hold in force, open when neither row ends it. The finding carries the period finding's code, file, line and subject, so the merge keeps the ranged text for the pair.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each ranged overlap finding this check raises.</param>
    internal static void RunOverlapRanges(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RecordFields.Rows, out var rows)
                || rows.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var spans = new List<(string From, string? To, int Index, string? Variant, string? Key, string? Role)>();
            var index = 0;

            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind == JsonValueKind.Object)
                {
                    spans.Add((Str(row, RecordFields.EffectiveFrom) ?? "", Str(row, RecordFields.EffectiveTo), index, Str(row, RecordFields.Variant), Str(row, RecordFields.Key), Str(row, RecordFields.Role)));
                }

                index++;
            }

            for (var left = 0; left < spans.Count; left++)
            {
                for (var right = left + 1; right < spans.Count; right++)
                {
                    // An inverted row already draws its own finding, so one defect draws one finding.
                    if (InvertedSpan(spans[left]) || InvertedSpan(spans[right]))
                    {
                        continue;
                    }

                    // Rows of different variants run on their own timelines.
                    if (!string.Equals(spans[left].Variant, spans[right].Variant, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Rows of different keys run on their own timelines, as do rows of
                    // different roles when rows carry no key: each key, or each role, is
                    // looked up on its own dates.
                    if (!string.Equals(RowTimeline(spans[left]), RowTimeline(spans[right]), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (OverlapSpan(spans[left], spans[right]))
                    {
                        var whose = spans[left].Variant is { } variant ? $" of variant '{variant}'" : "";

                        findings.Add(new Finding(
                            "reference-data-period-invalid",
                            $"Reference data '{table.Id}' holds rows {spans[left].Index} and {spans[right].Index}{whose} overlapping{OverlapSpanText(spans[left], spans[right])}.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }
            }
        }
    }

    /// <summary>The overlap two effective periods share as finding text: the shared range, open when neither period ends it, or no range when a start is missing.</summary>
    /// <param name="left">The first period.</param>
    /// <param name="right">The second period.</param>
    /// <returns>The overlap text opening with a space, naming the shared range.</returns>
    private static string OverlapSpanText((string From, string? To, int Index, string? Variant, string? Key, string? Role) left, (string From, string? To, int Index, string? Variant, string? Key, string? Role) right)
    {
        var start = string.Compare(left.From, right.From, StringComparison.Ordinal) >= 0 ? left.From : right.From;

        if (start.Length == 0)
        {
            return " in time";
        }

        var end = (left.To, right.To) switch
        {
            (null, null) => null,
            (null, { } only) => only,
            ({ } only, null) => only,
            ({ } first, { } second) => string.Compare(first, second, StringComparison.Ordinal) <= 0 ? first : second,
        };

        return end is null ? $" in time from '{start}' on" : $" in time from '{start}' to '{end}'";
    }

    /// <summary>Whether a period ends no later than it starts: an end date on or before its start date.</summary>
    /// <param name="span">The period.</param>
    /// <returns>True when the period carries an end no later than its start.</returns>
    private static bool InvertedSpan((string From, string? To, int Index, string? Variant, string? Key, string? Role) span) =>
        span.To is not null && string.Compare(span.To, span.From, StringComparison.Ordinal) <= 0;

    /// <summary>Whether two effective periods overlap: each starts before the other ends, with a missing end open. Dates compare as ISO text.</summary>
    /// <param name="left">The first period.</param>
    /// <param name="right">The second period.</param>
    /// <returns>True when the periods overlap.</returns>
    private static bool OverlapSpan((string From, string? To, int Index, string? Variant, string? Key, string? Role) left, (string From, string? To, int Index, string? Variant, string? Key, string? Role) right) =>
        (right.To is null || string.Compare(left.From, right.To, StringComparison.Ordinal) < 0)
        && (left.To is null || string.Compare(right.From, left.To, StringComparison.Ordinal) < 0);

    /// <summary>The timeline a row runs on: its key, or its role when the row carries no key.</summary>
    /// <param name="span">The period.</param>
    /// <returns>The row key, or the row role when the row carries no key.</returns>
    private static string? RowTimeline((string From, string? To, int Index, string? Variant, string? Key, string? Role) span) =>
        span.Key ?? span.Role;

    /// <summary>Reports amount fields one calculation rule computes whose sign conventions differ or are missing.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAmountSign(ProjectModel model, List<Finding> findings)
    {
        var rules = model.Records
            .Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "calculation")
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        var byRule = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            CheckFieldSigns(useCase, findings);

            foreach (var cited in useCase.Ids(RecordFields.ComputedBy).Order(StringComparer.Ordinal))
            {
                if (!byRule.TryGetValue(cited, out var citedBy))
                {
                    citedBy = [];
                    byRule[cited] = citedBy;
                }

                citedBy.Add(useCase);
            }
        }

        foreach (var rule in rules)
        {
            if (!byRule.TryGetValue(rule.Id, out var useCases) || useCases.Count < 2)
            {
                continue;
            }

            var voices = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);

            foreach (var useCase in useCases)
            {
                foreach (var field in AmountFieldsOf(useCase))
                {
                    if (!voices.TryGetValue(field.Name, out var holders))
                    {
                        holders = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                        voices[field.Name] = holders;
                    }

                    if (!holders.TryGetValue(useCase.Id, out var signs))
                    {
                        signs = [];
                        holders[useCase.Id] = signs;
                    }

                    if (!signs.Contains(field.Sign, StringComparer.Ordinal))
                    {
                        signs.Add(field.Sign);
                    }
                }
            }

            foreach (var name in voices.Keys.Order(StringComparer.Ordinal))
            {
                var holders = voices[name];

                if (holders.Count < 2)
                {
                    continue;
                }

                var conventions = holders.Values
                    .SelectMany(signs => signs)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                if (conventions.Count < 2)
                {
                    continue;
                }

                var stated = holders
                    .OrderBy(holder => holder.Key, StringComparer.Ordinal)
                    .Select(holder => $"'{holder.Key}' states {string.Join(" and ", holder.Value.Order(StringComparer.Ordinal))}");

                findings.Add(new Finding(
                    "amount-sign-drift",
                    $"Rule '{rule.Id}' computes '{name}' from use cases stating differing sign conventions: {string.Join("; ", stated)}.",
                    rule.File,
                    rule.Line,
                    SubjectOf(rule)));
            }
        }
    }

    /// <summary>The validity span of one reference-data row: its parsed dates with their written text, its row index and its variant.</summary>
    /// <param name="From">The first date in force, or null when the row states no parseable start.</param>
    /// <param name="To">The last date in force, or null while the row stands.</param>
    /// <param name="FromText">The written start date.</param>
    /// <param name="ToText">The written end date, or null while the row stands.</param>
    /// <param name="Inverted">Whether the row ends no later than it starts.</param>
    /// <param name="Index">The row index.</param>
    /// <param name="Variant">The variant the row belongs to, or null when it names none.</param>
    private sealed record RowSpan(DateOnly? From, DateOnly? To, string FromText, string? ToText, bool Inverted, int Index, string? Variant);

    /// <summary>One validity-span group: the spans of one variant in start-date order.</summary>
    /// <param name="Variant">The variant the spans belong to, or null for rows that name none.</param>
    /// <param name="Spans">The spans in start-date order.</param>
    private sealed record SpanGroup(string? Variant, List<RowSpan> Spans);

    /// <summary>One amount field of a use case: its name with the sign convention it states.</summary>
    /// <param name="Name">The field name.</param>
    /// <param name="Sign">The convention the field states, with a missing sign reading as none.</param>
    private sealed record AmountField(string Name, string Sign);

    /// <summary>The rows of a reference-data table: the object entries of its rows array.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The row objects in written order, or an empty list when the table holds no rows array.</returns>
    private static IReadOnlyList<JsonElement> RowsOf(Record table) => table.Objects(RecordFields.Rows);

    /// <summary>The validity spans of a table's rows, in start-date order.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The spans in start-date order, keeping each row's written index.</returns>
    private static List<RowSpan> SpansOf(Record table)
    {
        var spans = new List<RowSpan>();
        var index = 0;

        foreach (var row in RowsOf(table))
        {
            var fromText = Str(row, RecordFields.EffectiveFrom) ?? "";
            var toText = Str(row, RecordFields.EffectiveTo);
            DateOnly? from = IsoDate(fromText, out var starts) ? starts : null;
            DateOnly? to = toText is not null && IsoDate(toText, out var ends) ? ends : null;
            var inverted = toText is not null && string.Compare(toText, fromText, StringComparison.Ordinal) <= 0;
            spans.Add(new RowSpan(from, to, fromText, toText, inverted, index, Str(row, RecordFields.Variant)));
            index++;
        }

        spans.Sort((left, right) => string.Compare(left.FromText, right.FromText, StringComparison.Ordinal));

        return spans;
    }

    /// <summary>The distinct row variants of a table: the shared rows first, then each named variant in order.</summary>
    /// <param name="rows">The row objects in written order.</param>
    /// <returns>The distinct variants, with null for rows that name none.</returns>
    private static List<string?> VariantsOf(IReadOnlyList<JsonElement> rows)
    {
        var variants = new List<string?>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var shared = false;

        foreach (var row in rows)
        {
            if (Str(row, RecordFields.Variant) is { } variant)
            {
                if (seen.Add(variant))
                {
                    variants.Add(variant);
                }
            }
            else
            {
                shared = true;
            }
        }

        variants.Sort(StringComparer.Ordinal);

        return shared ? [null, .. variants] : variants;
    }

    /// <summary>The validity spans of a table grouped by variant: the shared rows first, then each named variant in order.</summary>
    /// <param name="spans">The spans in start-date order.</param>
    /// <returns>The groups, each keeping start-date order.</returns>
    private static List<SpanGroup> GroupsOf(List<RowSpan> spans)
    {
        var byVariant = new Dictionary<string, List<RowSpan>>(StringComparer.Ordinal);
        List<RowSpan>? shared = null;

        foreach (var span in spans)
        {
            if (span.Variant is { } variant)
            {
                if (!byVariant.TryGetValue(variant, out var held))
                {
                    held = [];
                    byVariant[variant] = held;
                }

                held.Add(span);
            }
            else
            {
                shared ??= [];
                shared.Add(span);
            }
        }

        var groups = new List<SpanGroup>();

        if (shared is not null)
        {
            groups.Add(new SpanGroup(null, shared));
        }

        foreach (var variant in byVariant.Keys.Order(StringComparer.Ordinal))
        {
            groups.Add(new SpanGroup(variant, byVariant[variant]));
        }

        return groups;
    }

    /// <summary>Whether two validity spans overlap: each starts before the other ends, with a missing end open. Dates compare as ISO text.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns>True when the spans overlap.</returns>
    private static bool Overlap(RowSpan left, RowSpan right) =>
        (right.ToText is null || string.Compare(left.FromText, right.ToText, StringComparison.Ordinal) < 0)
        && (left.ToText is null || string.Compare(right.FromText, left.ToText, StringComparison.Ordinal) < 0);

    /// <summary>Whether two row values are equal: strings by ordinal comparison, numbers by decimal value, and anything else by raw text.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>True when the values are equal.</returns>
    private static bool ValueEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.TryGetDecimal(out var a) && right.TryGetDecimal(out var b) && a == b,
            JsonValueKind.True or JsonValueKind.False => true,
            _ => left.GetRawText() == right.GetRawText(),
        };
    }

    /// <summary>One row value as finding text: a string in quotes, anything else as written.</summary>
    /// <param name="value">The row value.</param>
    /// <returns>The value text.</returns>
    private static string Show(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? $"'{value.GetString()}'" : value.GetRawText();

    /// <summary>Reports a row key outside kebab-case. One key repeats across disjoint spans to hold the dated history; the period check reports the spans of one key that overlap.</summary>
    /// <param name="table">The reference-data record whose row keys are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRowKeys(Record table, List<Finding> findings)
    {
        var index = 0;

        foreach (var row in RowsOf(table))
        {
            if (Str(row, RecordFields.Key) is { } key && !KebabName.IsMatch(key))
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} with key '{key}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
            }

            index++;
        }
    }

    /// <summary>Reports a cross-reference that names no reference-data row: a malformed target, or a key no row of the named table holds.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="keys">The row keys of each reference-data table, by table id.</param>
    /// <param name="table">The reference-data record holding the referring row.</param>
    /// <param name="target">The cross-reference as written.</param>
    /// <param name="index">The referring row index.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefersTo(ProjectModel model, Dictionary<string, List<string>> keys, Record table, string target, int index, List<Finding> findings)
    {
        var hash = target.IndexOf('#');
        var wellFormed = hash > 0 && hash < target.Length - 1
            && TableId.IsMatch(target[..hash])
            && KebabName.IsMatch(target[(hash + 1)..]);

        if (!wellFormed)
        {
            // A citation-shaped value that resolves nowhere belongs to the citation check, so one defect draws one finding.
            foreach (Match match in Identifiers.Citation.Matches(target))
            {
                if (!model.ById.ContainsKey(Identifiers.TrimCitation(match.Value)))
                {
                    return;
                }
            }

            findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} referring to '{target}', which names no reference-data row; write 'reference-data/<module>.<table>#<key>'.", table.File, table.Line, SubjectOf(table)));

            return;
        }

        var baseId = target[..hash];
        var key = target[(hash + 1)..];

        if (!keys.TryGetValue(baseId, out var held))
        {
            return;
        }

        if (!held.Contains(key, StringComparer.Ordinal))
        {
            findings.Add(new Finding("reference-data-key-unknown", $"Reference data '{table.Id}' holds row {index} referring to '{target}', which names a key no row of '{baseId}' holds.", table.File, table.Line, SubjectOf(table)));
        }
    }

    /// <summary>Reports a use-case field whose sign is outside the sign conventions, or that states a sign on a type that is no amount.</summary>
    /// <param name="useCase">The use-case record whose fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFieldSigns(Record useCase, List<Finding> findings)
    {
        foreach (var field in FieldEntriesOf(useCase))
        {
            if (Str(field, Sign) is not { } sign)
            {
                continue;
            }

            var name = Str(field, RecordFields.Name) ?? "";
            var type = Str(field, RecordFields.Type);

            if (type is not null && !AmountTypes.Contains(type, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' holds field '{name}' with a sign on type '{type}'; only money and decimal fields state a sign.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
            else if (!AmountSigns.Contains(sign, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' holds field '{name}' with sign '{sign}' outside money-in-positive, money-out-positive.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The amount fields of a use case: its money and decimal input and output fields with the convention each states.</summary>
    /// <param name="useCase">The use-case record whose fields are read.</param>
    /// <returns>Each amount field with its stated convention, a missing sign reading as none; a field with an unknown sign is left to the shape check.</returns>
    private static List<AmountField> AmountFieldsOf(Record useCase)
    {
        var found = new List<AmountField>();

        foreach (var field in FieldEntriesOf(useCase))
        {
            if (Str(field, RecordFields.Name) is not { Length: > 0 } name)
            {
                continue;
            }

            if (Str(field, RecordFields.Type) is not { } type || !AmountTypes.Contains(type, StringComparer.Ordinal))
            {
                continue;
            }

            var sign = Str(field, Sign);

            if (sign is not null && !AmountSigns.Contains(sign, StringComparer.Ordinal))
            {
                continue;
            }

            found.Add(new AmountField(name, sign ?? "none"));
        }

        return found;
    }

    /// <summary>The inputs and outputs field entries of a use case: its inputs list with its command outputs list or its query outputs groups.</summary>
    /// <param name="useCase">The use-case record whose fields are read.</param>
    /// <returns>The field entries in written order.</returns>
    private static List<JsonElement> FieldEntriesOf(Record useCase)
    {
        var fields = new List<JsonElement>();
        fields.AddRange(useCase.Objects(RecordFields.Inputs));

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Outputs, out var output))
        {
            return fields;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            fields.AddRange(output.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));

            return fields;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind == JsonValueKind.Object
                    && group.Value.TryGetProperty(RecordFields.Fields, out var grouped)
                    && grouped.ValueKind == JsonValueKind.Array)
                {
                    fields.AddRange(grouped.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));
                }
            }
        }

        return fields;
    }
}
