using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LiteSpec.Model.Checks;

/// <summary>Helpers the check domains share: record subjects, JSON field reads, question state, tier grading, and aggregate transition and data reads.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class FindingHelpers
{
    /// <summary>Text in backticks, which holds code rather than prose.</summary>
    internal static readonly Regex BacktickSpan = new("`[^`]*`", RegexOptions.Compiled);

    /// <summary>An ISO 8601 duration, as the failure policy delays and retention use.</summary>
    internal static readonly Regex IsoDuration = new(
        @"^P(?=.*\d)(?:\d+Y)?(?:\d+M)?(?:\d+W)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?$",
        RegexOptions.CultureInvariant);

    internal static string? SubjectOf(Record record) => record.Id.Length > 0 ? record.Id : null;

    internal static string? Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    internal static string Label(string type) => type switch
    {
        RecordTypes.UseCase => "Use case",
        RecordTypes.Reaction => "Reaction",
        RecordTypes.Job => "Job",
        RecordTypes.Notification => "Notification",
        RecordTypes.Setting => "Setting",
        _ => "Record",
    };

    /// <summary>The record types as prose names joined with commas and a final or.</summary>
    internal static string OrList(IEnumerable<string> types)
    {
        var names = types.Select(type => type.Replace('-', ' ')).ToList();

        return names.Count switch
        {
            0 => "",
            1 => names[0],
            _ => string.Join(", ", names[..^1]) + " or " + names[^1],
        };
    }

    /// <summary>
    ///     A coverage finding graded by risk: tier G when the record is tier 1,
    ///     else the code's own tier.
    /// </summary>
    /// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
    internal static Finding Tiered(string code, string message, Record record, ProjectModel model) =>
        new(code, message, record.File, record.Line, SubjectOf(record))
        {
            TierOverride = ReviewQueue.TierOf(model, record) == 1 ? "G" : null,
        };

    /// <summary>Whether the record carries any risk, which makes it tier 1.</summary>
    internal static bool HasRisk(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Risk, out var risk)
        && ((risk.ValueKind == JsonValueKind.Array && risk.GetArrayLength() > 0)
            || (risk.ValueKind == JsonValueKind.String && risk.GetString() is { Length: > 0 }));

    /// <summary>
    ///     The tier of a question entry: tier 1 when the record carrying it
    ///     carries risk, else tier 3. A question carries no tier of its own.
    /// </summary>
    internal static int TierOf(Record? parent) =>
        parent is not null && HasRisk(parent) ? 1 : 3;

    /// <summary>Whether the question entry carries a recommended answer.</summary>
    internal static bool HasRecommendation(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(RecordFields.Recommendation, out var value)
        && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
        && (value.ValueKind != JsonValueKind.String || value.GetString() is { Length: > 0 })
        && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0);

    /// <summary>The decide-by day of a question entry or a draft, when it parses.</summary>
    internal static bool DecideDay(JsonElement element, out DateOnly day)
    {
        day = default;

        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(RecordFields.DecideBy, out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() is { } text
            && ParseDay(text, out day);
    }

    internal static bool ParseDay(string text, out DateOnly day)
    {
        if (IsoDate(text, out day))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment))
        {
            day = DateOnly.FromDateTime(moment.UtcDateTime);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Whether the question entry counts as auto-approved: tier 3 with a
    ///     recommended answer, past its decide-by date plus the veto window.
    ///     Only a question auto-approves; no record does.
    /// </summary>
    internal static bool AutoApproved(JsonElement element, Record? parent, ConsumerConfig config, DateOnly today) =>
        TierOf(parent) == 3
        && HasRecommendation(element)
        && DecideDay(element, out var decideBy)
        && today > decideBy.AddDays(config.VetoWindowDays);

    /// <summary>Whether the question entry or draft stands past its decide-by date.</summary>
    internal static bool AgedPast(JsonElement element, DateOnly today) =>
        DecideDay(element, out var decideBy) && today > decideBy;

    /// <summary>The record's question entries: objects in its questions array.</summary>
    internal static IReadOnlyList<JsonElement> QuestionEntries(Record record)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Questions, out var questions)
            && questions.ValueKind == JsonValueKind.Array)
        {
            return questions.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>Whether the question entry is answered: it carries an answer or an answerer.</summary>
    internal static bool Answered(JsonElement entry) =>
        Str(entry, RecordFields.Answer) is { Length: > 0 } || Str(entry, RecordFields.AnsweredBy) is { Length: > 0 };

    internal static IReadOnlyList<JsonElement> ExceptEntries(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Except, out var except))
        {
            return [];
        }

        if (except.ValueKind == JsonValueKind.Object)
        {
            return [except];
        }

        return except.ValueKind == JsonValueKind.Array
            ? except.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList()
            : [];
    }

    /// <summary>The record's effect entries: objects in its effects array. A reaction or job entry without timing reads as async.</summary>
    internal static IReadOnlyList<JsonElement> EffectEntries(Record record)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Effects, out var effects)
            && effects.ValueKind == JsonValueKind.Array)
        {
            return effects.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>The aggregate's transitions; entries without both states are left to the shape check.</summary>
    internal static IReadOnlyList<AggregateTransition> TransitionsOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Transitions, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateTransition>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.From, out var from)
                && from.ValueKind == JsonValueKind.String
                && from.GetString() is { Length: > 0 } leave
                && entry.TryGetProperty(RecordFields.To, out var to)
                && to.ValueKind == JsonValueKind.String
                && to.GetString() is { Length: > 0 } enter)
            {
                found.Add(new AggregateTransition(
                    leave,
                    enter,
                    entry.TryGetProperty(RecordFields.By, out var by) && by.ValueKind == JsonValueKind.String ? by.GetString() : null));
            }
        }

        return found;
    }

    /// <summary>The aggregate's data fields; entries without a name and classification are left to the shape check.</summary>
    internal static IReadOnlyList<AggregateField> DataOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Data, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateField>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.Name, out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 } field
                && entry.TryGetProperty(RecordFields.Classification, out var classification)
                && classification.ValueKind == JsonValueKind.String
                && classification.GetString() is { Length: > 0 } level)
            {
                found.Add(new AggregateField(field, level));
            }
        }

        return found;
    }

    /// <summary>The screen's absolute file: screens keep the path relative to the consumer root.</summary>
    internal static string ScreenFile(ProjectModel model, string file) =>
        Path.IsPathRooted(file)
            ? file
            : Path.GetFullPath(Path.Combine(model.Config.Root, file.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Whether the claim confirms through the test body: a scenario or a rule.</summary>
    internal static bool IsBehaviorClaim(string claim) =>
        claim.StartsWith("scenario/", StringComparison.Ordinal)
        || claim.StartsWith("rule/", StringComparison.Ordinal);

    // Record dates are ISO 8601, so they parse the same under every culture.
    internal static bool IsoDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Every line's start offset, for offset-to-line reads.</summary>
    internal static int[] LineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return [.. starts];
    }

    /// <summary>The 1-based line holding the offset.</summary>
    internal static int LineOf(int[] starts, int offset)
    {
        var found = Array.BinarySearch(starts, offset);

        return found >= 0 ? found + 1 : ~found;
    }

    /// <summary>The kind name a mark finding shows: the code element in plain words.</summary>
    internal static string KindName(CodeKind kind) => kind switch
    {
        CodeKind.AggregateRoot => "Aggregate root",
        CodeKind.AggregateMethod => "Aggregate method",
        CodeKind.DomainEvent => "Domain event",
        CodeKind.RuleException => "Rule exception",
        CodeKind.Handler => "Handler",
        CodeKind.EventHandler => "Event handler",
        CodeKind.Job => "Job",
        CodeKind.Port => "Port",
        CodeKind.Adapter => "Adapter",
        CodeKind.Endpoint => "Endpoint",
        CodeKind.FrontendModule => "Frontend module",
        CodeKind.FrontendRoute => "Route entry",
        CodeKind.BrowserTest => "Browser test",
        CodeKind.Test => "Test",
        CodeKind.DomainService => "Domain service",
        CodeKind.Technical => "Technical class",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown code kind."),
    };

    /// <summary>The element a mark finding names: its type, or its type and member.</summary>
    internal static string DisplayName(MarkedElement element) =>
        element.Member is { Length: > 0 } member ? $"{element.Class}.{member}" : element.Class;

    /// <summary>The screen records of the model.</summary>
    internal static IReadOnlyList<Record> ScreenRecords(ProjectModel model) =>
        [.. model.Records.Where(record => record.Type == RecordTypes.Screen)];
}
