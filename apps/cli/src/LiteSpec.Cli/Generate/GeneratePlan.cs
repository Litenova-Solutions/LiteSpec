using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     The files <c>litespec generate</c> writes: every configured target rendered in memory
///     from the built model. The command and the generated-drift check share this plan, so
///     the check reports exactly what the command would write.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class GeneratePlan
{
    /// <summary>One file the plan renders: its absolute path and its expected text.</summary>
    /// <param name="AbsolutePath">The absolute file path.</param>
    /// <param name="Content">The expected file text with LF line endings.</param>
    public sealed record GeneratedFile(string AbsolutePath, string Content);

    /// <summary>The rendered files with the target errors that stopped nothing else: an invalid target reports and renders nothing.</summary>
    /// <param name="Files">The rendered files in target then module order.</param>
    /// <param name="Errors">The target errors, one per invalid target, in target order.</param>
    public sealed record Plan(IReadOnlyList<GeneratedFile> Files, IReadOnlyList<string> Errors);

    /// <summary>Renders every configured target in memory from the built model.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <returns>The rendered files with the target errors.</returns>
    public static Plan Build(ProjectModel model)
    {
        var files = new List<GeneratedFile>();
        var errors = new List<string>();

        foreach (var target in model.Config.Generate)
        {
            if (Validate(target, out var error) is false)
            {
                errors.Add(error!);
                continue;
            }

            files.AddRange(RenderTarget(model, target));
        }

        return new Plan(files, errors);
    }

    /// <summary>Whether the target names a supported kind and stack combination.</summary>
    /// <param name="target">The declared target.</param>
    /// <param name="error">The target error when the combination is unsupported.</param>
    /// <returns>True when the target renders; otherwise false.</returns>
    private static bool Validate(GenerateTarget target, out string? error)
    {
        error = null;

        if (target.Kind is not ("refusal-codes" or "permissions" or "event-decisions" or "closed-sets"))
        {
            error = $"Generate target kind '{target.Kind}' is unknown; use refusal-codes, permissions, event-decisions or closed-sets.";
            return false;
        }

        if (target.Stack is not ("dotnet" or "react" or "svelte" or "blazor" or "android"))
        {
            error = $"Generate target stack '{target.Stack}' is unknown; use dotnet, react, svelte, blazor or android.";
            return false;
        }

        if (IsTypeScript(target) && target.Kind is "event-decisions" or "closed-sets")
        {
            error = $"Generate target '{target.Kind}' needs a .NET stack, not '{target.Stack}'.";
            return false;
        }

        if (!IsTypeScript(target) && target.Namespace.Length == 0)
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no namespace.";
            return false;
        }

        if (IsTypeScript(target) && GenerateNaming.HasModulePlaceholder(target.Path))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' holds a module placeholder in its path, which only .NET stacks read.";
            return false;
        }

        return true;
    }

    /// <summary>Whether the target writes TypeScript: a react or svelte stack.</summary>
    /// <param name="target">The declared target.</param>
    /// <returns>True for a TypeScript stack; otherwise false.</returns>
    private static bool IsTypeScript(GenerateTarget target) => target.Stack is "react" or "svelte";

    /// <summary>Renders one validated target.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderTarget(ProjectModel model, GenerateTarget target) =>
        target.Kind switch
        {
            "refusal-codes" => IsTypeScript(target)
                ? RenderRefusalCodesTypeScript(model, target)
                : RenderRefusalCodesDotnet(model, target),
            "permissions" => IsTypeScript(target)
                ? RenderPermissionsTypeScript(model, target)
                : RenderPermissionsDotnet(model, target),
            "event-decisions" => RenderEventDecisions(model, target),
            _ => RenderClosedSets(model, target),
        };

    /// <summary>Renders one C# file per module holding refusals.</summary>
    /// <param name="model">The built project model holding the refusal sets.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderRefusalCodesDotnet(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();

        foreach (var (module, entries) in ByModule(model.RefusalSets.Select(entry => (entry.Module, Entry: entry))))
        {
            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = GenerateNaming.ModulePascal(module);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{name}RefusalCodes.generated.cs"),
                RefusalCodesRenderer.RenderDotnet(module, [.. entries], GenerateNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders every refusal code as one TypeScript file.</summary>
    /// <param name="model">The built project model holding the refusal sets and results.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The one rendered file, or an empty list when the records declare no refusal.</returns>
    private static List<GeneratedFile> RenderRefusalCodesTypeScript(ProjectModel model, GenerateTarget target)
    {
        if (model.RefusalSets.Count == 0)
        {
            return [];
        }

        var folder = Combine(model.Config.Root, target.Path);

        return [new GeneratedFile(
            Path.Combine(folder, "refusal-codes.ts"),
            RefusalCodesRenderer.RenderTypeScript([.. model.RefusalSets], RefusalSentences(model)))];
    }

    /// <summary>Renders one C# file per module holding permissions.</summary>
    /// <param name="model">The built project model holding the permission records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderPermissionsDotnet(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var permissions = model.Records
            .Where(record => record.Type == RecordTypes.Permission && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, record));

        foreach (var (module, entries) in ByModule(permissions))
        {
            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = GenerateNaming.ModulePascal(module);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{name}Permissions.generated.cs"),
                PermissionsRenderer.RenderDotnet(module, entries, GenerateNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders every permission id as one TypeScript file.</summary>
    /// <param name="model">The built project model holding the permission records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The one rendered file, or an empty list when the records declare no permission.</returns>
    private static List<GeneratedFile> RenderPermissionsTypeScript(ProjectModel model, GenerateTarget target)
    {
        var permissions = model.Records
            .Where(record => record.Type == RecordTypes.Permission && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, Permission: record))
            .ToList();

        if (permissions.Count == 0)
        {
            return [];
        }

        var folder = Combine(model.Config.Root, target.Path);

        return [new GeneratedFile(
            Path.Combine(folder, "permissions.ts"),
            PermissionsRenderer.RenderTypeScript(permissions))];
    }

    /// <summary>Renders one C# file per module holding domain events, with the decision the records state for each event.</summary>
    /// <param name="model">The built project model holding the domain event records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderEventDecisions(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
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
            var name = GenerateNaming.ModulePascal(module);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{name}EventDecisions.generated.cs"),
                EventDecisionsRenderer.RenderDotnet(module, decisions, GenerateNaming.ResolveModule(target.Namespace, module))));
        }

        return files;
    }

    /// <summary>Renders one C# file per home holding closed value sets.</summary>
    /// <param name="model">The built project model holding the reference-data and setting records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in home order.</returns>
    private static List<GeneratedFile> RenderClosedSets(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
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
            var pascal = GenerateNaming.ModulePascal(home);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{pascal}ClosedSets.generated.cs"),
                ClosedSetsRenderer.RenderDotnet(home, sets, GenerateNaming.ResolveModule(target.Namespace, home))));
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

    /// <summary>The English sentence by refusal code, the first result in identifier order stating one.</summary>
    /// <param name="model">The built project model holding the result records.</param>
    /// <returns>The sentence by refusal code.</returns>
    private static Dictionary<string, string> RefusalSentences(ProjectModel model)
    {
        var sentences = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var result in model.Records
            .Where(record => record.Type == RecordTypes.Result)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (result.Field(RecordFields.Outcome) != "refusal"
                || result.Field(RecordFields.Refusal) is not { Length: > 0 } refusal
                || !refusal.StartsWith("refusal/", StringComparison.Ordinal)
                || result.Field(RecordFields.Sentence) is not { Length: > 0 } sentence)
            {
                continue;
            }

            sentences.TryAdd(refusal["refusal/".Length..], sentence);
        }

        return sentences;
    }

    /// <summary>The folder one .NET module writes into: the resolved path, or the path with a PascalCase module folder.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="module">The record module name.</param>
    /// <returns>The absolute module folder.</returns>
    private static string DotnetFolder(string root, GenerateTarget target, string module) =>
        GenerateNaming.HasModulePlaceholder(target.Path)
            ? Combine(root, GenerateNaming.ResolveModule(target.Path, module))
            : Path.Combine(Combine(root, target.Path), GenerateNaming.ModulePascal(module));

    /// <summary>Combines a root with a forward-slash relative path into an absolute path.</summary>
    /// <param name="root">The absolute root.</param>
    /// <param name="relative">The relative path, written with forward slashes.</param>
    /// <returns>The absolute combined path.</returns>
    private static string Combine(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
}
