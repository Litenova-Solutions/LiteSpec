using System.Text.Json;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Refusals;

/// <summary>
///     The refusal sets of the model and the generic refusals it derives. Each module declares its codes once in
///     <c>behavior/modules/&lt;m&gt;/refusals.yaml</c>, under the <c>records</c> list. The six generic refusals are
///     derived from a use case's access, inputs and kind, and are never written.
/// </summary>
/// <follows>litespec/core/use-cases#declare-each-modules-refusals-once</follows>
static class RefusalSetReader
{
    /// <summary>The generic refusal codes the model derives for a use case, which no file writes.</summary>
    public static readonly IReadOnlyList<string> DerivedCodes =
    [
        "forbidden", "invalid-input", "not-found", "conflict", "unauthenticated", "sign-in-too-old",
    ];

    /// <summary>Reads every module's refusal set.</summary>
    /// <param name="config">The consumer configuration holding the behavior folder.</param>
    /// <param name="problems">The findings a set that does not read or holds a malformed code is reported into.</param>
    /// <returns>The declared refusal codes, in module then file order.</returns>
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
            var file = Path.Combine(folder, "refusals.yaml");

            if (!File.Exists(file))
            {
                continue;
            }

            JsonText root;

            try
            {
                root = YamlText.Parse(File.ReadAllText(file));
            }
            catch (JsonTextException exception)
            {
                problems.Add(new Finding("record-invalid", $"Refusal set '{file}' could not be read: {exception.Message}", file, exception.Line, null));
                continue;
            }

            if (root.Kind != JsonKind.Object
                || !root.TryGetProperty(RecordFields.Records, out var list)
                || list.Kind != JsonKind.Array)
            {
                problems.Add(new Finding("record-invalid", $"Refusal set '{file}' holds no records list of refusal codes.", file, 1, null));
                continue;
            }

            foreach (var item in list.Items)
            {
                var code = TextOf(item, RecordFields.Code);
                var meaning = TextOf(item, RecordFields.Meaning);

                if (code.Length == 0 || !code.StartsWith(module + ".", StringComparison.Ordinal))
                {
                    problems.Add(new Finding(
                        "record-invalid",
                        $"Refusal '{code}' in '{file}' needs a code named <module>.<name>.",
                        file,
                        item.Line,
                        null));
                    continue;
                }

                entries.Add(new RefusalEntry(module, code, meaning, file));
            }
        }

        return entries;
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
    ///     <c>recentSignInLimit</c>, <c>invalid-input</c> when an input carries rules, <c>not-found</c> when an input is an
    ///     id of an aggregate, and <c>conflict</c> when a command declares <c>conflicts</c>.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The derived generic refusal codes, in the order of <see cref="DerivedCodes" />.</returns>
    public static IReadOnlyList<string> DeriveFor(Record useCase)
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

        if (AccessHas(useCase, RecordFields.RecentSignInLimit))
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

        return codes;
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
}
