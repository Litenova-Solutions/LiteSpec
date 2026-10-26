namespace LiteSpec.Model;

/// <summary>The name-shape checks on records: use-case nouns, refusal codes, template keys, setting units and published event names.</summary>
static partial class Checker
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

    /// <summary>Reports use-case-noun-unknown, refusal-code-form, template-key-form, setting-unit-missing, integration-event-wording.</summary>
    private static void CheckRecordNames(ProjectModel model, List<Finding> findings)
    {
        CheckUseCaseNoun(model, findings);
        CheckRefusalCodeForm(model, findings);
        CheckTemplateKeyForm(model, findings);
        CheckSettingUnit(model, findings);
        CheckIntegrationEventWording(model, findings);
    }

    /// <summary>Reports a use case naming no aggregate, declared part, or glossary term after its verb.</summary>
    private static void CheckUseCaseNoun(ProjectModel model, List<Finding> findings)
    {
        var aggregates = model.Records.Where(record => record.Type == "aggregate").ToList();
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
                foreach (var part in aggregate.Strings("parts"))
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

        foreach (var term in model.Records.Where(record => record.Type == "term"))
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

        foreach (var useCase in model.Records.Where(record => record.Type == "use-case"))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out var home, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVocabulary.VerbOf(name, model.Config.PhrasalVerbs);
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
    private static bool NounMatches(string aggregate, string run) =>
        run.Equals(aggregate, StringComparison.Ordinal)
        || PluralForms(aggregate).Contains(run, StringComparer.Ordinal)
        || PluralForms(run).Contains(aggregate, StringComparer.Ordinal);

    /// <summary>The regular plural forms of one word.</summary>
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
    private static string NormalizeTerm(string text)
    {
        var words = text.Split([' ', '\t', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join("-", words).ToLowerInvariant();
    }

    /// <summary>Reports a refusal code outside its module, outside kebab-case, or carrying a vague word.</summary>
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

        foreach (var prefix in model.Config.RefusalPrefixes)
        {
            generic.Add(prefix);
        }

        // A refusal names the module that owns the refused state, which is often not the module of
        // the use case refusing: an admission use case answers events.not-found for a missing event.
        var modules = model.Records
                           .Where(record => record.Type == "module")
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

        foreach (var result in model.Records.Where(record => record.Type == "result" && record.Field("outcome") == "refusal"))
        {
            var code = result.Field("code");

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
                findings.Add(new Finding("refusal-code-form", $"Result '{result.Id}' has code '{code}', which names no module and no generic prefix.", result.File, result.Line, SubjectOf(result)));
            }

            if (!IsKebab(rest))
            {
                findings.Add(new Finding("refusal-code-form", $"Result '{result.Id}' has code '{code}', which is not kebab-case after its prefix.", result.File, result.Line, SubjectOf(result)));
            }

            var vague = rest.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => RefusalBannedWords.Contains(word));

            if (vague is not null)
            {
                findings.Add(new Finding("refusal-code-form", $"Result '{result.Id}' has code '{code}', which uses the vague word '{vague}' instead of naming the state.", result.File, result.Line, SubjectOf(result)));
            }
        }
    }

    /// <summary>The module holding a result: its page's module, else its use case's module.</summary>
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
    private static void CheckTemplateKeyForm(ProjectModel model, List<Finding> findings)
    {
        foreach (var notification in model.Records.Where(record => record.Type == "notification"))
        {
            if (notification.Field("template") is not { Length: > 0 } template)
            {
                continue;
            }

            var accepted = new HashSet<string>(TemplateAudienceAliases, StringComparer.Ordinal);

            foreach (var audience in notification.Ids("audience"))
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
                    findings.Add(new Finding("template-key-form", $"Notification '{notification.Id}' has template key '{key}', which names no audience the record declares.", notification.File, notification.Line, SubjectOf(notification)));
                }
            }
        }
    }

    /// <summary>Reports a setting holding a number or a duration with no unit.</summary>
    private static void CheckSettingUnit(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == "setting"))
        {
            if (setting.Field("valueType") is not { } valueType || !QuantityValueTypes.Contains(valueType))
            {
                continue;
            }

            if (setting.Field("unit") is not { Length: > 0 })
            {
                findings.Add(new Finding("setting-unit-missing", $"Setting '{setting.Id}' holds a {valueType} with no unit.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports an outbound integration event named with an internal word.</summary>
    private static void CheckIntegrationEventWording(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == "integration-event" && record.Field("direction") == "out"))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var hit = name.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => InternalEventWords.Contains(word));

            if (hit is not null)
            {
                findings.Add(new Finding("integration-event-wording", $"Integration event '{record.Id}' names the internal word '{hit}' instead of the business fact.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }
}
