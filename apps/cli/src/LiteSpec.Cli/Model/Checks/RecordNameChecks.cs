using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The name-shape checks on records: use-case nouns, refusal codes, template keys, setting units and published event names.</summary>
/// <follows>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</follows>
/// <follows>litespec/core/use-cases#keep-one-kind-per-verb</follows>
/// <follows>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</follows>
/// <follows>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</follows>
/// <follows>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</follows>
/// <follows>litespec/core/automation#name-each-reaction-for-its-act-and-its-trigger</follows>
/// <follows>litespec/core/automation#name-each-job-for-the-use-case-it-runs</follows>
/// <follows>litespec/core/edges#name-published-events-in-the-domains-words</follows>
/// <follows>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</follows>
internal static class RecordNameChecks
{
    /// <summary>Generic refusal prefixes every project shares, beside its own declared ones.</summary>
    private static readonly HashSet<string> GenericRefusalPrefixes = new(StringComparer.Ordinal)
    {
        "request", "authorization", "client",
    };

    /// <summary>Vague words a refusal code never carries as a whole word.</summary>
    private static readonly HashSet<string> RefusalBannedWords = new(StringComparer.Ordinal)
    {
        "invalid", "failed", "failure", "error", "bad", "wrong", "problem",
    };

    /// <summary>Audience words a template key may end with whatever the record declares.</summary>
    private static readonly HashSet<string> TemplateAudienceAliases = new(StringComparer.Ordinal)
    {
        "customer", "member", "owner", "team", "staff", "approver", "requester", "reader",
    };

    /// <summary>Setting value types that measure a quantity and need a unit.</summary>
    private static readonly HashSet<string> QuantityValueTypes = new(StringComparer.Ordinal)
    {
        "number", "integer", "decimal", "duration",
    };

    /// <summary>Internal machinery words an outbound integration event never carries.</summary>
    private static readonly HashSet<string> InternalEventWords = new(StringComparer.Ordinal)
    {
        "staged", "dispatch", "dispatched", "outbox", "handler", "queued", "worker", "job",
    };

    /// <summary>Reports use-case-noun-unknown, refusal-code-invalid, template-key-invalid, setting-unit-missing, integration-event-name-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckUseCaseNoun(model, findings);
        CheckRefusalCodeForm(model, findings);
        CheckTemplateKeyForm(model, findings);
        CheckSettingUnit(model, findings);
        CheckIntegrationEventWording(model, findings);
    }

    /// <summary>Reports a use case naming no aggregate, declared part, or glossary term after its verb.</summary>
    /// <param name="model">The loaded project model that holds every aggregate, term and use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseNoun(ProjectModel model, List<Finding> findings)
    {
        var aggregates = model.Records.Where(record => record.Type == RecordTypes.Aggregate).ToList();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var partsByModule = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var aggregate in aggregates)
        {
            if (!Identifiers.TrySplit(aggregate.Id, out _, out var home, out var name) || name is null)
            {
                continue;
            }

            names.Add(name);

            if (home is { Length: > 0 })
            {
                foreach (var part in aggregate.Strings(RecordFields.Parts))
                {
                    if (!partsByModule.TryGetValue(home, out var parts))
                    {
                        parts = new HashSet<string>(StringComparer.Ordinal);
                        partsByModule[home] = parts;
                    }

                    parts.Add(part);
                }
            }
        }

        var terms = new HashSet<string>(StringComparer.Ordinal);

        foreach (var term in model.Records.Where(record => record.Type == RecordTypes.Term))
        {
            if (Identifiers.TrySplit(term.Id, out _, out _, out var idName) && idName is { Length: > 0 })
            {
                terms.Add(idName);
            }

            if (term.Field("term") is { Length: > 0 } text)
            {
                terms.Add(NormalizeTerm(text));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out var home, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);
            var rest = name.Length == verb.Length ? "" : name[(verb.Length + 1)..];
            var words = rest.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var known = false;

            // The longest run wins, so a two-word part beats either word alone.
            for (var length = words.Length; length >= 1 && !known; length--)
            {
                for (var start = 0; start + length <= words.Length && !known; start++)
                {
                    var run = string.Join("-", words, start, length);

                    known = names.Any(aggregate => NounMatches(aggregate, run))
                        || terms.Any(term => NounMatches(term, run))
                        || (home is not null
                            && partsByModule.TryGetValue(home, out var parts)
                            && parts.Contains(run));
                }
            }

            if (!known)
            {
                findings.Add(new Finding("use-case-noun-unknown", $"Use case '{useCase.Id}' names no aggregate, declared part, or glossary term after its verb.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Whether the word run names the aggregate in its singular or plural form.</summary>
    /// <param name="aggregate">The aggregate or glossary term name in its declared form.</param>
    /// <param name="run">The run of words taken from the use case name after its verb.</param>
    /// <returns>True when the run equals the name or its regular plural in either direction; otherwise false.</returns>
    private static bool NounMatches(string aggregate, string run) =>
        run.Equals(aggregate, StringComparison.Ordinal)
        || PluralForms(aggregate).Contains(run, StringComparer.Ordinal)
        || PluralForms(run).Contains(aggregate, StringComparer.Ordinal);

    /// <summary>The regular plural forms of one word.</summary>
    /// <param name="word">The singular word whose plural forms are wanted.</param>
    /// <returns>The regular plural spellings, including the y-to-ies form where it applies.</returns>
    private static IEnumerable<string> PluralForms(string word)
    {
        yield return word + "s";
        yield return word + "es";

        if (word.Length > 1 && word.EndsWith('y') && !"aeiou".Contains(word[^2]))
        {
            yield return word[..^1] + "ies";
        }
    }

    /// <summary>A glossary term as kebab-case, so a phrase matches the words of a name.</summary>
    /// <param name="text">The glossary term text as written on the term record.</param>
    /// <returns>The term in lowercase kebab-case.</returns>
    private static string NormalizeTerm(string text)
    {
        var words = text.Split([' ', '\t', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join("-", words).ToLowerInvariant();
    }

    /// <summary>Reports a refusal code outside its module, outside kebab-case, or carrying a vague word.</summary>
    /// <param name="model">The loaded project model that holds every result record and the refusal configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusalCodeForm(ProjectModel model, List<Finding> findings)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal)
        {
            model.Config.ForbiddenCode,
            model.Config.InvalidInputCode,
        };

        foreach (var code in model.Config.RefusalStatuses.Keys)
        {
            declared.Add(code);
        }

        foreach (var code in model.Config.RefusalAliases.Values)
        {
            declared.Add(code);
        }

        var generic = new HashSet<string>(GenericRefusalPrefixes, StringComparer.Ordinal);

        foreach (var prefix in model.Config.RefusalPrefixes.Concat(model.Config.RefusalComposed.Select(RefusalTemplate.PrefixOf)))
        {
            generic.Add(prefix);
        }

        // A refusal names the module that owns the refused state, which is often not the module of
        // the use case refusing: an admission use case answers events.not-found for a missing event.
        var modules = model.Records
                           .Where(record => record.Type == RecordTypes.Module)
                           .Select(record => Identifiers.TrySplit(record.Id, out _, out _, out var name) ? name : null)
                           .OfType<string>()
                           .ToHashSet(StringComparer.Ordinal);

        foreach (var code in declared)
        {
            var separator = code.IndexOf('.');

            if (separator > 0)
            {
                generic.Add(code[..separator]);
            }
        }

        foreach (var result in model.Records.Where(record => record.Type == RecordTypes.Result && record.Field(RecordFields.Outcome) == "refusal"))
        {
            var code = result.Field(RecordFields.Code);

            if (code is not { Length: > 0 } || declared.Contains(code))
            {
                continue;
            }

            if (ResultModule(model, result) is not { } module)
            {
                continue;
            }

            var dot = code.IndexOf('.');
            var prefix = dot < 0 ? code : code[..dot];
            var rest = dot < 0 ? "" : code[(dot + 1)..];

            if (dot < 0 || (!prefix.Equals(module, StringComparison.Ordinal) && !modules.Contains(prefix) && !generic.Contains(prefix)))
            {
                findings.Add(new Finding("refusal-code-invalid", $"Result '{result.Id}' has code '{code}', which names no module and no generic prefix.", result.File, result.Line, SubjectOf(result)));
            }

            if (!IsKebab(rest))
            {
                findings.Add(new Finding("refusal-code-invalid", $"Result '{result.Id}' has code '{code}', which is not kebab-case after its prefix.", result.File, result.Line, SubjectOf(result)));
            }

            var vague = rest.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => RefusalBannedWords.Contains(word));

            if (vague is not null)
            {
                findings.Add(new Finding("refusal-code-invalid", $"Result '{result.Id}' has code '{code}', which uses the vague word '{vague}' instead of naming the state.", result.File, result.Line, SubjectOf(result)));
            }
        }
    }

    /// <summary>The module holding a result: its page's module, else its use case's module.</summary>
    /// <param name="model">The loaded project model that resolves the result use case.</param>
    /// <param name="result">The refusal result whose owning module is wanted.</param>
    /// <returns>The module name, or null when neither the page nor the use case names one.</returns>
    private static string? ResultModule(ProjectModel model, Record result)
    {
        if (result.PageId is { Length: > 0 } page
            && Identifiers.TrySplit(page, out var prefix, out var pageHome, out _)
            && prefix == "use-case"
            && pageHome is { Length: > 0 })
        {
            return pageHome;
        }

        if (Identifiers.TrySplit(result.Id, out _, out var home, out _)
            && home is { Length: > 0 }
            && ModelBuilder.UseCaseOf(model, home) is { } useCase
            && Identifiers.TrySplit(useCase.Id, out _, out var module, out _))
        {
            return module;
        }

        return null;
    }

    /// <summary>Whether the name is kebab-case: lowercase words joined by single hyphens.</summary>
    /// <param name="value">The code suffix under test.</param>
    /// <returns>True when the value is non-empty lowercase words joined by single hyphens; otherwise false.</returns>
    private static bool IsKebab(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        return value.Split('-').All(word =>
            word.Length > 0 && word.All(character => char.IsAsciiLetterLower(character) || char.IsDigit(character)));
    }

    /// <summary>Reports a notification template key ending in no audience the record declares.</summary>
    /// <param name="model">The loaded project model that holds every notification record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTemplateKeyForm(ProjectModel model, List<Finding> findings)
    {
        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            if (notification.Field(RecordFields.Template) is not { Length: > 0 } template)
            {
                continue;
            }

            var accepted = new HashSet<string>(TemplateAudienceAliases, StringComparer.Ordinal);

            foreach (var audience in notification.Ids(RecordFields.Addresses))
            {
                accepted.Add(audience);

                var slash = audience.LastIndexOf('/');

                if (slash >= 0 && slash < audience.Length - 1)
                {
                    accepted.Add(audience[(slash + 1)..]);
                }
            }

            var keys = template.Split(',')
                .Select(key => key.Trim())
                .Where(key => key.Length > 0 && !key.StartsWith("none", StringComparison.Ordinal));

            foreach (var key in keys)
            {
                var names = accepted.Any(ending =>
                    key.Equals(ending, StringComparison.Ordinal)
                    || key.EndsWith("-" + ending, StringComparison.Ordinal));

                if (!names)
                {
                    findings.Add(new Finding("template-key-invalid", $"Notification '{notification.Id}' has template key '{key}', which names no addresses the record declares.", notification.File, notification.Line, SubjectOf(notification)));
                }
            }
        }
    }

    /// <summary>Reports a setting holding a number or a duration with no unit.</summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSettingUnit(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            if (setting.Field(RecordFields.ValueType) is not { } valueType || !QuantityValueTypes.Contains(valueType))
            {
                continue;
            }

            if (setting.Field(RecordFields.Unit) is not { Length: > 0 })
            {
                findings.Add(new Finding("setting-unit-missing", $"Setting '{setting.Id}' holds a {valueType} with no unit.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports an outbound integration event named with an internal word.</summary>
    /// <param name="model">The loaded project model that holds every integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIntegrationEventWording(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent && record.Field(RecordFields.Direction) == "out"))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var hit = name.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => InternalEventWords.Contains(word));

            if (hit is not null)
            {
                findings.Add(new Finding("integration-event-name-invalid", $"Integration event '{record.Id}' names the internal word '{hit}' instead of the business fact.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports use-case-verb-unknown and use-case-verb-invalid.</summary>
    /// <param name="model">The loaded project model that holds every use case record and the verb vocabulary.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunVerbs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);

            if (!Declares(model.Config, verb))
            {
                findings.Add(new Finding("use-case-verb-unknown", UnknownVerbMessage(model.Config, $"Use case '{useCase.Id}'", verb), useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            // One verb, one kind: a reading verb opens only a query, and a changing verb only a command.
            var kind = KindOf(model.Config, verb);
            var declared = useCase.Field(RecordFields.Kind);

            if ((kind == UseCaseVerbs.VerbKind.Query && declared == "command")
                || (kind == UseCaseVerbs.VerbKind.Command && declared == "query"))
            {
                findings.Add(new Finding("use-case-verb-invalid", $"Use case '{useCase.Id}' is a {declared} but '{verb}' names a {(kind == UseCaseVerbs.VerbKind.Query ? "query" : "command")}.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports reaction-name-invalid and job-name-invalid.</summary>
    /// <param name="model">The loaded project model that holds every reaction and job record and the verb vocabulary.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunNames(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type is RecordTypes.Reaction or RecordTypes.Job))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);

            if (record.Type == RecordTypes.Reaction)
            {
                // <verb>-<what>-on-<fact>: what it does, then the fact it answers.
                if (!Declares(model.Config, verb) || !name.Contains("-on-", StringComparison.Ordinal))
                {
                    findings.Add(new Finding("reaction-name-invalid", $"Reaction '{record.Id}' is not named <verb>-<what>-on-<fact> with a declared verb.", record.File, record.Line, SubjectOf(record)));
                }

                continue;
            }

            // A job carries the verb of the use case it runs.
            var runs = record.EdgeIds(RecordFields.Runs)
                             .Where(id => Identifiers.TrySplit(id, out _, out _, out var target) && target is not null)
                             .Select(id => { Identifiers.TrySplit(id, out _, out _, out var target); return UseCaseVerbs.VerbOf(target!, model.Config.PhrasalVerbs); })
                             .ToList();

            if (runs.Count > 0 && !runs.Contains(verb, StringComparer.Ordinal))
            {
                findings.Add(new Finding("job-name-invalid", $"Job '{record.Id}' starts with '{verb}' but runs '{string.Join("', '", runs.Distinct())}'.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether the baseline or the project declares the verb.</summary>
    /// <param name="config">The project configuration that holds the project verb declarations.</param>
    /// <param name="verb">The leading verb taken from the record name.</param>
    /// <returns>True when the baseline or the project declares the verb; otherwise false.</returns>
    private static bool Declares(ConsumerConfig config, string verb) =>
        UseCaseVerbs.IsDeclared(verb, config.Verbs, config.QueryVerbs, config.PhrasalVerbs);

    /// <summary>The kind a declared verb names: the baseline kind, a project query verb, or either.</summary>
    /// <param name="config">The project configuration that holds the project query verbs.</param>
    /// <param name="verb">The declared verb whose kind is wanted.</param>
    /// <returns>The kind the verb names.</returns>
    private static UseCaseVerbs.VerbKind KindOf(ConsumerConfig config, string verb) =>
        UseCaseVerbs.KindOf(verb, config.QueryVerbs);

    /// <summary>The message for an undeclared verb, naming its replacement when the verb is a retired synonym.</summary>
    /// <param name="config">The project configuration that holds the retired verb replacements.</param>
    /// <param name="subject">The record description that opens the message.</param>
    /// <param name="verb">The undeclared verb taken from the record name.</param>
    /// <returns>The finding message, naming the replacement verb when the verb is a retired synonym.</returns>
    private static string UnknownVerbMessage(ConsumerConfig config, string subject, string verb) =>
        config.RetiredVerbs.TryGetValue(verb, out var replacement) || UseCaseVerbs.Retired.TryGetValue(verb, out replacement)
            ? $"{subject} starts with '{verb}' and '{replacement}' replaces it."
            : $"{subject} starts with '{verb}', which the vocabulary does not declare.";
}
