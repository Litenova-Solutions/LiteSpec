using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The screen shows checks: every field a screen names in `fieldSources` is a field the use case
///     it names in `from` outputs or accepts, and the screen offers that use case.
/// </summary>
/// <follows>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</follows>
internal static class ScreenShowsChecks
{
    /// <summary>The field holding the use-case fields a screen shows.</summary>
    private const string ShowsField = "fieldSources";

    /// <summary>Reports screen-shows-unknown, screen-shows-drift, reference-unknown and record-invalid for `fieldSources` entries that resolve to no offered use-case field.</summary>
    /// <param name="model">The loaded project model that holds every screen and use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var screen in ScreenRecords(model).OrderBy(screen => screen.Id, StringComparer.Ordinal))
        {
            CheckShows(model, screen, findings);
        }
    }

    /// <summary>Reports each `fieldSources` entry whose field the named use case neither outputs nor accepts, or whose use case the screen does not offer.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="screen">The screen record whose `fieldSources` entries are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckShows(ProjectModel model, Record screen, List<Finding> findings)
    {
        if (screen.Raw.ValueKind != JsonValueKind.Object
            || !screen.Raw.TryGetProperty(ShowsField, out var shows)
            || shows.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var offers = screen.Ids(RecordFields.Offers);

        foreach (var entry in shows.EnumerateArray())
        {
            if (Str(entry, RecordFields.Field) is not { Length: > 0 } field
                || Str(entry, RecordFields.From) is not { Length: > 0 } from)
            {
                continue;
            }

            if (!model.ById.TryGetValue(from, out var useCase))
            {
                findings.Add(new Finding(
                    "reference-unknown",
                    $"Screen '{screen.Id}' shows field '{field}' from '{from}', which no record holds.",
                    screen.File, screen.Line, SubjectOf(screen)));
                continue;
            }

            if (useCase.Type != RecordTypes.UseCase)
            {
                findings.Add(new Finding(
                    "record-invalid",
                    $"Screen '{screen.Id}' shows field '{field}' from '{from}', which is a {useCase.Type}.",
                    screen.File, screen.Line, SubjectOf(screen)));
                continue;
            }

            if (!offers.Contains(from, StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "screen-shows-unknown",
                    $"Screen '{screen.Id}' shows field '{field}' from '{from}', which the screen does not offer.",
                    screen.File, screen.Line, SubjectOf(screen)));
                continue;
            }

            if (FieldNamesOf(useCase).Contains(field))
            {
                continue;
            }

            if (OfferedUseCaseWithField(model, offers, from, field) is { } other)
            {
                findings.Add(new Finding(
                    "screen-shows-drift",
                    $"Screen '{screen.Id}' shows field '{field}' from '{from}', which neither outputs nor accepts it; '{other}' does.",
                    screen.File, screen.Line, SubjectOf(screen)));
                continue;
            }

            if (ClosestNamedField(model, offers, from, field) is { } near)
            {
                findings.Add(new Finding(
                    "screen-shows-drift",
                    $"Screen '{screen.Id}' shows field '{field}' from '{from}', which matches '{near.Field}' of '{near.UseCase}' only by name.",
                    screen.File, screen.Line, SubjectOf(screen)));
                continue;
            }

            findings.Add(new Finding(
                "screen-shows-unknown",
                $"Screen '{screen.Id}' shows field '{field}' from '{from}', which neither outputs nor accepts it, and no use case the screen offers does.",
                screen.File, screen.Line, SubjectOf(screen)));
        }
    }

    /// <summary>The offered use case, other than the named one, that outputs or accepts the field, in offers order.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="offers">The use case identifiers the screen offers, in file order.</param>
    /// <param name="from">The use case identifier the `fieldSources` entry names.</param>
    /// <param name="field">The field name the `fieldSources` entry names.</param>
    /// <returns>The first other offered use case carrying the field, or null when none does.</returns>
    private static string? OfferedUseCaseWithField(ProjectModel model, IReadOnlyList<string> offers, string from, string field)
    {
        foreach (var id in offers)
        {
            if (string.Equals(id, from, StringComparison.Ordinal)
                || !model.ById.TryGetValue(id, out var offered)
                || offered.Type != RecordTypes.UseCase)
            {
                continue;
            }

            if (FieldNamesOf(offered).Contains(field))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>The use-case field the named field matches only when case and separators are ignored, looking at the named use case first and then at each offered use case in offers order.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="offers">The use case identifiers the screen offers, in file order.</param>
    /// <param name="from">The use case identifier the `fieldSources` entry names.</param>
    /// <param name="field">The field name the `fieldSources` entry names.</param>
    /// <returns>The use case and field of the first name-only match, or null when nothing matches.</returns>
    private static (string UseCase, string Field)? ClosestNamedField(ProjectModel model, IReadOnlyList<string> offers, string from, string field)
    {
        var want = NormalizedName(field);

        if (model.ById.TryGetValue(from, out var home))
        {
            foreach (var candidate in FieldNamesOf(home).Order(StringComparer.Ordinal))
            {
                if (string.Equals(NormalizedName(candidate), want, StringComparison.Ordinal))
                {
                    return (from, candidate);
                }
            }
        }

        foreach (var id in offers)
        {
            if (string.Equals(id, from, StringComparison.Ordinal)
                || !model.ById.TryGetValue(id, out var offered)
                || offered.Type != RecordTypes.UseCase)
            {
                continue;
            }

            foreach (var candidate in FieldNamesOf(offered).Order(StringComparer.Ordinal))
            {
                if (string.Equals(NormalizedName(candidate), want, StringComparison.Ordinal))
                {
                    return (id, candidate);
                }
            }
        }

        return null;
    }

    /// <summary>The field names a use case outputs or accepts: its input names, its output names, and the row names under each list shape.</summary>
    /// <param name="useCase">The use case record whose inputs and outputs are read.</param>
    /// <returns>Each input and output field name, with row names, without duplicates.</returns>
    private static HashSet<string> FieldNamesOf(Record useCase)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (useCase.Raw.ValueKind != JsonValueKind.Object)
        {
            return names;
        }

        if (useCase.Raw.TryGetProperty(RecordFields.Inputs, out var inputs) && inputs.ValueKind == JsonValueKind.Array)
        {
            foreach (var input in inputs.EnumerateArray())
            {
                AddFieldAndRows(input, names);
            }
        }

        if (!useCase.Raw.TryGetProperty(RecordFields.Outputs, out var output))
        {
            return names;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in output.EnumerateArray())
            {
                AddFieldAndRows(field, names);
            }
        }
        else if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind == JsonValueKind.Object
                    && group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                    && fields.ValueKind == JsonValueKind.Array)
                {
                    foreach (var field in fields.EnumerateArray())
                    {
                        AddFieldAndRows(field, names);
                    }
                }
            }
        }

        return names;
    }

    /// <summary>Adds one contract field's name and the row names under its list shape, when it carries them.</summary>
    /// <param name="field">The input or output field object whose names are read.</param>
    /// <param name="names">The names collected so far, appended in place.</param>
    private static void AddFieldAndRows(JsonElement field, HashSet<string> names)
    {
        if (Str(field, RecordFields.Name) is { Length: > 0 } name)
        {
            names.Add(name);
        }

        if (field.ValueKind == JsonValueKind.Object
            && field.TryGetProperty(RecordFields.Items, out var items)
            && items.ValueKind == JsonValueKind.Object
            && items.TryGetProperty(RecordFields.Fields, out var leaves)
            && leaves.ValueKind == JsonValueKind.Array)
        {
            foreach (var leaf in leaves.EnumerateArray())
            {
                if (Str(leaf, RecordFields.Name) is { Length: > 0 } leafName)
                {
                    names.Add(leafName);
                }
            }
        }
    }

    /// <summary>Normalizes a field name for comparison: lowercase, with separators removed.</summary>
    /// <param name="name">The field name to normalize.</param>
    /// <returns>The lowercase name without underscores, dashes or spaces.</returns>
    private static string NormalizedName(string name) =>
        name.ToLowerInvariant().Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
}
