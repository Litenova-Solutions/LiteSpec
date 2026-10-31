using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// The record facts the generator reads from the project file and the record pages: the declared
/// mark style, the event identifiers, each rule's refusal code, each permission with its scope,
/// group and risks, each role with its scope and allowed permissions, each use case's access
/// grant, and the project's vocabulary additions. Statuses never gate generation: a mark to a
/// record of any status still resolves, and the checks own the reporting.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
/// <follows>litespec/core/access#state-the-scope-of-every-permission</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class RecordModel : IEquatable<RecordModel>
{
    /// <summary>The model when no project file is in reach or its settings fail to parse.</summary>
    public static readonly RecordModel Invalid = new(
        false,
        MarkStyle.Xml,
        ImmutableArray<string>.Empty,
        ImmutableArray<RuleCode>.Empty,
        ImmutableArray<PermissionFacts>.Empty,
        ImmutableArray<RoleFacts>.Empty,
        ImmutableArray<UseCaseGrant>.Empty,
        ImmutableArray<string>.Empty,
        ImmutableArray<string>.Empty,
        ImmutableArray<string>.Empty);

    /// <summary>Initializes a new instance of the <see cref="RecordModel"/> class.Creates a record model.</summary>
    /// <param name="valid">Whether a project file was found and parsed.</param>
    /// <param name="style">The declared mark style.</param>
    /// <param name="eventIds">The event identifiers, sorted.</param>
    /// <param name="ruleCodes">Each rule with a refusal code, sorted by rule.</param>
    /// <param name="permissions">Each permission with its scope, group and risks, sorted by id.</param>
    /// <param name="roles">Each role with its scope and allowed permissions, sorted by id.</param>
    /// <param name="grants">Each use case with an access grant, sorted by use case.</param>
    /// <param name="verbs">The domain verbs the project adds to the baseline.</param>
    /// <param name="queryVerbs">The project verbs that name a query.</param>
    /// <param name="phrasalVerbs">The multi-word verbs counted as one opening.</param>
    public RecordModel(
        bool valid,
        MarkStyle style,
        ImmutableArray<string> eventIds,
        ImmutableArray<RuleCode> ruleCodes,
        ImmutableArray<PermissionFacts> permissions,
        ImmutableArray<RoleFacts> roles,
        ImmutableArray<UseCaseGrant> grants,
        ImmutableArray<string> verbs,
        ImmutableArray<string> queryVerbs,
        ImmutableArray<string> phrasalVerbs)
    {
        Valid = valid;
        Style = style;
        EventIds = eventIds;
        RuleCodes = ruleCodes;
        Permissions = permissions;
        Roles = roles;
        Grants = grants;
        Verbs = verbs;
        QueryVerbs = queryVerbs;
        PhrasalVerbs = phrasalVerbs;
    }

    /// <summary>Gets a value indicating whether whether a project file was found and parsed.</summary>
    public bool Valid { get; }

    /// <summary>Gets the declared mark style.</summary>
    public MarkStyle Style { get; }

    /// <summary>Gets the event identifiers, sorted.</summary>
    public ImmutableArray<string> EventIds { get; }

    /// <summary>Gets each rule with a refusal code, sorted by rule.</summary>
    public ImmutableArray<RuleCode> RuleCodes { get; }

    /// <summary>Gets each permission with its scope, group and risks, sorted by id.</summary>
    public ImmutableArray<PermissionFacts> Permissions { get; }

    /// <summary>Gets each role with its scope and allowed permissions, sorted by id.</summary>
    public ImmutableArray<RoleFacts> Roles { get; }

    /// <summary>Gets each use case with an access grant, sorted by use case.</summary>
    public ImmutableArray<UseCaseGrant> Grants { get; }

    /// <summary>Gets the domain verbs the project adds to the baseline.</summary>
    public ImmutableArray<string> Verbs { get; }

    /// <summary>Gets the project verbs that name a query.</summary>
    public ImmutableArray<string> QueryVerbs { get; }

    /// <summary>Gets the multi-word verbs counted as one opening.</summary>
    public ImmutableArray<string> PhrasalVerbs { get; }

    /// <summary>
    /// Reads the model from the additional files: the project file beside the consumer root, and
    /// every Markdown page under the documentation roots it names. Files read in path order, and
    /// the first record wins when two carry one identifier, as the record index does.
    /// </summary>
    /// <param name="texts">Every additional file.</param>
    /// <param name="cancellationToken">The pipeline cancellation token.</param>
    /// <returns>The model, or <see cref="Invalid" /> with no usable project file.</returns>
    public static RecordModel Read(ImmutableArray<AdditionalText> texts, CancellationToken cancellationToken)
    {
        var ordered = texts.OrderBy(static text => text.Path, StringComparer.Ordinal).ToList();
        var project = FindProjectFile(ordered);

        if (project is null)
        {
            return Invalid;
        }

        var projectText = project.GetText(cancellationToken)?.ToString();

        if (projectText is null)
        {
            return Invalid;
        }

        if (!ProjectSettings.TryParse(projectText, out var settings, out _))
        {
            return Invalid;
        }

        var roots = DocRoots(projectText, DirectoryOf(project.Path));

        if (roots.Count == 0)
        {
            return Invalid;
        }

        var records = new List<IRecordFacts>();
        var grants = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var text in ordered)
        {
            if (!text.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!UnderAnyRoot(text.Path, roots))
            {
                continue;
            }

            var content = text.GetText(cancellationToken)?.ToString();

            if (content is null)
            {
                continue;
            }

            records.AddRange(RecordFiles.Read(text.Path, content));

            foreach (var grant in GrantReader.Read(content))
            {
                if (!grants.ContainsKey(grant.Key))
                {
                    grants.Add(grant.Key, grant.Value);
                }
            }
        }

        var index = new RecordIndex(records);

        return new RecordModel(
            true,
            settings.Marks,
            IdsOf(index, "event"),
            RuleCodesOf(index),
            PermissionsOf(index),
            RolesOf(index),
            GrantsOf(index, grants),
            ImmutableArray.CreateRange(settings.Verbs),
            ImmutableArray.CreateRange(settings.QueryVerbs),
            ImmutableArray.CreateRange(settings.PhrasalVerbs));
    }

    /// <inheritdoc />
    public bool Equals(RecordModel? other) =>
        other is not null
        && Valid == other.Valid
        && Style == other.Style
        && EventIds.SequenceEqual(other.EventIds)
        && RuleCodes.SequenceEqual(other.RuleCodes)
        && Permissions.SequenceEqual(other.Permissions)
        && Roles.SequenceEqual(other.Roles)
        && Grants.SequenceEqual(other.Grants)
        && Verbs.SequenceEqual(other.Verbs)
        && QueryVerbs.SequenceEqual(other.QueryVerbs)
        && PhrasalVerbs.SequenceEqual(other.PhrasalVerbs);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RecordModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Valid.GetHashCode();
            hash = hash * 31 + Style.GetHashCode();

            foreach (var id in EventIds)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(id);
            }

            foreach (var rule in RuleCodes)
            {
                hash = hash * 31 + rule.GetHashCode();
            }

            foreach (var permission in Permissions)
            {
                hash = hash * 31 + permission.GetHashCode();
            }

            foreach (var role in Roles)
            {
                hash = hash * 31 + role.GetHashCode();
            }

            foreach (var grant in Grants)
            {
                hash = hash * 31 + grant.GetHashCode();
            }

            foreach (var verb in Verbs)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(verb);
            }

            foreach (var verb in QueryVerbs)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(verb);
            }

            foreach (var verb in PhrasalVerbs)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(verb);
            }

            return hash;
        }
    }

    /// <summary>Returns the project file.</summary>
    /// <param name="texts">Every additional file handed to the generator.</param>
    /// <returns>The project file, or null when none applies.</returns>
    private static AdditionalText? FindProjectFile(List<AdditionalText> texts)
    {
        foreach (var text in texts)
        {
            if (text.Path.EndsWith("litespec.project.json", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>Returns the roots.</summary>
    /// <param name="projectText">The project file text to read the settings and roots from.</param>
    /// <param name="projectDir">The directory that holds the project file, used to resolve relative roots.</param>
    /// <returns>The roots, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<string> DocRoots(string projectText, string projectDir)
    {
        var docs = new List<string>();
        var behavior = new List<string>();
        var root = JsonText.Parse(projectText);

        if (root.Kind == JsonKind.Object
            && root.TryGetProperty("paths", out var paths)
            && paths.Kind == JsonKind.Object)
        {
            CollectRoots(paths, "docs", docs);
            CollectRoots(paths, "behaviorDocs", behavior);
        }

        if (docs.Count == 0)
        {
            docs.Add("docs");
        }

        if (behavior.Count == 0)
        {
            behavior.Add("docs/behavior");
        }

        var roots = new List<string>();

        foreach (var value in docs)
        {
            AddRoot(projectDir, value, roots);
        }

        foreach (var value in behavior)
        {
            AddRoot(projectDir, value, roots);
        }

        return roots;
    }

    /// <summary>Collects the roots.</summary>
    /// <param name="paths">The paths object from the project file, or null when it states none.</param>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="values">The values collected so far, in the order they were found.</param>
    private static void CollectRoots(JsonText paths, string name, List<string> values)
    {
        if (!paths.TryGetProperty(name, out var value))
        {
            return;
        }

        if (value.Kind == JsonKind.String
            && value.GetString() is string single
            && single.Length > 0)
        {
            values.Add(single);
        }
        else if (value.Kind == JsonKind.Array)
        {
            foreach (var entry in value.Items)
            {
                if (entry.Kind == JsonKind.String
                    && entry.GetString() is string item
                    && item.Length > 0)
                {
                    values.Add(item);
                }
            }
        }
    }

    /// <summary>Adds the root.</summary>
    /// <param name="projectDir">The directory that holds the project file, used to resolve relative roots.</param>
    /// <param name="value">The value to convert or test.</param>
    /// <param name="roots">The docs roots that bound the search, as absolute or project-relative paths.</param>
    private static void AddRoot(string projectDir, string value, List<string> roots)
    {
        var root = ResolveRoot(projectDir, value);

        if (root.Length > 0 && !roots.Contains(root))
        {
            roots.Add(root);
        }
    }

    /// <summary>Returns the root.</summary>
    /// <param name="projectDir">The directory that holds the project file, used to resolve relative roots.</param>
    /// <param name="value">The value to convert or test.</param>
    /// <returns>The root as text.</returns>
    private static string ResolveRoot(string projectDir, string value)
    {
        var cleaned = value.Replace('\\', '/');
        var joined = IsRooted(cleaned) || projectDir.Length == 0
            ? cleaned
            : projectDir + "/" + cleaned;
        var parts = joined.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var kept = new List<string>();

        foreach (var part in parts)
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (kept.Count > 0)
                {
                    kept.RemoveAt(kept.Count - 1);
                }

                continue;
            }

            kept.Add(part);
        }

        var resolved = string.Join("/", kept);

        if (joined.StartsWith("/", StringComparison.Ordinal))
        {
            return "/" + resolved;
        }

        return resolved;
    }

    /// <summary>Returns true when the path is rooted; otherwise false.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <returns>True when the path is rooted; otherwise false.</returns>
    private static bool IsRooted(string path) =>
        path.StartsWith("/", StringComparison.Ordinal)
        || (path.Length > 1 && path[1] == ':');

    /// <summary>Returns the directory of the path.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <returns>The directory of the path.</returns>
    private static string DirectoryOf(string path)
    {
        var cleaned = path.Replace('\\', '/');
        var slash = cleaned.LastIndexOf('/');

        return slash < 0 ? string.Empty : cleaned.Substring(0, slash);
    }

    /// <summary>Returns true when the under-any-root condition holds; otherwise false.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <param name="roots">The docs roots that bound the search, as absolute or project-relative paths.</param>
    /// <returns>True when the under-any-root condition holds; otherwise false.</returns>
    private static bool UnderAnyRoot(string path, List<string> roots)
    {
        var cleaned = path.Replace('\\', '/');

        foreach (var root in roots)
        {
            if (cleaned.Equals(root, StringComparison.OrdinalIgnoreCase)
                || cleaned.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the ids of the index.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The ids of the index.</returns>
    private static ImmutableArray<string> IdsOf(RecordIndex index, string type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new List<string>();

        foreach (var record in index.ByType(type))
        {
            if (record.Id.Length > 0 && seen.Add(record.Id))
            {
                ids.Add(record.Id);
            }
        }

        ids.Sort(static (left, right) => string.Compare(left, right, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(ids);
    }

    /// <summary>Returns the codes of.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <returns>The codes of, in the order this method builds them, or an empty collection when none apply.</returns>
    private static ImmutableArray<RuleCode> RuleCodesOf(RecordIndex index)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var codes = new List<RuleCode>();

        foreach (var record in index.ByType("rule"))
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            var code = RefusalCode.Of(record, index.ById);

            if (code is null)
            {
                continue;
            }

            codes.Add(new RuleCode(record.Id, code));
        }

        codes.Sort(static (left, right) => string.Compare(left.RuleId, right.RuleId, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(codes);
    }

    /// <summary>Returns the permissions of the index.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <returns>The permissions of the index.</returns>
    private static ImmutableArray<PermissionFacts> PermissionsOf(RecordIndex index)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<PermissionFacts>();

        foreach (var record in index.ByType("permission"))
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            found.Add(new PermissionFacts(
                record.Id,
                record.Field("scope") ?? string.Empty,
                record.Field("group"),
                ImmutableArray.CreateRange(record.Ids("risk"))));
        }

        found.Sort(static (left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(found);
    }

    /// <summary>Returns the roles of the index.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <returns>The roles of the index.</returns>
    private static ImmutableArray<RoleFacts> RolesOf(RecordIndex index)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<RoleFacts>();

        foreach (var record in index.ByType("role"))
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            found.Add(new RoleFacts(
                record.Id,
                record.Field("scope") ?? string.Empty,
                ImmutableArray.CreateRange(record.Ids("allows"))));
        }

        found.Sort(static (left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(found);
    }

    /// <summary>Returns the grants of the index.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <param name="grants">The access grants read from the record pages, by use-case identifier.</param>
    /// <returns>The grants of the index.</returns>
    private static ImmutableArray<UseCaseGrant> GrantsOf(
        RecordIndex index, Dictionary<string, string> grants)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<UseCaseGrant>();

        foreach (var record in index.ByType("use-case"))
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            if (grants.TryGetValue(record.Id, out var grant))
            {
                found.Add(new UseCaseGrant(record.Id, grant));
            }
        }

        found.Sort(static (left, right) => string.Compare(left.UseCaseId, right.UseCaseId, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(found);
    }
}
