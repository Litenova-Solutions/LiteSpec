using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The code with no record behind one finding each: a use-case folder holding a command or
///     query message and its handler with no use case record, an event handler with no reaction
///     record, and a job host with no job record. The folders and classes it names cover their
///     per-class mark findings, so one gap reads as one finding.
/// </summary>
/// <follows>litespec/core/layers#implement-only-declared-behavior</follows>
internal sealed class UnrecordedCode
{
    /// <summary>Initializes a new instance of the <see cref="UnrecordedCode" /> class.</summary>
    /// <param name="config">The consumer configuration that maps each file to its module folder.</param>
    /// <param name="folders">The use-case folders with no use case record.</param>
    /// <param name="triggers">The event handlers and job hosts with no record.</param>
    private UnrecordedCode(ConsumerConfig config, List<UnrecordedFolder> folders, List<UnrecordedTrigger> triggers)
    {
        Config = config;
        Folders = folders;
        Triggers = triggers;
        FolderKeys = folders.Select(folder => folder.Key).ToHashSet(StringComparer.Ordinal);
        TriggerClasses = triggers.Select(trigger => (trigger.File, trigger.Class)).ToHashSet();
    }

    /// <summary>Gets the consumer configuration that maps each file to its module folder.</summary>
    private ConsumerConfig Config { get; }

    /// <summary>Gets the use-case folders with no use case record, in read order.</summary>
    internal IReadOnlyList<UnrecordedFolder> Folders { get; }

    /// <summary>Gets the event handlers and job hosts with no record, in read order.</summary>
    internal IReadOnlyList<UnrecordedTrigger> Triggers { get; }

    /// <summary>Gets the implied record ids of the unrecorded folders, one per folder.</summary>
    private HashSet<string> FolderKeys { get; }

    /// <summary>Gets the file and class of each unrecorded event handler and job host.</summary>
    private HashSet<(string File, string Class)> TriggerClasses { get; }

    /// <summary>
    ///     Finds every unrecorded folder, event handler and job host: an Application use-case
    ///     folder holding a handler with a message counts when its implied use case names no
    ///     written record and no mark in the folder names a written use case; an event handler
    ///     or job host the reader recognized counts when no mark on it names a written record
    ///     and no written record carries its derived name. A trigger pointing at a written
    ///     record stays per-class: the mark checks judge whether the named type fits, and a
    ///     read-model projection keeps its read-model mark untouched.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record, handler, reaction and job.</param>
    /// <returns>The unrecorded folders and triggers.</returns>
    internal static UnrecordedCode Find(ProjectModel model)
    {
        var folders = FindFolders(model);
        var triggers = FindTriggers(model, model.Code.Reactions.Select(reaction => (reaction.Class, reaction.File, reaction.Line, reaction.RecordId, false)).ToList());
        triggers.AddRange(FindTriggers(model, model.Code.Jobs.Select(job => (job.Class, job.File, job.Line, job.RecordId, true)).ToList()));

        return new UnrecordedCode(model.Config, folders, triggers);
    }

    /// <summary>Whether the element sits in an unrecorded folder or is an unrecorded trigger, so its mark findings stay silent.</summary>
    /// <param name="element">The marked code element under test.</param>
    /// <returns>True when an unrecorded folder or trigger covers the element.</returns>
    internal bool Covers(MarkedElement element) =>
        TriggerClasses.Contains((element.File, element.Class))
        || (FolderKeyOf(Config, element.File) is { } key && FolderKeys.Contains(key));

    /// <summary>Whether the stray mark sits in an unrecorded folder or on an unrecorded trigger, so its mark finding stays silent.</summary>
    /// <param name="stray">The stray mark under test.</param>
    /// <returns>True when an unrecorded folder or trigger covers the stray mark.</returns>
    internal bool Covers(StrayMark stray) =>
        TriggerClasses.Contains((stray.File, stray.Class))
        || (FolderKeyOf(Config, stray.File) is { } key && FolderKeys.Contains(key));

    /// <summary>
    ///     Finds every Application use-case folder holding a handler with a message whose implied
    ///     use case names no written record and no mark in the folder names a written use case,
    ///     once per folder. A folder whose implied name already names a written use case keeps its
    ///     per-class marks: the record states the behavior, so a missing or mistyped mark is the gap.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and handler.</param>
    /// <returns>The unrecorded folders, in handler read order.</returns>
    private static List<UnrecordedFolder> FindFolders(ProjectModel model)
    {
        var folders = new List<UnrecordedFolder>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MessageType.Length == 0
                || !string.Equals(SourceRoots.LayerOf(model.Config, handler.File), "Application", StringComparison.Ordinal)
                || FolderKeyOf(model.Config, handler.File) is not { } key)
            {
                continue;
            }

            if (!seen.Add(key))
            {
                continue;
            }

            if (RecordedUseCase(model, key))
            {
                continue;
            }

            folders.Add(FolderOf(model, handler, key));
        }

        return folders;
    }

    /// <summary>Whether the implied use case names a written record, by id or by name, or a mark in its folder names a written use case.</summary>
    /// <param name="model">The loaded project model that holds every record and mark.</param>
    /// <param name="key">The implied use case id of the folder.</param>
    /// <returns>True when a written use case states the behavior or a mark in the folder names one.</returns>
    private static bool RecordedUseCase(ProjectModel model, string key)
    {
        if (model.ById.TryGetValue(key, out var record) && record.Type == RecordTypes.UseCase)
        {
            return true;
        }

        if (Identifiers.TrySplit(key, out _, out _, out var name)
            && model.RecordsByName[name].Any(candidate => candidate.Type == RecordTypes.UseCase))
        {
            return true;
        }

        foreach (var element in model.Code.Elements)
        {
            if (FolderKeyOf(model.Config, element.File) == key
                && element.Marks.Any(mark => mark.Verb == MarkVerb.Implements && NamesRecord(model, mark.RecordId, RecordTypes.UseCase)))
            {
                return true;
            }
        }

        foreach (var stray in model.Code.StrayMarks)
        {
            if (FolderKeyOf(model.Config, stray.File) == key
                && stray.Marks.Any(mark => mark.Verb == MarkVerb.Implements && NamesRecord(model, mark.RecordId, RecordTypes.UseCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The unrecorded folder of one handler: its display folder, message type, implied record id and record path.</summary>
    /// <param name="model">The loaded project model that holds the behavior docs root.</param>
    /// <param name="handler">The first handler read in the folder.</param>
    /// <param name="key">The implied use case id of the folder.</param>
    /// <returns>The unrecorded folder behind the finding.</returns>
    private static UnrecordedFolder FolderOf(ProjectModel model, CodeHandler handler, string key)
    {
        var segments = SourceRoots.ModulePath(model.Config, handler.File)!;
        var folder = "Application/" + string.Join("/", segments[1..^1]);
        var aggregates = segments.Length >= 4 ? Naming.Kebab(segments[1]) : null;
        var path = RecordPathOf(model, key, aggregates);

        return new UnrecordedFolder(
            key,
            folder,
            handler.MessageType,
            key,
            path,
            handler.File,
            handler.Line,
            $"Use-case folder '{folder}' holds message '{handler.MessageType}' with no use case record; write the record at '{path}', or delete the code.");
    }

    /// <summary>The record path the implied use case would have: its module file, under its aggregates folder when the code folder names one.</summary>
    /// <param name="model">The loaded project model that holds the behavior docs root.</param>
    /// <param name="key">The implied use case id of the folder.</param>
    /// <param name="aggregates">The aggregates folder, or null for a module-level use case.</param>
    /// <returns>The record path under the behavior docs root, with forward slashes.</returns>
    private static string RecordPathOf(ProjectModel model, string key, string? aggregates)
    {
        Identifiers.TrySplit(key, out _, out var home, out var name);

        var middle = aggregates is not null ? $"aggregates/{aggregates}/" : "";

        return $"{BehaviorPrefix(model)}/modules/{home}/{middle}use-cases/{name}/{name}.yaml";
    }

    /// <summary>
    ///     Finds every recognized event handler and job host with no record: one whose marks name
    ///     no written record and whose derived name names none either. The derived name comes from
    ///     the mark when one names a reaction or job id, else from the class stem. A class under a
    ///     ReadModels folder is a read-model projection, not a reaction, so it never counts.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and mark.</param>
    /// <param name="rows">The recognized reactions or jobs: class, file, line, record id and whether it is a job.</param>
    /// <returns>The unrecorded triggers, in read order.</returns>
    private static List<UnrecordedTrigger> FindTriggers(ProjectModel model, List<(string Class, string File, int Line, string? RecordId, bool IsJob)> rows)
    {
        var triggers = new List<UnrecordedTrigger>();

        foreach (var (cls, file, line, recordId, isJob) in rows)
        {
            var kind = isJob ? CodeKind.Job : CodeKind.EventHandler;
            var type = isJob ? RecordTypes.Job : RecordTypes.Reaction;
            var element = model.Code.Elements.FirstOrDefault(candidate =>
                candidate.Kind == kind
                && string.Equals(candidate.File, file, StringComparison.Ordinal)
                && string.Equals(candidate.Class, cls, StringComparison.Ordinal));

            if (element?.InTests == true)
            {
                continue;
            }

            if (!isJob && IsInReadModelsFolder(file))
            {
                continue;
            }

            if (recordId is { Length: > 0 } && model.ById.ContainsKey(recordId))
            {
                continue;
            }

            if (element?.Marks.Any(mark => mark.Verb == MarkVerb.Implements && model.ById.ContainsKey(mark.RecordId)) == true)
            {
                continue;
            }

            var (module, name) = TargetOf(model, element, cls, file, type, isJob);

            if (model.RecordsByName[name].Any(candidate => candidate.Type == type))
            {
                continue;
            }

            triggers.Add(TriggerOf(model, element, cls, file, line, type, isJob, module, name));
        }

        return triggers;
    }

    /// <summary>The module and name the record would carry: from the mark when one names the id, else from the file's module and the class stem.</summary>
    /// <param name="model">The loaded project model that maps the file to its module folder.</param>
    /// <param name="element">The trigger element, or null when the read holds no element for the row.</param>
    /// <param name="cls">The trigger class name.</param>
    /// <param name="file">The trigger file.</param>
    /// <param name="type">The record type: reaction or job.</param>
    /// <param name="isJob">Whether the trigger is a job host rather than an event handler.</param>
    /// <returns>The module, or null outside the module layout, and the record name.</returns>
    private static (string? Module, string Name) TargetOf(ProjectModel model, MarkedElement? element, string cls, string file, string type, bool isJob)
    {
        if (element is not null)
        {
            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Implements
                    && mark.RecordId.StartsWith(type + "/", StringComparison.Ordinal)
                    && Identifiers.TrySplit(mark.RecordId, out _, out var home, out var marked)
                    && home is { Length: > 0 }
                    && marked.Length > 0)
                {
                    return (home, marked);
                }
            }
        }

        var stem = isJob ? JobStem(cls) : ReactionStem(cls);
        var module = SourceRoots.ModulePath(model.Config, file) is { } segments ? Naming.Kebab(segments[0]) : null;

        return (module, Naming.Kebab(stem));
    }

    /// <summary>The unrecorded trigger behind the finding: its class, derived record id and record path when the file sits in the module layout.</summary>
    /// <param name="model">The loaded project model that holds the behavior docs root.</param>
    /// <param name="element">The trigger element, or null when the read holds no element for the row.</param>
    /// <param name="cls">The trigger class name.</param>
    /// <param name="file">The trigger file.</param>
    /// <param name="line">The trigger line.</param>
    /// <param name="type">The record type: reaction or job.</param>
    /// <param name="isJob">Whether the trigger is a job host rather than an event handler.</param>
    /// <param name="module">The module, or null outside the module layout.</param>
    /// <param name="name">The record name.</param>
    /// <returns>The unrecorded trigger behind the finding.</returns>
    private static UnrecordedTrigger TriggerOf(ProjectModel model, MarkedElement? element, string cls, string file, int line, string type, bool isJob, string? module, string name)
    {
        var code = isJob ? "job-unrecorded" : "reaction-unrecorded";
        var label = isJob ? "Job host" : "Event handler";
        var lineOf = element?.Line ?? line;
        var fileOf = element?.File ?? file;

        if (module is null)
        {
            return new UnrecordedTrigger(
                code, fileOf, cls, lineOf, null,
                $"{label} '{cls}' has no {type} record; write the {type} record, or delete the {(isJob ? "host" : "handler")}.");
        }

        var id = $"{type}/{module}.{name}";
        var folder = isJob ? "jobs" : "reactions";
        var path = $"{BehaviorPrefix(model)}/modules/{module}/{folder}/{name}/{name}.yaml";

        return new UnrecordedTrigger(
            code, fileOf, cls, lineOf, id,
            $"{label} '{cls}' has no {type} record; write the {type} at '{path}', or delete the {(isJob ? "host" : "handler")}.");
    }

    /// <summary>The reaction name the class states: without its Handler suffix.</summary>
    /// <param name="cls">The event handler class name.</param>
    /// <returns>The stem without the Handler suffix, or the class name when it carries none.</returns>
    private static string ReactionStem(string cls) =>
        cls.EndsWith("Handler", StringComparison.Ordinal) && cls.Length > "Handler".Length
            ? cls[..^"Handler".Length]
            : cls;

    /// <summary>The job name the class states: without its Job or Worker suffix.</summary>
    /// <param name="cls">The job host class name.</param>
    /// <returns>The stem without the suffix, or the class name when it carries none.</returns>
    private static string JobStem(string cls)
    {
        if (cls.EndsWith("Job", StringComparison.Ordinal) && cls.Length > "Job".Length)
        {
            return cls[..^"Job".Length];
        }

        if (cls.EndsWith("Worker", StringComparison.Ordinal) && cls.Length > "Worker".Length)
        {
            return cls[..^"Worker".Length];
        }

        return cls;
    }

    /// <summary>Whether the id names a written record of the type.</summary>
    /// <param name="model">The loaded project model that resolves the id.</param>
    /// <param name="id">The record id from the mark.</param>
    /// <param name="type">The record type the id must carry.</param>
    /// <returns>True when a written record of the type carries the id.</returns>
    private static bool NamesRecord(ProjectModel model, string id, string type) =>
        model.ById.TryGetValue(id, out var record) && record.Type == type;

    /// <summary>
    ///     Whether the file sits under a module ReadModels folder: a ReadModels folder below
    ///     another folder holds a read model's code, so an event-handler-shaped class there is a
    ///     projection. The module layout drops layer folders, so the raw path is read.
    /// </summary>
    /// <param name="file">The full path of the file under test.</param>
    /// <returns>True when the file sits under a ReadModels folder.</returns>
    private static bool IsInReadModelsFolder(string file)
    {
        var folders = file.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        return folders.Length > 2
            && folders[1..^1].Any(folder => string.Equals(folder, "ReadModels", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The implied use case id of the file's folder, or null outside a use-case folder.</summary>
    /// <param name="config">The consumer configuration that maps the file to its module folder.</param>
    /// <param name="file">The file whose folder key is wanted.</param>
    /// <returns>The implied use case id, or null when the file sits outside a use-case folder.</returns>
    private static string? FolderKeyOf(ConsumerConfig config, string file)
    {
        if (SourceRoots.ModulePath(config, file) is not { } segments || segments.Length < 3)
        {
            return null;
        }

        var useCase = segments.Length >= 4 ? segments[2] : segments[1];

        return $"use-case/{Naming.Kebab(segments[0])}.{Naming.Kebab(useCase)}";
    }

    /// <summary>The behavior docs root under the consumer root, with forward slashes.</summary>
    /// <param name="model">The loaded project model that holds the behavior docs root.</param>
    /// <returns>The behavior docs prefix, or the default when the project names none.</returns>
    private static string BehaviorPrefix(ProjectModel model)
    {
        if (model.Config.BehaviorDocs.Length > 0)
        {
            var relative = Path.GetRelativePath(model.Config.Root, model.Config.BehaviorDocs);

            if (relative.Length > 0
                && !relative.StartsWith("..", StringComparison.Ordinal)
                && !Path.IsPathRooted(relative)
                && relative != ".")
            {
                return relative.Replace('\\', '/').TrimEnd('/');
            }
        }

        return ".litespec/behavior";
    }

    /// <summary>One use-case folder with no use case record: its key, display folder, message type, implied record id and path, and the finding site.</summary>
    /// <param name="Key">The implied use case id of the folder.</param>
    /// <param name="Folder">The display folder under its layer.</param>
    /// <param name="MessageType">The command or query message type the handler takes.</param>
    /// <param name="RecordId">The implied use case id.</param>
    /// <param name="RecordPath">The record path the use case would have.</param>
    /// <param name="File">The handler file the finding points at.</param>
    /// <param name="Line">The handler line the finding points at.</param>
    /// <param name="Message">The finding message.</param>
    internal sealed record UnrecordedFolder(
        string Key,
        string Folder,
        string MessageType,
        string RecordId,
        string RecordPath,
        string File,
        int Line,
        string Message);

    /// <summary>One event handler or job host with no record: its code, class, derived record id and the finding site.</summary>
    /// <param name="Code">The finding code: reaction-unrecorded or job-unrecorded.</param>
    /// <param name="File">The trigger file the finding points at.</param>
    /// <param name="Class">The trigger class name.</param>
    /// <param name="Line">The trigger line the finding points at.</param>
    /// <param name="Subject">The derived record id, or null outside the module layout.</param>
    /// <param name="Message">The finding message.</param>
    internal sealed record UnrecordedTrigger(
        string Code,
        string File,
        string Class,
        int Line,
        string? Subject,
        string Message);
}
