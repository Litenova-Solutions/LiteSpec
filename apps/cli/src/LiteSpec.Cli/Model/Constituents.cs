namespace LiteSpec.Model;

/// <summary>
///     Derives each unmarked Domain type's aggregate from its folder: the first folder walking
///     upward that declares exactly one aggregate root carrying one <c>aggregate/</c> mark owns
///     the type, a type in the shared kernel belongs to the kernel, and anything else is orphaned.
///     A marked type is never a constituent.
/// </summary>
/// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
static class Constituents
{
    /// <summary>Computes the constituents map onto the model, keys and entries sorted.</summary>
    /// <param name="model">The built project model whose constituents map is filled.</param>
    public static void Compute(ProjectModel model)
    {
        var (grouped, _) = Derive(model);

        model.Constituents.Clear();

        foreach (var entry in grouped.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            model.Constituents[entry.Key] = entry.Value;
        }
    }

    /// <summary>Every orphaned Domain type: unmarked, under no single-root folder, outside the kernel.</summary>
    /// <param name="model">The built project model whose Domain types are examined.</param>
    /// <returns>The orphaned Domain types, sorted by file, line and name.</returns>
    public static IReadOnlyList<DomainType> Orphans(ProjectModel model) =>
        Derive(model).Orphans;

    /// <summary>Derives the constituents grouping and the orphan list from the Domain types.</summary>
    /// <param name="model">The built project model whose Domain types are grouped.</param>
    /// <returns>The entries grouped by owning aggregate or kernel, and the orphaned types.</returns>
    private static (Dictionary<string, List<string>> Grouped, List<DomainType> Orphans) Derive(ProjectModel model)
    {
        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var orphans = new List<DomainType>();

        if (model.Config.DomainRoot.Length == 0)
        {
            return (grouped, orphans);
        }

        var domain = Folder(model.Config.DomainRoot);
        var aggregates = AggregateByFolder(model, domain);

        foreach (var type in model.Code.DomainTypes)
        {
            if (type.HasMark || !IsUnder(model.Config.DomainRoot, type.File))
            {
                continue;
            }

            if (IsUnder(model.Config.SharedKernel, type.File))
            {
                Add(grouped, "kernel", Entry(model, type));
                continue;
            }

            var folder = FolderOf(type.File);

            if (Same(folder, domain) && model.Config.SharedKernel.Length == 0)
            {
                Add(grouped, "kernel", Entry(model, type));
                continue;
            }

            if (WalkUp(folder, domain, aggregates) is { } aggregate)
            {
                Add(grouped, aggregate, Entry(model, type));
            }
            else
            {
                orphans.Add(type);
            }
        }

        foreach (var entry in grouped.Values)
        {
            entry.Sort(StringComparer.Ordinal);
        }

        orphans.Sort((left, right) =>
        {
            var file = string.Compare(left.File, right.File, StringComparison.Ordinal);

            if (file != 0)
            {
                return file;
            }

            var line = left.Line.CompareTo(right.Line);

            return line != 0
                ? line
                : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        });

        return (grouped, orphans);
    }

    /// <summary>Adds one entry to the grouping, creating the list for the key on first use.</summary>
    /// <param name="grouped">The grouping to add to, keyed by aggregate or kernel.</param>
    /// <param name="key">The aggregate id or kernel key the entry belongs to.</param>
    /// <param name="entry">The path and type name entry to add.</param>
    private static void Add(Dictionary<string, List<string>> grouped, string key, string entry)
    {
        if (!grouped.TryGetValue(key, out var list))
        {
            list = [];
            grouped[key] = list;
        }

        list.Add(entry);
    }

    /// <summary>One map entry: the relative file path with forward slashes, then the type name.</summary>
    /// <param name="model">The built project model the type belongs to, for the relative path.</param>
    /// <param name="type">The Domain type the entry names.</param>
    /// <returns>The relative file path and type name joined by a colon.</returns>
    private static string Entry(ProjectModel model, DomainType type) =>
        $"{model.Config.Relative(type.File)}:{type.Name}";

    /// <summary>
    ///     The aggregate id per folder that declares exactly one aggregate root carrying one
    ///     <c>aggregate/</c> mark. Folders with no root, with several roots, or with a root
    ///     carrying none or several aggregate marks hold no entry, so the walk passes through.
    /// </summary>
    /// <param name="model">The built project model holding the aggregate root elements.</param>
    /// <param name="domain">The normalized Domain root folder the walk stays inside.</param>
    /// <returns>The aggregate id for each folder declaring exactly one aggregate root.</returns>
    private static Dictionary<string, string> AggregateByFolder(ProjectModel model, string domain)
    {
        var roots = new Dictionary<string, List<(string Class, string Aggregate)>>(Comparer);

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.AggregateRoot
                || SingleAggregate(element.Marks) is not { } aggregate
                || !IsUnder(model.Config.DomainRoot, element.File))
            {
                continue;
            }

            var folder = FolderOf(element.File);

            if (!roots.TryGetValue(folder, out var list))
            {
                list = [];
                roots[folder] = list;
            }

            list.Add((element.Class, aggregate));
        }

        var aggregates = new Dictionary<string, string>(Comparer);

        foreach (var (folder, list) in roots)
        {
            if (!UnderOrSame(domain, folder))
            {
                continue;
            }

            var distinct = list
                .GroupBy(root => root.Class, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            if (distinct.Count == 1)
            {
                aggregates[folder] = distinct[0].Aggregate;
            }
        }

        return aggregates;
    }

    /// <summary>The one aggregate mark the element carries, or null when it carries none or several.</summary>
    /// <param name="marks">The marks the element carries.</param>
    /// <returns>The aggregate id of the single aggregate mark, or null when the element carries none or several.</returns>
    private static string? SingleAggregate(IReadOnlyList<CodeMark> marks)
    {
        string? found = null;

        foreach (var mark in marks)
        {
            if (mark.Verb == MarkVerb.Follows
                || !mark.RecordId.StartsWith("aggregate/", StringComparison.Ordinal))
            {
                continue;
            }

            if (found is not null)
            {
                return null;
            }

            found = mark.RecordId;
        }

        return found;
    }

    /// <summary>
    ///     The aggregate owning the folder: the id of the first folder walking upward, stopping
    ///     at the Domain root, that declares exactly one aggregate root. Null when no folder does.
    /// </summary>
    /// <param name="folder">The folder to start walking from.</param>
    /// <param name="domain">The Domain root folder the walk stops at.</param>
    /// <param name="aggregates">The aggregate id declared by each qualifying folder.</param>
    /// <returns>The owning aggregate id, or null when no folder up to the Domain root declares one.</returns>
    private static string? WalkUp(string folder, string domain, Dictionary<string, string> aggregates)
    {
        var current = folder;

        while (true)
        {
            if (aggregates.TryGetValue(current, out var aggregate))
            {
                return aggregate;
            }

            if (Same(current, domain))
            {
                return null;
            }

            var parent = Path.GetDirectoryName(current);

            if (parent is null)
            {
                return null;
            }

            current = Folder(parent);

            if (!UnderOrSame(domain, current))
            {
                return null;
            }
        }
    }

    /// <summary>The folder holding the file, as a normalized full path.</summary>
    /// <param name="file">The file whose holding folder is wanted.</param>
    /// <returns>The normalized full path of the folder holding the file.</returns>
    private static string FolderOf(string file) =>
        Folder(Path.GetDirectoryName(file) ?? file);

    /// <summary>The normalized full path of a folder, without a trailing separator.</summary>
    /// <param name="path">The folder path to normalize.</param>
    /// <returns>The full path without a trailing separator.</returns>
    private static string Folder(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Gets the path comparer for the platform: case-insensitive on Windows, ordinal elsewhere.</summary>
    private static StringComparer Comparer => Path.DirectorySeparatorChar == '\\'
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>Compares two folder paths with the platform path comparison.</summary>
    /// <param name="left">The first folder path.</param>
    /// <param name="right">The second folder path.</param>
    /// <returns>True when the two paths name the same folder.</returns>
    private static bool Same(string left, string right) =>
        string.Equals(left, right, Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal);

    /// <summary>Whether the folder is the root or sits under it.</summary>
    /// <param name="root">The root folder to test against.</param>
    /// <param name="folder">The folder to test.</param>
    /// <returns>True when the folder equals the root or sits under it.</returns>
    private static bool UnderOrSame(string root, string folder) =>
        Same(root, folder) || IsUnder(root, folder);

    /// <summary>Tests whether a file sits under a root folder.</summary>
    /// <param name="root">The root folder; an empty root holds nothing.</param>
    /// <param name="file">The file path to test.</param>
    /// <returns>True when the file sits under the root.</returns>
    private static bool IsUnder(string root, string file)
    {
        if (root.Length == 0)
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }
}
