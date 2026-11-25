using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;

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
    /// <param name="Files">The rendered files in absolute path order.</param>
    /// <param name="Errors">The target errors, one per invalid target, in target order.</param>
    /// <param name="ConfigErrors">The configuration errors: a target inside another tool's output folder, or two targets writing the same file.</param>
    /// <param name="Skips">The use cases no target renders, each with the reason, in identifier order.</param>
    public sealed record Plan(
        IReadOnlyList<GeneratedFile> Files,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> ConfigErrors,
        IReadOnlyList<SkippedMessage> Skips);

    /// <summary>One use case the plan renders nothing for: the record states something the declaration vocabulary cannot express.</summary>
    /// <param name="UseCaseId">The skipped use case identifier.</param>
    /// <param name="Reason">Why the use case renders nothing.</param>
    public sealed record SkippedMessage(string UseCaseId, string Reason);

    /// <summary>Renders every configured target in memory from the built model.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <returns>The rendered files with the target and configuration errors and the skipped use cases.</returns>
    public static Plan Build(ProjectModel model)
    {
        var files = new List<GeneratedFile>();
        var errors = new List<string>();
        var valid = new List<(GenerateTarget Target, int Index)>();
        var fileTargets = new List<(GeneratedFile File, int TargetIndex)>();
        var skips = new List<SkippedMessage>();

        for (var index = 0; index < model.Config.Generate.Count; index++)
        {
            var target = model.Config.Generate[index];

            if (Validate(target, out var error) is false)
            {
                errors.Add(error!);
                continue;
            }

            valid.Add((target, index));

            foreach (var file in RenderTarget(model, target, skips))
            {
                files.Add(file);
                fileTargets.Add((file, index));
            }
        }

        var configErrors = ValidateConfig(model, valid, fileTargets);

        files.Sort((left, right) => string.Compare(left.AbsolutePath, right.AbsolutePath, StringComparison.Ordinal));

        var orderedSkips = skips
            .Distinct()
            .OrderBy(skip => skip.UseCaseId, StringComparer.Ordinal)
            .ToList();

        return new Plan(files, errors, configErrors, orderedSkips);
    }

    /// <summary>The configuration errors of the valid targets: a target writing inside a frontend's generated client folder, or two targets writing the same file.</summary>
    /// <param name="model">The built project model holding the targets and frontends.</param>
    /// <param name="valid">The valid targets with their declaration indices, in declaration order.</param>
    /// <param name="fileTargets">Every rendered file with the declaration index of the target that wrote it.</param>
    /// <returns>The configuration errors, client overlaps in target order then duplicates in path order.</returns>
    private static List<string> ValidateConfig(
        ProjectModel model,
        IReadOnlyList<(GenerateTarget Target, int Index)> valid,
        IReadOnlyList<(GeneratedFile File, int TargetIndex)> fileTargets)
    {
        var configErrors = new List<string>();

        foreach (var (target, index) in valid)
        {
            foreach (var frontend in model.Config.Frontends)
            {
                if (frontend.Client is not { } client)
                {
                    continue;
                }

                if (WritesInside(model.Config.Root, target, fileTargets, index, client.Generated))
                {
                    var relative = PathText.Relative(model.Config.Root, client.Generated);

                    configErrors.Add(
                        $"Generate target '{target.Kind}' for stack '{target.Stack}' writes inside frontend '{frontend.Name}' generated client folder '{relative}'; move the target out of '{relative}'.");
                    break;
                }
            }
        }

        var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var display = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (file, targetIndex) in fileTargets)
        {
            if (!groups.TryGetValue(file.AbsolutePath, out var writers))
            {
                writers = [];
                groups[file.AbsolutePath] = writers;
                display[file.AbsolutePath] = file.AbsolutePath;
            }

            writers.Add(targetIndex);
        }

        foreach (var path in groups.Keys.Order(StringComparer.Ordinal))
        {
            var writers = groups[path];

            if (writers.Count < 2)
            {
                continue;
            }

            var relative = PathText.Relative(model.Config.Root, display[path]);
            var distinct = writers.Distinct().Order().ToList();

            if (distinct.Count == 1)
            {
                var target = model.Config.Generate[distinct[0]];

                configErrors.Add(
                    $"Generate target '{target.Kind}' for stack '{target.Stack}' writes '{relative}' twice; give each home its own file.");
                continue;
            }

            var named = string.Join(
                " and ",
                distinct.Select(index =>
                {
                    var target = model.Config.Generate[index];
                    return $"'{target.Kind}' for stack '{target.Stack}' at '{target.Path}'";
                }));

            configErrors.Add($"Generate targets write the same file '{relative}': {named}; give each target its own file.");
        }

        return configErrors;
    }

    /// <summary>Whether the target writes inside the folder: its representative folder sits inside it, or any file it renders sits inside it.</summary>
    /// <param name="root">The consumer root the target path resolves against.</param>
    /// <param name="target">The valid target.</param>
    /// <param name="fileTargets">Every rendered file with the declaration index of the target that wrote it.</param>
    /// <param name="index">The declaration index of the target.</param>
    /// <param name="folder">The absolute folder another tool owns.</param>
    /// <returns>True when the target's folder or any of its files sits inside the folder.</returns>
    private static bool WritesInside(
        string root,
        GenerateTarget target,
        IReadOnlyList<(GeneratedFile File, int TargetIndex)> fileTargets,
        int index,
        string folder)
    {
        var representative = GenerateNaming.HasModulePlaceholder(target.Path)
            ? Combine(root, GenerateNaming.ResolveModule(target.Path, "__module__"))
            : GenerateNaming.HasKnowledgePlaceholder(target.Path)
                ? Combine(root, GenerateNaming.ResolveKnowledge(target.Path, "__area__", "__variant__"))
                : Combine(root, target.Path);

        if (IsInsideOrEqual(representative, folder))
        {
            return true;
        }

        foreach (var (file, targetIndex) in fileTargets)
        {
            if (targetIndex == index && IsInside(file.AbsolutePath, folder))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the path sits inside the folder or names the folder itself.</summary>
    /// <param name="path">The absolute path.</param>
    /// <param name="folder">The absolute folder.</param>
    /// <returns>True when the path equals the folder or sits under it.</returns>
    private static bool IsInsideOrEqual(string path, string folder) =>
        string.Equals(path, folder, StringComparison.Ordinal) || IsInside(path, folder);

    /// <summary>Whether the path sits under the folder.</summary>
    /// <param name="path">The absolute path.</param>
    /// <param name="folder">The absolute folder.</param>
    /// <returns>True when the path sits under the folder.</returns>
    private static bool IsInside(string path, string folder) =>
        path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    /// <summary>Whether the target names a supported kind and stack combination.</summary>
    /// <param name="target">The declared target.</param>
    /// <param name="error">The target error when the combination is unsupported.</param>
    /// <returns>True when the target renders; otherwise false.</returns>
    private static bool Validate(GenerateTarget target, out string? error)
    {
        error = null;

        if (target.Kind is not ("refusal-codes" or "permissions" or "event-decisions" or "closed-sets" or "knowledge-sets" or "exceptions" or "domain-events" or "module-events" or "declarations" or "validators" or "messages" or "results" or "endpoints"))
        {
            error = $"Generate target kind '{target.Kind}' is unknown; use refusal-codes, permissions, event-decisions, closed-sets, knowledge-sets, exceptions, domain-events, module-events, declarations, validators, messages, results or endpoints.";
            return false;
        }

        if (target.Stack is not ("dotnet" or "react" or "svelte" or "blazor" or "android"))
        {
            error = $"Generate target stack '{target.Stack}' is unknown; use dotnet, react, svelte, blazor or android.";
            return false;
        }

        if (IsTypeScript(target) && target.Kind is "event-decisions" or "closed-sets" or "knowledge-sets" or "exceptions" or "domain-events" or "module-events" or "declarations" or "validators" or "messages" or "results")
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

        if (target.Kind == "knowledge-sets" && GenerateNaming.HasModulePlaceholder(target.Path))
        {
            error = $"Generate target 'knowledge-sets' for stack '{target.Stack}' holds a module placeholder in its path, which knowledge sets do not read; use {{area}} and {{variant}}.";
            return false;
        }

        if (target.Kind == "exceptions" && target.BaseType.Length == 0)
        {
            error = $"Generate target 'exceptions' for stack '{target.Stack}' names no baseType.";
            return false;
        }

        if (target.Kind == "exceptions" && !GenerateNaming.HasAggregatePlaceholder(target.Path))
        {
            error = $"Generate target 'exceptions' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its path, which each aggregate's exceptions need.";
            return false;
        }

        if (target.Kind == "exceptions" && !GenerateNaming.HasAggregatePlaceholder(target.Namespace))
        {
            error = $"Generate target 'exceptions' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its namespace, which each aggregate's exceptions need.";
            return false;
        }

        if (target.Kind == "domain-events" && target.MarkerType.Length == 0)
        {
            error = $"Generate target 'domain-events' for stack '{target.Stack}' names no markerType.";
            return false;
        }

        if (target.Kind is "domain-events" or "module-events" && !GenerateNaming.HasAggregatePlaceholder(target.Path))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its path, which each aggregate's events need.";
            return false;
        }

        if (target.Kind is "domain-events" or "module-events" && !GenerateNaming.HasAggregatePlaceholder(target.Namespace))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its namespace, which each aggregate's events need.";
            return false;
        }

        if (target.Kind is "messages" or "results" && !GenerateNaming.HasAggregatePlaceholder(target.Path))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its path, which each use case folder needs.";
            return false;
        }

        if (target.Kind is "messages" or "results" && !GenerateNaming.HasAggregatePlaceholder(target.Namespace))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{Aggregate}} placeholder in its namespace, which each use case folder needs.";
            return false;
        }

        if (target.Kind is "messages" or "results" && !GenerateNaming.HasUseCasePlaceholder(target.Path))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{UseCase}} placeholder in its path, which each use case folder needs.";
            return false;
        }

        if (target.Kind is "messages" or "results" && !GenerateNaming.HasUseCasePlaceholder(target.Namespace))
        {
            error = $"Generate target '{target.Kind}' for stack '{target.Stack}' names no {{UseCase}} placeholder in its namespace, which each use case folder needs.";
            return false;
        }

        if (target.Kind == "declarations" && target.PermissionPolicy.Length > 0
            && !target.PermissionPolicy.Contains("{permission}", StringComparison.Ordinal)
            && !target.PermissionPolicy.Contains("{name}", StringComparison.Ordinal))
        {
            error = $"Generate target 'declarations' for stack '{target.Stack}' names permissionPolicy '{target.PermissionPolicy}', which holds neither {{permission}} nor {{name}}.";
            return false;
        }

        if (target.Kind == "declarations" && target.SelfPolicy.Length > 0
            && !target.SelfPolicy.Contains("{policy}", StringComparison.Ordinal))
        {
            error = $"Generate target 'declarations' for stack '{target.Stack}' names selfPolicy '{target.SelfPolicy}', which holds no {{policy}}.";
            return false;
        }

        if (target.Kind == "declarations" && target.RecentSignInPolicy.Length > 0
            && !target.RecentSignInPolicy.Contains("{policy}", StringComparison.Ordinal))
        {
            error = $"Generate target 'declarations' for stack '{target.Stack}' names recentSignInPolicy '{target.RecentSignInPolicy}', which holds no {{policy}}.";
            return false;
        }

        if (target.Kind == "declarations" && target.RecentSignInPolicy.Length > 0
            && !target.RecentSignInPolicy.Contains("{window}", StringComparison.Ordinal))
        {
            error = $"Generate target 'declarations' for stack '{target.Stack}' names recentSignInPolicy '{target.RecentSignInPolicy}', which holds no {{window}}.";
            return false;
        }

        if (target.Kind == "declarations" && target.RunsAsPolicy.Length > 0
            && !target.RunsAsPolicy.Contains("{policy}", StringComparison.Ordinal))
        {
            error = $"Generate target 'declarations' for stack '{target.Stack}' names runsAsPolicy '{target.RunsAsPolicy}', which holds no {{policy}}.";
            return false;
        }

        if (target.Kind == "endpoints" && target.Stack != "dotnet")
        {
            error = $"Generate target 'endpoints' needs stack 'dotnet', not '{target.Stack}'.";
            return false;
        }

        if (target.Kind == "endpoints" && target.ClaimReader.Length == 0)
        {
            error = $"Generate target 'endpoints' for stack '{target.Stack}' names no claimReader.";
            return false;
        }

        if (target.Kind == "endpoints" && target.ClaimReader.Split('.', StringSplitOptions.RemoveEmptyEntries) is not { Length: >= 3 })
        {
            error = $"Generate target 'endpoints' for stack '{target.Stack}' names claimReader '{target.ClaimReader}', which is no qualified method; use Namespace.Type.Method.";
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
    /// <param name="skips">The skipped use cases, appended in place.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderTarget(ProjectModel model, GenerateTarget target, List<SkippedMessage> skips) =>
        target.Kind switch
        {
            "refusal-codes" => IsTypeScript(target)
                ? RenderRefusalCodesTypeScript(model, target)
                : RenderRefusalCodesDotnet(model, target),
            "permissions" => IsTypeScript(target)
                ? RenderPermissionsTypeScript(model, target)
                : RenderPermissionsDotnet(model, target),
            "event-decisions" => RenderEventDecisions(model, target),
            "closed-sets" => RenderClosedSets(model, target),
            "exceptions" => RenderExceptions(model, target),
            "domain-events" => RenderDomainEvents(model, target),
            "module-events" => RenderModuleEvents(model, target),
            "declarations" => RenderDeclarations(model, target, skips),
            "validators" => RenderValidators(model, target),
            "messages" => RenderMessages(model, target),
            "results" => RenderResults(model, target),
            "endpoints" => RenderEndpoints(model, target, skips),
            _ => RenderKnowledgeSets(model, target),
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

                var values = ClosedSets.ChoiceTexts(record);

                if (values is null)
                {
                    continue;
                }

                sets.Add(new ClosedSetsRenderer.ClosedSet(
                    record.Id,
                    setName,
                    record.Field(RecordFields.Summary) ?? record.Field(RecordFields.Meaning),
                    values,
                    ClosedSets.ValueMeanings(record) ?? new Dictionary<string, string>(StringComparer.Ordinal)));
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

    /// <summary>Renders one C# file per aggregate-declared refusal, with the exception its records determine.</summary>
    /// <param name="model">The built project model holding the refusal sets and the rules naming them.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module, aggregate then code order.</returns>
    private static List<GeneratedFile> RenderExceptions(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var rules = RulesByRefusal(model);

        foreach (var entry in model.RefusalSets
            .Where(candidate => candidate.Aggregate is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Aggregate, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Code, StringComparer.Ordinal))
        {
            var aggregate = entry.Aggregate!;
            var folder = Combine(
                model.Config.Root,
                GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Path, aggregate), entry.Module));
            var @namespace = GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Namespace, aggregate), entry.Module);
            var recordName = AggregateNameOf(model, entry.Module, aggregate);
            var type = ExceptionsRenderer.TypeName(entry.Module, recordName, entry.Code);

            rules.TryGetValue("refusal/" + entry.Code, out var rule);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{type}.generated.cs"),
                ExceptionsRenderer.RenderDotnet(new ExceptionsRenderer.ExceptionData(
                    entry.Module, recordName, entry, rule, target.BaseType, @namespace, model.Config.Marks))));
        }

        return files;
    }

    /// <summary>Renders one C# file per domain event, with the payload its record carries.</summary>
    /// <param name="model">The built project model holding the domain event records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module, aggregate folder then event order.</returns>
    private static List<GeneratedFile> RenderDomainEvents(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var usings = target.Usings ?? [];

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.DomainEvent && !candidate.Derived && candidate.Module is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out var aggregate, out var name)
                || aggregate is null
                || AggregateFolderOf(record) is not { } folder
                || FieldsOf(record) is not { } fields)
            {
                continue;
            }

            var resolved = Combine(
                model.Config.Root,
                GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Path, folder), record.Module!));
            var @namespace = GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Namespace, folder), record.Module!);
            var type = DomainEventsRenderer.TypeName(aggregate, name);

            files.Add(new GeneratedFile(
                Path.Combine(resolved, $"{type}.generated.cs"),
                DomainEventsRenderer.RenderDotnet(new DomainEventsRenderer.EventData(
                    record.Id, aggregate, name, record.Field(RecordFields.Meaning),
                    fields, @namespace, target.MarkerType, usings, model.Config.Marks))));
        }

        return files;
    }

    /// <summary>Renders one C# file per module event in its fact folder, with the fields its record publishes.</summary>
    /// <param name="model">The built project model holding the module event records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module, aggregate folder then event order.</returns>
    private static List<GeneratedFile> RenderModuleEvents(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var usings = target.Usings ?? [];

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.ModuleEvent && !candidate.Derived && candidate.Module is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name)
                || AggregateFolderOf(record) is not { } folder
                || PublishedFieldsOf(model, record) is not { } fields)
            {
                continue;
            }

            var onFact = ModuleEventsRenderer.OnFolder(name);
            var resolved = Path.Combine(
                Combine(
                    model.Config.Root,
                    GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Path, folder), record.Module!)),
                onFact);
            var @namespace = GenerateNaming.ResolveModule(GenerateNaming.ResolveAggregate(target.Namespace, folder), record.Module!)
                + "." + onFact;
            var type = ModuleEventsRenderer.TypeName(name);

            files.Add(new GeneratedFile(
                Path.Combine(resolved, $"{type}.generated.cs"),
                ModuleEventsRenderer.RenderDotnet(new ModuleEventsRenderer.EventData(
                    record.Id, name, record.Field(RecordFields.Summary),
                    fields, @namespace, target.MarkerType, usings, model.Config.Marks))));
        }

        return files;
    }

    /// <summary>Renders one C# file per use-case message in its use case folder, with the inputs, implied members, and message interface its record states.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module, aggregate folder then use case order.</returns>
    private static List<GeneratedFile> RenderMessages(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var usings = target.Usings ?? [];
        var wantPublic = target.Visibility == "public";

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.UseCase && !candidate.Derived && candidate.Module is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => AggregateFolderOf(candidate) ?? "", StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (record.Field(RecordFields.Kind) is not ("command" or "query")
                || !Identifiers.TrySplit(record.Id, out _, out _, out var name)
                || IsPublicUseCase(record) != wantPublic
                || MessageInputsOf(record) is not { } inputs)
            {
                continue;
            }

            if (!TryMessageKey(record, out var key))
            {
                continue;
            }

            var kind = record.Field(RecordFields.Kind)!;
            var folder = AggregateFolderOf(record);
            var resolved = Combine(
                model.Config.Root,
                GenerateNaming.ResolveModule(GenerateNaming.ResolveUseCaseFolders(target.Path, folder, name), record.Module!));
            var @namespace = GenerateNaming.ResolveModule(GenerateNaming.ResolveUseCaseFolders(target.Namespace, folder, name), record.Module!);
            var message = MessagesRenderer.MessageTypeName(name, kind);
            var messageInterface = kind == "query" ? target.QueryInterface : target.CommandInterface;

            files.Add(new GeneratedFile(
                Path.Combine(resolved, $"{message}.generated.cs"),
                MessagesRenderer.RenderDotnet(new MessagesRenderer.MessageData(
                    record.Id, message, MessagesRenderer.ResultTypeName(name, kind, wantPublic),
                    record.Field(RecordFields.Summary), inputs, TenantPropertyOf(model, record),
                    PagingOf(record), key, @namespace, messageInterface, usings, wantPublic))));

            foreach (var row in inputs.Where(input => input.RowType is not null && input.Row.Count > 0))
            {
                files.Add(new GeneratedFile(
                    Path.Combine(resolved, $"{row.RowType}.generated.cs"),
                    ResultsRenderer.RenderRowDotnet(new ResultsRenderer.RowData(
                        row.RowType!, row.Property, row.Row, @namespace, usings, wantPublic))));
            }
        }

        return files;
    }

    /// <summary>Renders one C# file per use-case result in its use case folder, with the fields, groups, rows, and next cursor its output states.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module, aggregate folder then use case order.</returns>
    private static List<GeneratedFile> RenderResults(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var usings = target.Usings ?? [];
        var wantPublic = target.Visibility == "public";

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.UseCase && !candidate.Derived && candidate.Module is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => AggregateFolderOf(candidate) ?? "", StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (record.Field(RecordFields.Kind) is not ("command" or "query")
                || !Identifiers.TrySplit(record.Id, out _, out _, out var name)
                || IsPublicUseCase(record) != wantPublic
                || !HasOutput(record)
                || TryResultShape(record, out var fields, out var groups) is false)
            {
                continue;
            }

            var kind = record.Field(RecordFields.Kind)!;
            var folder = AggregateFolderOf(record);
            var resolved = Combine(
                model.Config.Root,
                GenerateNaming.ResolveModule(GenerateNaming.ResolveUseCaseFolders(target.Path, folder, name), record.Module!));
            var @namespace = GenerateNaming.ResolveModule(GenerateNaming.ResolveUseCaseFolders(target.Namespace, folder, name), record.Module!);
            var result = MessagesRenderer.ResultTypeName(name, kind, wantPublic);

            files.Add(new GeneratedFile(
                Path.Combine(resolved, $"{result}.generated.cs"),
                ResultsRenderer.RenderDotnet(new ResultsRenderer.ResultData(
                    record.Id, record.Title, result, fields, groups,
                    PagingOf(record) is not null, @namespace, usings, wantPublic))));

            var rows = fields.Concat(groups.SelectMany(group => group.Fields))
                .Where(field => field.RowType is not null && field.Row.Count > 0);

            foreach (var row in rows)
            {
                files.Add(new GeneratedFile(
                    Path.Combine(resolved, $"{row.RowType}.generated.cs"),
                    ResultsRenderer.RenderRowDotnet(new ResultsRenderer.RowData(
                        row.RowType!, row.Property, row.Row, @namespace, usings, wantPublic))));
            }
        }

        return files;
    }

    /// <summary>Whether a use case is public: another module reaches it through its reachedThrough.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>True when reachedThrough names a module; otherwise false.</returns>
    private static bool IsPublicUseCase(Record record) =>
        record.Ids(RecordFields.ReachedThrough).Any(id => id.StartsWith("module/", StringComparison.Ordinal));

    /// <summary>The input fields of a use case with their C# types, in written order.</summary>
    /// <param name="record">The use case record whose inputs are read.</param>
    /// <returns>The mapped fields, or null when any entry states no known shape; the record check reports the entry.</returns>
    private static List<MessageFieldTypes.MappedField>? MessageInputsOf(Record record)
    {
        var fields = new List<MessageFieldTypes.MappedField>();

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Input, out var inputs)
            || inputs.ValueKind != JsonValueKind.Array)
        {
            return fields;
        }

        foreach (var entry in inputs.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !MessageFieldTypes.TryMapInput(entry, out var field) || field is null)
            {
                return null;
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>The tenant property a message carries, from the tenancy selection, or null when the use case implies none.</summary>
    /// <param name="model">The built project model holding the tenancy selection and the tenant term.</param>
    /// <param name="record">The use case record whose tenant scope is read.</param>
    /// <returns>The tenant property name, or null.</returns>
    private static string? TenantPropertyOf(ProjectModel model, Record record)
    {
        var tenant = model.Config.TenancyTenantTerm;

        if (tenant.Length == 0
            || !model.ById.ContainsKey(tenant)
            || record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || RecordEntries.Str(access, RecordFields.Tenant) is not ("own" or "all")
            || !Identifiers.TrySplit(tenant, out _, out _, out var name)
            || name.Length == 0)
        {
            return null;
        }

        return Naming.Pascal(name) + "Id";
    }

    /// <summary>The paging members a use case carries, or null when the use case pages nothing.</summary>
    /// <param name="record">The use case record whose paging is read.</param>
    /// <returns>The paging members, or null.</returns>
    private static MessagesRenderer.MessagePaging? PagingOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Paging, out var paging)
            || paging.ValueKind != JsonValueKind.Object
            || RecordEntries.Str(paging, RecordFields.Mode) is not { Length: > 0 } mode
            || mode == "none")
        {
            return null;
        }

        if (!paging.TryGetProperty(RecordFields.DefaultLimit, out var limit)
            || limit.ValueKind != JsonValueKind.Number
            || !limit.TryGetInt32(out var defaultLimit))
        {
            return null;
        }

        return new MessagesRenderer.MessagePaging(
            RecordEntries.Str(paging, RecordFields.Cursor),
            RecordEntries.Str(paging, RecordFields.Offset),
            defaultLimit);
    }

    /// <summary>Reads the idempotency key of a keyed command: whether the key template renders over the mapped inputs.</summary>
    /// <param name="record">The use case record whose idempotency is read.</param>
    /// <param name="key">The key with its selector body, or null when the command keys none.</param>
    /// <returns>True with the key or with none; false when the template names an input the message does not carry, and the message renders no file.</returns>
    private static bool TryMessageKey(Record record, out MessagesRenderer.MessageKey? key)
    {
        key = null;

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object
            || RecordEntries.Str(idempotency, RecordFields.Mode) != "key")
        {
            return true;
        }

        if (RecordEntries.Str(idempotency, RecordFields.Key) is not { Length: > 0 } template)
        {
            return false;
        }

        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (record.Raw.TryGetProperty(RecordFields.Input, out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in array.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && RecordEntries.Str(entry, RecordFields.Name) is { Length: > 0 } name
                    && RecordEntries.Str(entry, RecordFields.Type) is { Length: > 0 } type)
                {
                    properties.TryAdd(name, MessageFieldTypes.PropertyName(name, type));
                }
            }
        }

        var parts = new List<(string? Literal, string? Property)>();
        var matches = KeyPlaceholders.Matches(template);
        var at = 0;

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Index > at)
            {
                parts.Add((template[at..match.Index], null));
            }

            var name = match.Groups["name"].Value.Trim();

            if (!properties.TryGetValue(name, out var property))
            {
                key = null;

                return false;
            }

            parts.Add((null, property));
            at = match.Index + match.Length;
        }

        if (at < template.Length)
        {
            parts.Add((template[at..], null));
        }

        key = new MessagesRenderer.MessageKey(template, KeyExpression(parts));

        return true;
    }

    /// <summary>A placeholder in a key template, written between braces.</summary>
    private static readonly System.Text.RegularExpressions.Regex KeyPlaceholders =
        new(@"\{(?<name>[^{}]+)\}", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>The key selector body: a plain literal when the template names no property, else an interpolated string over the message properties.</summary>
    /// <param name="parts">The key template parts in template order.</param>
    /// <returns>The selector body text.</returns>
    private static string KeyExpression(IReadOnlyList<(string? Literal, string? Property)> parts)
    {
        if (parts.All(part => part.Property is null))
        {
            return $"\"{GenerateNaming.EscapeLiteral(string.Concat(parts.Select(part => part.Literal ?? "")))}\"";
        }

        var body = string.Concat(parts.Select(part =>
            part.Property is not null
                ? $"{{{part.Property}}}"
                : GenerateNaming.EscapeLiteral(part.Literal ?? "")
                    .Replace("{", "{{", StringComparison.Ordinal)
                    .Replace("}", "}}", StringComparison.Ordinal)));

        return $"$\"{body}\"";
    }

    /// <summary>Whether a use case declares an output its result renders from.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>True when the record holds an output value; otherwise false.</returns>
    private static bool HasOutput(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Output, out _);

    /// <summary>Reads the result shape of a use case: its flat fields, or its groups when the output holds several.</summary>
    /// <param name="record">The use case record whose output is read.</param>
    /// <param name="fields">The flat output fields, or an empty list when the output holds several groups.</param>
    /// <param name="groups">The output groups, or an empty list when the output holds no groups.</param>
    /// <returns>True when the output maps; otherwise false, and the result renders no file.</returns>
    private static bool TryResultShape(
        Record record,
        out List<MessageFieldTypes.MappedField> fields,
        out List<ResultsRenderer.ResultGroup> groups)
    {
        fields = [];
        groups = [];

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return false;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || !MessageFieldTypes.TryMapOutput(entry, out var field) || field is null)
                {
                    return false;
                }

                fields.Add(field);
            }

            return true;
        }

        if (output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var held = include.EnumerateObject()
            .Where(group => group.Value.ValueKind == JsonValueKind.Object)
            .ToList();

        if (held.Count == 0)
        {
            return false;
        }

        if (held.Count == 1
            && held[0].Value.TryGetProperty(RecordFields.Fields, out var single)
            && single.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in single.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || !MessageFieldTypes.TryMapOutput(entry, out var field) || field is null)
                {
                    return false;
                }

                fields.Add(field);
            }

            return true;
        }

        foreach (var group in held)
        {
            if (!group.Value.TryGetProperty(RecordFields.Fields, out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var mapped = new List<MessageFieldTypes.MappedField>();

            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || !MessageFieldTypes.TryMapOutput(entry, out var field) || field is null)
                {
                    return false;
                }

                mapped.Add(field);
            }

            groups.Add(new ResultsRenderer.ResultGroup(group.Name, GenerateNaming.ConstName(group.Name), mapped));
        }

        return true;
    }

    /// <summary>The carried fields of a domain event with their C# types, in written order.</summary>
    /// <param name="record">The domain event record whose carries are read.</param>
    /// <returns>The mapped fields, or null when any entry states no known shape; the record check reports the entry.</returns>
    private static List<EventFieldTypes.MappedField>? FieldsOf(Record record)
    {
        var fields = new List<EventFieldTypes.MappedField>();

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Carries, out var carries)
            || carries.ValueKind != JsonValueKind.Array)
        {
            return fields;
        }

        foreach (var entry in carries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !EventFieldTypes.TryMap(entry, out var field) || field is null)
            {
                return null;
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>The published fields of a module event with their C# types from the translated domain event, in written order.</summary>
    /// <param name="model">The built project model resolving the translated domain event.</param>
    /// <param name="record">The module event record whose fields are read.</param>
    /// <returns>The mapped fields, or null when the translation or any field resolves to no known shape; the record check reports it.</returns>
    private static List<EventFieldTypes.MappedField>? PublishedFieldsOf(ProjectModel model, Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Fields, out var names)
            || names.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var translated = record.Ids(RecordFields.Translates)
            .Select(Identifiers.BaseId)
            .Select(id => model.ById.TryGetValue(id, out var domain) ? domain : null)
            .FirstOrDefault(domain => domain?.Type == RecordTypes.DomainEvent);

        if (translated is null)
        {
            return null;
        }

        var carried = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (translated.Raw.ValueKind == JsonValueKind.Object
            && translated.Raw.TryGetProperty(RecordFields.Carries, out var carries)
            && carries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in carries.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && RecordEntries.Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    carried.TryAdd(name, entry);
                }
            }
        }

        var fields = new List<EventFieldTypes.MappedField>();

        foreach (var entry in names.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String
                || entry.GetString() is not { Length: > 0 } name
                || !carried.TryGetValue(name, out var carry)
                || !EventFieldTypes.TryMap(carry, out var field)
                || field is null)
            {
                return null;
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>The aggregate folder holding an event record: the folder under aggregates its file sits in.</summary>
    /// <param name="record">The domain or module event record.</param>
    /// <returns>The aggregate folder name, or null when the file sits outside an aggregate folder.</returns>
    private static string? AggregateFolderOf(Record record)
    {
        var parts = record.File.Replace('\\', '/').Split('/');

        for (var index = 0; index + 1 < parts.Length; index++)
        {
            if (parts[index] == "aggregates" && parts[index + 1].Length > 0)
            {
                return parts[index + 1];
            }
        }

        return null;
    }

    /// <summary>The rule naming each refusal: the lowest rule identifier whose refusal field names it.</summary>
    /// <param name="model">The built project model holding the rule records.</param>
    /// <returns>The rule identifier by refusal identifier.</returns>
    private static Dictionary<string, string> RulesByRefusal(ProjectModel model)
    {
        var rules = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.Rule && !candidate.Derived)
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (record.Field(RecordFields.Refusal) is { Length: > 0 } refusal)
            {
                rules.TryAdd(refusal, record.Id);
            }
        }

        return rules;
    }

    /// <summary>The aggregate record name behind an aggregate folder: the name part of the aggregate record filed under it, or the singular folder name when no record is filed there.</summary>
    /// <param name="model">The built project model holding the aggregate records.</param>
    /// <param name="module">The record module holding the folder.</param>
    /// <param name="folder">The aggregate folder name.</param>
    /// <returns>The aggregate record name.</returns>
    private static string AggregateNameOf(ProjectModel model, string module, string folder)
    {
        var marker = "/aggregates/" + folder + "/";

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.Aggregate && candidate.Module == module)
            .OrderBy(candidate => candidate.File, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (record.File.Replace('\\', '/').Contains(marker, StringComparison.Ordinal)
                && Identifiers.TrySplit(record.Id, out var prefix, out _, out var name)
                && prefix == RecordTypes.Aggregate
                && name.Length > 0)
            {
                return name;
            }
        }

        return Singularize(folder);
    }

    /// <summary>The singular of an aggregate folder name, for a folder with no aggregate record filed under it.</summary>
    /// <param name="folder">The aggregate folder name.</param>
    /// <returns>The folder name singularized.</returns>
    private static string Singularize(string folder)
    {
        if (folder.EndsWith("ies", StringComparison.Ordinal) && folder.Length > 3)
        {
            return folder[..^3] + "y";
        }

        if (folder.EndsWith('s')
            && !folder.EndsWith("ss", StringComparison.Ordinal)
            && !folder.EndsWith("us", StringComparison.Ordinal))
        {
            return folder[..^1];
        }

        return folder;
    }

    /// <summary>Gets the name of the `row` field: how a reference-data table builds its knowledge rows.</summary>
    private const string RowField = "row";

    /// <summary>Gets the name of the `columns` field: the column-to-parameter mapping building each row.</summary>
    private const string ColumnsField = "columns";

    /// <summary>Gets the name of the `column` field: the row column one factory parameter reads.</summary>
    private const string ColumnField = "column";

    /// <summary>Gets the name of the `parameter` field: the factory parameter name.</summary>
    private const string ParameterField = "parameter";

    /// <summary>Gets the name of the `as` field: how the column value renders.</summary>
    private const string AsField = "as";

    /// <summary>Gets the name of the `factory` field: the static factory method building one row or key.</summary>
    private const string FactoryField = "factory";

    /// <summary>Gets the name of the `literals` field: the literal each row value maps to.</summary>
    private const string LiteralsField = "literals";

    /// <summary>Renders one C# file per knowledge table and variant holding row construction.</summary>
    /// <param name="model">The built project model holding the reference-data records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in table then variant order.</returns>
    private static List<GeneratedFile> RenderKnowledgeSets(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var tables = model.Records
            .Where(record => record.Type == RecordTypes.ReferenceData && !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var table in tables)
        {
            if (!Identifiers.TrySplit(table.Id, out _, out var home, out var name) || home is null)
            {
                continue;
            }

            if (!model.ById.ContainsKey($"{RecordTypes.KnowledgeArea}/{home}"))
            {
                continue;
            }

            if (!RowConstructionOf(table, out var rowType, out var factory))
            {
                continue;
            }

            var mappings = MappingsOf(table);

            if (mappings.Count == 0)
            {
                continue;
            }

            foreach (var (variant, rows) in RowsByVariant(table))
            {
                if (rows.Count == 0)
                {
                    continue;
                }

                var folder = KnowledgeFolder(model.Config.Root, target, home, variant);
                var file = $"{GenerateNaming.ConstName(variant)}{GenerateNaming.ConstName(name)}.generated.cs";

                files.Add(new GeneratedFile(
                    Path.Combine(folder, file),
                    KnowledgeSetsRenderer.RenderDotnet(
                        home,
                        variant,
                        name,
                        table.Id,
                        rowType,
                        factory,
                        mappings,
                        rows,
                        GenerateNaming.ResolveKnowledge(target.Namespace, home, variant))));
            }
        }

        return files;
    }

    /// <summary>Reads how a table builds its rows: the qualified row type with its factory, defaulting to Create.</summary>
    /// <param name="table">The reference-data record whose row construction is read.</param>
    /// <param name="rowType">The qualified model row type, or empty when the table states none.</param>
    /// <param name="factory">The static factory method building one row.</param>
    /// <returns>True when the table states a row type; otherwise false.</returns>
    private static bool RowConstructionOf(Record table, out string rowType, out string factory)
    {
        rowType = "";
        factory = "Create";

        if (table.Raw.ValueKind != JsonValueKind.Object
            || !table.Raw.TryGetProperty(RowField, out var row)
            || row.ValueKind != JsonValueKind.Object
            || RecordEntries.Str(row, RecordFields.Type) is not { } type)
        {
            return false;
        }

        rowType = type;
        factory = RecordEntries.Str(row, FactoryField) is { Length: > 0 } named ? named : "Create";

        return true;
    }

    /// <summary>Reads the column mappings of a table: one entry per factory parameter, in written order.</summary>
    /// <param name="table">The reference-data record whose column entries are read.</param>
    /// <returns>The well-shaped mappings; a malformed entry renders nothing, and the column check reports it.</returns>
    private static List<KnowledgeSetsRenderer.ColumnMapping> MappingsOf(Record table)
    {
        var mappings = new List<KnowledgeSetsRenderer.ColumnMapping>();

        if (table.Raw.ValueKind != JsonValueKind.Object
            || !table.Raw.TryGetProperty(ColumnsField, out var columns)
            || columns.ValueKind != JsonValueKind.Array)
        {
            return mappings;
        }

        foreach (var entry in columns.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || RecordEntries.Str(entry, ColumnField) is not { } column
                || RecordEntries.Str(entry, ParameterField) is not { } parameter
                || RecordEntries.Str(entry, AsField) is not { } rendering)
            {
                continue;
            }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            if (entry.TryGetProperty(LiteralsField, out var map) && map.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in map.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } literal)
                    {
                        values[property.Name] = literal;
                    }
                }
            }

            mappings.Add(new KnowledgeSetsRenderer.ColumnMapping(
                column,
                parameter,
                rendering,
                RecordEntries.Str(entry, RecordFields.Type),
                RecordEntries.Str(entry, FactoryField) is { Length: > 0 } factory ? factory : "From",
                RecordEntries.Str(entry, RecordFields.Currency),
                values));
        }

        return mappings;
    }

    /// <summary>Groups a table's rows by variant in variant order, skipping rows that name none.</summary>
    /// <param name="table">The reference-data record whose rows are read.</param>
    /// <returns>The non-empty groups in variant order.</returns>
    private static IReadOnlyList<(string Variant, IReadOnlyList<JsonElement> Rows)> RowsByVariant(Record table)
    {
        var groups = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);

        foreach (var row in table.Objects(RecordFields.Rows))
        {
            if (RecordEntries.Str(row, RecordFields.Variant) is not { } variant)
            {
                continue;
            }

            if (!groups.TryGetValue(variant, out var held))
            {
                held = [];
                groups[variant] = held;
            }

            held.Add(row);
        }

        return [.. groups
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, (IReadOnlyList<JsonElement>)group.Value))];
    }

    /// <summary>The policy name a permission grant reads when the target names no permissionPolicy: the permission id itself.</summary>
    private const string DefaultPermissionPolicy = "{permission}";

    /// <summary>The policy name a signed-in grant reads when the target names no signedInPolicy.</summary>
    private const string DefaultSignedInPolicy = "signed-in";

    /// <summary>The policy name an anonymous grant reads when the target names no anonymousPolicy.</summary>
    private const string DefaultAnonymousPolicy = "anonymous";

    /// <summary>The policy name a system grant reads when the target names no systemPolicy.</summary>
    private const string DefaultSystemPolicy = "system";

    /// <summary>How a self grant or a self scope wraps its policy when the target names no selfPolicy.</summary>
    private const string DefaultSelfPolicy = "Self({policy})";

    /// <summary>How a recent sign-in wraps its policy when the target names no recentSignInPolicy.</summary>
    private const string DefaultRecentSignInPolicy = "RecentSignIn({policy},{window})";

    /// <summary>How a use case the system also runs wraps its policy when the target names no runsAsPolicy.</summary>
    private const string DefaultRunsAsPolicy = "SystemOr({policy})";

    /// <summary>A placeholder in an idempotency key template, written between braces.</summary>
    private static readonly Regex KeyPlaceholder = new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

    /// <summary>Renders one C# file per module holding use cases, with the message declarations each use case record states.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="skips">The skipped use cases, appended in place.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderDeclarations(ProjectModel model, GenerateTarget target, List<SkippedMessage> skips)
    {
        var files = new List<GeneratedFile>();
        var uses = model.Records
            .Where(record => record.Type == RecordTypes.UseCase && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, record));

        foreach (var (module, entries) in ByModule(uses))
        {
            var messages = new List<DeclarationsRenderer.DeclaredMessage>();
            var resolved = GenerateNaming.ResolveModule(target.Namespace, module);

            foreach (var record in entries.OrderBy(entry => entry.Id, StringComparer.Ordinal))
            {
                var message = DeclaredMessageOf(record, target, resolved, out var skip);

                if (skip is not null)
                {
                    skips.Add(new SkippedMessage(record.Id, skip));
                }
                else if (message is not null)
                {
                    messages.Add(message);
                }
            }

            if (messages.Count == 0)
            {
                continue;
            }

            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = GenerateNaming.ModulePascal(module);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{name}Declarations.generated.cs"),
                DeclarationsRenderer.RenderDotnet(module, messages, resolved)));
        }

        return files;
    }

    /// <summary>Reads one use case record as the message its catalog source declares, or the reason it renders nothing.</summary>
    /// <param name="record">The use case record.</param>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="resolvedNamespace">The target namespace with the module resolved.</param>
    /// <param name="skip">The skip reason when the record states something the vocabulary cannot express.</param>
    /// <returns>The declared message, or null when the record renders nothing.</returns>
    private static DeclarationsRenderer.DeclaredMessage? DeclaredMessageOf(
        Record record,
        GenerateTarget target,
        string resolvedNamespace,
        out string? skip)
    {
        skip = null;

        var kind = record.Field(RecordFields.Kind);

        if (kind is not ("command" or "query"))
        {
            return null;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out _, out var useCaseName) || useCaseName.Length == 0)
        {
            return null;
        }

        if (PolicyOf(record, target, out var policySkip) is not { } policy)
        {
            skip = policySkip;
            return null;
        }

        var messageName = Naming.Pascal(useCaseName);
        var messageType = messageName + (kind == "query" ? "Query" : "Command");
        var parameter = kind == "query" ? "query" : "command";

        var audit = AuditCallOf(record, messageType, parameter);

        if (audit.Skip is not null)
        {
            skip = audit.Skip;
            return null;
        }

        var idempotency = IdempotencyCallOf(record, messageType);

        if (idempotency.Skip is not null)
        {
            skip = idempotency.Skip;
            return null;
        }

        return new DeclarationsRenderer.DeclaredMessage(
            record.Id,
            messageType,
            MessageNamespaceOf(resolvedNamespace, record, messageName),
            kind,
            DeclarationsRenderer.AuthorizedCall(policy),
            audit.Call,
            idempotency.Call);
    }

    /// <summary>One declaration position a record reads: its call, its skip reason, or neither when the record states nothing usable.</summary>
    /// <param name="Call">The declaration call, or null when the record states no position.</param>
    /// <param name="Skip">The skip reason, or null when the record renders or stays silent.</param>
    private sealed record DeclarationPosition(string? Call, string? Skip);

    /// <summary>The authorization policy name one use case record states: its grant composed with its scope, sign-in window and system runs.</summary>
    /// <param name="record">The use case record whose access block is read.</param>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="skip">The skip reason when the record states something the vocabulary cannot express.</param>
    /// <returns>The policy name, or null when the record renders nothing.</returns>
    private static string? PolicyOf(Record record, GenerateTarget target, out string? skip)
    {
        skip = null;

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var tenant = RecordEntries.Str(access, RecordFields.Tenant);

        if (tenant is not null && tenant != "own")
        {
            skip = $"access states tenant '{tenant}', which no authorization policy names";
            return null;
        }

        if (RecordEntries.Str(access, RecordFields.Grant) is not { Length: > 0 } grant)
        {
            return null;
        }

        string policy;

        if (grant == "signed-in")
        {
            policy = target.SignedInPolicy.Length > 0 ? target.SignedInPolicy : DefaultSignedInPolicy;
        }
        else if (grant == "anonymous")
        {
            policy = target.AnonymousPolicy.Length > 0 ? target.AnonymousPolicy : DefaultAnonymousPolicy;
        }
        else if (grant == "system")
        {
            policy = target.SystemPolicy.Length > 0 ? target.SystemPolicy : DefaultSystemPolicy;
        }
        else if (grant.StartsWith("permission/", StringComparison.Ordinal) && grant.Length > "permission/".Length)
        {
            policy = PermissionPolicyOf(target, grant);
        }
        else if (SelfPermissionOf(grant) is { } inner)
        {
            policy = SelfPolicyOf(target, PermissionPolicyOf(target, inner));
        }
        else
        {
            skip = $"grant '{grant}' is no signed-in, anonymous, system, permission or self grant";
            return null;
        }

        if (FirstCondition(access) is { Length: > 0 } condition)
        {
            skip = $"authorization condition '{condition}' names a guard check the policy vocabulary cannot state";
            return null;
        }

        // Preconditions stay out of the policy name: their rules refuse in the handler,
        // so the grant names the check and the cited rules carry the rest.

        if (RecordEntries.Str(access, RecordFields.Scope) == "self")
        {
            policy = SelfPolicyOf(target, policy);
        }

        if (RecordEntries.Str(access, RecordFields.RecentSignIn) is { Length: > 0 } window)
        {
            policy = RecentSignInPolicyOf(target, policy, window);
        }

        if (RecordEntries.Str(access, RecordFields.RunsAs) == "system")
        {
            policy = RunsAsPolicyOf(target, policy);
        }

        return policy;
    }

    /// <summary>The audit call one use case record states, or the reason it renders nothing.</summary>
    /// <param name="record">The use case record whose audit block is read.</param>
    /// <param name="messageType">The simple message type name the target selector reads.</param>
    /// <param name="parameter">The lambda parameter name: command or query.</param>
    /// <returns>The audit position: its call, its skip reason, or neither.</returns>
    private static DeclarationPosition AuditCallOf(Record record, string messageType, string parameter)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Audit, out var audit)
            || audit.ValueKind != JsonValueKind.Object)
        {
            return new DeclarationPosition(null, null);
        }

        if (RecordEntries.Str(audit, RecordFields.ExemptBecause) is { Length: > 0 } rationale)
        {
            return new DeclarationPosition(DeclarationsRenderer.NotAuditedCall(rationale), null);
        }

        var level = RecordEntries.Str(audit, RecordFields.Level);

        if (level is null)
        {
            return new DeclarationPosition(null, null);
        }

        if (level == "none")
        {
            return new DeclarationPosition(null, "audit level 'none' states no exemption rationale");
        }

        if (level is not ("required" or "optional"))
        {
            return new DeclarationPosition(null, null);
        }

        var category = RecordEntries.Str(audit, RecordFields.Category);
        var targetKind = RecordEntries.Str(audit, RecordFields.TargetKind);

        if (RecordEntries.Str(audit, RecordFields.Target) is not { Length: > 0 } target)
        {
            return new DeclarationPosition(DeclarationsRenderer.AuditedCall(record.Id, category, targetKind), null);
        }

        var (property, isText) = TargetPropertyOf(record, target);

        return new DeclarationPosition(
            DeclarationsRenderer.AuditedWithTargetCall(record.Id, category, targetKind, messageType, parameter, property, isText),
            null);
    }

    /// <summary>The idempotency call one use case record states, or the reason it renders nothing.</summary>
    /// <param name="record">The use case record whose idempotency block is read.</param>
    /// <param name="messageType">The simple message type name the key selector reads.</param>
    /// <returns>The idempotency position: its call, its skip reason, or neither.</returns>
    private static DeclarationPosition IdempotencyCallOf(Record record, string messageType)
    {
        if (record.Field(RecordFields.Kind) != "command")
        {
            return new DeclarationPosition(null, null);
        }

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object)
        {
            return new DeclarationPosition(null, null);
        }

        var mode = RecordEntries.Str(idempotency, RecordFields.Mode);

        if (mode is null)
        {
            return new DeclarationPosition(null, null);
        }

        if (mode == "none")
        {
            if (RecordEntries.Str(idempotency, RecordFields.Reason) is not { Length: > 0 } reason)
            {
                return new DeclarationPosition(null, "idempotency mode 'none' states no reason");
            }

            return new DeclarationPosition(DeclarationsRenderer.IdempotencyExemptCall(reason), null);
        }

        if (mode == "state")
        {
            var rationale = RecordEntries.Str(idempotency, RecordFields.RecognizedBy) is { Length: > 0 } recognized
                ? $"recognized by state: {recognized}"
                : RecordEntries.Str(idempotency, RecordFields.Reason);

            if (rationale is not { Length: > 0 })
            {
                return new DeclarationPosition(null, "idempotency mode 'state' states no recognized repeat");
            }

            return new DeclarationPosition(DeclarationsRenderer.IdempotencyExemptCall(rationale), null);
        }

        if (mode != "key")
        {
            return new DeclarationPosition(null, null);
        }

        var scope = RecordEntries.Str(idempotency, RecordFields.KeyScope) ?? "";
        var replay = RecordEntries.Str(idempotency, RecordFields.Replay) == "returns-first-result";

        return new DeclarationPosition(
            DeclarationsRenderer.KeyedIdempotencyCall(messageType, "command", KeyPartsOf(record, RecordEntries.Str(idempotency, RecordFields.Key) ?? ""), scope, replay),
            null);
    }

    /// <summary>The policy name a permission grant reads: the target form with the permission id and name resolved.</summary>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="permission">The full permission id, such as permission/request-expense.</param>
    /// <returns>The resolved policy name.</returns>
    private static string PermissionPolicyOf(GenerateTarget target, string permission) =>
        (target.PermissionPolicy.Length > 0 ? target.PermissionPolicy : DefaultPermissionPolicy)
            .Replace("{permission}", permission, StringComparison.Ordinal)
            .Replace("{name}", permission["permission/".Length..], StringComparison.Ordinal);

    /// <summary>The policy name a self position reads: the target form with the wrapped policy resolved.</summary>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="policy">The wrapped policy name.</param>
    /// <returns>The resolved policy name.</returns>
    private static string SelfPolicyOf(GenerateTarget target, string policy) =>
        (target.SelfPolicy.Length > 0 ? target.SelfPolicy : DefaultSelfPolicy)
            .Replace("{policy}", policy, StringComparison.Ordinal);

    /// <summary>The policy name a recent sign-in reads: the target form with the wrapped policy and the window resolved.</summary>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="policy">The wrapped policy name.</param>
    /// <param name="window">The sign-in window as the record states it: a duration or a setting id.</param>
    /// <returns>The resolved policy name.</returns>
    private static string RecentSignInPolicyOf(GenerateTarget target, string policy, string window) =>
        (target.RecentSignInPolicy.Length > 0 ? target.RecentSignInPolicy : DefaultRecentSignInPolicy)
            .Replace("{policy}", policy, StringComparison.Ordinal)
            .Replace("{window}", window, StringComparison.Ordinal);

    /// <summary>The policy name a system run reads: the target form with the wrapped policy resolved.</summary>
    /// <param name="target">The validated target holding the policy forms.</param>
    /// <param name="policy">The wrapped policy name.</param>
    /// <returns>The resolved policy name.</returns>
    private static string RunsAsPolicyOf(GenerateTarget target, string policy) =>
        (target.RunsAsPolicy.Length > 0 ? target.RunsAsPolicy : DefaultRunsAsPolicy)
            .Replace("{policy}", policy, StringComparison.Ordinal);

    /// <summary>The permission id a self grant wraps, or null when the grant is no self grant.</summary>
    /// <param name="grant">The grant string.</param>
    /// <returns>The wrapped permission id, or null.</returns>
    private static string? SelfPermissionOf(string grant)
    {
        const string prefix = "self(permission/";

        if (!grant.StartsWith(prefix, StringComparison.Ordinal) || !grant.EndsWith(')'))
        {
            return null;
        }

        var name = grant[prefix.Length..^1];

        return name.Length > 0 ? "permission/" + name : null;
    }

    /// <summary>The first authorization condition the access block states, or null when it states none.</summary>
    /// <param name="access">The access object of the use case.</param>
    /// <returns>The first condition id, or null.</returns>
    private static string? FirstCondition(JsonElement access)
    {
        foreach (var condition in RecordEntries.LinkStrings(access, RecordFields.Conditions))
        {
            if (condition.Length > 0)
            {
                return condition;
            }
        }

        return null;
    }

    /// <summary>The message property an audit target reads: the PascalCase input name with the identity suffix the message check expects.</summary>
    /// <param name="record">The use case record whose inputs are read.</param>
    /// <param name="target">The input name the audit block states.</param>
    /// <returns>The property name with whether it already reads as text.</returns>
    private static (string Property, bool IsText) TargetPropertyOf(Record record, string target)
    {
        foreach (var input in record.Objects(RecordFields.Input))
        {
            if (RecordEntries.Str(input, RecordFields.Name) == target)
            {
                var type = RecordEntries.Str(input, RecordFields.Type);

                return (PropertyOf(target, type), type == "text");
            }
        }

        return (Naming.Pascal(target), false);
    }

    /// <summary>The key template parts in template order: literal text with the message property each placeholder names.</summary>
    /// <param name="record">The use case record whose inputs are read.</param>
    /// <param name="template">The key template as the record states it.</param>
    /// <returns>The template parts.</returns>
    private static List<DeclarationsRenderer.KeyPart> KeyPartsOf(Record record, string template)
    {
        var parts = new List<DeclarationsRenderer.KeyPart>();
        var inputs = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var input in record.Objects(RecordFields.Input))
        {
            if (RecordEntries.Str(input, RecordFields.Name) is { Length: > 0 } name)
            {
                inputs.TryAdd(name, RecordEntries.Str(input, RecordFields.Type));
            }
        }

        var written = 0;

        foreach (Match match in KeyPlaceholder.Matches(template))
        {
            if (match.Index > written)
            {
                parts.Add(new DeclarationsRenderer.KeyPart(template[written..match.Index], null));
            }

            var name = match.Groups["name"].Value.Trim();

            inputs.TryGetValue(name, out var type);
            parts.Add(new DeclarationsRenderer.KeyPart(null, PropertyOf(name, type)));
            written = match.Index + match.Length;
        }

        if (written < template.Length)
        {
            parts.Add(new DeclarationsRenderer.KeyPart(template[written..], null));
        }

        return parts;
    }

    /// <summary>The message property an input reads: its PascalCase name, with the identity suffix for an id the name does not already carry.</summary>
    /// <param name="name">The input name.</param>
    /// <param name="type">The input type, or null when the record states none.</param>
    /// <returns>The property name the message carries.</returns>
    private static string PropertyOf(string name, string? type)
    {
        var property = Naming.Pascal(name);

        return type == "id" && !property.EndsWith("Id", StringComparison.Ordinal) ? property + "Id" : property;
    }

    /// <summary>The namespace declaring a use case message: the module namespace with the Application folder the record tree mirrors.</summary>
    /// <param name="resolved">The target namespace with the module resolved.</param>
    /// <param name="record">The use case record whose file names the aggregate folder.</param>
    /// <param name="messageName">The use case name in PascalCase, without its message suffix.</param>
    /// <returns>The message namespace.</returns>
    private static string MessageNamespaceOf(string resolved, Record record, string messageName)
    {
        var segments = record.File.Replace('\\', '/').Split('/');

        for (var index = 0; index + 1 < segments.Length; index++)
        {
            if (segments[index] == "aggregates"
                && segments.Skip(index + 1).Contains("use-cases", StringComparer.Ordinal))
            {
                return $"{resolved}.Application.{Naming.Pascal(segments[index + 1])}.{messageName}";
            }
        }

        return $"{resolved}.Application.{messageName}";
    }

    /// <summary>Renders one C# file per command and query carrying constrained inputs, with the validator its record constraints determine.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <param name="target">The validated target.</param>
    /// <returns>The rendered files in module then use case order.</returns>
    private static List<GeneratedFile> RenderValidators(ProjectModel model, GenerateTarget target)
    {
        var files = new List<GeneratedFile>();
        var usings = target.Usings ?? [];

        foreach (var record in model.Records
            .Where(candidate => candidate.Type == RecordTypes.UseCase && !candidate.Derived && candidate.Module is not null)
            .OrderBy(candidate => candidate.Module, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            var kind = record.Field(RecordFields.Kind);

            if (kind is not ("command" or "query"))
            {
                continue;
            }

            if (!Identifiers.TrySplit(record.Id, out _, out _, out var useCaseName) || useCaseName.Length == 0)
            {
                continue;
            }

            var (rules, usesGuid, usesRegex) = ValidatorRulesOf(model, record);

            if (rules.Count == 0)
            {
                continue;
            }

            var messageName = Naming.Pascal(useCaseName);
            var messageType = messageName + (kind == "query" ? "Query" : "Command");
            var folder = UseCaseFolderOf(model.Config.Root, target, record, messageName);
            var @namespace = MessageNamespaceOf(GenerateNaming.ResolveModule(target.Namespace, record.Module!), record, messageName);

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{messageType}Validator.generated.cs"),
                ValidatorsRenderer.RenderDotnet(new ValidatorsRenderer.ValidatorData(
                    useCaseName,
                    kind,
                    messageType,
                    @namespace,
                    usings,
                    rules,
                    usesGuid,
                    usesRegex))));
        }

        return files;
    }

    /// <summary>The rules one use case record states: one presence rule per required field the type admits missing, with one rule per constraint Domain refuses nothing for.</summary>
    /// <param name="model">The built project model resolving the rules each constraint cites.</param>
    /// <param name="record">The use case record whose inputs are read.</param>
    /// <returns>The rules in written input order, with whether any compares against Guid.Empty or matches a regular expression.</returns>
    private static (List<ValidatorsRenderer.ValidationRule> Rules, bool UsesGuid, bool UsesRegex) ValidatorRulesOf(
        ProjectModel model, Record record)
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();
        var usesGuid = false;
        var usesRegex = false;
        var parameter = record.Field(RecordFields.Kind) == "query" ? "query" : "command";

        foreach (var input in record.Objects(RecordFields.Input))
        {
            if (input.ValueKind != JsonValueKind.Object
                || RecordEntries.Str(input, RecordFields.Name) is not { Length: > 0 } name
                || RecordEntries.Str(input, RecordFields.Type) is not { } type)
            {
                continue;
            }

            var (guid, regex) = ValidatorsRenderer.AddRulesFor(
                rules, PropertyOf(name, type), parameter, CitesRefusalRule(model, input), input);

            usesGuid = usesGuid || guid;
            usesRegex = usesRegex || regex;
        }

        return (rules, usesGuid, usesRegex);
    }

    /// <summary>Whether an input cites a rule stating a refusal, so Domain refuses its constraints through the written result.</summary>
    /// <param name="model">The built project model resolving the cited rules.</param>
    /// <param name="input">The input field as the record states it.</param>
    /// <returns>True when any cited rule states a refusal; otherwise false.</returns>
    private static bool CitesRefusalRule(ProjectModel model, JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object
            || !input.TryGetProperty(RecordFields.Rules, out var cited)
            || cited.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var rule in cited.EnumerateArray())
        {
            if (rule.ValueKind == JsonValueKind.String
                && rule.GetString() is { Length: > 0 } id
                && model.ById.TryGetValue(id, out var record)
                && record.Field(RecordFields.Refusal) is { Length: > 0 })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The folder one use case writes its validator into: the resolved path with the Application folder the record tree mirrors.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="record">The use case record whose file names the aggregate folder.</param>
    /// <param name="messageName">The use case name in PascalCase, without its message suffix.</param>
    /// <returns>The absolute use case folder.</returns>
    private static string UseCaseFolderOf(string root, GenerateTarget target, Record record, string messageName)
    {
        var folder = Path.Combine(
            Combine(root, GenerateNaming.ResolveModule(target.Path, record.Module!)),
            "Application");

        if (AggregateFolderOf(record) is { } aggregate)
        {
            folder = Path.Combine(folder, GenerateNaming.ModulePascal(aggregate));
        }

        return Path.Combine(folder, messageName);
    }

    /// <summary>Renders one C# file per module holding HTTP endpoints, with one nested endpoint class per use case.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="skips">The skipped use cases, appended in place.</param>
    /// <returns>The rendered files in module order.</returns>
    private static List<GeneratedFile> RenderEndpoints(ProjectModel model, GenerateTarget target, List<SkippedMessage> skips)
    {
        var files = new List<GeneratedFile>();
        var uses = model.Records
            .Where(record => record.Type == RecordTypes.UseCase && !record.Derived && record.Module is not null)
            .Select(record => (record.Module!, record));

        foreach (var (module, entries) in ByModule(uses))
        {
            var endpoints = new List<EndpointsRenderer.EndpointData>();
            var resolved = GenerateNaming.ResolveModule(target.Namespace, module);

            foreach (var record in entries.OrderBy(entry => entry.Id, StringComparer.Ordinal))
            {
                var endpoint = EndpointOf(model, record, resolved, model.Config.Marks, out var skip);

                if (skip is not null)
                {
                    skips.Add(new SkippedMessage(record.Id, skip));
                }
                else if (endpoint is not null)
                {
                    endpoints.Add(endpoint);
                }
            }

            if (endpoints.Count == 0)
            {
                continue;
            }

            var folder = DotnetFolder(model.Config.Root, target, module);
            var name = GenerateNaming.ModulePascal(module);
            var usings = target.Usings ?? [];

            files.Add(new GeneratedFile(
                Path.Combine(folder, $"{name}Endpoints.generated.cs"),
                EndpointsRenderer.RenderDotnet(module, endpoints, resolved + ".WebApi", target.ClaimReader, usings)));
        }

        return files;
    }

    /// <summary>Reads one use case record as the endpoint its HTTP surface renders, or the reason it renders nothing.</summary>
    /// <param name="model">The built project model holding the results and refusal sets.</param>
    /// <param name="record">The use case record.</param>
    /// <param name="resolvedNamespace">The target namespace with the module resolved.</param>
    /// <param name="marks">The mark style the project declares.</param>
    /// <param name="skip">The skip reason when the record states something the vocabulary cannot express.</param>
    /// <returns>The endpoint, or null when the record renders nothing.</returns>
    private static EndpointsRenderer.EndpointData? EndpointOf(
        ProjectModel model,
        Record record,
        string resolvedNamespace,
        MarkStyle marks,
        out string? skip)
    {
        skip = null;

        var kind = record.Field(RecordFields.Kind);

        if (kind is not ("command" or "query"))
        {
            return null;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out _, out var useCaseName) || useCaseName.Length == 0)
        {
            return null;
        }

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Http, out var http)
            || http.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (http.TryGetProperty(RecordFields.Generated, out var generated)
            && generated.ValueKind == JsonValueKind.False)
        {
            return null;
        }

        var method = RecordEntries.Str(http, RecordFields.Method) ?? "";
        var route = RecordEntries.Str(http, RecordFields.Route) ?? "";

        if (method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE"))
        {
            skip = $"http method '{method}' is no GET, POST, PUT, PATCH, or DELETE";
            return null;
        }

        if (route.Length == 0)
        {
            return null;
        }

        if (BoundInputsOf(record, out skip) is not { } inputs || skip is not null)
        {
            return null;
        }

        var created = kind == "command" ? RecordEntries.Str(http, RecordFields.Created) : null;
        var projects = kind == "query" || created is { Length: > 0 };

        if (projects && !record.Raw.TryGetProperty(RecordFields.Output, out _))
        {
            skip = created is { Length: > 0 }
                ? "states a created location with no output, which endpoints do not shape"
                : "states no output, which endpoints do not shape";
            return null;
        }

        var messageName = Naming.Pascal(useCaseName);
        var messageType = messageName + (kind == "query" ? "Query" : "Command");

        return new EndpointsRenderer.EndpointData(
            record.Id,
            messageName,
            record.Field(RecordFields.Title) ?? messageName,
            record.Field(RecordFields.Summary),
            kind,
            method,
            route,
            created,
            inputs,
            LocationPropertiesOf(record, created),
            projects,
            RefusalStatusesOf(model, record),
            messageType,
            MessageNamespaceOf(resolvedNamespace, record, messageName),
            messageType + "Result",
            messageName + "ResponseModel",
            ResponseNamespaceOf(resolvedNamespace, record, messageName),
            marks);
    }

    /// <summary>Reads the bound inputs of a use case in written order, or the reason the endpoint renders nothing.</summary>
    /// <param name="record">The use case record whose inputs are read.</param>
    /// <param name="skip">The skip reason when an input states something the vocabulary cannot express.</param>
    /// <returns>The bound inputs, or null when the endpoint renders nothing.</returns>
    private static List<EndpointsRenderer.BoundInput>? BoundInputsOf(Record record, out string? skip)
    {
        skip = null;

        var inputs = new List<EndpointsRenderer.BoundInput>();
        var siblings = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in record.Objects(RecordFields.Input))
        {
            if (RecordEntries.Str(input, RecordFields.Name) is { Length: > 0 } name)
            {
                siblings.Add(name);
            }
        }

        foreach (var input in record.Objects(RecordFields.Input))
        {
            var name = RecordEntries.Str(input, RecordFields.Name) ?? "";
            var type = RecordEntries.Str(input, RecordFields.Type) ?? "";
            var binding = RecordEntries.Str(input, RecordFields.Binding) ?? "";
            var origin = RecordEntries.Str(input, RecordFields.Origin) ?? "";
            var required = !input.TryGetProperty(RecordFields.Required, out var flag) || flag.ValueKind != JsonValueKind.False;

            if (name.Length == 0)
            {
                continue;
            }

            if (type is "file" or "list")
            {
                skip = $"input '{name}' carries a {type}, which endpoints do not bind";
                return null;
            }

            if (input.TryGetProperty(RecordFields.Many, out var many) && many.ValueKind == JsonValueKind.True)
            {
                skip = $"input '{name}' holds many values, which endpoints do not bind";
                return null;
            }

            if (type == "choice" && SetTypeOf(input) is not { Length: > 0 })
            {
                skip = $"input '{name}' names no closed set, which endpoints do not bind";
                return null;
            }

            if (binding == "claim" && origin != "derived" && type != "id")
            {
                skip = $"input '{name}' binds from the claim with type '{type}', which endpoints do not read";
                return null;
            }

            if (type == "money")
            {
                if (RecordEntries.Str(input, RecordFields.CurrencyHome) is { Length: > 0 } home)
                {
                    skip = $"input '{name}' takes its currency from '{home}', which endpoints do not read";
                    return null;
                }

                if (RecordEntries.Str(input, RecordFields.CurrencyField) is { Length: > 0 } carrier && !siblings.Contains(carrier))
                {
                    skip = $"input '{name}' takes its currency from '{carrier}', which no input of the use case carries";
                    return null;
                }
            }

            var wire = WireNameOf(name, type);
            var sanitized = GenerateNaming.SanitizeConstName(wire);

            inputs.Add(new EndpointsRenderer.BoundInput(
                name,
                PropertyOf(name, type),
                char.ToLowerInvariant(sanitized[0]) + sanitized[1..],
                wire,
                type,
                origin == "derived" ? "derived" : binding,
                required,
                RecordEntries.Str(input, RecordFields.Meaning),
                type == "choice" ? SetTypeOf(input) : null,
                type == "id" ? IdentifierTypeOf(RecordEntries.Str(input, RecordFields.Identifies)) : null,
                binding == "claim" ? ActorOf(input, name) : null,
                origin));
        }

        return inputs;
    }

    /// <summary>The route, query or header name an input travels under: its name, with the identity suffix for an id the name does not already carry.</summary>
    /// <param name="name">The input name.</param>
    /// <param name="type">The input type, or null when the record states none.</param>
    /// <returns>The wire name.</returns>
    private static string WireNameOf(string name, string? type) =>
        type == "id" && !name.EndsWith("Id", StringComparison.Ordinal) ? name + "Id" : name;

    /// <summary>The typed identifier an id input binds as: the aggregate or part identifier, or null for a Guid. The event generators read the same rule from their fields.</summary>
    /// <param name="identifies">The identifies reference, or null when the record states none.</param>
    /// <returns>The identifier type, or null for a Guid.</returns>
    private static string? IdentifierTypeOf(string? identifies)
    {
        if (identifies is not { Length: > 0 } || !identifies.StartsWith("aggregate/", StringComparison.Ordinal))
        {
            return null;
        }

        var rest = identifies["aggregate/".Length..];
        var hash = rest.IndexOf('#');

        if (hash >= 0)
        {
            var part = rest[(hash + 1)..];

            return part.Length > 0 ? GenerateNaming.ConstName(part) + "Id" : null;
        }

        var dot = rest.IndexOf('.');

        if (dot < 0 || dot == rest.Length - 1)
        {
            return null;
        }

        return GenerateNaming.ConstName(rest[(dot + 1)..]) + "Id";
    }

    /// <summary>The closed-set type a choice input converts through, or null when the input names no set.</summary>
    /// <param name="input">The choice input holding the set reference.</param>
    /// <returns>The set base name the closed-sets generator writes, or null when the input names no set.</returns>
    private static string? SetTypeOf(JsonElement input)
    {
        if (RecordEntries.Str(input, RecordFields.Set) is { Length: > 0 } set
            && Identifiers.TrySplit(set, out _, out _, out var name)
            && name.Length > 0)
        {
            return GenerateNaming.ConstName(name);
        }

        return null;
    }

    /// <summary>The actor a claim input reads: the identifies target name, or the input name when the record states none.</summary>
    /// <param name="input">The claim input holding the identifies reference.</param>
    /// <param name="name">The input name.</param>
    /// <returns>The actor name the claim reader takes.</returns>
    private static string ActorOf(JsonElement input, string name)
    {
        if (RecordEntries.Str(input, RecordFields.Identifies) is not { Length: > 0 } identifies)
        {
            return name;
        }

        var cut = Math.Max(identifies.LastIndexOf('/'), identifies.LastIndexOf('.'));

        return cut >= 0 && cut + 1 < identifies.Length ? identifies[(cut + 1)..] : identifies;
    }

    /// <summary>The namespace declaring a use case response model: the module WebApi namespace with the aggregate folder the record tree mirrors.</summary>
    /// <param name="resolved">The target namespace with the module resolved.</param>
    /// <param name="record">The use case record whose file names the aggregate folder.</param>
    /// <param name="messageName">The use case name in PascalCase, without its message suffix.</param>
    /// <returns>The response model namespace.</returns>
    private static string ResponseNamespaceOf(string resolved, Record record, string messageName)
    {
        var segments = record.File.Replace('\\', '/').Split('/');

        for (var index = 0; index + 1 < segments.Length; index++)
        {
            if (segments[index] == "aggregates"
                && segments.Skip(index + 1).Contains("use-cases", StringComparer.Ordinal))
            {
                return $"{resolved}.WebApi.{Naming.Pascal(segments[index + 1])}.{messageName}";
            }
        }

        return $"{resolved}.WebApi.{messageName}";
    }

    /// <summary>The result property by created-location parameter, resolved against the command output fields.</summary>
    /// <param name="record">The use case record whose output is read.</param>
    /// <param name="created">The location template, or null when the record states none.</param>
    /// <returns>The result property by parameter, in template order.</returns>
    private static Dictionary<string, string> LocationPropertiesOf(Record record, string? created)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (created is not { Length: > 0 })
        {
            return properties;
        }

        var outputs = new List<(string Name, string Property, string Type)>();

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Output, out var output)
            && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && RecordEntries.Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    var type = RecordEntries.Str(entry, RecordFields.Type) ?? "";
                    outputs.Add((name, PropertyOf(name, type), type));
                }
            }
        }

        foreach (var parameter in TemplateParamsOf(created))
        {
            var field = outputs.FirstOrDefault(field => HttpSurfaceChecks.NamesInput(parameter, field.Name, field.Type));

            properties.TryAdd(parameter, field.Property ?? GenerateNaming.ConstName(parameter));
        }

        return properties;
    }

    /// <summary>The parameters a location template names, in template order.</summary>
    /// <param name="created">The location template.</param>
    /// <returns>The trimmed parameter names.</returns>
    private static List<string> TemplateParamsOf(string created)
    {
        var parameters = new List<string>();
        var pattern = new Regex(@"\{(?<name>[^{}]+)\}");

        foreach (Match match in pattern.Matches(created))
        {
            if (match.Groups["name"].Value.Trim() is { Length: > 0 } name)
            {
                parameters.Add(name);
            }
        }

        return parameters;
    }

    /// <summary>The distinct refusal statuses the endpoint answers, from the refusal catalog with the fixed derived statuses, ascending.</summary>
    /// <param name="model">The built project model holding the results and refusal sets.</param>
    /// <param name="record">The use case record whose results are read.</param>
    /// <returns>The declared statuses.</returns>
    private static List<int> RefusalStatusesOf(ProjectModel model, Record record)
    {
        var statuses = new SortedSet<int>();
        var catalog = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entry in model.RefusalSets)
        {
            if (entry.Status is { } status)
            {
                catalog.TryAdd(entry.Code, status);
            }
        }

        foreach (var result in model.Records.Where(candidate =>
            candidate.Type == RecordTypes.Result
            && string.Equals(candidate.PageId, record.Id, StringComparison.Ordinal)
            && candidate.Field(RecordFields.Outcome) == "refusal"))
        {
            if (RefusalCode.CodeOf(result.Field(RecordFields.Refusal)) is not { Length: > 0 } code)
            {
                continue;
            }

            if (catalog.TryGetValue(code, out var status))
            {
                statuses.Add(status);
                continue;
            }

            if (DerivedStatusOf(code) is { } derived)
            {
                statuses.Add(derived);
            }
        }

        return [.. statuses];
    }

    /// <summary>The fixed status a derived generic refusal answers with, or null when the code names none.</summary>
    /// <param name="code">The refusal code.</param>
    /// <returns>The fixed status, or null.</returns>
    private static int? DerivedStatusOf(string code)
    {
        var name = code.Contains('.', StringComparison.Ordinal) ? code[(code.LastIndexOf('.') + 1)..] : code;

        return name switch
        {
            "invalid-input" => 400,
            "unauthenticated" or "sign-in-too-old" => 401,
            "forbidden" => 403,
            "not-found" => 404,
            "conflict" or "rule-not-stated" => 409,
            _ => null,
        };
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

    /// <summary>The folder one knowledge variant writes into: the resolved path, or the path with a PascalCase area and variant folder.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="target">The validated target.</param>
    /// <param name="area">The knowledge area name.</param>
    /// <param name="variant">The variant name.</param>
    /// <returns>The absolute variant folder.</returns>
    private static string KnowledgeFolder(string root, GenerateTarget target, string area, string variant) =>
        GenerateNaming.HasKnowledgePlaceholder(target.Path)
            ? Combine(root, GenerateNaming.ResolveKnowledge(target.Path, area, variant))
            : Path.Combine(Combine(root, target.Path), $"{GenerateNaming.ModulePascal(area)}.{GenerateNaming.ModulePascal(variant)}");

    /// <summary>Combines a root with a forward-slash relative path into an absolute path.</summary>
    /// <param name="root">The absolute root.</param>
    /// <param name="relative">The relative path, written with forward slashes.</param>
    /// <returns>The absolute combined path.</returns>
    private static string Combine(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
}
