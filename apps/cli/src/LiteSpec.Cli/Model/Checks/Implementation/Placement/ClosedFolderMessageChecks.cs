using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for use case message files: every public use case keeps its message and result in Contracts, and every other message, result or transport model sits in its use case folder.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>
    ///     Reports each query or command message or result of a public use case that sits in the module project instead of
    ///     the Contracts project. A public use case keeps its message and result at its use case path in
    ///     Contracts, so a file named for one outside Contracts is drift, and the finding names the expected path.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project or single modules project holding the files.</param>
    /// <param name="allRoots">Every module root, holding the sibling Contracts project of each module.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckPublicUseCaseFiles(ConsumerConfig config, string modulesRoot, IReadOnlyList<string> allRoots, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, List<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        if (publicUseCases.Count == 0)
        {
            return;
        }

        if (SourceRoots.IsSingleModulesProject(modulesRoot))
        {
            foreach (var module in ChildFolders(modulesRoot))
            {
                var name = Path.GetFileName(module);
                var contracts = Path.Combine(module, "Contracts");

                CheckPublicUseCaseTree(config, module, contracts, name, $"{name}/Contracts", aggregates, publicUseCases, findings);
            }

            return;
        }

        if (SourceRoots.IsContractsProject(modulesRoot))
        {
            return;
        }

        var owner = SourceRoots.ModuleNameOf(modulesRoot) ?? Path.GetFileName(modulesRoot);
        var sibling = allRoots.FirstOrDefault(root => SourceRoots.IsContractsProject(root)
            && string.Equals(SourceRoots.ModuleNameOf(root), owner, StringComparison.OrdinalIgnoreCase));
        var display = sibling is null ? "Contracts" : Path.GetFileName(sibling.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        CheckPublicUseCaseTree(config, modulesRoot, contracts: null, owner, display, aggregates, publicUseCases, findings);
    }

    /// <summary>Reports each query or command message or result of a public use case under one module tree that sits outside its Contracts path.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="tree">The absolute module tree to scan.</param>
    /// <param name="contracts">The nested Contracts folder to leave out, or null when the tree holds none.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="display">The Contracts folder name as printed in the expected path.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPublicUseCaseTree(ConsumerConfig config, string tree, string? contracts, string owner, string display, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        List<string> files;

        try
        {
            files = Directory.EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(tree, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Where(file => contracts is null || !file.StartsWith(contracts + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var owned = publicUseCases.Where(useCase => SameName(owner, useCase.Module)).ToList();

        if (owned.Count == 0)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var querySuffix = QuerySuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));
            var commandSuffix = querySuffix is null
                ? CommandSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal))
                : null;

            if (querySuffix is null && commandSuffix is null)
            {
                continue;
            }

            var kind = querySuffix is not null ? "query" : "command";
            var prefix = fileName[..^(querySuffix ?? commandSuffix!).Length];
            var match = owned.FirstOrDefault(useCase => useCase.Kind == kind && SameName(prefix, useCase.Name));

            if (match.Id is null)
            {
                continue;
            }

            var useCaseFolder = Naming.Pascal(match.Name);
            var home = match.Aggregate is null
                ? $"{display}/{useCaseFolder}"
                : $"{display}/{ExpectedOf(aggregates, owner, match.Aggregate)}/{useCaseFolder}";

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the message of public {kind} '{match.Id}'; it belongs in '{home}/'.", file, 1, null));
        }
    }

    /// <summary>The public use cases: query and command use cases whose via names a consuming module, by module, aggregate, name, identifier and kind.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate, name, identifier and kind of each public use case placed in a module folder.</returns>
    private static List<(string Module, string? Aggregate, string Name, string Id, string Kind)> DeclaredPublicUseCases(ProjectModel model)
    {
        var useCases = new List<(string Module, string? Aggregate, string Name, string Id, string Kind)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.UseCase
                || record.Field(RecordFields.Kind) is not ("query" or "command")
                || !record.Ids(RecordFields.Via).Any(IsModuleId))
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is not null && path.Name is not null)
            {
                useCases.Add((path.Module, path.Aggregate, path.Name, record.Id, record.Field(RecordFields.Kind)!));
            }
        }

        return useCases;
    }

    /// <summary>The declared facts by module, aggregate, name, identifier and kind: each module event and each integration event with its record folder's aggregate, or the aggregate of the domain event it translates when its folder states none.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate folder, name, identifier and kind of each fact placed in a module folder.</returns>
    private static List<(string Module, string? Aggregate, string Name, string Id, string Kind)> DeclaredFacts(ProjectModel model)
    {
        var facts = new List<(string Module, string? Aggregate, string Name, string Id, string Kind)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.ModuleEvent && record.Type != RecordTypes.IntegrationEvent)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is null || path.Name is null)
            {
                continue;
            }

            var aggregate = path.Aggregate;

            if (aggregate is null
                && record.Field(RecordFields.Translates) is { } translated
                && model.ById.TryGetValue(translated, out var domainEvent)
                && domainEvent.Type == RecordTypes.DomainEvent)
            {
                aggregate = PathOf(domainEvent).Aggregate;
            }

            var kind = record.Type == RecordTypes.ModuleEvent ? "module" : "integration";
            facts.Add((path.Module, aggregate, path.Name, record.Id, kind));
        }

        return facts;
    }

    /// <summary>Whether the identifier names a module.</summary>
    /// <param name="id">The identifier under test.</param>
    /// <returns>True when the identifier opens with the module prefix.</returns>
    private static bool IsModuleId(string id) =>
        Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix == RecordTypes.Module;

    /// <summary>
    ///     Reports each message, result or transport model file in one module project that sits outside a use case
    ///     folder. A use case's own models live in its folder, so a file named for a use case outside one is drift. The
    ///     finding names the use case folder the file belongs in when the file name's prefix names a declared use case.
    ///     Files under Persistence stay with the persistence check, which names that folder.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding the files.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/backend-architecture#give-each-use-case-its-own-models</follows>
    private static void CheckMessageFiles(ConsumerConfig config, string modulesRoot, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        var single = SourceRoots.IsSingleModulesProject(modulesRoot);

        // A Contracts project holds public use case messages at its own use case paths, which the Contracts level owns.
        if (!single && SourceRoots.IsContractsProject(modulesRoot))
        {
            return;
        }

        var own = single ? null : SourceRoots.ModuleNameOf(modulesRoot);

        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories).OrderBy(child => child, StringComparer.Ordinal))
        {
            var segments = PathText.Relative(modulesRoot, file).Split('/');

            if (segments.Length < 2
                || segments.Any(segment => SourceTree.SkippedFolders.Contains(segment))
                || segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase)
                || segments.Any(segment => segment.Equals("Persistence", StringComparison.OrdinalIgnoreCase))
                || (single && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var fileName = segments[^1];
            var suffix = UseCaseModelSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (suffix is null || InsideUseCase(segments))
            {
                continue;
            }

            var prefix = fileName[..^suffix.Length];
            var match = single
                ? useCases.FirstOrDefault(useCase => SameName(segments[0], useCase.Module) && SameName(prefix, useCase.Name))
                : useCases.FirstOrDefault(useCase => own is not null && SameName(own, useCase.Module) && SameName(prefix, useCase.Name));
            var relative = PathText.Relative(config.Root, file);

            if (match.Module is null)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{relative}' holds a message, result or transport model outside a use case folder.", file, 1, null));
                continue;
            }

            var layer = suffix is "RequestModel.cs" or "ResponseModel.cs" ? "WebApi" : "Application";
            var useCaseFolder = Naming.Pascal(match.Name);
            var home = match.Aggregate is null
                ? $"{layer}/{useCaseFolder}"
                : $"{layer}/{ExpectedOf(aggregates, match.Module, match.Aggregate)}/{useCaseFolder}";
            var belongs = single
                ? $"{segments[0]}/{home}"
                : home;

            findings.Add(new Finding("code-placement-drift", $"File '{relative}' holds a message, result or transport model outside a use case folder; it belongs in '{belongs}'.", file, 1, null));
        }
    }

    /// <summary>Whether a module-relative path sits in a use case folder under Application or WebApi.</summary>
    /// <param name="segments">The module-relative path segments, with the file name last.</param>
    /// <returns>True when a layer folder names a use case folder that holds the file.</returns>
    private static bool InsideUseCase(string[] segments)
    {
        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (UseCaseHomeLayers.Contains(segments[index]) && !FixedLayerFolders.Contains(segments[index + 1]))
            {
                return true;
            }
        }

        return false;
    }
}
