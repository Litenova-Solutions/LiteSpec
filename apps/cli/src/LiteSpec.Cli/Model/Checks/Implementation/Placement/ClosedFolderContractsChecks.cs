using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the Contracts projects: each module Contracts folder with one folder per declared aggregate and public module-level use case, and each use case folder with its message files only.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>Reports every folder under the module contracts that names no declared aggregate or public module-level use case of the owner, every file directly beside them, every interface, every fact outside its On fact folder, and every command of no public use case.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsLevel(ConsumerConfig config, string contracts, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, List<Finding> findings)
    {
        foreach (var child in ChildFolders(contracts))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Any(aggregate => SameName(owner, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                CheckContractsAggregate(config, child, owner, name, aggregates, useCases, publicUseCases, facts, findings);
                continue;
            }

            if (publicUseCases.Any(useCase => SameName(owner, useCase.Module) && useCase.Aggregate is null && SameName(name, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            var singular = aggregates.FirstOrDefault(aggregate => SameName(owner, aggregate.Module) && SameAggregate(name, aggregate.Aggregate));

            if (singular.Module is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate; the aggregate folder is '{singular.Expected}/'.", child, 1, null));
                continue;
            }

            var flat = useCases.FirstOrDefault(useCase => SameName(owner, useCase.Module) && SameName(name, useCase.Name));

            if (flat.Module is not null)
            {
                if (flat.Aggregate is not null
                    && publicUseCases.Any(useCase => SameName(owner, useCase.Module) && SameName(name, useCase.Name)))
                {
                    var expected = ExpectedOf(aggregates, owner, flat.Aggregate);
                    findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names the public use case '{flat.Name}', which sits in its aggregate folder '{expected}/'.", child, 1, null));
                    continue;
                }

                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names '{flat.Name}', which is no public use case; only public use cases sit in Contracts.", child, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate or public module-level use case of module '{owner}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(contracts))
        {
            // The Contracts project file lives at the project root.
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Contracts/, which holds only one folder per aggregate and public module-level use case.", file, 1, null));
        }

        CheckContractsTypes(config, contracts, owner, aggregates, publicUseCases, findings);
    }

    /// <summary>Reports every folder under one Contracts aggregate folder that names no public use case of that aggregate and no On fact folder, every fact directly beside them, every query or command message or result directly beside them, and every declared fact with no On fact folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Contracts.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsAggregate(ConsumerConfig config, string aggregate, string owner, string name, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, List<Finding> findings)
    {
        var owned = publicUseCases
            .Where(useCase => SameName(owner, useCase.Module)
                && useCase.Aggregate is not null
                && SameName(name, ExpectedOf(aggregates, owner, useCase.Aggregate)))
            .ToList();
        var children = ChildFolders(aggregate);

        foreach (var child in children)
        {
            var childName = Path.GetFileName(child);

            if (FactOf(owner, name, aggregates, facts, childName) is { } fact)
            {
                CheckContractsFact(config, child, owner, name, aggregates, facts, fact, findings);
                continue;
            }

            var elsewhere = facts.FirstOrDefault(candidate =>
                SameName(owner, candidate.Module)
                && candidate.Aggregate is not null
                && !SameAggregate(name, candidate.Aggregate)
                && string.Equals(childName, OnFolderOf(candidate.Name), StringComparison.OrdinalIgnoreCase));

            if (elsewhere.Id is not null)
            {
                var home = ExpectedOf(aggregates, owner, elsewhere.Aggregate!);
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' holds the fact '{elsewhere.Id}'; it belongs in '{home}/{OnFolderOf(elsewhere.Name)}/'.", child, 1, elsewhere.Id));
                continue;
            }

            if (owned.Any(useCase => SameName(childName, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            var level = publicUseCases.FirstOrDefault(useCase =>
                SameName(owner, useCase.Module) && useCase.Aggregate is null && SameName(childName, useCase.Name));

            if (level.Id is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names the public use case '{level.Name}', which sits directly in Contracts/.", child, 1, null));
                continue;
            }

            var flat = useCases.FirstOrDefault(useCase =>
                SameName(owner, useCase.Module)
                && useCase.Aggregate is not null
                && SameName(name, ExpectedOf(aggregates, owner, useCase.Aggregate))
                && SameName(childName, useCase.Name));

            if (flat.Module is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names '{flat.Name}', which is no public use case; only public use cases sit in Contracts.", child, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no public use case or On fact folder of aggregate '{name}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(aggregate))
        {
            var fileName = Path.GetFileName(file);
            var factSuffix = FactSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (factSuffix is not null)
            {
                var factKind = factSuffix.StartsWith("Module", StringComparison.Ordinal) ? "module" : "integration";
                var factPrefix = fileName[..^factSuffix.Length];
                var factMatch = facts.FirstOrDefault(fact =>
                    SameName(owner, fact.Module)
                    && fact.Kind == factKind
                    && SameName(factPrefix, fact.Name));
                var expected = factMatch.Id is not null && factMatch.Aggregate is not null
                    ? ExpectedOf(aggregates, owner, factMatch.Aggregate)
                    : name;
                var folder = factMatch.Id is not null ? OnFolderOf(factMatch.Name) : "On" + factPrefix;

                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a fact outside its On fact folder; it belongs in '{expected}/{folder}/'.", file, 1, null));
                continue;
            }

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
                var shape = kind == "query" ? "a query message" : "a command message";
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds {shape} outside a use case folder.", file, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the message of public {kind} '{match.Id}'; it belongs in '{Naming.Pascal(match.Name)}/'.", file, 1, null));
        }

        var held = children
            .Select(child => Path.GetFileName(child))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var fact in facts
            .Where(fact => SameName(owner, fact.Module)
                && fact.Aggregate is not null
                && SameAggregate(name, fact.Aggregate))
            .OrderBy(fact => fact.Id, StringComparer.Ordinal))
        {
            var folder = OnFolderOf(fact.Name);

            if (!held.Contains(folder))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, aggregate)}' holds no '{folder}/' folder for declared {fact.Kind} event '{fact.Id}'.", aggregate, 1, fact.Id));
            }
        }
    }

    /// <summary>The On fact folder a declared fact takes under its Contracts aggregate folder: On with the PascalCase record name.</summary>
    /// <param name="name">The fact name segment of its identifier, such as session-ended.</param>
    /// <returns>The folder name, such as OnSessionEnded.</returns>
    private static string OnFolderOf(string name) => "On" + Naming.Pascal(name);

    /// <summary>The declared fact an On fact folder names, or null when it names none: a fact of the owner module whose On folder the child spells and whose aggregate the folder sits under.</summary>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="childName">The child folder name as written on disk.</param>
    /// <returns>The declared fact the folder names, or null.</returns>
    private static (string Module, string? Aggregate, string Name, string Id, string Kind)? FactOf(string owner, string name, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, string childName)
    {
        foreach (var fact in facts)
        {
            if (!SameName(owner, fact.Module)
                || !string.Equals(childName, OnFolderOf(fact.Name), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (fact.Aggregate is null || SameAggregate(name, fact.Aggregate))
            {
                return fact;
            }
        }

        return null;
    }

    /// <summary>Reports every subfolder under one Contracts On fact folder, which holds files only, every fact file in it that names no declared fact, and every fact outside the aggregate of the domain event it translates. The payload types only the fact carries sit beside it.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="folder">The absolute On fact folder under Contracts.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregate">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="fact">The declared fact the folder names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsFact(ConsumerConfig config, string folder, string owner, string aggregate, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, (string Module, string? Aggregate, string Name, string Id, string Kind) fact, List<Finding> findings)
    {
        var folderName = Path.GetFileName(folder);

        foreach (var child in ChildFolders(folder))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in '{aggregate}/{folderName}/', which holds files only.", child, 1, null));
        }

        foreach (var file in ChildFiles(folder))
        {
            var fileName = Path.GetFileName(file);
            var suffix = FactSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (suffix is null)
            {
                continue;
            }

            var kind = suffix.StartsWith("Module", StringComparison.Ordinal) ? "module" : "integration";
            var prefix = fileName[..^suffix.Length];
            var match = facts.FirstOrDefault(candidate =>
                SameName(owner, candidate.Module)
                && candidate.Kind == kind
                && SameName(prefix, candidate.Name));

            if (match.Id is null)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a {kind} event naming no declared {kind} event of module '{owner}'.", file, 1, null));
                continue;
            }

            if (!string.Equals(match.Id, fact.Id, StringComparison.Ordinal))
            {
                var home = match.Aggregate is null ? aggregate : ExpectedOf(aggregates, owner, match.Aggregate);
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the fact '{match.Id}'; it belongs in '{home}/{OnFolderOf(match.Name)}/'.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every interface and every command file under the module contracts that is no public command's message in its use case folder, which publish data only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsTypes(ConsumerConfig config, string contracts, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        List<string> files;

        try
        {
            if (!Directory.Exists(contracts))
            {
                return;
            }

            files = Directory.EnumerateFiles(contracts, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(contracts, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (CommandSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal))
                && ContractsCommandFinding(config, contracts, file, fileName, owner, aggregates, publicUseCases) is { } message)
            {
                findings.Add(new Finding("code-placement-drift", message, file, 1, null));
            }

            foreach (var face in InterfacesOf(file))
            {
                findings.Add(new Finding("code-placement-drift", $"Interface '{face.Name}' sits in Contracts, which publishes data only.", face.File, face.Line, null));
            }
        }
    }

    /// <summary>The finding message for a command file under the module contracts, or null when the file is its public command's message in its use case folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="file">The absolute command file under Contracts.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <returns>The finding message, or null when the file is allowed or the folder checks own it.</returns>
    private static string? ContractsCommandFinding(ConsumerConfig config, string contracts, string file, string fileName, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases)
    {
        var segments = PathText.Relative(contracts, file).Split('/');
        var suffix = CommandSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

        if (suffix is null)
        {
            return null;
        }

        var prefix = fileName[..^suffix.Length];
        var relative = PathText.Relative(config.Root, file);

        // The folder checks own a file directly in Contracts/, a file directly in an
        // aggregate folder, and a file nested past a use case folder, so those stay
        // silent here.
        if (segments.Length == 1
            || (segments.Length == 2 && aggregates.Any(aggregate => SameName(owner, aggregate.Module) && SameName(segments[0], aggregate.Expected)))
            || segments.Length > 3)
        {
            return null;
        }

        var home = segments.Length == 2
            ? PublicCommandHome(publicUseCases, owner, aggregate: null, segments[0])
            : PublicCommandHome(publicUseCases, owner, segments[0], segments[1], aggregates);

        if (home is not null && SameName(prefix, home.Value.Name))
        {
            return null;
        }

        var named = publicUseCases.FirstOrDefault(useCase =>
            useCase.Kind == "command"
            && SameName(owner, useCase.Module)
            && SameName(prefix, useCase.Name));

        if (named.Id is not null)
        {
            var expected = named.Aggregate is null
                ? $"{PathText.Relative(config.Root, contracts)}/{Naming.Pascal(named.Name)}"
                : $"{PathText.Relative(config.Root, contracts)}/{ExpectedOf(aggregates, owner, named.Aggregate)}/{Naming.Pascal(named.Name)}";

            return $"File '{relative}' holds the message of public command '{named.Id}'; it belongs in '{expected}/'.";
        }

        return $"File '{relative}' holds a command of no public use case; only public use cases sit in Contracts.";
    }

    /// <summary>The public command whose use case folder the segments name, or null when they name none.</summary>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregate">The aggregate folder holding the use case folder, or null for a module-level folder.</param>
    /// <param name="folder">The use case folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder; read only for an aggregate folder.</param>
    /// <returns>The public command the folders name, or null.</returns>
    private static (string Module, string? Aggregate, string Name, string Id, string Kind)? PublicCommandHome(
        IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases,
        string owner,
        string? aggregate,
        string folder,
        IReadOnlyList<(string Module, string Aggregate, string Expected)>? aggregates = null)
    {
        foreach (var useCase in publicUseCases)
        {
            if (useCase.Kind != "command"
                || !SameName(owner, useCase.Module)
                || !SameName(folder, useCase.Name))
            {
                continue;
            }

            if (aggregate is null && useCase.Aggregate is null)
            {
                return useCase;
            }

            if (aggregate is not null
                && useCase.Aggregate is not null
                && aggregates is not null
                && SameName(aggregate, ExpectedOf(aggregates, owner, useCase.Aggregate)))
            {
                return useCase;
            }
        }

        return null;
    }

    /// <summary>One interface a C# file declares: its name, file and line.</summary>
    /// <param name="Name">The simple interface name as declared.</param>
    /// <param name="File">The file declaring the interface.</param>
    /// <param name="Line">The 1-based line of the interface name.</param>
    sealed record ContractsInterface(string Name, string File, int Line);

    /// <summary>The interfaces one C# file declares, in file order.</summary>
    /// <param name="file">The absolute C# file to read.</param>
    /// <returns>Each declared interface in file order; a file that cannot be read reads as empty.</returns>
    private static List<ContractsInterface> InterfacesOf(string file)
    {
        var faces = new List<ContractsInterface>();
        SyntaxTree tree;

        try
        {
            if (!File.Exists(file))
            {
                return faces;
            }

            tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return faces;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            // A C# 14 extension block is a type declaration with no name, so it names no type.
            if (declared.Identifier.IsMissing || declared.Identifier.Span.Length == 0)
            {
                continue;
            }

            var line = tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1;
            faces.Add(new ContractsInterface(declared.Identifier.ValueText, file, line));
        }

        return faces;
    }
}
