using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.PlanWaves;

/// <summary>
///     The conflict-free parallel slices of a consumer: one slice per aggregate
///     use-case set, the waves in dependency order, and the shared paths every
///     slice leaves alone. The command prints this plan; nothing writes it.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class PlanWavesPlan
{
    /// <summary>One parallel slice: the aggregate use-case set with its exclusive path globs.</summary>
    /// <param name="Id">The aggregate id, or the module id for a module-level slice.</param>
    /// <param name="Module">The module id the slice belongs to.</param>
    /// <param name="Records">The member record ids in ordinal order.</param>
    /// <param name="Paths">The exclusive path globs in print order.</param>
    public sealed record Slice(string Id, string Module, IReadOnlyList<string> Records, IReadOnlyList<string> Paths);

    /// <summary>The planned waves: the slices, the waves holding them, and the shared frozen paths.</summary>
    /// <param name="Slices">The slices in ordinal id order.</param>
    /// <param name="Waves">The wave per depth level, each holding its slice ids in ordinal order.</param>
    /// <param name="Frozen">The shared Kernel, Platform and Contracts globs, listed once.</param>
    public sealed record Plan(
        IReadOnlyList<Slice> Slices,
        IReadOnlyList<IReadOnlyList<string>> Waves,
        IReadOnlyList<string> Frozen);

    /// <summary>Builds the slices, waves and frozen paths from the built project model.</summary>
    /// <param name="model">The built project model the slices read.</param>
    /// <returns>The planned slices, waves and frozen paths.</returns>
    public static Plan Build(ProjectModel model)
    {
        var aggregates = model.Records
            .Where(record => record.Type == RecordTypes.Aggregate && !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        var aggregateByDir = new Dictionary<string, Record>(StringComparer.Ordinal);

        foreach (var aggregate in aggregates)
        {
            var folder = Path.GetDirectoryName(aggregate.File) ?? "";

            if (!aggregateByDir.TryGetValue(folder, out var held)
                || string.CompareOrdinal(aggregate.Id, held.Id) < 0)
            {
                aggregateByDir[folder] = aggregate;
            }
        }

        var aggregateById = aggregates.ToDictionary(record => record.Id, StringComparer.Ordinal);
        var moduleById = model.Records
            .Where(record => record.Type == RecordTypes.Module && !record.Derived)
            .ToDictionary(record => record.Id, StringComparer.Ordinal);

        var membersByAggregate = new Dictionary<string, List<Record>>(StringComparer.Ordinal);
        var membersByModule = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var member in model.Records
                     .Where(record => !record.Derived
                         && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job)
                     .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var owner = member.File.Length > 0 ? AggregateFolderNames.OwnerDirectory(member.File) : null;

            if (owner is not null && aggregateByDir.ContainsKey(owner))
            {
                if (!membersByAggregate.TryGetValue(owner, out var held))
                {
                    held = [];
                    membersByAggregate[owner] = held;
                }

                held.Add(member);
            }
            else if (member.Module is { Length: > 0 } moduleName)
            {
                if (!membersByModule.TryGetValue(moduleName, out var held))
                {
                    held = [];
                    membersByModule[moduleName] = held;
                }

                held.Add(member);
            }
        }

        var slices = new List<Slice>();
        var sliceByRecordId = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var aggregate in aggregates)
        {
            var folder = Path.GetDirectoryName(aggregate.File) ?? "";

            if (!membersByAggregate.TryGetValue(folder, out var members))
            {
                continue;
            }

            var module = ModuleOf(aggregate.Id, moduleById);
            var slice = new Slice(
                aggregate.Id,
                module,
                [.. members.Select(member => member.Id)],
                PathsFor(model.Config, aggregate, null, members));
            slices.Add(slice);
            sliceByRecordId[aggregate.Id] = aggregate.Id;

            foreach (var member in members)
            {
                sliceByRecordId[member.Id] = aggregate.Id;
            }
        }

        foreach (var (moduleName, members) in membersByModule.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var moduleId = moduleById.Values
                .Where(record => Identifiers.TrySplit(record.Id, out _, out _, out var name)
                    && string.Equals(name, moduleName, StringComparison.Ordinal))
                .Select(record => record.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault() ?? $"module/{moduleName}";

            var slice = new Slice(
                moduleId,
                moduleId,
                [.. members.Select(member => member.Id)],
                PathsFor(model.Config, null, moduleName, members));
            slices.Add(slice);

            foreach (var member in members)
            {
                sliceByRecordId[member.Id] = moduleId;
            }
        }

        // Every other record under an aggregate folder resolves to that aggregate's
        // slice, so an event or rule a member cites pulls in the owning slice.
        foreach (var record in model.Records.Where(record => !record.Derived && record.File.Length > 0))
        {
            if (sliceByRecordId.ContainsKey(record.Id))
            {
                continue;
            }

            var owner = AggregateFolderNames.OwnerDirectory(record.File);

            if (owner is not null
                && aggregateByDir.TryGetValue(owner, out var aggregate)
                && sliceByRecordId.ContainsKey(aggregate.Id))
            {
                sliceByRecordId[record.Id] = aggregate.Id;
            }
        }

        var dependencies = slices.ToDictionary(
            slice => slice.Id,
            slice => (ISet<string>)new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            foreach (var record in model.Records.Where(record =>
                         !record.Derived && sliceByRecordId.TryGetValue(record.Id, out var owner) && owner == slice.Id))
            {
                foreach (var reference in ReferencesOf(record))
                {
                    if (sliceByRecordId.TryGetValue(reference, out var target) && target != slice.Id)
                    {
                        dependencies[slice.Id].Add(target);
                    }
                    else if (moduleById.TryGetValue(reference, out var module))
                    {
                        foreach (var owned in slices.Where(candidate => candidate.Module == module.Id))
                        {
                            if (owned.Id != slice.Id)
                            {
                                dependencies[slice.Id].Add(owned.Id);
                            }
                        }
                    }
                }
            }
        }

        var depths = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            DepthOf(slice.Id, dependencies, depths);
        }

        var waves = depths.Values
            .Distinct()
            .OrderBy(depth => depth)
            .Select(depth => (IReadOnlyList<string>)slices
                .Where(slice => depths[slice.Id] == depth)
                .Select(slice => slice.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList())
            .ToList();

        return new Plan(slices, waves, FrozenFor(model.Config));
    }

    /// <summary>The depth of a slice: one more than its deepest dependency, or zero with none.</summary>
    /// <param name="slice">The slice id.</param>
    /// <param name="dependencies">The slice dependencies by slice id.</param>
    /// <param name="depths">The computed depths, holding zero while a slice is on the stack.</param>
    /// <returns>The depth of the slice.</returns>
    static int DepthOf(string slice, IReadOnlyDictionary<string, ISet<string>> dependencies, Dictionary<string, int> depths)
    {
        if (depths.TryGetValue(slice, out var depth))
        {
            return depth;
        }

        depths[slice] = 0;

        var deepest = 0;

        foreach (var dependency in dependencies[slice])
        {
            deepest = Math.Max(deepest, DepthOf(dependency, dependencies, depths) + 1);
        }

        depths[slice] = deepest;
        return deepest;
    }

    /// <summary>The module id of an aggregate id, or the id itself when it names no module record.</summary>
    /// <param name="aggregateId">The aggregate id.</param>
    /// <param name="moduleById">The module records by id.</param>
    /// <returns>The owning module id.</returns>
    static string ModuleOf(string aggregateId, IReadOnlyDictionary<string, Record> moduleById)
    {
        if (Identifiers.TrySplit(aggregateId, out _, out var home, out _)
            && home is not null)
        {
            var module = moduleById.Values
                .Where(record => Identifiers.TrySplit(record.Id, out _, out _, out var name)
                    && string.Equals(name, home, StringComparison.Ordinal))
                .Select(record => record.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (module is not null)
            {
                return module;
            }

            return $"module/{home}";
        }

        return aggregateId;
    }

    /// <summary>Every record id one record points at across modules or aggregates.</summary>
    /// <param name="record">The record to read.</param>
    /// <returns>The referenced record ids.</returns>
    static List<string> ReferencesOf(Record record)
    {
        var references = new List<string>();
        references.AddRange(record.Ids(RecordFields.Reads));
        references.AddRange(record.Ids(RecordFields.Writes));
        references.AddRange(record.Ids(RecordFields.Runs));
        references.AddRange(record.Ids(RecordFields.TriggeredBy));
        references.AddRange(record.Ids(RecordFields.Translates));
        references.AddRange(record.Ids(RecordFields.DependsOn));
        references.AddRange(record.EdgeIds(RecordFields.Runs));
        references.AddRange(record.EdgeIds(RecordFields.Sends));
        references.AddRange(record.EdgeIds(RecordFields.Calls));
        references.AddRange(record.EdgeIds(RecordFields.Queries));

        foreach (var input in record.Objects(RecordFields.Inputs))
        {
            if (input.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                continue;
            }

            if (input.TryGetProperty(RecordFields.Identifies, out var identifies))
            {
                if (identifies.ValueKind == System.Text.Json.JsonValueKind.String
                    && identifies.GetString() is { Length: > 0 } single)
                {
                    references.Add(single);
                }
                else if (identifies.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    references.AddRange(identifies.EnumerateArray()
                        .Where(entry => entry.ValueKind == System.Text.Json.JsonValueKind.String)
                        .Select(entry => entry.GetString()!));
                }
            }
        }

        foreach (var transition in record.Objects(RecordFields.Transitions))
        {
            if (transition.ValueKind == System.Text.Json.JsonValueKind.Object
                && transition.TryGetProperty(RecordFields.MovedBy, out var movedBy)
                && movedBy.ValueKind == System.Text.Json.JsonValueKind.String
                && movedBy.GetString() is { Length: > 0 } mover)
            {
                references.Add(mover);
            }
        }

        return references;
    }

    /// <summary>The exclusive path globs of one slice: its record folder, then its code folders.</summary>
    /// <param name="config">The consumer configuration holding the roots.</param>
    /// <param name="aggregate">The aggregate record, or null for a module-level slice.</param>
    /// <param name="moduleName">The module folder name for a module-level slice, else null.</param>
    /// <param name="members">The member records of the slice.</param>
    /// <returns>The globs in print order.</returns>
    static List<string> PathsFor(
        ConsumerConfig config,
        Record? aggregate,
        string? moduleName,
        IReadOnlyList<Record> members)
    {
        var paths = new List<string>();

        if (aggregate is not null)
        {
            var folder = Path.GetDirectoryName(aggregate.File) ?? "";

            if (folder.Length > 0)
            {
                paths.Add(PathText.Relative(config.Root, folder) + "/**");
            }

            if (Identifiers.TrySplit(aggregate.Id, out _, out var home, out var name) && home is not null)
            {
                paths.AddRange(CodeGlobs(config, home, Naming.PluralPascal(name)));
            }

            foreach (var frontend in config.Frontends)
            {
                var relative = PathText.Relative(config.Root, frontend.ModulesRoot);

                if (!LeavesRoot(relative) && Identifiers.TrySplit(aggregate.Id, out _, out var owner, out var owned) && owner is not null)
                {
                    paths.Add($"{relative}/{owner}/{Naming.PluralKebab(owned)}/**");
                }
            }
        }
        else if (moduleName is not null)
        {
            foreach (var member in members)
            {
                var folder = Path.GetDirectoryName(member.File) ?? "";

                if (folder.Length > 0
                    && !paths.Contains(PathText.Relative(config.Root, folder) + "/**", StringComparer.Ordinal))
                {
                    paths.Add(PathText.Relative(config.Root, folder) + "/**");
                }

                if (Identifiers.TrySplit(member.Id, out _, out var home, out var name) && home is not null)
                {
                    foreach (var glob in CodeGlobs(config, home, Naming.Pascal(name)))
                    {
                        if (!paths.Contains(glob, StringComparer.Ordinal))
                        {
                            paths.Add(glob);
                        }
                    }
                }
            }
        }

        return paths;
    }

    /// <summary>The code globs of one aggregate or use case folder across the module layers.</summary>
    /// <param name="config">The consumer configuration holding the module roots.</param>
    /// <param name="moduleName">The record module name.</param>
    /// <param name="folder">The PascalCase code folder.</param>
    /// <returns>The Application, WebApi and Infrastructure globs, or none outside the root.</returns>
    static List<string> CodeGlobs(ConsumerConfig config, string moduleName, string folder)
    {
        var root = ModuleRootFor(config, moduleName);

        if (root.Length == 0)
        {
            return [];
        }

        var relative = PathText.Relative(config.Root, root);

        if (LeavesRoot(relative))
        {
            return [];
        }

        return
        [
            $"{relative}/Application/{folder}/**",
            $"{relative}/WebApi/{folder}/**",
            $"{relative}/Infrastructure/{folder}/**",
        ];
    }

    /// <summary>The module root holding a record module: the root whose folder names it, else the first.</summary>
    /// <param name="config">The consumer configuration holding the module roots.</param>
    /// <param name="moduleName">The record module name.</param>
    /// <returns>The absolute module root, or empty when the project declares none.</returns>
    static string ModuleRootFor(ConsumerConfig config, string moduleName)
    {
        var pascal = Naming.Pascal(moduleName);

        foreach (var root in config.ModulesRoots)
        {
            var folder = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar));

            if (string.Equals(folder, pascal, StringComparison.OrdinalIgnoreCase)
                || folder.EndsWith("." + pascal, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }
        }

        return config.ModulesRoots.Count > 0 ? config.ModulesRoots[0] : "";
    }

    /// <summary>The shared frozen globs: the Kernel, the Platform and the Contracts projects that exist.</summary>
    /// <param name="config">The consumer configuration holding the roots.</param>
    /// <returns>The frozen globs in print order.</returns>
    static List<string> FrozenFor(ConsumerConfig config)
    {
        var frozen = new List<string>();

        if (config.KernelRoot.Length > 0 && Directory.Exists(config.KernelRoot))
        {
            var relative = PathText.Relative(config.Root, config.KernelRoot);

            if (!LeavesRoot(relative))
            {
                frozen.Add(relative + "/**");
            }
        }

        if (config.PlatformRoot.Length > 0 && Directory.Exists(config.PlatformRoot))
        {
            var relative = PathText.Relative(config.Root, config.PlatformRoot);

            if (!LeavesRoot(relative))
            {
                frozen.Add(relative + "/**");
            }
        }

        foreach (var root in config.ModulesRoots)
        {
            var parent = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar));

            if (parent is null || !Directory.Exists(parent))
            {
                continue;
            }

            var folder = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar));

            foreach (var contracts in Directory.GetDirectories(parent, folder + ".Contracts"))
            {
                var relative = PathText.Relative(config.Root, contracts);

                if (!LeavesRoot(relative) && !frozen.Contains(relative + "/**", StringComparer.Ordinal))
                {
                    frozen.Add(relative + "/**");
                }
            }
        }

        frozen.Sort(StringComparer.Ordinal);
        return frozen;
    }

    /// <summary>Whether a consumer-relative path leaves the consumer root.</summary>
    /// <param name="relative">The relative path with forward slashes.</param>
    /// <returns>True when the path climbs above the root.</returns>
    static bool LeavesRoot(string relative) =>
        relative == ".." || relative.StartsWith("../", StringComparison.Ordinal);
}
