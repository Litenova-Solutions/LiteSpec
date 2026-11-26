using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Refusals;

/// <summary>
///     The refusal sets of the model and the generic refusals it derives. Each aggregate declares the refusals
///     its rules raise in <c>behavior/modules/&lt;m&gt;/aggregates/&lt;a&gt;/refusals.yaml</c>, and the module
///     declares only refusals no aggregate raises in <c>behavior/modules/&lt;m&gt;/refusals.yaml</c>, each under
///     the <c>records</c> list. The seven generic refusals are derived from a use case's access, inputs, kind
///     and knowledge reads, and are never written.
/// </summary>
/// <follows>litespec/core/use-cases#declare-each-modules-refusals-once</follows>
/// <follows>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</follows>
/// <follows>litespec/core/knowledge#refuse-a-missing-rule-with-a-reason</follows>
static class RefusalSetReader
{
    /// <summary>The generic refusal codes the model derives for a use case, which no file writes.</summary>
    public static readonly IReadOnlyList<string> DerivedCodes =
    [
        "forbidden", "invalid-input", "not-found", "conflict", "unauthenticated", "sign-in-too-old", "rule-not-stated",
    ];

    /// <summary>A reference-data table identifier named in a rule statement.</summary>
    private static readonly Regex TableId = new(
        @"\breference-data/[a-z0-9][a-z0-9-]*\.[a-z0-9][a-z0-9-]*\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Reads every aggregate's and every module's refusal set.</summary>
    /// <param name="config">The consumer configuration holding the behavior folder.</param>
    /// <param name="problems">The findings a set that does not read or holds a malformed refusal is reported into.</param>
    /// <returns>The declared refusals, in module then aggregate then file order.</returns>
    public static List<RefusalEntry> Read(ConsumerConfig config, List<Finding> problems)
    {
        var entries = new List<RefusalEntry>();
        var modules = Path.Combine(config.BehaviorDocs, "modules");

        if (!Directory.Exists(modules))
        {
            return entries;
        }

        foreach (var folder in Directory.GetDirectories(modules).OrderBy(folder => folder, StringComparer.Ordinal))
        {
            var module = Path.GetFileName(folder);
            ReadFile(module, null, Path.Combine(folder, "refusals.yaml"), entries, problems);

            var aggregates = Path.Combine(folder, "aggregates");

            if (!Directory.Exists(aggregates))
            {
                continue;
            }

            foreach (var aggregateFolder in Directory.GetDirectories(aggregates).OrderBy(candidate => candidate, StringComparer.Ordinal))
            {
                ReadFile(module, Path.GetFileName(aggregateFolder), Path.Combine(aggregateFolder, "refusals.yaml"), entries, problems);
            }
        }

        return entries;
    }

    /// <summary>Reads one refusal set file into entries, reporting a file that does not read.</summary>
    /// <param name="module">The module that holds the file.</param>
    /// <param name="aggregate">The aggregate folder that holds the file, or null for a module-level file.</param>
    /// <param name="file">The refusal set file path.</param>
    /// <param name="entries">The entries read so far, appended in place.</param>
    /// <param name="problems">The findings a set that does not read or holds a malformed refusal is reported into.</param>
    private static void ReadFile(string module, string? aggregate, string file, List<RefusalEntry> entries, List<Finding> problems)
    {
        if (!File.Exists(file))
        {
            return;
        }

        JsonText root;

        try
        {
            root = YamlText.Parse(File.ReadAllText(file));
        }
        catch (JsonTextException exception)
        {
            problems.Add(new Finding("record-invalid", $"Refusal set '{file}' could not be read: {exception.Message}", file, exception.Line, null));
            return;
        }

        if (root.Kind != JsonKind.Object
            || !root.TryGetProperty(RecordFields.Records, out var list)
            || list.Kind != JsonKind.Array)
        {
            problems.Add(new Finding("record-invalid", $"Refusal set '{file}' holds no records list of refusals.", file, 1, null));
            return;
        }

        foreach (var item in list.Items)
        {
            var id = TextOf(item, RecordFields.Id);
            var meaning = TextOf(item, RecordFields.Meaning);

            if (!LiteSpec.Model.Identifiers.TrySplit(id, out var prefix, out var home, out var name)
                || prefix != "refusal"
                || home != module
                || name.Length == 0)
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' needs an id named refusal/<module>.<name>.",
                    file,
                    item.Line,
                    null));
                continue;
            }

            if (!TryReadParameters(item, id, file, meaning, problems, out var parameters))
            {
                continue;
            }

            entries.Add(new RefusalEntry(module, home + "." + name, meaning, file, aggregate, item.Line)
            {
                Parameters = parameters,
                Status = NumberOf(item, RecordFields.Status),
            });
        }
    }

    /// <summary>Reads one refusal's parameter list, reporting a list that is malformed or disagrees with the meaning.</summary>
    /// <param name="item">The refusal entry holding the parameters.</param>
    /// <param name="id">The refusal identifier, for the finding message.</param>
    /// <param name="file">The refusal set file path.</param>
    /// <param name="meaning">The refusal meaning naming each parameter as <c>{name}</c>.</param>
    /// <param name="problems">The findings a malformed list is reported into.</param>
    /// <param name="parameters">The read parameters, in written order.</param>
    /// <returns>True when the list reads and matches the meaning; otherwise false.</returns>
    private static bool TryReadParameters(JsonText item, string id, string file, string meaning, List<Finding> problems, out List<RefusalParameter> parameters)
    {
        parameters = [];

        if (!item.TryGetProperty(RecordFields.Parameters, out var list))
        {
            foreach (var named in PlaceholdersOf(meaning))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' names '{{{named}}}' in its meaning but declares no parameters list.",
                    file,
                    item.Line,
                    null));
                return false;
            }

            return true;
        }

        if (list.Kind != JsonKind.Array)
        {
            problems.Add(new Finding(
                "record-invalid",
                $"Refusal '{id}' in '{file}' holds parameters that are no list of values.",
                file,
                item.Line,
                null));
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in list.Items)
        {
            var name = TextOf(entry, "name");

            if (!IsIdentifier(name))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' names parameter '{name}' that is no identifier.",
                    file,
                    entry.Line,
                    null));
                return false;
            }

            if (!seen.Add(name))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' declares parameter '{name}' twice.",
                    file,
                    entry.Line,
                    null));
                return false;
            }

            parameters.Add(new RefusalParameter(name, TextOf(entry, RecordFields.Meaning)));
        }

        foreach (var named in PlaceholdersOf(meaning))
        {
            if (!seen.Contains(named))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' names '{{{named}}}' in its meaning that its parameters list does not declare.",
                    file,
                    item.Line,
                    null));
                return false;
            }
        }

        foreach (var declared in parameters)
        {
            if (!PlaceholdersOf(meaning).Contains(declared.Name, StringComparer.Ordinal))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Refusal '{id}' in '{file}' declares parameter '{declared.Name}' its meaning never names.",
                    file,
                    item.Line,
                    null));
                return false;
            }
        }

        return true;
    }

    /// <summary>The placeholder names a meaning states: each <c>{name}</c> outside a <c>{{</c> or <c>}}</c> escape, in order.</summary>
    /// <param name="meaning">The refusal meaning.</param>
    /// <returns>The placeholder names, in order of appearance.</returns>
    private static List<string> PlaceholdersOf(string meaning)
    {
        var names = new List<string>();

        for (var at = 0; at < meaning.Length; at++)
        {
            if (meaning[at] == '{' && at + 1 < meaning.Length && meaning[at + 1] == '{')
            {
                at++;
                continue;
            }

            if (meaning[at] == '}' && at + 1 < meaning.Length && meaning[at + 1] == '}')
            {
                at++;
                continue;
            }

            if (meaning[at] != '{')
            {
                continue;
            }

            var end = meaning.IndexOf('}', at + 1);

            if (end < 0)
            {
                break;
            }

            var token = meaning.Substring(at + 1, end - at - 1);

            if (token.Length > 0)
            {
                names.Add(token);
            }

            at = end;
        }

        return names;
    }

    /// <summary>Whether the name reads as a C# identifier: a letter or underscore opening letters, digits or underscores.</summary>
    /// <param name="name">The parameter name.</param>
    /// <returns>True when the name reads as an identifier; otherwise false.</returns>
    private static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || (name[0] != '_' && !char.IsLetter(name[0])))
        {
            return false;
        }

        for (var index = 1; index < name.Length; index++)
        {
            if (name[index] != '_' && !char.IsLetterOrDigit(name[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The problem type of a refusal code: the project's base followed by the code's part after its module.</summary>
    /// <param name="code">The refusal code, <c>&lt;module&gt;.&lt;name&gt;</c>.</param>
    /// <param name="problemTypeBase">The URI prefix the project declares in <c>problemTypeBase</c>.</param>
    /// <returns>The problem type of the code, as an RFC 9457 type.</returns>
    public static string ProblemTypeOf(string code, string problemTypeBase)
    {
        var name = code.Contains('.', StringComparison.Ordinal)
            ? code[(code.IndexOf('.', StringComparison.Ordinal) + 1)..]
            : code;

        return problemTypeBase + name;
    }

    /// <summary>
    ///     The generic refusals a use case derives: <c>forbidden</c> when it names a grant other than anonymous or
    ///     system, <c>unauthenticated</c> for <c>signed-in</c>, <c>sign-in-too-old</c> when it sets
    ///     <c>recentSignIn</c>, <c>invalid-input</c> when an input carries rules, <c>not-found</c> when an input is an
    ///     id of an aggregate, <c>conflict</c> when a command declares <c>conflicts</c>, and <c>rule-not-stated</c>
    ///     when the use case reads knowledge with no rule in force for the asked key or day.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="byId">The written records by id, resolving the rules and tables the use case takes.</param>
    /// <returns>The derived generic refusal codes, in the order of <see cref="DerivedCodes" />.</returns>
    public static IReadOnlyList<string> DeriveFor(Record useCase, IReadOnlyDictionary<string, Record> byId)
    {
        var codes = new List<string>();
        var grant = AccessText(useCase, RecordFields.Grant);

        if (grant is { Length: > 0 } and not "anonymous" and not "system")
        {
            codes.Add("forbidden");
        }

        if (grant == "signed-in")
        {
            codes.Add("unauthenticated");
        }

        if (AccessHas(useCase, RecordFields.RecentSignIn))
        {
            codes.Add("sign-in-too-old");
        }

        if (UseCaseQueries.HasValidatedInput(useCase))
        {
            codes.Add("invalid-input");
        }

        if (useCase.Objects(RecordFields.Input).Any(input =>
            TextOf(input, RecordFields.Type) == "id" && TextOf(input, RecordFields.Identifies).StartsWith("aggregate/", StringComparison.Ordinal)))
        {
            codes.Add("not-found");
        }

        if (useCase.Field(RecordFields.Kind) == "command" && useCase.Has(RecordFields.Conflicts))
        {
            codes.Add("conflict");
        }

        if (ReadsKnowledge(useCase, byId))
        {
            codes.Add("rule-not-stated");
        }

        return codes;
    }

    /// <summary>
    ///     The product refusal a derived generic result carries: the refusal of the product-level rule
    ///     that triggers it, or of a rule naming the generic refusal itself, which no written result
    ///     may state. Only not-found and rule-not-stated carry a rule refusal; every other generic
    ///     keeps its configured or generic code.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="generic">The derived generic refusal name.</param>
    /// <param name="byId">The written records by id, resolving the rules the use case takes.</param>
    /// <returns>The refusal identifier the derived result carries, or null when no triggering rule states a carried refusal.</returns>
    public static string? ProductRefusalFor(Record useCase, string generic, IReadOnlyDictionary<string, Record> byId) =>
        generic switch
        {
            "not-found" => NotFoundRefusal(useCase, byId),
            "rule-not-stated" => RuleNotStatedRefusal(useCase, byId),
            _ => null,
        };

    /// <summary>The refusal the derived not-found result carries: the carried refusal of the first rule taken on an input, in input then rules order.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="byId">The written records by id, resolving the rules the inputs take.</param>
    /// <returns>The carried refusal identifier, or null when no input takes a rule stating one.</returns>
    private static string? NotFoundRefusal(Record useCase, IReadOnlyDictionary<string, Record> byId)
    {
        foreach (var id in InputRules(useCase))
        {
            if (byId.TryGetValue(Identifiers.BaseId(id), out var rule)
                && rule.Type == RecordTypes.Rule
                && CarriedRefusal(rule, "not-found") is { } refusal)
            {
                return refusal;
            }
        }

        return null;
    }

    /// <summary>The refusal the derived rule-not-stated result carries: the carried refusal of the rule that triggers it, an input rule refused as rule-not-stated or a taken rule citing a knowledge table.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="byId">The written records by id, resolving the rules the use case takes.</param>
    /// <returns>The carried refusal identifier, or null when the triggering rule states none.</returns>
    private static string? RuleNotStatedRefusal(Record useCase, IReadOnlyDictionary<string, Record> byId)
    {
        foreach (var id in InputRules(useCase))
        {
            if (byId.TryGetValue(Identifiers.BaseId(id), out var rule)
                && rule.Type == RecordTypes.Rule
                && IsRuleNotStatedRefusal(RefusalCode.Of(rule))
                && CarriedRefusal(rule, "rule-not-stated") is { } refusal)
            {
                return refusal;
            }
        }

        foreach (var id in TakenRules(useCase))
        {
            if (byId.TryGetValue(Identifiers.BaseId(id), out var rule)
                && rule.Type == RecordTypes.Rule
                && CitesKnowledgeTable(rule, byId)
                && CarriedRefusal(rule, "rule-not-stated") is { } refusal)
            {
                return refusal;
            }
        }

        return null;
    }

    /// <summary>
    ///     The refusal a derived generic result carries from one triggering rule: its refusal when the rule
    ///     stands at product level, or when the refusal names the generic itself. A module rule refusing
    ///     anything else keeps its written result.
    /// </summary>
    /// <param name="rule">The triggering rule record.</param>
    /// <param name="generic">The derived generic refusal name.</param>
    /// <returns>The carried refusal identifier, or null when the rule states none or stays written.</returns>
    private static string? CarriedRefusal(Record rule, string generic)
    {
        if (rule.Field(RecordFields.Refusal) is not { Length: > 0 } refusal)
        {
            return null;
        }

        if (rule.Module is null)
        {
            return refusal;
        }

        return IsGenericName(RefusalCode.CodeOf(refusal), generic) ? refusal : null;
    }

    /// <summary>Whether the refusal code names the generic refusal: any home with that name.</summary>
    /// <param name="code">The refusal code under test, or null when the rule names none.</param>
    /// <param name="generic">The derived generic refusal name.</param>
    /// <returns>True when the name after the home is the generic; otherwise false.</returns>
    private static bool IsGenericName(string? code, string generic)
    {
        if (code is not { Length: > 0 })
        {
            return false;
        }

        var name = code.Contains('.', StringComparison.Ordinal)
            ? code[(code.IndexOf('.', StringComparison.Ordinal) + 1)..]
            : code;

        return name == generic;
    }

    /// <summary>
    ///     Whether the use case reads knowledge: an input takes a rule refused as the generic
    ///     <c>rule-not-stated</c>, or a taken rule cites a reference-data table of a knowledge area.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="byId">The written records by id, resolving the rules and tables the use case takes.</param>
    /// <returns>True when either knowledge signal holds; otherwise false.</returns>
    private static bool ReadsKnowledge(Record useCase, IReadOnlyDictionary<string, Record> byId)
    {
        foreach (var id in InputRules(useCase))
        {
            if (byId.TryGetValue(Identifiers.BaseId(id), out var rule)
                && rule.Type == RecordTypes.Rule
                && IsRuleNotStatedRefusal(RefusalCode.Of(rule)))
            {
                return true;
            }
        }

        foreach (var id in TakenRules(useCase))
        {
            if (byId.TryGetValue(Identifiers.BaseId(id), out var rule)
                && rule.Type == RecordTypes.Rule
                && CitesKnowledgeTable(rule, byId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the refusal code names the generic rule-not-stated refusal: any home with that name.</summary>
    /// <param name="code">The refusal code under test, or null when the rule names none.</param>
    /// <returns>True when the name after the home is <c>rule-not-stated</c>; otherwise false.</returns>
    private static bool IsRuleNotStatedRefusal(string? code) => IsGenericName(code, "rule-not-stated");

    /// <summary>Whether the rule cites a reference-data table owned by a knowledge area.</summary>
    /// <param name="rule">The rule record whose statement is read.</param>
    /// <param name="byId">The written records by id, resolving the cited tables and areas.</param>
    /// <returns>True when the statement names a table whose home is a knowledge area; otherwise false.</returns>
    private static bool CitesKnowledgeTable(Record rule, IReadOnlyDictionary<string, Record> byId)
    {
        if (rule.Field(RecordFields.Statement) is not { Length: > 0 } statement)
        {
            return false;
        }

        foreach (Match match in TableId.Matches(statement))
        {
            if (byId.TryGetValue(match.Value, out var table)
                && table.Type == RecordTypes.ReferenceData
                && Identifiers.TrySplit(table.Id, out _, out var home, out _)
                && home is { Length: > 0 }
                && byId.TryGetValue($"knowledge-area/{home}", out var area)
                && area.Type == RecordTypes.KnowledgeArea)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The rule identifiers the use case takes: its steps' rule cites, its input rules, and its consistency boundBy.</summary>
    /// <param name="useCase">The use case record whose citations are read.</param>
    /// <returns>The taken rule identifiers, in steps, input, then boundBy order.</returns>
    private static IEnumerable<string> TakenRules(Record useCase)
    {
        foreach (var cited in useCase.AppliedCitations())
        {
            if (cited.StartsWith("rule/", StringComparison.Ordinal))
            {
                yield return cited;
            }
        }

        foreach (var rule in InputRules(useCase))
        {
            yield return rule;
        }

        if (useCase.Raw.ValueKind == JsonValueKind.Object
            && useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency))
        {
            foreach (var rule in LinkStrings(consistency, RecordFields.BoundBy))
            {
                yield return rule;
            }
        }
    }

    /// <summary>The rule identifiers each input of a use case names.</summary>
    /// <param name="useCase">The use case record whose input array is read.</param>
    /// <returns>The rule identifiers named by its inputs, in written order.</returns>
    private static IEnumerable<string> InputRules(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Input, out var inputs)
            || inputs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return inputs.EnumerateArray().SelectMany(input => LinkStrings(input, RecordFields.Rules));
    }

    /// <summary>Reads one field that holds one id or an array of ids.</summary>
    /// <param name="element">The object holding the field.</param>
    /// <param name="name">The field name to read.</param>
    /// <returns>The single id or the array of ids the field holds, or an empty list when it holds neither.</returns>
    private static IReadOnlyList<string> LinkStrings(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } single)
        {
            return [single];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. value.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetString()!),
            ];
        }

        return [];
    }

    /// <summary>The string a use case's access object holds under a name.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="name">The member name inside <c>access</c>.</param>
    /// <returns>The string value, or null when the access object or the member is missing.</returns>
    private static string? AccessText(Record useCase, string name)
    {
        var raw = useCase.Raw;

        return raw.ValueKind == JsonValueKind.Object
            && raw.TryGetProperty(RecordFields.Access, out var access)
            && access.ValueKind == JsonValueKind.Object
            && access.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    /// <summary>Whether a use case's access object carries a member.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <param name="name">The member name inside <c>access</c>.</param>
    /// <returns>True when the access object holds the member.</returns>
    private static bool AccessHas(Record useCase, string name)
    {
        var raw = useCase.Raw;

        return raw.ValueKind == JsonValueKind.Object
            && raw.TryGetProperty(RecordFields.Access, out var access)
            && access.ValueKind == JsonValueKind.Object
            && access.TryGetProperty(name, out _);
    }

    /// <summary>Reads one string member of a JSON object, or an empty string when it is missing or not a string.</summary>
    /// <param name="entry">The object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or an empty string.</returns>
    private static string TextOf(JsonElement entry, string name) =>
        entry.ValueKind == JsonValueKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>Reads one string member of a parsed YAML object, or an empty string when it is missing or not a string.</summary>
    /// <param name="entry">The parsed object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or an empty string.</returns>
    private static string TextOf(JsonText entry, string name) =>
        entry.Kind == JsonKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.GetString() is { } text
            ? text
            : "";

    /// <summary>Reads one integer member of a parsed YAML object, or null when it is missing or not an integer.</summary>
    /// <param name="entry">The parsed object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The integer value, or null.</returns>
    private static int? NumberOf(JsonText entry, string name) =>
        entry.Kind == JsonKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.GetNumber() is { } text
        && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
}
