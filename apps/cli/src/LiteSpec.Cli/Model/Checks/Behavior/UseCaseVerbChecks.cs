using System.Text.RegularExpressions;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The use-case verb checks: each use-case name opens with a declared verb of its own kind, no glossary verb restates a baseline verb, and each retaken retired synonym names the baseline verb it differs from.</summary>
/// <follows>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</follows>
/// <follows>litespec/core/use-cases#keep-one-kind-per-verb</follows>
internal static class UseCaseVerbChecks
{
    /// <summary>The glossary term field holding the use-case kind a verb term opens.</summary>
    private const string VerbKindField = "verbKind";

    /// <summary>The baseline verb each retired synonym was retired into, as the use-cases page states it.</summary>
    internal static readonly Dictionary<string, string> Retired = new(StringComparer.Ordinal)
    {
        ["get"] = "read",
        ["fetch"] = "read",
        ["lookup"] = "read",
        ["count"] = "read",
        ["browse"] = "list",
        ["find"] = "search",
        ["update"] = "change",
        ["edit"] = "change",
        ["modify"] = "change",
        ["revise"] = "change",
        ["set"] = "change",
        ["adjust"] = "change",
        ["configure"] = "change",
        ["rename"] = "change",
        ["define"] = "create",
        ["compose"] = "create",
        ["save"] = "create",
        ["new"] = "create",
        ["delete"] = "remove",
        ["notify"] = "send",
        ["remind"] = "send",
        ["email"] = "send",
        ["suggest"] = "answer",
        ["ask"] = "answer",
        ["mark"] = "record",
        ["note"] = "record",
        ["log"] = "record",
        ["end"] = "close",
        ["finish"] = "complete",
        ["abandon"] = "cancel",
        ["lapse"] = "expire",
        ["clone"] = "duplicate",
        ["copy"] = "duplicate",
        ["replay"] = "retry",
        ["recheck"] = "verify",
        ["archive"] = "retire",
        ["reject"] = "refuse",
        ["deny"] = "refuse",
        ["unpublish"] = "withdraw",
        ["hide"] = "withdraw",
    };

    /// <summary>Runs every use-case verb check.</summary>
    /// <param name="model">The loaded project model that holds every record and the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var verbs = GlossaryVerbs(model);

        CheckUseCaseVerbs(model, verbs, findings);
        CheckVerbKinds(model, verbs, findings);
        CheckBaselineVerbs(model, findings);
        CheckVerbDistinctions(model, findings);
    }

    /// <summary>Every glossary verb term by its verb, each with the kind it opens.</summary>
    /// <param name="model">The loaded project model that holds the glossary terms.</param>
    /// <returns>The verb each glossary verb term declares with its kind, skipping entries outside the three kinds.</returns>
    internal static Dictionary<string, string> GlossaryVerbs(ProjectModel model)
    {
        var verbs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var term in model.Records.Where(record => record.Type == RecordTypes.Term && !record.Derived))
        {
            var verb = term.Field(RecordFields.Term);
            var kind = term.Field(VerbKindField);

            // A verbKind outside the three kinds is left to glossary-invalid.
            if (verb is { Length: > 0 } && kind is "command" or "query" or "either" && !verbs.ContainsKey(verb))
            {
                verbs[verb] = kind;
            }
        }

        return verbs;
    }

    /// <summary>The declared verb a name opens with: the longest leading run of kebab words the baseline vocabulary or the glossary declares.</summary>
    /// <param name="name">The use-case or permission name whose opening verb is wanted.</param>
    /// <param name="verbs">The glossary verbs by their verb.</param>
    /// <returns>The declared opening verb, or null when no leading run is declared.</returns>
    internal static string? DeclaredVerbOf(string name, IReadOnlyDictionary<string, string> verbs)
    {
        var words = name.Split('-');

        for (var count = words.Length; count >= 1; count--)
        {
            var candidate = string.Join('-', words, 0, count);

            if (UseCaseVerbs.Baseline.Contains(candidate) || verbs.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>The kind a declared verb opens: the baseline kind first, then the glossary kind.</summary>
    /// <param name="verb">The declared verb whose kind is wanted.</param>
    /// <param name="verbs">The glossary verbs by their verb.</param>
    /// <returns>The kind the verb opens, or null when neither list declares it.</returns>
    internal static string? KindOfVerb(string verb, IReadOnlyDictionary<string, string> verbs)
    {
        if (UseCaseVerbs.Kinds.TryGetValue(verb, out var baseline))
        {
            return baseline;
        }

        return verbs.TryGetValue(verb, out var kind) ? kind : null;
    }

    /// <summary>Reports each use case whose name opens with a verb neither the baseline vocabulary nor the glossary declares.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="verbs">The glossary verbs by their verb.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseVerbs(ProjectModel model, IReadOnlyDictionary<string, string> verbs, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && !record.Derived))
        {
            // A name outside the identifier form is left to identifier-invalid.
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            if (DeclaredVerbOf(name, verbs) is not null)
            {
                continue;
            }

            var first = name.Split('-')[0];

            if (first.Length == 0)
            {
                continue;
            }

            findings.Add(new Finding("use-case-verb-unknown", $"Use case '{useCase.Id}' opens with '{first}', which is no verb of the use-case verb table or the project glossary.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>Reports each use case opening with a verb of the other kind: a query with a command verb, or a command with a query verb.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="verbs">The glossary verbs by their verb.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckVerbKinds(ProjectModel model, IReadOnlyDictionary<string, string> verbs, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && !record.Derived))
        {
            var kind = useCase.Field(RecordFields.Kind);

            if (kind is not "command" and not "query")
            {
                continue;
            }

            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            // A verb neither list declares is left to use-case-verb-unknown.
            var verb = DeclaredVerbOf(name, verbs);

            if (verb is null)
            {
                continue;
            }

            var verbKind = KindOfVerb(verb, verbs);

            if (verbKind is null || verbKind == "either" || verbKind == kind)
            {
                continue;
            }

            findings.Add(new Finding("use-case-verb-invalid", $"Use case '{useCase.Id}' is a {kind} opening with the {verbKind} verb '{verb}'.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>Reports each glossary verb term restating a baseline verb.</summary>
    /// <param name="model">The loaded project model that holds the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBaselineVerbs(ProjectModel model, List<Finding> findings)
    {
        foreach (var term in model.Records.Where(record => record.Type == RecordTypes.Term && !record.Derived))
        {
            var verb = term.Field(RecordFields.Term);

            if (verb is not { Length: > 0 } || !term.Has(VerbKindField))
            {
                continue;
            }

            if (!UseCaseVerbs.Baseline.Contains(verb))
            {
                continue;
            }

            findings.Add(new Finding("term-verb-duplicate", $"Term '{term.Id}' declares the verb '{verb}', which the use-case verb table already declares.", term.File, term.Line, SubjectOf(term)));
        }
    }

    /// <summary>Reports each glossary verb term retaking a retired synonym whose meaning names no baseline verb it differs from.</summary>
    /// <param name="model">The loaded project model that holds the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckVerbDistinctions(ProjectModel model, List<Finding> findings)
    {
        foreach (var term in model.Records.Where(record => record.Type == RecordTypes.Term && !record.Derived))
        {
            var verb = term.Field(RecordFields.Term);

            if (verb is not { Length: > 0 } || !term.Has(VerbKindField))
            {
                continue;
            }

            if (!Retired.TryGetValue(verb, out var baseline))
            {
                continue;
            }

            if (NamesBaseline(term.Field(RecordFields.Meaning) ?? "", baseline))
            {
                continue;
            }

            findings.Add(new Finding("verb-distinction-missing", $"Term '{term.Id}' declares the verb '{verb}', whose meaning names no '{baseline}', the baseline verb it differs from.", term.File, term.Line, SubjectOf(term)));
        }
    }

    /// <summary>Whether a meaning names a baseline verb as a whole word.</summary>
    /// <param name="meaning">The term meaning under inspection.</param>
    /// <param name="baseline">The baseline verb the meaning must name.</param>
    /// <returns>True when the meaning holds the baseline verb as a whole word; otherwise false.</returns>
    private static bool NamesBaseline(string meaning, string baseline) =>
        Regex.IsMatch(meaning, $@"\b{Regex.Escape(baseline)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
