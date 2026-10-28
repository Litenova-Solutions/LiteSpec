using System.Text;

namespace LiteSpec.Model;

/// <summary>One counted name: a verb or a noun with its uses.</summary>
/// <param name="Name">The verb or noun.</param>
/// <param name="Count">How many use cases use it.</param>
sealed record NameCount(string Name, int Count);

/// <summary>One module's names: verbs, nouns, facts, events, codes, keys, and settings.</summary>
/// <param name="Module">The module name.</param>
/// <param name="UseCases">How many use cases the module holds.</param>
/// <param name="Verbs">The verbs in use with counts, most used first.</param>
/// <param name="Nouns">The nouns after the verb with counts, most used first.</param>
/// <param name="ReactionFacts">The facts the module's reactions answer.</param>
/// <param name="Events">The module's event ids.</param>
/// <param name="RefusalCodes">The refusal codes of the module's use cases.</param>
/// <param name="TemplateKeys">The module's notification template keys.</param>
/// <param name="Settings">The module's setting names.</param>
sealed record ModuleNames(
    string Module,
    int UseCases,
    IReadOnlyList<NameCount> Verbs,
    IReadOnlyList<NameCount> Nouns,
    IReadOnlyList<string> ReactionFacts,
    IReadOnlyList<string> Events,
    IReadOnlyList<string> RefusalCodes,
    IReadOnlyList<string> TemplateKeys,
    IReadOnlyList<string> Settings);

/// <summary>
///     The per-module names inventory behind <c>litespec model names</c>.
/// </summary>
static class ModelNames
{
    /// <summary>Builds the inventory: one entry per module, modules sorted by name.</summary>
    public static IReadOnlyList<ModuleNames> Build(ProjectModel model)
    {
        var modules = model.Records
                           .Where(record => record.Type == "module"
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
    public static string Report(ProjectModel model, string format) =>
        format == "json" ? Json(model) : Text(model);

    /// <summary>One module's names from the records the module holds.</summary>
    static ModuleNames Describe(ProjectModel model, string module, IReadOnlyList<Record> records)
    {
        var useCases = records.Where(record => record.Type == "use-case")
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
            records.Where(record => record.Type == "reaction")
                   .SelectMany(record => record.Ids("on"))
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == "event")
                   .Select(record => record.Id)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == "result" && record.Field("code") is { Length: > 0 })
                   .Select(record => record.Field("code")!)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == "notification")
                   .SelectMany(TemplateKeysOf)
                   .Where(key => key.Length > 0)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList(),
            records.Where(record => record.Type == "setting")
                   .Select(record => Identifiers.TrySplit(record.Id, out _, out _, out var name) ? name : "")
                   .Where(name => name.Length > 0)
                   .Distinct(StringComparer.Ordinal)
                   .Order(StringComparer.Ordinal)
                   .ToList());
    }

    /// <summary>A notification's template keys: its template, one or many.</summary>
    static IReadOnlyList<string> TemplateKeysOf(Record record) =>
        record.Field("template") is { Length: > 0 } key ? [key] : record.Strings("template");

    /// <summary>Counts ordered by uses, most used first, then by name.</summary>
    static IReadOnlyList<NameCount> Counted(Dictionary<string, int> counts) =>
        counts.OrderByDescending(entry => entry.Value)
              .ThenBy(entry => entry.Key, StringComparer.Ordinal)
              .Select(entry => new NameCount(entry.Key, entry.Value))
              .ToList();

    /// <summary>The module a record belongs to: its home when one, else its parent's home.</summary>
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
    static string? AggregateId(ProjectModel model, string name) =>
        model.ById.Values
             .Where(record => record.Type == "aggregate")
             .FirstOrDefault(record => Identifiers.TrySplit(record.Id, out _, out _, out var named) && named == name)
             ?.Id;

    /// <summary>The inventory with the lookalike groups as text.</summary>
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
    static string JoinCounts(IReadOnlyList<NameCount> counts) =>
        counts.Count == 0 ? "none" : string.Join(", ", counts.Select(count => $"{count.Name} {count.Count}"));

    /// <summary>Names joined with commas, or none.</summary>
    static string Join(IReadOnlyList<string> names) =>
        names.Count == 0 ? "none" : string.Join(", ", names);

    /// <summary>The inventory with the lookalike groups as JSON.</summary>
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
