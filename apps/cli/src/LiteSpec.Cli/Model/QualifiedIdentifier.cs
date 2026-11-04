namespace LiteSpec.Model;

/// <summary>
///     Computes the qualified identifier of a record from its identifier segments
///     plus placement, following the placement table on the model page. The type
///     and name come from the identifier; the module, aggregate and entry point
///     prefixes come from the folders the file sits under.
/// </summary>
/// <follows>litespec/core/model#give-every-record-one-parent</follows>
static class QualifiedIdentifier
{
    /// <summary>Fixed type folders inside <c>behavior/modules/&lt;m&gt;/</c>; any other subfolder is an aggregate.</summary>
    static readonly HashSet<string> BehaviorModuleFolders = new(StringComparer.Ordinal)
    {
        "use-cases", "rules", "reactions", "jobs", "notifications", "settings",
        "integration-events", "external-systems",
    };

    /// <summary>Computes the qualified identifier of one record.</summary>
    /// <param name="type">The record type in kebab case.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="file">The absolute path of the record file.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The qualified identifier, or the identifier itself when placement adds no prefix.</returns>
    public static string Of(string type, string id, string file, ConsumerConfig config) =>
        Of(type, id, RelativeToRoot(file, config));

    /// <summary>Computes the qualified identifier from a path relative to the record root.</summary>
    /// <param name="type">The record type in kebab case.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="relativePath">The record file path relative to its record root, with any separator.</param>
    /// <returns>The qualified identifier, or the identifier itself when placement adds no prefix.</returns>
    public static string Of(string type, string id, string relativePath)
    {
        if (id.Length == 0)
        {
            return "";
        }

        if (!Identifiers.TrySplit(id, out _, out var home, out var name) || name.Length == 0)
        {
            return id;
        }

        var placement = Parse(relativePath);

        if (string.Equals(type, RecordTypes.Product, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Term, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Journey, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Actor, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Role, StringComparison.Ordinal))
        {
            return id;
        }

        if (string.Equals(type, RecordTypes.Module, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.EntryPoint, StringComparison.Ordinal))
        {
            return name;
        }

        if (string.Equals(type, RecordTypes.Aggregate, StringComparison.Ordinal))
        {
            var module = placement.Module ?? home;

            if (module is not { Length: > 0 })
            {
                return id;
            }

            return $"{module}/{placement.Aggregate ?? name}";
        }

        if (string.Equals(type, RecordTypes.Feature, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.IntegrationEvent, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Permission, StringComparison.Ordinal))
        {
            return placement.Module is { Length: > 0 } module
                ? $"{module}/{type}/{name}"
                : id;
        }

        if (string.Equals(type, RecordTypes.Requirement, StringComparison.Ordinal))
        {
            if (home is not { Length: > 0 })
            {
                return id;
            }

            return placement.Module is { Length: > 0 } module
                ? $"{module}/feature/{home}/requirement/{name}"
                : $"feature/{home}/requirement/{name}";
        }

        if (string.Equals(type, RecordTypes.UseCase, StringComparison.Ordinal))
        {
            var module = placement.Module ?? home;

            if (module is not { Length: > 0 })
            {
                return id;
            }

            return placement.Aggregate is { Length: > 0 } aggregate
                ? $"{module}/{aggregate}/use-case/{name}"
                : $"{module}/use-case/{name}";
        }

        if (string.Equals(type, RecordTypes.Result, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Scenario, StringComparison.Ordinal))
        {
            if (home is not { Length: > 0 })
            {
                return id;
            }

            if (placement is { Module: { Length: > 0 } module, Aggregate: { Length: > 0 } aggregate })
            {
                return $"{module}/{aggregate}/use-case/{home}/{type}/{name}";
            }

            return placement.Module is { Length: > 0 } single
                ? $"{single}/use-case/{home}/{type}/{name}"
                : $"use-case/{home}/{type}/{name}";
        }

        if (string.Equals(type, RecordTypes.Event, StringComparison.Ordinal))
        {
            if (placement is { Module: { Length: > 0 } module, Aggregate: { Length: > 0 } aggregate })
            {
                return $"{module}/{aggregate}/event/{name}";
            }

            if (placement.Module is { Length: > 0 } alone && home is { Length: > 0 })
            {
                return $"{alone}/{home}/event/{name}";
            }

            return id;
        }

        if (string.Equals(type, RecordTypes.Rule, StringComparison.Ordinal))
        {
            if (placement is { Module: { Length: > 0 } module, Aggregate: { Length: > 0 } aggregate })
            {
                return $"{module}/{aggregate}/rule/{name}";
            }

            return placement.Module is { Length: > 0 } single
                ? $"{single}/rule/{name}"
                : $"rule/{name}";
        }

        if (string.Equals(type, RecordTypes.Reaction, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Job, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Notification, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Setting, StringComparison.Ordinal))
        {
            var module = placement.Module ?? home;

            return module is { Length: > 0 }
                ? $"{module}/{type}/{name}"
                : id;
        }

        if (string.Equals(type, RecordTypes.Screen, StringComparison.Ordinal))
        {
            var entryPoint = placement.EntryPoint ?? home;

            return entryPoint is { Length: > 0 }
                ? $"{entryPoint}/screen/{name}"
                : id;
        }

        if (string.Equals(type, RecordTypes.Goal, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Problem, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Constraint, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Research, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Decision, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Runbook, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.Exception, StringComparison.Ordinal)
            || string.Equals(type, RecordTypes.ExternalSystem, StringComparison.Ordinal))
        {
            return placement.Module is { Length: > 0 } module
                ? $"{module}/{type}/{name}"
                : id;
        }

        return id;
    }

    /// <summary>Reads the folder nodes from a record file path.</summary>
    /// <param name="path">The record file path, relative or absolute, with any separator.</param>
    /// <returns>The module, aggregate and entry point folders the path sits under.</returns>
    static (string? Module, string? Aggregate, string? EntryPoint) Parse(string path)
    {
        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? module = null;
        string? aggregate = null;
        string? entryPoint = null;
        var moduleAt = -1;

        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (string.Equals(segments[at], "modules", StringComparison.Ordinal))
            {
                module = segments[at + 1];
                moduleAt = at;
                break;
            }

            if (string.Equals(segments[at], "entry-points", StringComparison.Ordinal))
            {
                entryPoint = segments[at + 1];
            }
        }

        if (moduleAt >= 0 && moduleAt + 2 < segments.Length)
        {
            var next = segments[moduleAt + 2];

            if (!next.Contains('.', StringComparison.Ordinal)
                && !BehaviorModuleFolders.Contains(next)
                && IsBehaviorModules(segments, moduleAt))
            {
                aggregate = next;
            }
        }

        return (module, aggregate, entryPoint);
    }

    /// <summary>Whether the modules segment sits under behavior, where aggregates live.</summary>
    /// <param name="segments">The path segments.</param>
    /// <param name="moduleAt">The index of the modules segment.</param>
    /// <returns>True when the segment before modules is behavior.</returns>
    static bool IsBehaviorModules(string[] segments, int moduleAt) =>
        moduleAt > 0 && string.Equals(segments[moduleAt - 1], "behavior", StringComparison.Ordinal);

    /// <summary>The file path relative to its record root, or the file itself outside every root.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The path relative to the record root holding the file, or the file itself.</returns>
    static string RelativeToRoot(string file, ConsumerConfig config)
    {
        foreach (var root in config.RecordRoots)
        {
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return file[prefix.Length..];
            }
        }

        return file;
    }
}
