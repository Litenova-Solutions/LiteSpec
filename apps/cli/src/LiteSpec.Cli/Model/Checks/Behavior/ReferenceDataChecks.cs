using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The reference-data checks: every row value stays within its table's closed set, every stated value meaning names a listed value, every cross-reference names a row key its table holds, every required role is played by a row, every row states its start date, the rows leave no date uncovered, the use cases computing one value agree on its sign, and every column mapping names columns its rows hold with the rendering their values need.</summary>
/// <follows>litespec/core/domain-model#date-every-reference-data-row</follows>
/// <follows>litespec/core/domain-model#close-the-value-set-of-every-meaning-column</follows>
/// <follows>litespec/backend/knowledge#name-a-role-not-a-row</follows>
/// <follows>litespec/backend/knowledge#validate-reference-data-before-serving</follows>
/// <follows>litespec/core/domain-model#cover-every-date-in-use</follows>
/// <follows>litespec/core/domain-model#state-the-sign-of-every-amount</follows>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
internal static class ReferenceDataChecks
{
    /// <summary>Gets the name of the `requiredRoles` field.</summary>
    const string RequiredRoles = "requiredRoles";

    /// <summary>Gets the name of the `refersTo` field.</summary>
    const string RefersTo = "refersTo";

    /// <summary>Gets the name of the `sign` field.</summary>
    const string Sign = "sign";

    /// <summary>Gets the name of the `row` field.</summary>
    const string Row = "row";

    /// <summary>Gets the name of the `columns` field.</summary>
    const string Columns = "columns";

    /// <summary>Gets the name of the `column` field.</summary>
    const string Column = "column";

    /// <summary>Gets the name of the `parameter` field.</summary>
    const string Parameter = "parameter";

    /// <summary>Gets the name of the `as` field.</summary>
    const string As = "as";

    /// <summary>Gets the name of the `factory` field.</summary>
    const string Factory = "factory";

    /// <summary>Gets the name of the `literals` field.</summary>
    const string Literals = "literals";

    /// <summary>Gets the name of the `singleton` field.</summary>
    const string Singleton = "singleton";

    /// <summary>Gets the name of the `items` field.</summary>
    const string Items = "items";

    /// <summary>The sign conventions an amount field states.</summary>
    private static readonly string[] AmountSigns = ["money-in-positive", "money-out-positive"];

    /// <summary>The field types that may state a sign convention.</summary>
    private static readonly string[] AmountTypes = ["money", "decimal"];

    /// <summary>The fixed row fields a column mapping reads, beside the extra fields the rows state.</summary>
    private static readonly string[] MappingColumns =
    [
        RecordFields.Value, RecordFields.Key, RecordFields.Role, RecordFields.Variant,
        RefersTo, RecordFields.EffectiveFrom, RecordFields.EffectiveTo, RecordFields.Source,
    ];

    /// <summary>The renderings a column mapping states.</summary>
    private static readonly string[] MappingRenderings =
        ["string", "number", "boolean", "date", "money", "decimal", "key", "member", "case", "object", "map", "list"];

    /// <summary>The renderings a list entry states for its items: every column rendering but a list.</summary>
    private static readonly string[] ItemRenderings =
        ["string", "number", "boolean", "date", "money", "decimal", "key", "member", "case", "object", "map"];

    /// <summary>The static factories a typed key builds through.</summary>
    private static readonly string[] KeyFactories = ["Parse", "From", "FromCode", "Of", "Create"];

    /// <summary>A kebab-case row key or role: a lowercase letter or digit opening, then lowercase letters, digits or hyphens.</summary>
    private static readonly Regex KebabName = new("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>A camel-case row field name: a lowercase letter opening, then letters or digits.</summary>
    private static readonly Regex CamelName = new("^[a-z][A-Za-z0-9]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    /// <summary>Reports a reference-data table whose value meanings name a value outside the closed set its table lists in choices, or hold a meaning that is not a sentence.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunValueMeanings(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object
                || !table.Raw.TryGetProperty(RecordFields.ValueMeanings, out var meanings))
            {
                continue;
            }

            if (meanings.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds value meanings that are not a map.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var choices = ClosedSets.ChoiceTexts(table);

            if (choices is null)
            {
                continue;
            }

            var allowed = choices.ToHashSet(StringComparer.Ordinal);

            foreach (var entry in meanings.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (!allowed.Contains(entry.Name))
                {
                    findings.Add(new Finding(
                        "record-invalid",
                        $"Reference data '{table.Id}' holds a value meaning for '{entry.Name}', which its choices do not list.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }
                else if (entry.Value.ValueKind != JsonValueKind.String || entry.Value.GetString() is not { Length: > 0 })
                {
                    findings.Add(new Finding(
                        "record-invalid",
                        $"Reference data '{table.Id}' holds a value meaning for '{entry.Name}' that is not a sentence.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }
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

    /// <summary>Reports a reference-data row stating no start date: under the dated-row rule every row states in `effectiveFrom` the earliest date the source covers, and a row the source states with no start date is no exception.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRowStart(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var index = 0;

            foreach (var row in RowsOf(table))
            {
                if (Str(row, RecordFields.EffectiveFrom) is null)
                {
                    findings.Add(new Finding(
                        "record-invalid",
                        $"Reference data '{table.Id}' holds row {index} with no start date; state in `effectiveFrom` the earliest date the source covers.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }

                index++;
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
    }

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

    /// <summary>Reports a reference-data table whose row construction its rows cannot build: a half mapping, a table no knowledge area owns, a column no row holds, an extra field no column reads, a shared parameter, a missing model type, currency, value map or sub-columns list, a variant the map skips, a value of the wrong kind, a list entry without its item rendering, an item of the wrong kind, an item field no sub-column reads, or a singleton row with a key or a variant with more than one row.</summary>
    /// <param name="model">The loaded project model that holds every reference-data record and knowledge area.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunColumnMapping(ProjectModel model, List<Finding> findings)
    {
        foreach (var table in model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (table.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var hasRow = table.Raw.TryGetProperty(Row, out var row);
            var hasColumns = table.Raw.TryGetProperty(Columns, out var columns);
            var hasSingleton = table.Raw.TryGetProperty(Singleton, out var singleton)
                && singleton.ValueKind == JsonValueKind.True;

            if (!hasRow && !hasColumns && !hasSingleton)
            {
                continue;
            }

            if (hasRow != hasColumns)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' states `{(hasRow ? Row : Columns)}` without `{(hasRow ? Columns : Row)}`; state both together.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            if (hasSingleton && !(hasRow && hasColumns))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' states `singleton` without `row` and `columns`; state all three together.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            if (!IsKnowledgeTable(model, table))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' states row construction while no knowledge area owns it; only knowledge tables generate sets.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            CheckRowConstruction(table, row, findings);
            CheckColumnEntries(table, columns, findings);

            if (hasSingleton)
            {
                CheckSingletonRows(table, findings);
            }
        }
    }

    /// <summary>Reports each row of a singleton table stating a key, and each variant holding more than one row: a singleton holds one dated row with no key per variant.</summary>
    /// <param name="table">The reference-data record whose rows are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSingletonRows(Record table, List<Finding> findings)
    {
        var rows = RowsOf(table);
        var index = 0;

        foreach (var row in rows)
        {
            if (Str(row, RecordFields.Key) is { } key)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds row {index} with key '{key}' while the table is a singleton; remove the key, since a singleton holds one row with no key.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            index++;
        }

        var byVariant = new Dictionary<string, int>(StringComparer.Ordinal);
        var shared = 0;

        foreach (var row in rows)
        {
            if (Str(row, RecordFields.Variant) is { } variant)
            {
                byVariant[variant] = byVariant.TryGetValue(variant, out var held) ? held + 1 : 1;
            }
            else
            {
                shared++;
            }
        }

        if (shared > 1)
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds {shared} rows naming no variant; a singleton table holds one row.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }

        foreach (var variant in byVariant.Keys.Order(StringComparer.Ordinal))
        {
            if (byVariant[variant] > 1)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds {byVariant[variant]} rows of variant '{variant}'; a singleton table holds one row per variant.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }
    }

    /// <summary>Whether a knowledge area owns the table: the table identifier names a home holding a knowledge-area record.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="table">The reference-data record whose home is read.</param>
    /// <returns>True when a knowledge-area record owns the table's home.</returns>
    private static bool IsKnowledgeTable(ProjectModel model, Record table) =>
        Identifiers.TrySplit(table.Id, out _, out var home, out _)
        && home is not null
        && model.ById.ContainsKey($"{RecordTypes.KnowledgeArea}/{home}");

    /// <summary>Reports a row construction that names no qualified model row type.</summary>
    /// <param name="table">The reference-data record whose row construction is judged.</param>
    /// <param name="row">The row construction as written.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRowConstruction(Record table, JsonElement row, List<Finding> findings)
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds `row` that is not an object.", table.File, table.Line, SubjectOf(table)));
            return;
        }

        if (Str(row, RecordFields.Type) is not { } type || !IsQualified(type))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds `row` without a qualified model row type; name the row type with its namespace.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }
    }

    /// <summary>Reports each column entry that names no row field, shares its parameter, misses what its kind needs, or reads values of the wrong kind, and each extra row field no column reads.</summary>
    /// <param name="table">The reference-data record whose column entries are judged.</param>
    /// <param name="columns">The column entries as written.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckColumnEntries(Record table, JsonElement columns, List<Finding> findings)
    {
        if (columns.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds `columns` that are not a list.", table.File, table.Line, SubjectOf(table)));
            return;
        }

        var entries = columns.EnumerateArray().ToList();
        var scope = new FieldScope(RowMaps, MappingColumns, "row");
        CheckEntries(table, entries, "", scope, findings);
        CheckExtrasRead(table, entries, scope, null, findings);
        CheckRowVariants(table, findings);
    }

    /// <summary>Reports each entry of one column level that misses its shape, shares its parameter, or reads values of the wrong kind; object entries judge their sub-columns in turn, and list entries judge their items in turn.</summary>
    /// <param name="table">The reference-data record holding the column entries.</param>
    /// <param name="entries">The column entries of one level in written order.</param>
    /// <param name="prefix">The dotted index path opening each entry number, or empty at the top level.</param>
    /// <param name="scope">The field level the entries read: the rows, or the item maps of one list.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEntries(Record table, List<JsonElement> entries, string prefix, FieldScope scope, List<Finding> findings)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var at = $"entry {prefix}{index}";

            if (entry.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds column {at} that is not an object.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            var parameter = Str(entry, Parameter);
            var rendering = Str(entry, As);

            if (parameter is null || rendering is null)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds column {at} without a parameter or a rendering.", table.File, table.Line, SubjectOf(table)));
                continue;
            }

            if (!MappingRenderings.Contains(rendering, StringComparer.Ordinal))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} with rendering '{rendering}' outside string, number, boolean, date, money, decimal, key, member, case, object, map, list.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            if (seen.TryGetValue(parameter, out var first))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds parameter '{parameter}' on column entries {first} and {prefix}{index}.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
            else
            {
                seen.Add(parameter, $"{prefix}{index}");
            }

            if (rendering == "object")
            {
                CheckObjectEntry(table, entry, at, $"{prefix}{index}.", parameter, scope, findings);
                continue;
            }

            if (rendering == "list")
            {
                CheckListEntry(table, entry, at, $"{prefix}{index}.", parameter, scope, findings);
                continue;
            }

            var column = Str(entry, Column);

            if (column is null)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} without a column; name the {scope.Holder} field the parameter reads.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            if (!IsKnownField(table, column, scope))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} reading column '{column}', which no {scope.Holder} holds.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
                continue;
            }

            CheckColumnExtras(table, entry, at, rendering, findings);
            CheckColumnValues(table, entry, at, column, rendering, scope, findings);
        }
    }

    /// <summary>Reports an object entry naming a row field, missing its model type or its sub-columns, and judges the sub-columns in turn.</summary>
    /// <param name="table">The reference-data record holding the column entry.</param>
    /// <param name="entry">The object entry as written.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="prefix">The dotted index path opening each sub-column number.</param>
    /// <param name="parameter">The nested object parameter name.</param>
    /// <param name="scope">The field level the sub-columns read: the object groups its own level's fields.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckObjectEntry(Record table, JsonElement entry, string at, string prefix, string parameter, FieldScope scope, List<Finding> findings)
    {
        if (Str(entry, Column) is { } column)
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} naming column '{column}', which no object reads; move it to a sub-column.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }

        if (Str(entry, RecordFields.Type) is not { } type || !IsQualified(type))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} without a qualified model type; name the object type with its namespace.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }

        if (!entry.TryGetProperty(Columns, out var columns))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} without sub-columns; list the nested parameters building the '{parameter}' object.",
                table.File,
                table.Line,
                SubjectOf(table)));
            return;
        }

        if (columns.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds column {at} with `columns` that are not a list.", table.File, table.Line, SubjectOf(table)));
            return;
        }

        CheckEntries(table, columns.EnumerateArray().ToList(), prefix, scope, findings);
    }

    /// <summary>Reports a list entry naming no field, missing its item rendering or what its items need, reading a value that is no list, or holding an item of the wrong kind, and judges object items against the sub-columns in turn.</summary>
    /// <param name="table">The reference-data record holding the column entry.</param>
    /// <param name="entry">The list entry as written.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="prefix">The dotted index path opening each sub-column number.</param>
    /// <param name="parameter">The list parameter name.</param>
    /// <param name="scope">The field level the list field is read from.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckListEntry(Record table, JsonElement entry, string at, string prefix, string parameter, FieldScope scope, List<Finding> findings)
    {
        var column = Str(entry, Column);

        if (column is null)
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} without a column; name the {scope.Holder} field holding the list the parameter reads.",
                table.File,
                table.Line,
                SubjectOf(table)));
            return;
        }

        if (!IsKnownField(table, column, scope))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} reading column '{column}', which no {scope.Holder} holds.",
                table.File,
                table.Line,
                SubjectOf(table)));
            return;
        }

        var items = Str(entry, Items);

        if (items is null)
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} without items; name how each item renders.",
                table.File,
                table.Line,
                SubjectOf(table)));
            return;
        }

        if (!ItemRenderings.Contains(items, StringComparer.Ordinal))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} with items '{items}' outside {string.Join(", ", ItemRenderings)}.",
                table.File,
                table.Line,
                SubjectOf(table)));
            return;
        }

        if (items == "object")
        {
            if (Str(entry, RecordFields.Type) is not { } type || !IsQualified(type))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} without a qualified model type; name the item type with its namespace.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            if (!entry.TryGetProperty(Columns, out var columns))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} without sub-columns; list the nested parameters building each '{parameter}' item.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
            else if (columns.ValueKind != JsonValueKind.Array)
            {
                findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds column {at} with `columns` that are not a list.", table.File, table.Line, SubjectOf(table)));
            }
            else
            {
                var sub = columns.EnumerateArray().ToList();
                var itemScope = new FieldScope(row => ItemMaps(scope, row, column), [], "item");
                CheckEntries(table, sub, prefix, itemScope, findings);
                CheckExtrasRead(table, sub, itemScope, at, findings);
            }
        }
        else
        {
            CheckColumnExtras(table, entry, at, items, findings);
        }

        CheckListValues(table, entry, at, column, items, scope, findings);
    }

    /// <summary>Whether a column entry names a field its level holds: a fixed field, or a field at least one map of the level states.</summary>
    /// <param name="table">The reference-data record holding the rows.</param>
    /// <param name="column">The field name as written.</param>
    /// <param name="scope">The field level the entry reads.</param>
    /// <returns>True when the name is fixed or held by a map of the level; otherwise false.</returns>
    private static bool IsKnownField(Record table, string column, FieldScope scope)
    {
        if (scope.Fixed.Contains(column, StringComparer.Ordinal))
        {
            return true;
        }

        if (!CamelName.IsMatch(column))
        {
            return false;
        }

        return RowsOf(table).Any(row => scope.Maps(row).Any(map => map.Map.TryGetProperty(column, out _)));
    }

    /// <summary>Reports each extra field no column entry of the level reads: every extra row field feeds the generated rows, and every item field feeds its list.</summary>
    /// <param name="table">The reference-data record holding the rows and columns.</param>
    /// <param name="entries">The column entries of the level in written order.</param>
    /// <param name="scope">The field level the entries read.</param>
    /// <param name="owner">The owning list entry number as finding text, or null at row level.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExtrasRead(Record table, List<JsonElement> entries, FieldScope scope, string? owner, List<Finding> findings)
    {
        var read = new HashSet<string>(StringComparer.Ordinal);
        CollectColumns(entries, read);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        var rowIndex = 0;

        foreach (var row in RowsOf(table))
        {
            foreach (var (map, where) in scope.Maps(row))
            {
                foreach (var property in map.EnumerateObject())
                {
                    if (scope.Fixed.Contains(property.Name, StringComparer.Ordinal)
                        || read.Contains(property.Name))
                    {
                        continue;
                    }

                    if (owner is null)
                    {
                        if (reported.Add(property.Name))
                        {
                            findings.Add(new Finding(
                                "reference-data-column-invalid",
                                $"Reference data '{table.Id}' holds extra field '{property.Name}' no column reads; name it in a column entry or remove it.",
                                table.File,
                                table.Line,
                                SubjectOf(table)));
                        }
                    }
                    else if (reported.Add($"{owner} {property.Name}"))
                    {
                        findings.Add(new Finding(
                            "reference-data-column-invalid",
                            $"Reference data '{table.Id}' holds row {rowIndex}{where} with field '{property.Name}' no sub-column of {owner} reads; name it in a sub-column or remove it.",
                            table.File,
                            table.Line,
                            SubjectOf(table)));
                    }
                }
            }

            rowIndex++;
        }
    }

    /// <summary>Collects every row field the column entries read: each leaf column at any nesting level.</summary>
    /// <param name="entries">The column entries of one level.</param>
    /// <param name="read">The row field names, appended in place.</param>
    private static void CollectColumns(List<JsonElement> entries, HashSet<string> read)
    {
        foreach (var entry in entries)
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (Str(entry, As) == "object"
                && entry.TryGetProperty(Columns, out var columns)
                && columns.ValueKind == JsonValueKind.Array)
            {
                CollectColumns(columns.EnumerateArray().ToList(), read);
                continue;
            }

            if (Str(entry, Column) is { } column)
            {
                read.Add(column);
            }
        }
    }

    /// <summary>Reports a column entry missing the model type, currency or value map its rendering needs.</summary>
    /// <param name="table">The reference-data record holding the column entry.</param>
    /// <param name="entry">The column entry as written.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="rendering">The rendering the entry states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckColumnExtras(Record table, JsonElement entry, string at, string rendering, List<Finding> findings)
    {
        if (rendering is "money" or "key" or "member" or "case")
        {
            if (Str(entry, RecordFields.Type) is not { } type || !IsQualified(type))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} without a qualified model type; name the {rendering} type with its namespace.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }

        if (rendering == "money")
        {
            if (Str(entry, RecordFields.Currency) is not { Length: 3 } currency)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} without a three-letter currency.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
            else if (currency.Any(character => !char.IsAsciiLetterUpper(character)))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds column {at} with currency '{currency}' outside three uppercase letters.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }
        }

        if (rendering == "key"
            && Str(entry, Factory) is { } factory
            && !KeyFactories.Contains(factory, StringComparer.Ordinal))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} with key factory '{factory}' outside Parse, From, FromCode, Of, Create.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }

        if (rendering == "map"
            && (!entry.TryGetProperty(Literals, out var values)
                || values.ValueKind != JsonValueKind.Object
                || !values.EnumerateObject().Any()))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds column {at} without a value map; map each row value to its literal.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }
    }

    /// <summary>Reports each map of the level the column entry cannot read: a value of the wrong kind, or a value the map skips.</summary>
    /// <param name="table">The reference-data record holding the rows.</param>
    /// <param name="entry">The column entry as written.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="column">The field the entry reads.</param>
    /// <param name="rendering">The rendering the entry states.</param>
    /// <param name="scope">The field level the entry reads.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckColumnValues(Record table, JsonElement entry, string at, string column, string rendering, FieldScope scope, List<Finding> findings)
    {
        var mapped = rendering == "map"
            && entry.TryGetProperty(Literals, out var values)
            && values.ValueKind == JsonValueKind.Object
            ? values.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
            : null;
        var rowIndex = 0;

        foreach (var row in RowsOf(table))
        {
            foreach (var (map, where) in scope.Maps(row))
            {
                if (!map.TryGetProperty(column, out var value))
                {
                    continue;
                }

                if (!FitsRendering(value, rendering))
                {
                    findings.Add(new Finding(
                        "reference-data-column-invalid",
                        $"Reference data '{table.Id}' holds row {rowIndex}{where} with {column} {Show(value)} of the wrong kind for a {rendering} column.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }
                else if (mapped is not null
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { } text
                    && !mapped.Contains(text))
                {
                    findings.Add(new Finding(
                        "reference-data-column-invalid",
                        $"Reference data '{table.Id}' holds row {rowIndex}{where} with {column} '{text}' the value map of {at} skips.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                }
            }

            rowIndex++;
        }
    }

    /// <summary>Reports each map of the level whose list field the list entry cannot read: a value that is no list, an item of the wrong kind, or an item the map skips.</summary>
    /// <param name="table">The reference-data record holding the rows.</param>
    /// <param name="entry">The list entry as written.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="column">The field holding the list.</param>
    /// <param name="items">The rendering each item states.</param>
    /// <param name="scope">The field level the list field is read from.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckListValues(Record table, JsonElement entry, string at, string column, string items, FieldScope scope, List<Finding> findings)
    {
        var mapped = items == "map"
            && entry.TryGetProperty(Literals, out var values)
            && values.ValueKind == JsonValueKind.Object
            ? values.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
            : null;
        var rowIndex = 0;

        foreach (var row in RowsOf(table))
        {
            foreach (var (map, where) in scope.Maps(row))
            {
                if (!map.TryGetProperty(column, out var value))
                {
                    continue;
                }

                if (value.ValueKind != JsonValueKind.Array)
                {
                    findings.Add(new Finding(
                        "reference-data-column-invalid",
                        $"Reference data '{table.Id}' holds row {rowIndex}{where} with {column} {Show(value)} of the wrong kind for a list column.",
                        table.File,
                        table.Line,
                        SubjectOf(table)));
                    continue;
                }

                var itemIndex = 0;

                foreach (var item in value.EnumerateArray())
                {
                    CheckListItem(table, at, items, mapped, rowIndex, where, itemIndex, item, findings);
                    itemIndex++;
                }
            }

            rowIndex++;
        }
    }

    /// <summary>Reports one list item the list entry cannot read: an item of the wrong kind, or an item the map skips.</summary>
    /// <param name="table">The reference-data record holding the rows.</param>
    /// <param name="at">The entry number as finding text.</param>
    /// <param name="items">The rendering each item states.</param>
    /// <param name="mapped">The row values the value map holds, or null when the entry maps none.</param>
    /// <param name="rowIndex">The row index.</param>
    /// <param name="where">How the finding names the map past the row: empty at row level, or the item number within.</param>
    /// <param name="itemIndex">The item index within its list.</param>
    /// <param name="item">The item value as written.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckListItem(Record table, string at, string items, HashSet<string>? mapped, int rowIndex, string where, int itemIndex, JsonElement item, List<Finding> findings)
    {
        if (items == "object")
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds row {rowIndex}{where} item {itemIndex} with {Show(item)} of the wrong kind for an object item.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            return;
        }

        if (!FitsRendering(item, items))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds row {rowIndex}{where} item {itemIndex} with {Show(item)} of the wrong kind for a {items} item.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }
        else if (mapped is not null
            && item.ValueKind == JsonValueKind.String
            && item.GetString() is { } text
            && !mapped.Contains(text))
        {
            findings.Add(new Finding(
                "reference-data-column-invalid",
                $"Reference data '{table.Id}' holds row {rowIndex}{where} item {itemIndex} with '{text}' the value map of {at} skips.",
                table.File,
                table.Line,
                SubjectOf(table)));
        }
    }

    /// <summary>One field level the column entries read: the rows of the table, or the item maps of one list.</summary>
    /// <param name="Maps">The maps one row yields with how a finding names each past the row: the row with an empty name, or each item map with its item number.</param>
    /// <param name="Fixed">The fixed field names the extra check skips: the row fields at row level, none at item level.</param>
    /// <param name="Holder">How a finding names what holds a field: row at row level, item at item level.</param>
    private sealed record FieldScope(
        Func<JsonElement, IReadOnlyList<(JsonElement Map, string Where)>> Maps,
        string[] Fixed,
        string Holder);

    /// <summary>The maps one row yields at row level: the row itself with an empty name.</summary>
    /// <param name="row">The row object as written.</param>
    /// <returns>The row with an empty name.</returns>
    private static IReadOnlyList<(JsonElement Map, string Where)> RowMaps(JsonElement row) => [(row, "")];

    /// <summary>The item maps one row yields for one list field: each object item of the lists the level holds, with its item number past the map name.</summary>
    /// <param name="outer">The field level the list field is read from.</param>
    /// <param name="row">The row object as written.</param>
    /// <param name="column">The field holding the list.</param>
    /// <returns>The object items with their names, in list order; items of the wrong kind are left to the value check.</returns>
    private static List<(JsonElement Map, string Where)> ItemMaps(FieldScope outer, JsonElement row, string column)
    {
        var found = new List<(JsonElement Map, string Where)>();

        foreach (var (map, where) in outer.Maps(row))
        {
            if (!map.TryGetProperty(column, out var list) || list.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var itemIndex = 0;

            foreach (var item in list.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    found.Add((item, $"{where} item {itemIndex}"));
                }

                itemIndex++;
            }
        }

        return found;
    }

    /// <summary>Reports each row naming no variant in a table that varies: a set builds from one variant, so every row of a generated table names its variant.</summary>
    /// <param name="table">The reference-data record whose rows are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRowVariants(Record table, List<Finding> findings)
    {
        if (table.Raw.ValueKind != JsonValueKind.Object
            || !table.Raw.TryGetProperty(RecordFields.Variation, out var variation)
            || variation.ValueKind != JsonValueKind.String
            || variation.GetString() is not { } dimension)
        {
            return;
        }

        var rowIndex = 0;

        foreach (var row in RowsOf(table))
        {
            if (!row.TryGetProperty(RecordFields.Variant, out _))
            {
                findings.Add(new Finding(
                    "reference-data-column-invalid",
                    $"Reference data '{table.Id}' holds row {rowIndex} naming no variant while the table varies by '{dimension}'; name the variant of each row.",
                    table.File,
                    table.Line,
                    SubjectOf(table)));
            }

            rowIndex++;
        }
    }

    /// <summary>Whether the value renders under the rendering: numbers for number, money and decimal, a boolean for boolean, and a string for the rest.</summary>
    /// <param name="value">The row value as written.</param>
    /// <param name="rendering">The rendering the entry states.</param>
    /// <returns>True when the value fits the rendering.</returns>
    private static bool FitsRendering(JsonElement value, string rendering) => rendering switch
    {
        "money" or "number" or "decimal" => value.ValueKind == JsonValueKind.Number,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        _ => value.ValueKind == JsonValueKind.String,
    };

    /// <summary>Whether the type name carries its namespace: two or more dot-separated segments.</summary>
    /// <param name="type">The type name as written.</param>
    /// <returns>True when the name holds a namespace.</returns>
    private static bool IsQualified(string type)
    {
        var parts = type.Split('.');

        return parts.Length > 1 && parts.All(part => part.Length > 0);
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

    /// <summary>Reports a row key outside kebab-case and a key two rows of one table share.</summary>
    /// <param name="table">The reference-data record whose row keys are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRowKeys(Record table, List<Finding> findings)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;

        foreach (var row in RowsOf(table))
        {
            if (Str(row, RecordFields.Key) is { } key)
            {
                if (!KebabName.IsMatch(key))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds row {index} with key '{key}' outside lowercase kebab-case.", table.File, table.Line, SubjectOf(table)));
                }
                else if (seen.TryGetValue(key, out var first))
                {
                    findings.Add(new Finding("record-invalid", $"Reference data '{table.Id}' holds key '{key}' on rows {first} and {index}.", table.File, table.Line, SubjectOf(table)));
                }
                else
                {
                    seen.Add(key, index);
                }
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

    /// <summary>The input and output field entries of a use case: its input list with its command output list or its query output groups.</summary>
    /// <param name="useCase">The use-case record whose fields are read.</param>
    /// <returns>The field entries in written order.</returns>
    private static List<JsonElement> FieldEntriesOf(Record useCase)
    {
        var fields = new List<JsonElement>();
        fields.AddRange(useCase.Objects(RecordFields.Input));

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Output, out var output))
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
