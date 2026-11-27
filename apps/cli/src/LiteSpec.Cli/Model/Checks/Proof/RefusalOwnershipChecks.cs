using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Proof;

/// <summary>The refusal ownership checks: every refusal code is carried from its own module, by one exception type.</summary>
/// <follows>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</follows>
internal static class RefusalOwnershipChecks
{
    /// <summary>Reports refusal-module-foreign and refusal-code-duplicate.</summary>
    /// <param name="model">The loaded project model that holds every refusal set and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in model.RefusalSets)
        {
            owners.TryAdd(entry.Code, entry.Module);
        }

        var firstCarrier = new Dictionary<string, (string Class, string File)>(StringComparer.Ordinal);
        var seen = new HashSet<(string Code, string Class)>();

        foreach (var literal in model.Code.Literals
            .Where(literal => literal.Declares)
            .OrderBy(literal => literal.File, StringComparer.Ordinal)
            .ThenBy(literal => literal.Line)
            .ThenBy(literal => literal.Value, StringComparer.Ordinal))
        {
            // A code no refusal set declares is unknown, generic or composed,
            // which the record checks own; ownership starts from a declared code.
            if (!owners.TryGetValue(literal.Value, out var owner))
            {
                continue;
            }

            // Only LiteSpec's own code layout names the module by the first folder
            // under the modules root; an own layout carries no module in its folders.
            if (model.Config.Layers.Implementation == LayerPractice.LiteSpec
                && SourceRoots.ModulePath(model.Config, literal.File) is { } segments
                && !SameName(segments[0], owner))
            {
                findings.Add(new Finding(
                    "refusal-module-foreign",
                    $"Code '{literal.Value}' is declared by module '{owner}' but carried from module '{segments[0]}'.",
                    literal.File,
                    literal.Line,
                    null));
            }

            // Only an exception type carries a code: a handler that passes a code
            // string at a throw site is reported where the exception shape is checked.
            if (literal.Class is not { } carrier || !IsExceptionType(carrier) || !seen.Add((literal.Value, carrier)))
            {
                continue;
            }

            if (firstCarrier.TryGetValue(literal.Value, out var first))
            {
                findings.Add(new Finding(
                    "refusal-code-duplicate",
                    DuplicateMessage(model, literal.Value, first, (carrier, literal.File)),
                    literal.File,
                    literal.Line,
                    null));
            }
            else
            {
                firstCarrier[literal.Value] = (carrier, literal.File);
            }
        }
    }

    /// <summary>
    ///     The duplicate message: when both carriers are rule exceptions marking one rule whose refusal is the code,
    ///     the message names that rule and the one-type-per-rule provision, so the reader fixes the rule instead of
    ///     hand-written constants; otherwise it names the two carriers.
    /// </summary>
    /// <param name="model">The loaded project model that holds the marked rule exceptions and the rule records.</param>
    /// <param name="code">The refusal code both carriers carry.</param>
    /// <param name="first">The class and file of the first carrier.</param>
    /// <param name="second">The class and file of the second carrier.</param>
    /// <returns>The finding message for the duplicate code.</returns>
    private static string DuplicateMessage(ProjectModel model, string code, (string Class, string File) first, (string Class, string File) second)
    {
        var shared = SharedRule(model, code, first, second);

        return shared is not null
            ? $"Code '{code}' is carried by '{first.Class}' and '{second.Class}', which both mark '{shared}'. Keep one exception type per rule, as litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions states."
            : $"Code '{code}' is carried by '{first.Class}' and '{second.Class}'.";
    }

    /// <summary>The rule both carriers mark whose refusal is the code, or null when they mark no one such rule.</summary>
    /// <param name="model">The loaded project model that holds the marked rule exceptions and the rule records.</param>
    /// <param name="code">The refusal code both carriers carry.</param>
    /// <param name="first">The class and file of the first carrier.</param>
    /// <param name="second">The class and file of the second carrier.</param>
    /// <returns>The shared rule id, or null when the carriers mark no one rule refusing with the code.</returns>
    private static string? SharedRule(ProjectModel model, string code, (string Class, string File) first, (string Class, string File) second)
    {
        var firstRules = RulesOf(model, code, first);
        var secondRules = RulesOf(model, code, second);

        foreach (var rule in firstRules)
        {
            if (secondRules.Contains(rule, StringComparer.Ordinal))
            {
                return rule;
            }
        }

        return null;
    }

    /// <summary>The rules the carrier marks whose refusal is the code: its rule marks resolved through the records.</summary>
    /// <param name="model">The loaded project model that holds the marked rule exceptions and the rule records.</param>
    /// <param name="code">The refusal code the carrier carries.</param>
    /// <param name="carrier">The class and file of the carrier.</param>
    /// <returns>The marked rule ids refusing with the code, in mark order.</returns>
    private static List<string> RulesOf(ProjectModel model, string code, (string Class, string File) carrier)
    {
        var rules = new List<string>();

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.RuleException
                || !string.Equals(element.Class, carrier.Class, StringComparison.Ordinal)
                || !string.Equals(element.File, carrier.File, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows
                    || !mark.RecordId.StartsWith("rule/", StringComparison.Ordinal)
                    || !model.ById.TryGetValue(mark.RecordId, out var rule)
                    || rule.Type != RecordTypes.Rule
                    || Model.Proof.RefusalCodes.ResultCode(model, rule) != code
                    || rules.Contains(mark.RecordId, StringComparer.Ordinal))
                {
                    continue;
                }

                rules.Add(mark.RecordId);
            }
        }

        return rules;
    }

    /// <summary>Whether the class carries refusal codes as an exception type: its name ends with Exception, Failure or Refusal.</summary>
    /// <param name="name">The simple class name.</param>
    /// <returns>True when the name ends with Exception, Failure or Refusal.</returns>
    internal static bool IsExceptionType(string name) =>
        name.EndsWith("Exception", StringComparison.Ordinal)
        || name.EndsWith("Failure", StringComparison.Ordinal)
        || name.EndsWith("Refusal", StringComparison.Ordinal);

    /// <summary>Whether a folder name and a module name spell one name: case, hyphens and underscores ignored.</summary>
    /// <param name="folder">The module folder name as written on disk.</param>
    /// <param name="name">The module name the refusal set declares.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    private static bool SameName(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
