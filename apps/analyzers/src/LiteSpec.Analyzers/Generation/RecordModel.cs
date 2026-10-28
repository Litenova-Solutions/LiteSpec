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
/// mark style, the event identifiers, each rule's refusal code, the permission identifiers, and
/// each use case's access grant. Statuses never gate generation: a mark to a record of any status
/// still resolves, and the checks own the reporting.
/// </summary>
internal sealed class RecordModel : IEquatable<RecordModel>
{
    /// <summary>The model when no project file is in reach or its settings fail to parse.</summary>
    public static readonly RecordModel Invalid = new(
        false,
        MarkStyle.Xml,
        ImmutableArray<string>.Empty,
        ImmutableArray<RuleCode>.Empty,
        ImmutableArray<string>.Empty,
        ImmutableArray<UseCaseGrant>.Empty);

    /// <summary>Creates a record model.</summary>
    /// <param name="valid">Whether a project file was found and parsed.</param>
    /// <param name="style">The declared mark style.</param>
    /// <param name="eventIds">The event identifiers, sorted.</param>
    /// <param name="ruleCodes">Each rule with a refusal code, sorted by rule.</param>
    /// <param name="permissionIds">The permission identifiers, sorted.</param>
    /// <param name="grants">Each use case with an access grant, sorted by use case.</param>
    public RecordModel(
        bool valid,
        MarkStyle style,
        ImmutableArray<string> eventIds,
        ImmutableArray<RuleCode> ruleCodes,
        ImmutableArray<string> permissionIds,
        ImmutableArray<UseCaseGrant> grants)
    {
        Valid = valid;
        Style = style;
        EventIds = eventIds;
        RuleCodes = ruleCodes;
        PermissionIds = permissionIds;
        Grants = grants;
    }

    /// <summary>Whether a project file was found and parsed.</summary>
    public bool Valid { get; }

    /// <summary>The declared mark style.</summary>
    public MarkStyle Style { get; }

    /// <summary>The event identifiers, sorted.</summary>
    public ImmutableArray<string> EventIds { get; }

    /// <summary>Each rule with a refusal code, sorted by rule.</summary>
    public ImmutableArray<RuleCode> RuleCodes { get; }

    /// <summary>The permission identifiers, sorted.</summary>
    public ImmutableArray<string> PermissionIds { get; }

    /// <summary>Each use case with an access grant, sorted by use case.</summary>
    public ImmutableArray<UseCaseGrant> Grants { get; }

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
            IdsOf(index, "permission"),
            GrantsOf(index, grants));
    }

    /// <inheritdoc />
    public bool Equals(RecordModel? other) =>
        other is not null
        && Valid == other.Valid
        && Style == other.Style
        && EventIds.SequenceEqual(other.EventIds)
        && RuleCodes.SequenceEqual(other.RuleCodes)
        && PermissionIds.SequenceEqual(other.PermissionIds)
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

            foreach (var id in PermissionIds)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(id);
            }

            foreach (var grant in Grants)
            {
                hash = hash * 31 + grant.GetHashCode();
            }

            return hash;
        }
    }

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

    private static void AddRoot(string projectDir, string value, List<string> roots)
    {
        var root = ResolveRoot(projectDir, value);

        if (root.Length > 0 && !roots.Contains(root))
        {
            roots.Add(root);
        }
    }

    private static string ResolveRoot(string projectDir, string value)
    {
        var cleaned = value.Replace('\\', '/');
        var joined = IsRooted(cleaned) || projectDir.Length == 0
            ? cleaned
            : projectDir + "/" + cleaned;
        var parts = joined.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
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

    private static bool IsRooted(string path) =>
        path.StartsWith("/", StringComparison.Ordinal)
        || (path.Length > 1 && path[1] == ':');

    private static string DirectoryOf(string path)
    {
        var cleaned = path.Replace('\\', '/');
        var slash = cleaned.LastIndexOf('/');

        return slash < 0 ? string.Empty : cleaned.Substring(0, slash);
    }

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

/// <summary>One rule with the refusal code its exception carries.</summary>
internal sealed class RuleCode : IEquatable<RuleCode>
{
    /// <summary>Creates a rule code.</summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="code">The code of its refusal result, or its bare refusal code.</param>
    public RuleCode(string ruleId, string code)
    {
        RuleId = ruleId;
        Code = code;
    }

    /// <summary>The rule identifier.</summary>
    public string RuleId { get; }

    /// <summary>The code of its refusal result, or its bare refusal code.</summary>
    public string Code { get; }

    /// <inheritdoc />
    public bool Equals(RuleCode? other) =>
        other is not null
        && string.Equals(RuleId, other.RuleId, StringComparison.Ordinal)
        && string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RuleCode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(RuleId);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Code);

            return hash;
        }
    }
}

/// <summary>One use case with its access grant.</summary>
internal sealed class UseCaseGrant : IEquatable<UseCaseGrant>
{
    /// <summary>Creates a use-case grant.</summary>
    /// <param name="useCaseId">The use-case identifier.</param>
    /// <param name="grant">Its access grant.</param>
    public UseCaseGrant(string useCaseId, string grant)
    {
        UseCaseId = useCaseId;
        Grant = grant;
    }

    /// <summary>The use-case identifier.</summary>
    public string UseCaseId { get; }

    /// <summary>Its access grant.</summary>
    public string Grant { get; }

    /// <inheritdoc />
    public bool Equals(UseCaseGrant? other) =>
        other is not null
        && string.Equals(UseCaseId, other.UseCaseId, StringComparison.Ordinal)
        && string.Equals(Grant, other.Grant, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is UseCaseGrant other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(UseCaseId);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Grant);

            return hash;
        }
    }
}
