using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     The per-module names inventory behind <c>litespec model names</c>.
/// </summary>
/// <follows>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</follows>
static class ModelNames
{
    /// <summary>Builds the inventory: one entry per module, modules sorted by name.</summary>
    /// <param name="model">The built project model holding the module records.</param>
    /// <returns>One names entry per module, in module name order.</returns>
    public static IReadOnlyList<ModuleNames> Build(ProjectModel model)
    {
        var modules = model.Records
                           .Where(record => record.Type == RecordTypes.Module
                               && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                               && name.Length > 0)
                           .Select(record => { Identifiers.TrySplit(record.Id, out _, out _, out var name); return name; })
                           .Distinct(StringComparer.Ordinal)
                           .Order(StringComparer.Ordinal)
                           .ToList();
        var byModule = model.Records
                            .Select(record => (Record: record, Module: ModuleOf(model, record)))
                            .Where(entry => entry.Module is not null)
                            .GroupBy(entry => entry.Module!, StringComparer.Ordinal)
                            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Record).ToList(), StringComparer.Ordinal);

        return modules.Select(module => Describe(model, module, byModule.GetValueOrDefault(module, []))).ToList();
    }

    /// <summary>Prints the inventory with the lookalike groups, as text or JSON.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="format">The output format: json for JSON, anything else for text.</param>
    /// <returns>The inventory in the requested format.</returns>
    public static string Report(ProjectModel model, string format) =>
        format == "json" ? Json(model) : Text(model);

    /// <summary>One module's names from the records the module holds.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="module">The module name to describe.</param>
    /// <param name="records">The records the module holds.</param>
    /// <returns>The names inventory entry for the module.</returns>
    static ModuleNames Describe(ProjectModel model, string module, IReadOnlyList<Record> records)
    {
        var useCases = records.Where(record => record.Type == RecordTypes.UseCase)
                              .OrderBy(record => record.Id, StringComparer.Ordinal)
                              .ToList();
        var verbs = new Dictionary<string, int>(StringComparer.Ordinal);
        var nouns = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var useCase in useCases)
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);
            verbs[verb] = verbs.GetValueOrDefault(verb) + 1;

            if (name.Length > verb.Length)
            {
                var noun = name[(verb.Length + 1)..];
                nouns[noun] = nouns.GetValueOrDefault(noun) + 1;
            }
        }

        return new ModuleNames(
            module,
            useCases.Count,
            Counted(verbs),
            Counted(nouns),
            records.Where(record => record.Type == RecordTypes.Reaction)
                   .SelectMany(record => record.Ids(RecordFields.On))
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == RecordTypes.Event)
                   .Select(record => record.Id)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == RecordTypes.Result && record.Field(RecordFields.Code) is { Length: > 0 })
                   .Select(record => record.Field(RecordFields.Code)!)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == RecordTypes.Notification)
                   .SelectMany(TemplateKeysOf)
                   .Where(key => key.Length > 0)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == RecordTypes.Setting)
                   .Select(record => Identifiers.TrySplit(record.Id, out _, out _, out var name) ? name : "")
                   .Where(name => name.Length > 0)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList());
    }

    /// <summary>A notification's template keys: its template, one or many.</summary>
    /// <param name="record">The notification record to read.</param>
    /// <returns>The single template key, or the template key array the record holds.</returns>
    static IReadOnlyList<string> TemplateKeysOf(Record record) =>
        record.Field(RecordFields.Template) is { Length: > 0 } key ? [key] : record.Strings(RecordFields.Template);

    /// <summary>Counts ordered by uses, most used first, then by name.</summary>
    /// <param name="counts">The use count for each name.</param>
    /// <returns>The counted names, most used first, then in name order.</returns>
    static List<NameCount> Counted(Dictionary<string, int> counts) =>
        counts.OrderByDescending(entry => entry.Value)
              .ThenBy(entry => entry.Key, StringComparer.Ordinal)
              .Select(entry => new NameCount(entry.Key, entry.Value))
              .ToList();

    /// <summary>The module a record belongs to: its home when one, else its parent's home.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="record">The record whose module is wanted.</param>
    /// <returns>The module name, or null when no module holds the record.</returns>
    static string? ModuleOf(ProjectModel model, Record record)
    {
        if (!Identifiers.TrySplit(record.Id, out _, out var home, out var name) || home is null)
        {
            return null;
        }

        if (model.ById.ContainsKey($"module/{home}"))
        {
            return home;
        }

        // A result or scenario home names a use case, and an event or rule home an aggregate.
        var parent = ModelBuilder.UseCaseOf(model, home)?.Id ?? AggregateId(model, home);

        return parent is not null && Identifiers.TrySplit(parent, out _, out var parentHome, out _)
            ? parentHome
            : null;
    }

    /// <summary>The aggregate id whose name the home carries, or null when none holds it.</summary>
    /// <param name="model">The built project model holding the aggregates.</param>
    /// <param name="name">The aggregate name to look up.</param>
    /// <returns>The aggregate id, or null when no aggregate carries the name.</returns>
    static string? AggregateId(ProjectModel model, string name) =>
        model.ById.Values
             .Where(record => record.Type == RecordTypes.Aggregate)
             .FirstOrDefault(record => Identifiers.TrySplit(record.Id, out _, out _, out var named) && named == name)
             ?.Id;

    /// <summary>The inventory with the lookalike groups as text.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <returns>The inventory as text lines, with no trailing newline.</returns>
    static string Text(ProjectModel model)
    {
        var built = new StringBuilder();

        foreach (var module in Build(model))
        {
            if (built.Length > 0)
            {
                built.Append('\n');
            }

            built.Append("Module ").Append(module.Module).Append(" (")
                 .Append(module.UseCases == 1 ? "1 use case" : $"{module.UseCases} use cases").Append("):\n");
            built.Append("  Verbs: ").Append(JoinCounts(module.Verbs)).Append('\n');
            built.Append("  Nouns: ").Append(JoinCounts(module.Nouns)).Append('\n');
            built.Append("  Reaction facts: ").Append(Join(module.ReactionFacts)).Append('\n');
            built.Append("  Events: ").Append(Join(module.Events)).Append('\n');
            built.Append("  Refusal codes: ").Append(Join(module.RefusalCodes)).Append('\n');
            built.Append("  Template keys: ").Append(Join(module.TemplateKeys)).Append('\n');
            built.Append("  Settings: ").Append(Join(module.Settings)).Append('\n');
        }

        if (built.Length > 0)
        {
            built.Append('\n');
        }

        built.Append("Lookalikes:\n");

        var pairs = TermLookalikes.FindPairs(model);

        if (pairs.Count == 0)
        {
            built.Append("  none\n");
        }

        foreach (var pair in pairs)
        {
            built.Append("  ").Append(pair.First.Form).Append(" (").Append(pair.First.Seen).Append(") / ")
                 .Append(pair.Second.Form).Append(" (").Append(pair.Second.Seen).Append("): ")
                 .Append(pair.Kind).Append('\n');
        }

        return built.ToString().TrimEnd('\n');
    }

    /// <summary>Counted names as "name count" pairs, or none.</summary>
    /// <param name="counts">The counted names to join.</param>
    /// <returns>The comma-joined name and count pairs, or none when the list is empty.</returns>
    static string JoinCounts(IReadOnlyList<NameCount> counts) =>
        counts.Count == 0 ? "none" : string.Join(", ", counts.Select(count => $"{count.Name} {count.Count}"));

    /// <summary>Names joined with commas, or none.</summary>
    /// <param name="names">The names to join.</param>
    /// <returns>The comma-joined names, or none when the list is empty.</returns>
    static string Join(IReadOnlyList<string> names) =>
        names.Count == 0 ? "none" : string.Join(", ", names);

    /// <summary>The inventory with the lookalike groups as JSON.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <returns>The inventory serialized as JSON.</returns>
    static string Json(ProjectModel model) =>
        CliJson.Serialize(new
        {
            modules = Build(model).Select(module => new
            {
                module = module.Module,
                useCases = module.UseCases,
                verbs = module.Verbs.Select(count => new { verb = count.Name, count = count.Count }).ToList(),
                nouns = module.Nouns.Select(count => new { noun = count.Name, count = count.Count }).ToList(),
                reactionFacts = module.ReactionFacts,
                events = module.Events,
                refusalCodes = module.RefusalCodes,
                templateKeys = module.TemplateKeys,
                settings = module.Settings,
            }).ToList(),
            lookalikes = TermLookalikes.FindPairs(model).Select(pair => new
            {
                first = pair.First.Form,
                second = pair.Second.Form,
                kind = pair.Kind,
                firstSeen = pair.First.Seen,
                secondSeen = pair.Second.Seen,
            }).ToList(),
        });
}
