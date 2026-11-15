using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Proof;

/// <summary>The refusal ownership checks: every refusal code is carried from its own module, by one exception type.</summary>
/// <follows>litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns</follows>
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

        var firstCarrier = new Dictionary<string, string>(StringComparer.Ordinal);
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

            if (SourceRoots.ModulePath(model.Config, literal.File) is { } segments
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
                    $"Code '{literal.Value}' is carried by '{first}' and '{carrier}'.",
                    literal.File,
                    literal.Line,
                    null));
            }
            else
            {
                firstCarrier[literal.Value] = carrier;
            }
        }
    }

    /// <summary>Whether the class carries refusal codes as an exception type: its name ends with Exception, Failure or Refusal.</summary>
    /// <param name="name">The simple class name.</param>
    /// <returns>True when the name ends with Exception, Failure or Refusal.</returns>
    private static bool IsExceptionType(string name) =>
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
