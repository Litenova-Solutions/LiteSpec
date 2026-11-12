using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.NamespacePlacement;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The test placement check: every test outside Infrastructure mirrors the folder and namespace of the use case its scenario proves.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class TestPlacementChecks
{
    /// <summary>
    ///     Reports every test whose folder or namespace misses the code folder of the use case its scenario
    ///     proves. A test proving another record carries no folder rule, and an adapter test under its adapter's
    ///     Infrastructure path is exempt by kind. A mark naming no scenario is <c>mark-unknown</c>, not drift.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each marked test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var placed = model.Code.Elements.Where(element => HasTestPlacement(model.Config, element)).ToList();
        var namespaces = NamespacesOf(placed.Where(element => element.Marks.Count > 0).Select(element => element.File));

        foreach (var element in placed)
        {
            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Proves)
                {
                    continue;
                }

                if (!model.ById.TryGetValue(mark.RecordId, out var record)
                    || record.Type != RecordTypes.Scenario)
                {
                    continue;
                }

                var path = UseCasePathOf(record);

                if (path.Module is null || path.Name is null)
                {
                    continue;
                }

                if (!CheckTestFolder(model.Config, element, mark, path, findings))
                {
                    CheckTestNamespace(model.Config, element, mark, path, namespaces, findings);
                }
            }
        }
    }

    /// <summary>Reports the test folder that misses the code folder of the scenario's use case.</summary>
    /// <param name="config">The project configuration that holds each test root.</param>
    /// <param name="element">The marked test whose folder is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's folder path states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckTestFolder(ConsumerConfig config, MarkedElement element, CodeMark mark, PlacementPath path, List<Finding> findings)
    {
        if (TestSegments(config, element.File) is not { } segments)
        {
            return false;
        }

        // Tests mirror the module tree, so a UseCases level states no module,
        // aggregate or use case segment of its own. A module-level UseCases folder
        // holding an aggregate-owned use case keeps its name, so the aggregate
        // check below names it.
        var kept = new List<string> { segments[0] };

        for (var index = 1; index < segments.Length - 1; index++)
        {
            if (!segments[index].Equals("UseCases", StringComparison.OrdinalIgnoreCase)
                || (path.Aggregate is not null && index == 1))
            {
                kept.Add(segments[index]);
            }
        }

        kept.Add(segments[^1]);
        segments = [.. kept];

        if (!SameName(segments[0], path.Module!))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (path.Aggregate is not null && segments.Length >= 3 && !SameName(segments[1], path.Aggregate))
        {
            if (SameName(path.Aggregate, path.Module!))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        var useCaseAt = path.Aggregate is not null && segments.Length >= 3 && SameName(segments[1], path.Aggregate) ? 2 : 1;

        if (segments.Length > useCaseAt + 1 && !SameName(segments[useCaseAt], path.Name!))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[useCaseAt]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        return false;
    }

    /// <summary>Reports the test namespace that names another path than the scenario's use case.</summary>
    /// <param name="config">The project configuration that holds the product name.</param>
    /// <param name="element">The marked test whose namespace is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's folder path states.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestNamespace(ConsumerConfig config, MarkedElement element, CodeMark mark, PlacementPath path, Dictionary<string, string> namespaces, List<Finding> findings)
    {
        var product = SourceRoots.ProductName(config);

        if (product.Length == 0)
        {
            return;
        }

        if (NamespaceOf(element.File, namespaces) is not { Length: > 0 } actual)
        {
            return;
        }

        var expected = new List<string> { Naming.Pascal(path.Module!) };

        if (path.Aggregate is not null && !SameName(path.Aggregate, path.Module!))
        {
            expected.Add(Naming.Pascal(path.Aggregate));
        }

        expected.Add(Naming.Pascal(path.Name!));

        if (PlacedSegments(product, actual) is not { } placed
            || (!NamespaceMatches(placed, expected) && !NamespaceMatches(placed, WithModuleNamedAggregate(path, expected))))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
        }
    }

    /// <summary>Whether the element is a test outside the Infrastructure layer, whose folder and namespace follow its scenario.</summary>
    /// <param name="config">The project configuration naming the layer roots.</param>
    /// <param name="element">The marked code element.</param>
    /// <returns>True when the element is a test whose placement is checked.</returns>
    private static bool HasTestPlacement(ConsumerConfig config, MarkedElement element) =>
        element.Kind == CodeKind.Test
        && SourceRoots.LayerOf(config, element.File) != "Infrastructure"
        && !SourceRoots.IsUnder(config.InfrastructureRoot, element.File)
        && ArchitectureLayerResolver.FolderLayer(element.File) != ArchitectureLayer.Infrastructure;

    /// <summary>The test path segments under the test project, or null when the file sits outside every test root or in a project root.</summary>
    /// <param name="config">The project configuration that holds each test root.</param>
    /// <param name="file">The full path of the test file.</param>
    /// <returns>The path segments with the module first and the file name last, or null outside the test mirror.</returns>
    private static string[]? TestSegments(ConsumerConfig config, string file)
    {
        foreach (var root in config.TestRoots)
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            var segments = PathText.Relative(root, file).Split('/');

            if (segments.Length >= 3)
            {
                return segments.Skip(1).ToArray();
            }

            return null;
        }

        return null;
    }
}
