using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Sync;

/// <summary>
///     The files <c>litespec sync</c> writes: the C# data files every configured .NET target
///     renders in memory from the built model, and the TypeScript files every frontend with
///     a client receives beside its client. The command and the sync-drift check share this
///     plan, so the check reports exactly what the command would write.
/// </summary>
/// <follows>litespec/core/model#sync-record-determined-code-from-the-records</follows>
static class SyncPlan
{
    /// <summary>One file the plan renders: its absolute path and its expected text.</summary>
    /// <param name="AbsolutePath">The absolute file path.</param>
    /// <param name="Content">The expected file text with LF line endings.</param>
    public sealed record SyncedFile(string AbsolutePath, string Content);

    /// <summary>One replaced file the plan deletes: the old generated file with the file that replaces it.</summary>
    /// <param name="AbsolutePath">The absolute path of the old file, which exists on disk.</param>
    /// <param name="ReplacedBy">The absolute path of the file that replaces it.</param>
    public sealed record DeletedFile(string AbsolutePath, string ReplacedBy);

    /// <summary>The rendered files with the target errors that stopped nothing else: an invalid target reports and renders nothing.</summary>
    /// <param name="Files">The rendered files in target then module order.</param>
    /// <param name="Deletions">The replaced files that exist on disk, in the order of the files that replace them.</param>
    /// <param name="Errors">The target errors, one per invalid target, in target order.</param>
    public sealed record Plan(IReadOnlyList<SyncedFile> Files, IReadOnlyList<DeletedFile> Deletions, IReadOnlyList<string> Errors);

    /// <summary>Renders every configured target and every frontend client in memory from the built model.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <returns>The rendered files with the target errors.</returns>
    public static Plan Build(ProjectModel model)
    {
        var files = new List<SyncedFile>();
        var errors = new List<string>();

        foreach (var target in model.Config.Sync)
        {
            if (target.Stack is not ("dotnet" or "android"))
            {
                continue;
            }

            if (Validate(target, out var error) is false)
            {
                errors.Add(error!);
                continue;
            }

            files.AddRange(RenderTarget(model, target));
        }

        files.AddRange(RenderFrontends(model));

        return new Plan(files, DeletionsOf(files), errors);
    }

    /// <summary>Whether the target names a supported kind and stack combination.</summary>
    /// <param name="target">The declared target.</param>
    /// <param name="error">The target error when the combination is unsupported.</param>
    /// <returns>True when the target renders; otherwise false.</returns>
    private static bool Validate(SyncTarget target, out string? error)
    {
        error = null;

        if (target.Kind is not ("refusal-codes" or "permissions" or "event-decisions" or "closed-sets"))
        {
            error = $"Sync target kind '{target.Kind}' is unknown; use refusal-codes, permissions, event-decisions or closed-sets.";
            return false;
        }

        if (target.Stack is not ("dotnet" or "android"))
        {
            error = $"Sync target stack '{target.Stack}' is unknown; use dotnet or android.";
            return false;
        }

        if (target.Namespace.Length == 0)
        {
            error = $"Sync target '{target.Kind}' for stack '{target.Stack}' names no namespace.";
            return false;
        }

        return true;
    }

    /// <summary>Renders one validated .NET target.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<SyncedFile> RenderTarget(ProjectModel model, SyncTarget target) =>
        target.Kind switch
        {
            "refusal-codes" => RenderRefusalCodesDotnet(model, target),
            "permissions" => RenderPermissionsDotnet(model, target),
            "event-decisions" => RenderEventDecisions(model, target),
            _ => RenderClosedSets(model, target),
        };

    /// <summary>Renders the TypeScript files of every frontend that names a client.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <returns>The rendered files in frontend order.</returns>
    private static List<SyncedFile> RenderFrontends(ProjectModel model)
    {
        var files = new List<SyncedFile>();
        var refusals = model.RefusalSets.OrderBy(entry => entry.Code, StringComparer.Ordinal).ToList();
        var permissions = model.Records
            .Where(record => record.Type == RecordTypes.Permission && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, Permission: record))
            .ToList();
        var capabilities = ScreenCapabilities(model);

        foreach (var frontend in model.Config.Frontends)
        {
            if (frontend.Client is null)
            {
                continue;
            }

            var folder = LitespecFolder(frontend.Client.Generated);

            if (refusals.Count > 0)
            {
                files.Add(new SyncedFile(
                    Path.Combine(folder, "refusals.ts"),
                    RefusalCodesRenderer.RenderTypeScript(refusals)));
            }

            if (permissions.Count > 0 || capabilities.Count > 0)
            {
                files.Add(new SyncedFile(
                    Path.Combine(folder, "permissions.ts"),
                    PermissionsRenderer.RenderTypeScript(permissions)));
            }

            if (capabilities.Count > 0)
            {
                files.Add(new SyncedFile(
                    Path.Combine(folder, "capabilities.ts"),
                    CapabilitiesRenderer.RenderTypeScript(capabilities)));
            }
        }

        return files;
    }

    /// <summary>Every screen route with the permissions the use cases the screen offers need.</summary>
    /// <param name="model">The built project model holding the screen and use case records.</param>
    /// <returns>The capabilities in route order.</returns>
    private static List<CapabilitiesRenderer.ScreenCapability> ScreenCapabilities(ProjectModel model)
    {
        var screens = new List<CapabilitiesRenderer.ScreenCapability>();

        foreach (var screen in model.Records
            .Where(record => record.Type == RecordTypes.Screen && !record.Derived)
            .OrderBy(record => record.Field(RecordFields.Route) ?? "", StringComparer.Ordinal)
            .ThenBy(record => record.Id, StringComparer.Ordinal))
        {
            if (screen.Field(RecordFields.Route) is not { Length: > 0 } route)
            {
                continue;
            }

            var permissions = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var offered in screen.Ids(RecordFields.Offers))
            {
                if (model.ById.TryGetValue(offered, out var useCase)
                    && UseCaseQueries.GrantedPermission(useCase) is { } grant)
                {
                    permissions.Add(grant);
                }
            }

            if (screens.All(known => known.Route != route))
            {
                screens.Add(new CapabilitiesRenderer.ScreenCapability(route, [.. permissions]));
            }
        }

        return screens;
    }

    /// <summary>The replaced generated files that exist on disk, one per rendered C# file.</summary>
    /// <param name="files">The rendered files.</param>
    /// <returns>The old files that exist and the command deletes, in render order.</returns>
    private static List<DeletedFile> DeletionsOf(IReadOnlyList<SyncedFile> files)
    {
        var deletions = new List<DeletedFile>();

        foreach (var file in files)
        {
            if (!file.AbsolutePath.EndsWith(".cs", StringComparison.Ordinal))
            {
                continue;
            }

            var old = Path.Combine(
                Path.GetDirectoryName(file.AbsolutePath) ?? "",
                Path.GetFileNameWithoutExtension(file.AbsolutePath) + ".generated.cs");

            if (File.Exists(old))
            {
                deletions.Add(new DeletedFile(old, file.AbsolutePath));
            }
        }

        return deletions;
    }

    /// <summary>Renders one C# file per module holding refusals.</summary>
    /// <param name="model">The built project model holding the refusal sets.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<SyncedFile> RenderRefusalCodesDotnet(ProjectModel model, SyncTarget target)
    {
        var files = new List<SyncedFile>();

        foreach (var (module, entries) in ByModule(model.RefusalSets.Select(entry => (entry.Module, Entry: entry))))
        {
            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = SyncNaming.ModulePascal(module);

            files.Add(new SyncedFile(
                Path.Combine(folder, $"{name}RefusalCodes.cs"),
                RefusalCodesRenderer.RenderDotnet(module, [.. entries], SyncNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders one C# file per module holding permissions.</summary>
    /// <param name="model">The built project model holding the permission records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<SyncedFile> RenderPermissionsDotnet(ProjectModel model, SyncTarget target)
    {
        var files = new List<SyncedFile>();
        var permissions = model.Records
            .Where(record => record.Type == RecordTypes.Permission && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, record));

        foreach (var (module, entries) in ByModule(permissions))
        {
            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = SyncNaming.ModulePascal(module);

            files.Add(new SyncedFile(
                Path.Combine(folder, $"{name}Permissions.cs"),
                PermissionsRenderer.RenderDotnet(module, entries, SyncNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders one C# file per module holding domain events, with the decision the records state for each event.</summary>
    /// <param name="model">The built project model holding the domain event records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<SyncedFile> RenderEventDecisions(ProjectModel model, SyncTarget target)
    {
        var files = new List<SyncedFile>();
        var consumers = RecordEntries.DomainEventConsumers(model);
        var events = model.Records
            .Where(record => record.Type == RecordTypes.DomainEvent && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, record));

        foreach (var (module, entries) in ByModule(events))
        {
            var decisions = new List<EventDecisionsRenderer.EventDecision>();

            foreach (var record in entries)
            {
                if (!Identifiers.TrySplit(record.Id, out _, out var aggregate, out var eventName) || aggregate is null)
                {
                    continue;
                }

                var ignored = record.Flag(RecordFields.Ignored);
                var consumed = consumers.TryGetValue(record.Id, out var named) && named.Count > 0;

                decisions.Add(new EventDecisionsRenderer.EventDecision(
                    record.Id, aggregate, eventName, DomainEventOutcomes.FromRecord(ignored, consumed)));
            }

            if (decisions.Count == 0)
            {
                continue;
            }

            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = SyncNaming.ModulePascal(module);

            files.Add(new SyncedFile(
                Path.Combine(folder, $"{name}EventDecisions.cs"),
                EventDecisionsRenderer.RenderDotnet(module, decisions, SyncNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders one C# file per home holding closed value sets.</summary>
    /// <param name="model">The built project model holding the reference-data and setting records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in home order.</returns>
    private static List<SyncedFile> RenderClosedSets(ProjectModel model, SyncTarget target)
    {
        var files = new List<SyncedFile>();
        var candidates = model.Records
            .Where(record => !record.Derived && record.Type is RecordTypes.ReferenceData or RecordTypes.Setting)
            .Select(record => (Home: HomeOf(record), Record: record))
            .Where(entry => entry.Home is not null)
            .Select(entry => (entry.Home!, entry.Record));

        foreach (var (home, entries) in ByModule(candidates))
        {
            var sets = new List<ClosedSetsRenderer.ClosedSet>();

            foreach (var record in entries)
            {
                if (!Identifiers.TrySplit(record.Id, out _, out _, out var setName))
                {
                    continue;
                }

                var values = ChoiceTexts(record);

                if (values is null)
                {
                    continue;
                }

                sets.Add(new ClosedSetsRenderer.ClosedSet(record.Id, setName, values));
            }

            if (sets.Count == 0)
            {
                continue;
            }

            var folder = DotnetFolder(model.Config.Root, target, home);
            var pascal = SyncNaming.ModulePascal(home);

            files.Add(new SyncedFile(
                Path.Combine(folder, $"{pascal}ClosedSets.cs"),
                ClosedSetsRenderer.RenderDotnet(home, sets, SyncNaming.ResolveModule(target.Namespace, home))));
        }

        return files;
    }

    /// <summary>Groups entries by module in module order, skipping empty groups.</summary>
    /// <param name="entries">The entries with their module.</param>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <returns>The non-empty groups in module order.</returns>
    private static IReadOnlyList<(string Module, IReadOnlyList<T> Entries)> ByModule<T>(IEnumerable<(string Module, T Entry)> entries)
    {
        var groups = new Dictionary<string, List<T>>(StringComparer.Ordinal);

        foreach (var (module, entry) in entries)
        {
            if (!groups.TryGetValue(module, out var held))
            {
                held = [];
                groups[module] = held;
            }

            held.Add(entry);
        }

        return [.. groups
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, (IReadOnlyList<T>)group.Value))];
    }

    /// <summary>The home declaring a closed value set: the record's module, or the home its identifier names.</summary>
    /// <param name="record">The reference-data or setting record.</param>
    /// <returns>The home name, or null when neither the folder nor the identifier names one.</returns>
    private static string? HomeOf(Record record)
    {
        if (record.Module is not null)
        {
            return record.Module;
        }

        return Identifiers.TrySplit(record.Id, out _, out var home, out _) ? home : null;
    }

    /// <summary>The allowed value texts of a closed value set, or null when the record states no choices list.</summary>
    /// <param name="record">The reference-data or setting record.</param>
    /// <returns>The value texts, or null when the record holds no choices array.</returns>
    private static List<string>? ChoiceTexts(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Choices, out var choices)
            || choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();

        foreach (var choice in choices.EnumerateArray())
        {
            if (choice.ValueKind == JsonValueKind.String && choice.GetString() is { } text)
            {
                values.Add(text);
            }
            else if (choice.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                values.Add(choice.GetRawText());
            }
        }

        return values;
    }

    /// <summary>The folder one .NET module writes into: the resolved path, or the path with a PascalCase module folder.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="module">The record module name.</param>
    /// <returns>The absolute module folder.</returns>
    private static string DotnetFolder(string root, SyncTarget target, string module) =>
        SyncNaming.HasModulePlaceholder(target.Path)
            ? Combine(root, SyncNaming.ResolveModule(target.Path, module))
            : Path.Combine(Combine(root, target.Path), SyncNaming.ModulePascal(module));

    /// <summary>The litespec folder beside a frontend client: the src folder of the generated client folder with litespec appended.</summary>
    /// <param name="generated">The absolute generated client folder.</param>
    /// <returns>The absolute litespec folder.</returns>
    private static string LitespecFolder(string generated) =>
        Path.Combine(Path.GetDirectoryName(generated) ?? generated, "litespec");

    /// <summary>Combines a root with a forward-slash relative path into an absolute path.</summary>
    /// <param name="root">The absolute root.</param>
    /// <param name="relative">The relative path, written with forward slashes.</param>
    /// <returns>The absolute combined path.</returns>
    private static string Combine(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
}
