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

    /// <summary>
    ///     Where a reaction or job trigger's code lives, read from its record's effects in this order: the use case it
    ///     runs, its folder under Application, inside its aggregate folder when the use case sits under one; else the first
    ///     notification it sends; else the first external system it calls.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the use case the trigger runs.</param>
    /// <param name="record">The reaction or job record whose effects are read.</param>
    /// <returns>The container folder, the PascalCase folder name under it, and the aggregate folder above it or null, or null when the trigger names no effect.</returns>
    /// <follows>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
    internal static (string Container, string Name, string? Aggregate)? TriggerHomeOf(ProjectModel model, Record record)
    {
        if (FirstIdOf(record.EdgeIds(RecordFields.Runs)) is { } run)
        {
            var aggregate = AggregateFolderOfUseCase(model, run);
            return new("Application", Naming.Pascal(UseCaseNameOf(run)), aggregate);
        }

        if (FirstNameOf(record.EdgeIds(RecordFields.Sends)) is { } sent)
        {
            return new("Notifications", Naming.Pascal(sent), null);
        }

        if (FirstNameOf(record.EdgeIds(RecordFields.Calls)) is { } called)
        {
            return new("ExternalSystems", Naming.Pascal(called), null);
        }

        return null;
    }

    /// <summary>The expected aggregate folder of the use case the runs edge names: its plural folder, or null for a module-level use case or an unresolved edge.</summary>
    /// <param name="model">The loaded project model that resolves the use case record.</param>
    /// <param name="id">The runs edge identifier, with any '#' operation part dropped.</param>
    /// <returns>The plural aggregate folder, or null when the use case sits in no aggregate folder.</returns>
    private static string? AggregateFolderOfUseCase(ProjectModel model, string id)
    {
        if (!model.ById.TryGetValue(id, out var useCase) || useCase.Type != RecordTypes.UseCase)
        {
            return null;
        }

        var path = PathOf(useCase);

        return path.Module is not null && path.Aggregate is not null
            ? ExpectedAggregateFolder(model, path.Module, path.Aggregate)
            : null;
    }

    /// <summary>The folder name a read model takes under its module ReadModels folder: the PascalCase plural of the read model name.</summary>
    /// <param name="name">The read model name segment of its identifier, such as reimbursed-expense.</param>
    /// <returns>The plural folder name, such as ReimbursedExpenses.</returns>
    /// <follows>litespec/packs/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</follows>
    internal static string ReadModelFolderOf(string name)
    {
        var pascal = Naming.Pascal(name);

        return pascal.Length > 1 && pascal[^1] == 'y' && !"aeiouAEIOU".Contains(pascal[^2], StringComparison.Ordinal)
            ? pascal[..^1] + "ies"
            : pascal + "s";
    }

    /// <summary>The first identifier, with any '#' operation part dropped.</summary>
    /// <param name="ids">The identifiers in declared order.</param>
    /// <returns>The first identifier without its operation part, or null when no identifier names one.</returns>
    private static string? FirstIdOf(IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
        {
            var bare = id.Split('#')[0];

            if (Identifiers.TrySplit(bare, out _, out _, out var name) && name.Length > 0)
            {
                return bare;
            }
        }

        return null;
    }

    /// <summary>The use case name of a runs edge identifier: its name segment.</summary>
    /// <param name="id">The runs edge identifier, with any '#' operation part dropped.</param>
    /// <returns>The name segment, or the identifier when it names none.</returns>
    private static string UseCaseNameOf(string id) =>
        Identifiers.TrySplit(id, out _, out _, out var name) && name.Length > 0 ? name : id;

    /// <summary>The name segment of the first identifier that names one, with any '#' operation part dropped.</summary>
    /// <param name="ids">The identifiers in declared order.</param>
    /// <returns>The name segment, or null when no identifier names one.</returns>
    private static string? FirstNameOf(IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
        {
            if (Identifiers.TrySplit(id.Split('#')[0], out _, out _, out var name) && name.Length > 0)
            {
                return name;
            }
        }

        return null;
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

        // An aggregate sits in the aggregates folder of its module; the folder after it names the aggregate.
        if (string.Equals(next, "aggregates", StringComparison.Ordinal))
        {
            return moduleAt + 3 < segments.Length ? segments[moduleAt + 3] : null;
        }

        return next.Contains('.', StringComparison.Ordinal) || RecordFolders.BehaviorModuleFolders.Contains(next)
            ? null
            : next;
    }

    /// <summary>The expected aggregate folder in PascalCase: the plural of the aggregate id name, or the record folder when no declared aggregate matches.</summary>
    /// <param name="model">The loaded project model that holds the declared aggregates.</param>
    /// <param name="module">The module the aggregate sits in.</param>
    /// <param name="aggregate">The aggregate folder the record sits under.</param>
    /// <returns>The plural aggregate folder, such as Expenses.</returns>
    internal static string ExpectedAggregateFolder(ProjectModel model, string module, string aggregate)
    {
        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is null || path.Aggregate is null
                || !SameName(path.Module, module) || !SameAggregate(path.Aggregate, aggregate))
            {
                continue;
            }

            // The plural comes from the name part of the aggregate id: an id names one
            // thing, so expense reads Expenses, while the record folder already holds the plural.
            if (Identifiers.TrySplit(record.Id, out _, out _, out var name) && name.Length > 0)
            {
                return Naming.PluralPascal(name);
            }

            return Naming.Pascal(path.Aggregate);
        }

        return Naming.Pascal(aggregate);
    }

    /// <summary>The expected namespace segments after the product for the code folder of the marked record type.</summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <returns>The expected segments in placement order, or an empty list when the path states no placement.</returns>
    internal static List<string> ExpectedSegments(ProjectModel model, PlacementPath path)
    {
        if (path.Module is null)
        {
            return [];
        }

        var expected = new List<string> { Naming.Pascal(path.Module) };

        // A record under an aggregate names its plural aggregate folder, as its code folder
        // does: every module keeps the folder whatever its number of aggregates.
        if (path.Aggregate is not null)
        {
            expected.Add(ExpectedAggregateFolder(model, path.Module, path.Aggregate));
        }

        if (path.Name is not null && path.IsUseCase && !SameName(path.Name, path.Aggregate ?? ""))
        {
            expected.Add(Naming.Pascal(path.Name));
        }

        return expected;
    }

    /// <summary>Whether a code aggregate folder and a declared aggregate spell one aggregate: case, hyphens, underscores and number ignored, so Domain/Expense/ still matches the declared expenses aggregate.</summary>
    /// <param name="folder">The code folder name as written on disk.</param>
    /// <param name="aggregate">The declared aggregate name from its record folder, in the plural.</param>
    /// <returns>True when both names spell one aggregate with case, hyphens, underscores and number ignored; otherwise false.</returns>
    internal static bool SameAggregate(string folder, string aggregate)
    {
        if (SameName(folder, aggregate))
        {
            return true;
        }

        return SameName(SingularOf(folder), SingularOf(aggregate));
    }

    /// <summary>The singular of an aggregate name: expenses to expense, policies to policy.</summary>
    /// <param name="name">The aggregate name in either number.</param>
    /// <returns>The name with a trailing plural lowered to the singular, or the name unchanged when it names none.</returns>
    internal static string SingularOf(string name)
    {
        if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name.Length > 3)
        {
            return name[..^3] + "y";
        }

        if (name.EndsWith('s') && !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && name.Length > 1)
        {
            return name[..^1];
        }

        return name;
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
