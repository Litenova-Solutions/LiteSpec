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

    /// <summary>The subject a finding on the record names: its identifier, or null when it holds none.</summary>
    /// <param name="record">The record the finding belongs to.</param>
    /// <returns>The record identifier, or null when the identifier is empty.</returns>
    internal static string? SubjectOf(Record record) => record.Id.Length > 0 ? record.Id : null;

    /// <summary>The string value of one property on a JSON object, read without throwing on shape drift.</summary>
    /// <param name="element">The JSON value expected to hold the property.</param>
    /// <param name="name">The property name whose string value is wanted.</param>
    /// <returns>The property string, or null when the value is no object, the property is absent, or it holds no string.</returns>
    internal static string? Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>The prose label a finding shows for a behavior record type.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label, or the generic record label for any other type.</returns>
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
    /// <param name="types">The record type identifiers to name.</param>
    /// <returns>The prose names joined with commas and a final or, or an empty string when no type is given.</returns>
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
    /// <param name="code">The finding code that sets the default tier.</param>
    /// <param name="message">The finding message shown to the reader.</param>
    /// <param name="record">The record the finding belongs to, which carries the risk that lifts the tier.</param>
    /// <param name="model">The loaded project model used to grade the record tier.</param>
    /// <returns>The finding, graded tier G when the record is tier 1.</returns>
    internal static Finding Tiered(string code, string message, Record record, ProjectModel model) =>
        new(code, message, record.File, record.Line, SubjectOf(record))
        {
            TierOverride = ReviewQueue.TierOf(model, record) == 1 ? "G" : null,
        };

    /// <summary>Whether the record carries any risk, which makes it tier 1.</summary>
    /// <param name="record">The record whose risk field is inspected.</param>
    /// <returns>True when the record names at least one risk entry or a non-empty risk text; otherwise false.</returns>
    internal static bool HasRisk(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Risk, out var risk)
        && ((risk.ValueKind == JsonValueKind.Array && risk.GetArrayLength() > 0)
            || (risk.ValueKind == JsonValueKind.String && risk.GetString() is { Length: > 0 }));

    /// <summary>
    ///     The tier of a question entry: tier 1 when the record carrying it
    ///     carries risk, else tier 3. A question carries no tier of its own.
    /// </summary>
    /// <param name="parent">The record carrying the question, or null when it stands alone.</param>
    /// <returns>Tier 1 when the carrying record carries risk; otherwise tier 3.</returns>
    internal static int TierOf(Record? parent) =>
        parent is not null && HasRisk(parent) ? 1 : 3;

    /// <summary>Whether the question entry carries a recommended answer.</summary>
    /// <param name="element">The question entry under inspection.</param>
    /// <returns>True when the entry holds a non-empty recommendation value; otherwise false.</returns>
    internal static bool HasRecommendation(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(RecordFields.Recommendation, out var value)
        && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
        && (value.ValueKind != JsonValueKind.String || value.GetString() is { Length: > 0 })
        && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0);

    /// <summary>The decide-by day of a question entry or a draft, when it parses.</summary>
    /// <param name="element">The question entry or draft whose decide-by text is read.</param>
    /// <param name="day">The parsed decide-by day, or the default date when no day parses.</param>
    /// <returns>True when a decide-by day parses from the entry; otherwise false.</returns>
    internal static bool DecideDay(JsonElement element, out DateOnly day)
    {
        day = default;

        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(RecordFields.DecideBy, out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() is { } text
            && ParseDay(text, out day);
    }

    /// <summary>Parses a day from ISO date text or a timestamp, reading timestamps as their UTC day.</summary>
    /// <param name="text">The date or timestamp text to parse.</param>
    /// <param name="day">The parsed day, or the default date when the text parses as neither form.</param>
    /// <returns>True when the text parses as a date or a timestamp; otherwise false.</returns>
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
    /// <param name="element">The question entry under inspection.</param>
    /// <param name="parent">The record carrying the question, or null when it stands alone.</param>
    /// <param name="config">The project configuration that supplies the veto window length in days.</param>
    /// <param name="today">The current date the decide-by day is aged against.</param>
    /// <returns>True when the question is tier 3, carries a recommendation, and stands past its decide-by date plus the veto window; otherwise false.</returns>
    internal static bool AutoApproved(JsonElement element, Record? parent, ConsumerConfig config, DateOnly today) =>
        TierOf(parent) == 3
        && HasRecommendation(element)
        && DecideDay(element, out var decideBy)
        && today > decideBy.AddDays(config.VetoWindowDays);

    /// <summary>Whether the question entry or draft stands past its decide-by date.</summary>
    /// <param name="element">The question entry or draft whose decide-by day is read.</param>
    /// <param name="today">The current date the decide-by day is aged against.</param>
    /// <returns>True when a decide-by day parses and stands before today; otherwise false.</returns>
    internal static bool AgedPast(JsonElement element, DateOnly today) =>
        DecideDay(element, out var decideBy) && today > decideBy;

    /// <summary>The record's question entries: objects in its questions array.</summary>
    /// <param name="record">The record whose questions array is read.</param>
    /// <returns>The question objects in array order, or an empty list when the record holds no questions array.</returns>
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
    /// <param name="entry">The question entry under inspection.</param>
    /// <returns>True when the entry names an answer or an answerer; otherwise false.</returns>
    internal static bool Answered(JsonElement entry) =>
        Str(entry, RecordFields.Answer) is { Length: > 0 } || Str(entry, RecordFields.AnsweredBy) is { Length: > 0 };

    /// <summary>The record's exception entries: its single exception object or the objects in its exception array.</summary>
    /// <param name="record">The record whose exception field is read.</param>
    /// <returns>The exception objects, or an empty list when the record holds no exception field.</returns>
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
    /// <param name="record">The record whose effects array is read.</param>
    /// <returns>The effect objects in array order, or an empty list when the record holds no effects array.</returns>
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
    /// <param name="aggregate">The aggregate record whose transitions array is read.</param>
    /// <returns>The transitions with both states named, or an empty list when the aggregate holds no transitions array.</returns>
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
    /// <param name="aggregate">The aggregate record whose data array is read.</param>
    /// <returns>The data fields with a name and a classification, or an empty list when the aggregate holds no data array.</returns>
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
    /// <param name="model">The loaded project model that supplies the consumer root.</param>
    /// <param name="file">The screen path, rooted or relative to the consumer root with forward slashes.</param>
    /// <returns>The absolute screen file path.</returns>
    internal static string ScreenFile(ProjectModel model, string file) =>
        Path.IsPathRooted(file)
            ? file
            : Path.GetFullPath(Path.Combine(model.Config.Root, file.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Whether the claim confirms through the test body: a scenario or a rule.</summary>
    /// <param name="claim">The claimed record identifier under test.</param>
    /// <returns>True when the claim names a scenario or a rule; otherwise false.</returns>
    internal static bool IsBehaviorClaim(string claim) =>
        claim.StartsWith("scenario/", StringComparison.Ordinal)
        || claim.StartsWith("rule/", StringComparison.Ordinal);

    // Record dates are ISO 8601, so they parse the same under every culture.
    /// <summary>Parses an exact ISO calendar date, independent of the current culture.</summary>
    /// <param name="text">The date text in year-month-day form.</param>
    /// <param name="date">The parsed date, or the default date when the text is no exact ISO date.</param>
    /// <returns>True when the text is an exact ISO date; otherwise false.</returns>
    internal static bool IsoDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Every line's start offset, for offset-to-line reads.</summary>
    /// <param name="text">The text whose line starts are indexed.</param>
    /// <returns>The offset of each line start, beginning with zero.</returns>
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
    /// <param name="starts">The line start offsets produced for the same text.</param>
    /// <param name="offset">The character offset whose line is wanted.</param>
    /// <returns>The one-based line number that holds the offset.</returns>
    internal static int LineOf(int[] starts, int offset)
    {
        var found = Array.BinarySearch(starts, offset);

        return found >= 0 ? found + 1 : ~found;
    }

    /// <summary>The kind name a mark finding shows: the code element in plain words.</summary>
    /// <param name="kind">The scanned code element kind.</param>
    /// <returns>The plain-words name of the kind.</returns>
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
    /// <param name="element">The marked code element whose display name is wanted.</param>
    /// <returns>The type name alone, or the type and member names joined by a dot when a member is marked.</returns>
    internal static string DisplayName(MarkedElement element) =>
        element.Member is { Length: > 0 } member ? $"{element.Class}.{member}" : element.Class;

    /// <summary>The screen records of the model.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>Each screen record in model order.</returns>
    internal static IReadOnlyList<Record> ScreenRecords(ProjectModel model) =>
        [.. model.Records.Where(record => record.Type == RecordTypes.Screen)];
}
