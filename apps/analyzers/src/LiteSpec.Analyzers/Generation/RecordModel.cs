using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// The record facts the generator reads from the project file and the record files: the declared
/// mark style, the event identifiers, each rule's refusal code, each permission with its scope,
/// group and risks, each role with its scope and allowed permissions, and each use case's
/// access grant.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
/// <follows>litespec/core/access#scope-every-role</follows>
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
        ImmutableArray<UseCaseGrant>.Empty);

    /// <summary>Initializes a new instance of the <see cref="RecordModel"/> class.Creates a record model.</summary>
    /// <param name="valid">Whether a project file was found and parsed.</param>
    /// <param name="style">The declared mark style.</param>
    /// <param name="eventIds">The event identifiers, sorted.</param>
    /// <param name="ruleCodes">Each rule with a refusal code, sorted by rule.</param>
    /// <param name="permissions">Each permission with its scope, group and risks, sorted by id.</param>
    /// <param name="roles">Each role with its scope and allowed permissions, sorted by id.</param>
    /// <param name="grants">Each use case with an access grant, sorted by use case.</param>
    public RecordModel(
        bool valid,
        MarkStyle style,
        ImmutableArray<string> eventIds,
        ImmutableArray<RuleCode> ruleCodes,
        ImmutableArray<PermissionFacts> permissions,
        ImmutableArray<RoleFacts> roles,
        ImmutableArray<UseCaseGrant> grants)
    {
        Valid = valid;
        Style = style;
        EventIds = eventIds;
        RuleCodes = ruleCodes;
        Permissions = permissions;
        Roles = roles;
        Grants = grants;
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

    /// <summary>
    /// Reads the model from the additional files: the project file beside the consumer root, and
    /// every record file under the documentation roots it names. Files read in path order, and
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

        if (!ProjectPaths.TryRead(project.Path, projectText, out var paths, out _) || paths is null)
        {
            return Invalid;
        }

        var roots = paths.DocsRoots;
        var records = new List<IRecordFields>();
        var grants = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var text in ordered)
        {
            if (!text.Path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
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
            paths.Settings.Marks,
            IdsOf(index, "event"),
            RuleCodesOf(index),
            PermissionsOf(index),
            RolesOf(index),
            GrantsOf(index, grants));
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
        && Grants.SequenceEqual(other.Grants);

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
            if (ProjectPaths.IsProjectFile(text.Path))
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>Returns true when the under-any-root condition holds; otherwise false.</summary>
    /// <param name="path">The file path as the host reports it.</param>
    /// <param name="roots">The docs roots that bound the search, as absolute or project-relative paths.</param>
    /// <returns>True when the under-any-root condition holds; otherwise false.</returns>
    private static bool UnderAnyRoot(string path, IReadOnlyList<string> roots)
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

    /// <summary>Returns the permissions of the index, each with the one reach its allowing roles share.</summary>
    /// <remarks>
    ///     A role holds the scope of its grant, so a permission takes its reach from the roles that allow it. When those
    ///     roles grant it at one scope, the permission carries that scope; when they grant it at several, or no role allows
    ///     it, the permission carries an empty scope, because the reach comes from the role the caller holds.
    /// </remarks>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <returns>The permissions of the index.</returns>
    private static ImmutableArray<PermissionFacts> PermissionsOf(RecordIndex index)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<PermissionFacts>();
        var reaches = ReachesByPermission(index);

        foreach (var record in index.ByType("permission"))
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            found.Add(new PermissionFacts(
                record.Id,
                reaches.TryGetValue(record.Id, out var reach) && reach.Count == 1 ? reach.First() : string.Empty,
                record.Field("group"),
                ImmutableArray.CreateRange(record.Ids("risk"))));
        }

        found.Sort(static (left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));

        return ImmutableArray.CreateRange(found);
    }

    /// <summary>Collects the distinct scopes of the roles that allow each permission.</summary>
    /// <param name="index">The record index that resolves identifiers to records.</param>
    /// <returns>The distinct role scopes by permission identifier.</returns>
    private static Dictionary<string, HashSet<string>> ReachesByPermission(RecordIndex index)
    {
        var reaches = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var role in index.ByType("role"))
        {
            if (role.Field("scope") is not { Length: > 0 } scope)
            {
                continue;
            }

            foreach (var permission in role.Ids("allows"))
            {
                if (!reaches.TryGetValue(permission, out var scopes))
                {
                    scopes = new HashSet<string>(StringComparer.Ordinal);
                    reaches[permission] = scopes;
                }

                scopes.Add(scope);
            }
        }

        return reaches;
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
    /// <param name="grants">The access grants read from the record files, by use-case identifier.</param>
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
