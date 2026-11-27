using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior.ReferenceData;

/// <summary>The reference-data coverage checks: the rows of each variant leave no date uncovered, cover the date the cast declares as today, and no two rows of one timeline overlap without a finding naming the shared range.</summary>
/// <follows>litespec/core/domain-model#cover-every-date-in-use</follows>
internal static class ReferenceDataCoverage
{
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

    /// <summary>The validity spans of a table's rows, in start-date order.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The spans in start-date order, keeping each row's written index.</returns>
    private static List<RowSpan> SpansOf(Record table)
    {
        var spans = new List<RowSpan>();
        var index = 0;

        foreach (var row in ReferenceDataKeys.RowsOf(table))
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
}
