using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The folder checks: every folder is one of the closed set of its layer and level, every module folder names a declared module, and every record sits in the folder and level its type allows.</summary>
/// <follows>litespec/core/model#write-every-record-in-its-home</follows>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
/// <follows>litespec/core/model#hold-module-rules-in-rule-sets</follows>
internal static class FolderChecks
{
    /// <summary>Type folders directly under <c>intent/</c>, beside <c>modules/</c>.</summary>
    private static readonly HashSet<string> IntentRootFolders = new(StringComparer.Ordinal)
    {
        "goals", "problems", "constraints", "decisions", "research", "actors", "journeys", "roles",
    };

    /// <summary>Type folders under <c>intent/modules/&lt;m&gt;/</c>.</summary>
    private static readonly HashSet<string> IntentModuleFolders = new(StringComparer.Ordinal)
    {
        "goals", "problems", "constraints", "decisions", "research", "features",
    };

    /// <summary>Type folders under <c>behavior/modules/&lt;m&gt;/&lt;aggregate&gt;/</c>.</summary>
    private static readonly HashSet<string> BehaviorAggregateFolders = new(StringComparer.Ordinal)
    {
        "rules", "events",
    };

    /// <summary>Folders directly under <c>implementation/</c>.</summary>
    private static readonly HashSet<string> ImplementationRootFolders = new(StringComparer.Ordinal)
    {
        "decisions", "modules", "entry-points", "api", "guides", "patterns", "writing", "operations", "runbooks",
    };

    /// <summary>Folders under <c>implementation/modules/&lt;m&gt;/</c>.</summary>
    private static readonly HashSet<string> ImplementationModuleFolders = new(StringComparer.Ordinal)
    {
        "decisions", "runbooks",
    };

    /// <summary>Folders under <c>implementation/</c> whose documents nest freely: no record type lives there.</summary>
    private static readonly HashSet<string> ImplementationDocumentFolders = new(StringComparer.Ordinal)
    {
        "api", "guides", "patterns", "writing", "operations",
    };

    /// <summary>Record layers with folders the check walks, beside the generated folders the check skips.</summary>
    private static readonly HashSet<string> Layers = new(StringComparer.Ordinal)
    {
        "intent", "behavior", "implementation", "governance", "proof",
    };

    /// <summary>Generated folders the check skips: the builder owns their shape.</summary>
    private static readonly HashSet<string> GeneratedFolders = new(StringComparer.Ordinal)
    {
        "model", "book",
    };

    /// <summary>Directory names the walk leaves out, mirroring the source tree: build output, installed packages and tool folders.</summary>
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git",
        ".svelte-kit", "build", "dist", "coverage",
    };

    /// <summary>Reports folder-invalid, module-undeclared and record-placement-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var declared = DeclaredModules(model);
        var flat = FlatModules(model);

        CheckFolders(model, declared, flat, findings);
        CheckRecords(model, flat, findings);
    }

    /// <summary>The names of the declared modules.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>Each module name the model declares, from its module records.</returns>
    private static HashSet<string> DeclaredModules(ProjectModel model)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.Module
                && Identifiers.TrySplit(record.Id, out var prefix, out var home, out var name)
                && home is null
                && prefix == RecordTypes.Module)
            {
                declared.Add(name);
            }
        }

        return declared;
    }

    /// <summary>The modules that may stay flat: one aggregate carrying the module's own name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>Each module name holding exactly one aggregate named for the module.</returns>
    private static HashSet<string> FlatModules(ProjectModel model)
    {
        var names = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Derived || record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            if (Identifiers.TrySplit(record.Id, out var prefix, out var home, out var name)
                && prefix == RecordTypes.Aggregate
                && home is not null)
            {
                if (!names.TryGetValue(home, out var grouped))
                {
                    grouped = [];
                    names[home] = grouped;
                }

                grouped.Add(name);
            }
        }

        return names
            .Where(entry => entry.Value.Count == 1 && entry.Value[0] == entry.Key)
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Reports each folder outside the closed set and each module folder naming no declared module.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="declared">The names of the declared modules.</param>
    /// <param name="flat">The modules that may stay flat.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFolders(ProjectModel model, HashSet<string> declared, HashSet<string> flat, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var modules = new List<(string Directory, string Name)>();

        foreach (var root in model.Config.RecordRoots)
        {
            var anchored = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!seen.Add(anchored) || !Directory.Exists(anchored))
            {
                continue;
            }

            var stack = new Stack<string>(Directory
                .EnumerateDirectories(anchored)
                .OrderByDescending(child => child, StringComparer.Ordinal));

            while (stack.Count > 0)
            {
                var directory = stack.Pop();

                if (SkippedFolders.Contains(Path.GetFileName(directory)))
                {
                    continue;
                }

                var full = AnchoredSegments(anchored, directory);

                // Generated folders and free-form documents are allowed and their
                // children are skipped; an invalid folder is reported and pruned,
                // so one stray subtree draws one finding.
                if (SkipSubtree(full))
                {
                    CollectModule(full, directory, modules);
                    continue;
                }

                if (!IsFolder(full, anchored, directory, flat))
                {
                    findings.Add(new Finding(
                        "folder-invalid",
                        $"Folder '{PathText.Relative(model.Config.Root, directory)}' sits outside the closed folder set of its layer and level.",
                        directory,
                        1,
                        null));
                    continue;
                }

                CollectModule(full, directory, modules);

                foreach (var child in Directory
                    .EnumerateDirectories(directory)
                    .OrderByDescending(next => next, StringComparer.Ordinal))
                {
                    stack.Push(child);
                }
            }
        }

        foreach (var (directory, name) in modules.OrderBy(entry => entry.Directory, StringComparer.Ordinal))
        {
            if (!declared.Contains(name))
            {
                findings.Add(new Finding(
                    "module-undeclared",
                    $"Module folder '{PathText.Relative(model.Config.Root, directory)}' names no declared module.",
                    directory,
                    1,
                    null));
            }
        }
    }

    /// <summary>The directory path as segments anchored at its record layer.</summary>
    /// <param name="root">The record root the directory sits under.</param>
    /// <param name="directory">The absolute directory path.</param>
    /// <returns>The segments from the record layer down, or the relative segments when the root names no layer.</returns>
    private static string[] AnchoredSegments(string root, string directory)
    {
        var relative = PathText.Relative(root, directory);

        if (relative == ".")
        {
            return [];
        }

        var segments = relative.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var anchor = Path.GetFileName(root);

        return IsLayer(anchor) ? [anchor, .. segments] : segments;
    }

    /// <summary>Whether the name is a record layer or a generated folder.</summary>
    /// <param name="name">The folder name under test.</param>
    /// <returns>True when the name is a record layer or a generated folder; otherwise false.</returns>
    private static bool IsLayer(string name) => Layers.Contains(name) || GeneratedFolders.Contains(name);

    /// <summary>Whether the folder's subtree is skipped: generated output or free-form documents.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <returns>True when the folder is allowed and its children are skipped; otherwise false.</returns>
    private static bool SkipSubtree(string[] full)
    {
        if (full.Length == 0)
        {
            return true;
        }

        if (GeneratedFolders.Contains(full[0]) || full[0] == "proof")
        {
            return true;
        }

        // The UI catalog, captures and patterns per entry point nest freely: no record type lives there.
        if (full[0] == "implementation" && full is [_, "entry-points", _, ..] && full.Length >= 4)
        {
            return true;
        }

        // Documents and contracts nest freely: no record type lives there.
        if (full[0] == "implementation" && full.Length >= 3 && ImplementationDocumentFolders.Contains(full[1]))
        {
            return true;
        }

        return false;
    }

    /// <summary>Whether the folder is one of the closed set of its layer and level.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <param name="root">The record root the directory sits under.</param>
    /// <param name="directory">The absolute directory path.</param>
    /// <param name="flat">The modules that may stay flat.</param>
    /// <returns>True when the folder is allowed; otherwise false.</returns>
    private static bool IsFolder(string[] full, string root, string directory, HashSet<string> flat)
    {
        if (full.Length == 0)
        {
            return true;
        }

        return full[0] switch
        {
            "intent" => IsIntentFolder(full),
            "behavior" => IsBehaviorFolder(root, directory, full, flat),
            "implementation" => IsImplementationFolder(full),
            "governance" => IsGovernanceFolder(full),
            _ when GeneratedFolders.Contains(full[0]) || full[0] == "proof" => true,
            _ => false,
        };
    }

    /// <summary>Whether the folder is one of the closed set of the intent layer.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <returns>True when the folder is allowed; otherwise false.</returns>
    private static bool IsIntentFolder(string[] full)
    {
        if (full.Length == 1)
        {
            return true;
        }

        if (full[1] == "modules")
        {
            if (full.Length <= 3)
            {
                return true;
            }

            return full.Length == 4 && IntentModuleFolders.Contains(full[3]);
        }

        return full.Length == 2 && IntentRootFolders.Contains(full[1]);
    }

    /// <summary>Whether the folder is one of the closed set of the behavior layer.</summary>
    /// <param name="root">The record root the directory sits under.</param>
    /// <param name="directory">The absolute directory path.</param>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <param name="flat">The modules that may stay flat.</param>
    /// <returns>True when the folder is allowed; otherwise false.</returns>
    private static bool IsBehaviorFolder(string root, string directory, string[] full, HashSet<string> flat)
    {
        if (full.Length == 1)
        {
            return true;
        }

        if (full[1] == "modules")
        {
            if (full.Length <= 3)
            {
                return true;
            }

            if (full.Length == 4)
            {
                if (RecordFolders.BehaviorModuleFolders.Contains(full[3]))
                {
                    return true;
                }

                if (full[3] == "events" && flat.Contains(full[2]))
                {
                    return true;
                }

                return HoldsAggregate(directory);
            }

            if (RecordFolders.BehaviorModuleFolders.Contains(full[3]))
            {
                return false;
            }

            if (full[3] == "events" && flat.Contains(full[2]))
            {
                return false;
            }

            if (!HoldsAggregate(ChildPath(root, full, 4)))
            {
                return false;
            }

            return full.Length == 5 && BehaviorAggregateFolders.Contains(full[4]);
        }

        if (full[1] == "entry-points")
        {
            if (full.Length <= 3)
            {
                return true;
            }

            return full.Length == 4 && full[3] == "screens";
        }

        return full.Length == 2 && (full[1] == "rules" || full[1] == "external-systems");
    }

    /// <summary>Whether the folder is one of the closed set of the implementation layer.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <returns>True when the folder is allowed; otherwise false.</returns>
    private static bool IsImplementationFolder(string[] full)
    {
        if (full.Length == 1)
        {
            return true;
        }

        if (full[1] == "modules")
        {
            if (full.Length <= 3)
            {
                return true;
            }

            return full.Length == 4 && ImplementationModuleFolders.Contains(full[3]);
        }

        if (full[1] == "entry-points")
        {
            return full.Length <= 3;
        }

        return full.Length == 2 && ImplementationRootFolders.Contains(full[1]);
    }

    /// <summary>Whether the folder is one of the closed set of the governance layer.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <returns>True when the folder is allowed; otherwise false.</returns>
    private static bool IsGovernanceFolder(string[] full)
    {
        if (full.Length == 1)
        {
            return true;
        }

        if (full[1] == "modules")
        {
            if (full.Length <= 3)
            {
                return true;
            }

            return full.Length == 4 && full[3] == "exceptions";
        }

        return full.Length == 2 && full[1] == "exceptions";
    }

    /// <summary>Whether the folder holds an aggregate: an <c>aggregate.yaml</c> file sits directly under it.</summary>
    /// <param name="directory">The absolute directory path.</param>
    /// <returns>True when the folder holds <c>aggregate.yaml</c> directly; otherwise false.</returns>
    private static bool HoldsAggregate(string directory) =>
        File.Exists(Path.Combine(directory, "aggregate.yaml"));

    /// <summary>The absolute path of an ancestor of the walked directory.</summary>
    /// <param name="root">The record root the directory sits under.</param>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <param name="take">How many anchored segments the ancestor keeps.</param>
    /// <returns>The absolute ancestor path.</returns>
    private static string ChildPath(string root, string[] full, int take) =>
        Path.Combine([root, .. full.Take(take)]);

    /// <summary>Collects the module folder for the declaration check.</summary>
    /// <param name="full">The directory segments anchored at the record layer.</param>
    /// <param name="directory">The absolute directory path.</param>
    /// <param name="modules">The module folders collected so far, appended in place.</param>
    private static void CollectModule(string[] full, string directory, List<(string Directory, string Name)> modules)
    {
        if (full is [var layer, "modules", var name]
            && (layer == "intent" || layer == "behavior" || layer == "implementation" || layer == "governance"))
        {
            modules.Add((directory, name));
        }
    }

    /// <summary>Reports each record outside the folder and level its type allows.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="flat">The modules that may stay flat.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRecords(ProjectModel model, HashSet<string> flat, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Derived || !RecordTypes.All.Contains(record.Type))
            {
                continue;
            }

            if (!UnderRoot(model.Config, record.File, out var segments))
            {
                continue;
            }

            if (segments.Length > 0 && GeneratedFolders.Contains(segments[0]))
            {
                continue;
            }

            CheckRecord(model, record, segments, flat, findings);
        }
    }

    /// <summary>The record file path as segments relative to its record root, anchored at its record layer.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="file">The absolute record file path.</param>
    /// <param name="segments">The segments from the record layer down.</param>
    /// <returns>True when the file sits under a record root; otherwise false.</returns>
    private static bool UnderRoot(ConsumerConfig config, string file, out string[] segments)
    {
        foreach (var root in config.RecordRoots)
        {
            var trimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var prefix = trimmed + Path.DirectorySeparatorChar;

            if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = file[prefix.Length..].Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            if (relative.Length == 0)
            {
                segments = [];
                return false;
            }

            var anchor = Path.GetFileName(trimmed);

            segments = IsLayer(anchor) ? [anchor, .. relative] : relative;
            return true;
        }

        segments = [];
        return false;
    }

    /// <summary>Reports the record when it sits outside the folder and level its type allows.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="record">The record whose file is inspected.</param>
    /// <param name="segments">The record file path as segments anchored at its record layer.</param>
    /// <param name="flat">The modules that may stay flat.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRecord(ProjectModel model, Record record, string[] segments, HashSet<string> flat, List<Finding> findings)
    {
        var fileName = segments[^1];
        var folders = segments[..^1];

        if (record.Type == RecordTypes.Term)
        {
            if (!IsGlossary(model.Config, record.File))
            {
                findings.Add(new Finding(
                    "record-placement-invalid",
                    $"Term '{record.Id}' sits in '{Display(folders)}', but {Expected(RecordTypes.Term)}.",
                    record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (fileName == "module.yaml" && record.Type == RecordTypes.Rule)
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Rule '{record.Id}' sits in '{PathText.Relative(model.Config.Root, record.File)}', but module rules live in rule-set files under 'behavior/modules/<m>/rules/'.",
                record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (record.Type == RecordTypes.Module && record.Has(RecordFields.Rules))
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Module '{record.Id}' holds rules in '{PathText.Relative(model.Config.Root, record.File)}'; module rules live in rule-set files under 'behavior/modules/<m>/rules/'.",
                record.File, record.Line, SubjectOf(record)));
        }

        if (!RecordFolders.Places(record.Type, folders, fileName, flat))
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"{Label(record.Type)} '{record.Id}' sits in '{Display(folders)}', but {Expected(record.Type)}.",
                record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Whether the file is the glossary.</summary>
    /// <param name="config">The consumer configuration holding the glossary path.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>True when the file is the glossary; otherwise false.</returns>
    private static bool IsGlossary(ConsumerConfig config, string file) =>
        string.Equals(FullPath(config.GlossaryPath), FullPath(file), StringComparison.OrdinalIgnoreCase);

    /// <summary>The absolute path, or the path itself when it cannot be resolved.</summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The absolute path.</returns>
    private static string FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    /// <summary>The folders as a relative path with forward slashes.</summary>
    /// <param name="folders">The folder segments.</param>
    /// <returns>The joined folders, or a dot when the file sits directly under its record root.</returns>
    private static string Display(string[] folders) =>
        folders.Length == 0 ? "." : string.Join('/', folders);

    /// <summary>The prose label a finding shows for a record type.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label.</returns>
    private static string Label(string type)
    {
        var words = type.Replace('-', ' ');

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>Where a record of the type lives, as a finding says it.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The phrase, from the folder map; a requirement lives in its feature file, which the phrase names.</returns>
    private static string Expected(string type) =>
        type == RecordTypes.Requirement
            ? "requirements live in their feature file under 'intent/modules/<m>/features/'"
            : RecordFolders.Phrase(type);
}
