using LiteSpec.Cli.Files;
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
    ///     proves, and every test folder under a module that names no declared aggregate or use case.
    ///     A test proving another record carries no folder rule, and an adapter test under its adapter's
    ///     Infrastructure path is exempt by kind. A mark naming no scenario is <c>mark-unknown</c>, not drift.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each marked test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var placed = model.Code.Elements.Where(element => HasTestPlacement(element)).ToList();
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

                if (!CheckTestFolder(model, element, mark, path, findings))
                {
                    CheckTestNamespace(model, element, mark, path, namespaces, findings);
                }
            }
        }

        CheckTestTree(model, findings);
    }

    /// <summary>Reports every test folder under a module folder that names no declared aggregate or use case of that module. A Support folder, an adapter test path under Infrastructure, and a module with no declared aggregates or use cases state no mirror, so they are left out.</summary>
    /// <param name="model">The loaded project model that holds every declared aggregate and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestTree(ProjectModel model, List<Finding> findings)
    {
        var aggregates = ClosedFolderChecks.DeclaredAggregates(model);
        var useCases = ClosedFolderChecks.DeclaredUseCases(model);

        if (aggregates.Count == 0 && useCases.Count == 0)
        {
            return;
        }

        foreach (var root in model.Config.TestRoots)
        {
            if (root.Length == 0 || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var project in ChildFolders(root))
            {
                CheckTestProject(model, project, aggregates, useCases, findings);
            }
        }
    }

    /// <summary>Reports every aggregate and use case folder under each module folder of one test project that names no declared record.</summary>
    /// <param name="model">The loaded project model that holds the consumer root for relative paths.</param>
    /// <param name="project">The absolute test project folder.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestProject(ProjectModel model, string project, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        foreach (var module in ChildFolders(project))
        {
            var moduleName = Path.GetFileName(module);

            if (ExemptTestFolder(moduleName))
            {
                continue;
            }

            var ownedAggregates = aggregates
                .Where(aggregate => SameName(moduleName, aggregate.Module))
                .ToList();
            var ownedUseCases = useCases
                .Where(useCase => SameName(moduleName, useCase.Module))
                .ToList();

            if (ownedAggregates.Count == 0 && ownedUseCases.Count == 0)
            {
                continue;
            }

            foreach (var child in ChildFolders(module))
            {
                CheckTestModuleChild(model, moduleName, child, ownedAggregates, ownedUseCases, findings);
            }
        }
    }

    /// <summary>Reports one folder under a test module folder that names no declared aggregate or module-level use case of the module, and descends into an aggregate folder.</summary>
    /// <param name="model">The loaded project model that holds the consumer root for relative paths.</param>
    /// <param name="moduleName">The test module folder name as written on disk.</param>
    /// <param name="child">The absolute child folder under the test module folder.</param>
    /// <param name="ownedAggregates">The declared aggregates of the module.</param>
    /// <param name="ownedUseCases">The declared use cases of the module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestModuleChild(ProjectModel model, string moduleName, string child, IReadOnlyList<(string Module, string Aggregate, string Expected)> ownedAggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> ownedUseCases, List<Finding> findings)
    {
        var name = Path.GetFileName(child);

        if (ExemptTestFolder(name))
        {
            return;
        }

        if (ownedAggregates.Any(aggregate => SameName(name, aggregate.Expected)))
        {
            foreach (var useCase in ChildFolders(child))
            {
                var useCaseName = Path.GetFileName(useCase);

                if (ExemptTestFolder(useCaseName))
                {
                    continue;
                }

                var home = ownedAggregates.First(aggregate => SameName(name, aggregate.Expected)).Aggregate;

                if (!ownedUseCases.Any(declared => declared.Aggregate is not null && SameAggregate(declared.Aggregate, home) && SameName(useCaseName, declared.Name)))
                {
                    findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(model.Config.Root, useCase)}' names no use case of aggregate '{name}'.", useCase, 1, null));
                }
            }

            return;
        }

        if (ownedUseCases.Any(declared => declared.Aggregate is null && SameName(name, declared.Name)))
        {
            return;
        }

        findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(model.Config.Root, child)}' names no declared aggregate or module-level use case of module '{moduleName}'.", child, 1, null));
    }

    /// <summary>Whether a test folder is exempt from the mirror: shared support, or adapter tests under their Infrastructure path.</summary>
    /// <param name="name">The folder name as written on disk.</param>
    /// <returns>True for a Support or Infrastructure folder; otherwise false.</returns>
    private static bool ExemptTestFolder(string name) =>
        string.Equals(name, "Support", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "Infrastructure", StringComparison.OrdinalIgnoreCase);

    /// <summary>The child folders of one folder in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="folder">The absolute folder to read.</param>
    /// <returns>The child folders in ordinal order, without build output and tool folders.</returns>
    private static List<string> ChildFolders(string folder)
    {
        try
        {
            return Directory.EnumerateDirectories(folder)
                .Where(child => !SourceTree.SkippedFolders.Contains(Path.GetFileName(child)))
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Reports the test folder that misses the code folder of the scenario's use case.</summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="element">The marked test whose folder is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's folder path states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckTestFolder(ProjectModel model, MarkedElement element, CodeMark mark, PlacementPath path, List<Finding> findings)
    {
        if (TestSegments(model.Config, element.File) is not { } segments)
        {
            return false;
        }

        // Tests mirror the module, the plural aggregate folder and the use case, so the
        // first segment names the module, the next the aggregate, and the next the use case.
        // A module-level use case drops the aggregate segment.
        if (!SameName(segments[0], path.Module!))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (segments.Length == 2)
        {
            return false;
        }

        if (path.Aggregate is null)
        {
            // Nested under another folder the test reports that folder.
            if (segments.Length >= 4 && SameName(segments[2], path.Name!))
            {
                findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                return true;
            }

            if (!SameName(segments[1], path.Name!))
            {
                findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                return true;
            }

            return false;
        }

        var expected = ExpectedAggregateFolder(model, path.Module!, path.Aggregate);
        var useCase = Naming.Pascal(path.Name!);

        if (segments.Length >= 4 && SameName(segments[2], path.Name!))
        {
            if (SameName(segments[1], expected))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}', so it belongs in '{segments[0]}/{expected}/{useCase}/'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}', so it belongs in '{segments[0]}/{expected}/{useCase}/'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>Reports the test namespace that names another path than the scenario's use case.</summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="element">The marked test whose namespace is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's folder path states.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestNamespace(ProjectModel model, MarkedElement element, CodeMark mark, PlacementPath path, Dictionary<string, string> namespaces, List<Finding> findings)
    {
        var product = SourceRoots.ProductName(model.Config);

        if (product.Length == 0)
        {
            return;
        }

        if (NamespaceOf(element.File, namespaces) is not { Length: > 0 } actual)
        {
            return;
        }

        var expected = new List<string> { Naming.Pascal(path.Module!) };

        if (path.Aggregate is not null)
        {
            expected.Add(ExpectedAggregateFolder(model, path.Module!, path.Aggregate));
        }

        expected.Add(Naming.Pascal(path.Name!));

        if (PlacedSegments(product, actual) is not { } placed
            || !NamespaceMatches(placed, expected))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
        }
    }

    /// <summary>Whether the element is a test outside the Infrastructure layer, whose folder and namespace follow its scenario.</summary>
    /// <param name="element">The marked code element.</param>
    /// <returns>True when the element is a test whose placement is checked.</returns>
    private static bool HasTestPlacement(MarkedElement element) =>
        element.Kind == CodeKind.Test
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
