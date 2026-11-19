using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Proof;

/// <summary>The refusal placement checks: a refusal an aggregate's rules raise is declared under that aggregate, and the module level keeps only refusals no aggregate raises.</summary>
/// <follows>litespec/core/use-cases#declare-each-aggregates-refusals-once</follows>
/// <follows>litespec/core/use-cases#declare-each-modules-refusals-once</follows>
internal static class RefusalPlacementChecks
{
    /// <summary>Reports a refusal declared under an aggregate whose exception lives elsewhere, and a module-level refusal raised by exactly one aggregate.</summary>
    /// <param name="model">The loaded project model that holds every refusal set and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var entry in model.RefusalSets)
        {
            if (entry.Aggregate is not null)
            {
                CheckAggregateDeclared(model, entry, findings);
            }
            else
            {
                CheckModuleDeclared(model, entry, findings);
            }
        }
    }

    /// <summary>Reports each carrier of an aggregate-declared refusal that lives under another aggregate, directly in Domain/, or outside Domain/.</summary>
    /// <param name="model">The loaded project model that holds the scanned code.</param>
    /// <param name="entry">The per-aggregate refusal entry.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateDeclared(ProjectModel model, Refusals.RefusalEntry entry, List<Finding> findings)
    {
        foreach (var carrier in CarriersOf(model, entry.Code))
        {
            // A carrier in another module is refusal-module-foreign, so only the
            // owning module and the shared roots report here.
            if (SourceRoots.ModulePath(model.Config, carrier.File) is { } segments
                && !PlacementPath.SameName(segments[0], entry.Module))
            {
                continue;
            }

            var (found, aggregate) = DomainAggregateOf(carrier.File);

            if (!found)
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Code '{entry.Code}' is declared under aggregate '{entry.Aggregate}', but '{carrier.Class}' carries it outside Domain/.",
                    carrier.File,
                    carrier.Line,
                    null));
            }
            else if (aggregate is null)
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Code '{entry.Code}' is declared under aggregate '{entry.Aggregate}', but '{carrier.Class}' carries it directly in Domain/.",
                    carrier.File,
                    carrier.Line,
                    null));
            }
            else if (!PlacementPath.SameAggregate(aggregate, entry.Aggregate!))
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Code '{entry.Code}' is declared under aggregate '{entry.Aggregate}', but '{carrier.Class}' carries it under aggregate '{aggregate}'.",
                    carrier.File,
                    carrier.Line,
                    null));
            }
        }
    }

    /// <summary>Reports a module-level refusal raised from exactly one aggregate: it belongs under that aggregate. A refusal raised from several aggregates stays module-level, a refusal no code raises stays with refusal-unimplemented, and a carrier outside Domain/ or directly in Domain/ is placed wrong wherever its refusal is declared.</summary>
    /// <param name="model">The loaded project model that holds the scanned code.</param>
    /// <param name="entry">The module-level refusal entry.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleDeclared(ProjectModel model, Refusals.RefusalEntry entry, List<Finding> findings)
    {
        var raising = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var carrier in CarriersOf(model, entry.Code))
        {
            if (SourceRoots.ModulePath(model.Config, carrier.File) is not { } segments
                || !PlacementPath.SameName(segments[0], entry.Module))
            {
                continue;
            }

            var (found, aggregate) = DomainAggregateOf(carrier.File);

            if (!found)
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Code '{entry.Code}' is declared at module level, but '{carrier.Class}' carries it outside Domain/.",
                    carrier.File,
                    carrier.Line,
                    null));
            }
            else if (aggregate is null)
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Code '{entry.Code}' is declared at module level, but '{carrier.Class}' carries it directly in Domain/.",
                    carrier.File,
                    carrier.Line,
                    null));
            }
            else
            {
                raising.Add(aggregate);
            }
        }

        if (raising.Count == 1)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Refusal '{entry.Code}' is declared at module level, but only aggregate '{raising.First()}' raises it; declare it under the aggregate.",
                entry.File,
                entry.Line,
                null));
        }
    }

    /// <summary>The carriers of a refusal code: each declaring literal on an exception type, and each rule exception without a hand-written Code whose rule mark names the refusal, in file then line order.</summary>
    /// <param name="model">The loaded project model that holds the scanned literals and the marked rule exceptions.</param>
    /// <param name="code">The refusal code.</param>
    /// <returns>The carrying literals in file then line order.</returns>
    private static List<CodeLiteral> CarriersOf(ProjectModel model, string code)
    {
        var carriers = model.Code.Literals
            .Where(literal => literal.Declares
                && string.Equals(literal.Value, code, StringComparison.Ordinal)
                && literal.Class is not null
                && RefusalOwnershipChecks.IsExceptionType(literal.Class))
            .ToList();

        var held = carriers
            .Select(literal => (literal.Value, literal.File, literal.Class))
            .ToHashSet();

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.RuleException
                || element.RuntimeValue is not null
                || element.Class.Length == 0
                || !RefusalOwnershipChecks.IsExceptionType(element.Class))
            {
                continue;
            }

            if (!MarksRuleRefusing(model, element, code))
            {
                continue;
            }

            if (held.Add((code, element.File, element.Class)))
            {
                carriers.Add(new CodeLiteral(code, element.File, element.Line, element.Class, Declares: true));
            }
        }

        return carriers
            .OrderBy(literal => literal.File, StringComparer.Ordinal)
            .ThenBy(literal => literal.Line)
            .ToList();
    }

    /// <summary>Whether the rule exception marks a rule refusing with the code: one of its rule marks resolves to a rule record whose refusal is the code.</summary>
    /// <param name="model">The loaded project model that holds every rule record.</param>
    /// <param name="element">The marked rule exception without a hand-written Code.</param>
    /// <param name="code">The refusal code.</param>
    /// <returns>True when such a rule mark exists.</returns>
    private static bool MarksRuleRefusing(ProjectModel model, MarkedElement element, string code)
    {
        foreach (var mark in element.Marks)
        {
            if (mark.Verb == MarkVerb.Follows
                || !mark.RecordId.StartsWith("rule/", StringComparison.Ordinal)
                || !model.ById.TryGetValue(mark.RecordId, out var rule)
                || rule.Type != RecordTypes.Rule)
            {
                continue;
            }

            if (string.Equals(RefusalCodes.ResultCode(model, rule), code, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The Domain aggregate folder a file sits in: whether a Domain folder holds it, and the aggregate folder above the file, or null directly in Domain/.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <returns>Whether a Domain folder holds the file, and the aggregate folder above it.</returns>
    private static (bool Found, string? Aggregate) DomainAggregateOf(string file)
    {
        var segments = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var at = 0; at < segments.Length; at++)
        {
            if (!string.Equals(segments[at], "Domain", StringComparison.Ordinal))
            {
                continue;
            }

            // The last segment is the file itself, so a Domain holding only the file is flat.
            return at + 1 < segments.Length - 1 ? (true, segments[at + 1]) : (true, null);
        }

        return (false, null);
    }
}
