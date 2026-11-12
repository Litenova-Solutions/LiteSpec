using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The code path a record's folder states for its marked code: its module, the aggregate under it, its name, and whether it is a use case, which takes a folder of its own.</summary>
/// <param name="Module">The module folder the record sits under, or null when its folder states no module.</param>
/// <param name="Aggregate">The aggregate folder under the module, or null when the record sits in no aggregate folder.</param>
/// <param name="Name">The record name the code folder takes, or null when the folder states none.</param>
/// <param name="IsUseCase">Whether the path ends in a use case folder of its own.</param>
/// <follows>litespec/core/model#match-each-backend-mark-to-its-folder</follows>
internal readonly record struct PlacementPath(string? Module, string? Aggregate, string? Name, bool IsUseCase)
{
    /// <summary>The module, aggregate and name a record's placement states: its module folder, the aggregate folder under it, and its identifier name.</summary>
    /// <param name="record">The marked record.</param>
    /// <returns>The module, aggregate and name, with nulls where the record's folder states no module.</returns>
    internal static PlacementPath PathOf(Record record)
    {
        if (record.Module is not { Length: > 0 } module)
        {
            return new(null, null, null, record.Type == RecordTypes.UseCase);
        }

        if (record.Type == RecordTypes.Aggregate)
        {
            var aggregate = AggregateOf(record) ?? NameOf(record);
            return new(module, aggregate, aggregate, false);
        }

        // The aggregate sits directly under the module, even when the record nests
        // deeper, such as an event its use case raises.
        return new(module, AggregateOf(record), NameOf(record), record.Type == RecordTypes.UseCase);
    }

    /// <summary>The module, aggregate and use case the scenario's placement states for its test.</summary>
    /// <param name="record">The marked scenario record.</param>
    /// <returns>The module, aggregate and use case home, with nulls where the record's folder states no module.</returns>
    internal static PlacementPath UseCasePathOf(Record record)
    {
        if (record.Module is not { Length: > 0 } module)
        {
            return new(null, null, null, false);
        }

        // A scenario or result takes the use case it belongs to as its name.
        var name = record.Type is RecordTypes.Scenario or RecordTypes.Result
            ? Identifiers.TrySplit(record.Id, out _, out var home, out _) ? home : null
            : NameOf(record);

        return new(module, AggregateOf(record), name, true);
    }

    /// <summary>The identifier name of a record, or its whole identifier when the name is not split out.</summary>
    /// <param name="record">The record.</param>
    /// <returns>The name segment of the identifier.</returns>
    internal static string NameOf(Record record) =>
        Identifiers.TrySplit(record.Id, out _, out _, out var name) && name.Length > 0 ? name : record.Id;

    /// <summary>The aggregate folder a behavior record sits under: the folder after its module, when the module holds aggregates.</summary>
    /// <param name="record">The record whose folder is read.</param>
    /// <returns>The aggregate folder name, or null when the record sits in no aggregate folder.</returns>
    private static string? AggregateOf(Record record)
    {
        var segments = record.Folder.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var moduleAt = Array.IndexOf(segments, "modules");

        if (moduleAt < 1
            || !string.Equals(segments[moduleAt - 1], "behavior", StringComparison.Ordinal)
            || moduleAt + 2 >= segments.Length)
        {
            return null;
        }

        var next = segments[moduleAt + 2];

        return next.Contains('.', StringComparison.Ordinal) || RecordFolders.BehaviorModuleFolders.Contains(next)
            ? null
            : next;
    }

    /// <summary>The expected namespace segments after the product for the code folder of the marked record type.</summary>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <returns>The expected segments in placement order, or an empty list when the path states no placement.</returns>
    internal static List<string> ExpectedSegments(PlacementPath path)
    {
        if (path.Module is null)
        {
            return [];
        }

        var expected = new List<string> { Naming.Pascal(path.Module) };

        if (path.Aggregate is not null && !SameName(path.Aggregate, path.Module))
        {
            expected.Add(Naming.Pascal(path.Aggregate));
        }

        if (path.Name is not null && path.IsUseCase && !SameName(path.Name, path.Aggregate ?? ""))
        {
            expected.Add(Naming.Pascal(path.Name));
        }

        return expected;
    }

    /// <summary>
    ///     The expected segments with the aggregate kept after its module when both share a name, because the
    ///     module layout gives such an aggregate its own folder, as in Directory.Directory.
    /// </summary>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="expected">The expected segments that drop the aggregate named like its module.</param>
    /// <returns>The expected segments with that aggregate, or the expected segments unchanged.</returns>
    internal static List<string> WithModuleNamedAggregate(PlacementPath path, List<string> expected)
    {
        if (path.Module is null || path.Aggregate is null || !SameName(path.Aggregate, path.Module) || expected.Count == 0)
        {
            return expected;
        }

        return [expected[0], Naming.Pascal(path.Aggregate), .. expected.Skip(1)];
    }

    /// <summary>Whether a folder name and a record name spell one name: case, hyphens and underscores ignored.</summary>
    /// <param name="folder">The folder name as written on disk.</param>
    /// <param name="name">The record name segment taken from the identifier.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    internal static bool SameName(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
