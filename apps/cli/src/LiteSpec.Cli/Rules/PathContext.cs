using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     What a rules target names: a file or folder path with its stack, layer,
///     artifact and marked records, or a record identifier with its layer and artifact.
/// </summary>
/// <param name="Display">The target as printed: the root-relative path or the record identifier.</param>
/// <param name="IsRecord">Whether the target names a record rather than a path.</param>
/// <param name="Root">The consumer root.</param>
/// <param name="RelativePath">The root-relative path with forward slashes, or empty for a record.</param>
/// <param name="Stack">The owning stack id, or empty when no stack owns the target.</param>
/// <param name="Layer">The derived layer, or empty when the path names none.</param>
/// <param name="Artifact">The derived artifact, or empty when the path names none.</param>
/// <param name="Marked">The record and provision identifiers the file marks, in file order.</param>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed record PathContext(
    string Display,
    bool IsRecord,
    string Root,
    string RelativePath,
    string Stack,
    string Layer,
    string Artifact,
    IReadOnlyList<string> Marked)
{
    /// <summary>The record type to artifact map: the artifact whose provisions govern the record.</summary>
    static readonly Dictionary<string, string> RecordArtifacts = new(StringComparer.Ordinal)
    {
        ["use-case"] = "use-case",
        ["aggregate"] = "aggregate",
        ["screen"] = "screen",
        ["entry-point"] = "entry-point",
        ["job"] = "job",
        ["reaction"] = "reaction",
        ["decision"] = "decision",
        ["exception"] = "exception",
        ["goal"] = "goal",
        ["problem"] = "problem",
        ["feature"] = "feature",
        ["constraint"] = "constraint",
        ["research"] = "research",
        ["product"] = "product",
        ["journey"] = "journey",
        ["actor"] = "actor",
        ["role"] = "role",
        ["term"] = "term",
        ["module"] = "module",
        ["rule"] = "rule",
        ["rule-set"] = "rule-set",
        ["requirement"] = "requirement",
        ["result"] = "result",
        ["scenario"] = "scenario",
        ["setting"] = "setting",
        ["notification"] = "notification",
        ["runbook"] = "runbook",
        ["permission"] = "permission",
        ["permission-list"] = "permission-list",
        ["external-system"] = "external-system",
        ["integration-event"] = "integration-event",
        ["retention-schedule"] = "retention-schedule",
        ["event"] = "domain-event",
        ["migration"] = "migration",
    };

    /// <summary>An XML mark: <c>&lt;implements&gt;</c>, <c>&lt;follows&gt;</c> or <c>&lt;proves&gt;</c>.</summary>
    static readonly Regex XmlMark = new(@"<(implements|follows|proves)>([^<>]+)</\1>", RegexOptions.Compiled);

    /// <summary>A generated C# mark attribute.</summary>
    static readonly Regex AttributeMark = new(@"\[(Implements|Follows|Proves)\(\s*""([^""]+)""", RegexOptions.Compiled);

    /// <summary>A frontend <c>litespec</c> constant entry.</summary>
    static readonly Regex ConstMark = new(@"\b(implements|follows)\s*:\s*[""']([^""']+)[""']", RegexOptions.Compiled);

    /// <summary>A browser test <c>proves</c> annotation.</summary>
    static readonly Regex ProvesCall = new(@"\bproves\(\s*[""']([^""']+)[""']", RegexOptions.Compiled);

    /// <summary>A record type line in a record file.</summary>
    static readonly Regex TypeLine = new(@"^type:\s*(\S+)\s*$", RegexOptions.Compiled);

    /// <summary>Resolves the target against the consumer root.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="backendStack">The selected backend stack id, or empty when the project selects none.</param>
    /// <param name="target">The path or record identifier.</param>
    /// <returns>The derived context, or null when the target names neither a path nor a record.</returns>
    public static PathContext? Resolve(ConsumerConfig config, string backendStack, string target)
    {
        var trimmed = target.Trim().Replace('\\', '/');

        if (trimmed.Length == 0)
        {
            return null;
        }

        var candidate = Path.IsPathFullyQualified(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(config.Root, trimmed));

        if (File.Exists(candidate) || Directory.Exists(candidate))
        {
            return FromPath(config, backendStack, candidate, Directory.Exists(candidate) && !File.Exists(candidate));
        }

        return FromRecord(config, trimmed);
    }

    /// <summary>The path spellings a convention scope or departure path matches against.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The root-relative path, each record-root-relative path, and the bare file name.</returns>
    public IReadOnlyList<string> GlobCandidates(ConsumerConfig config)
    {
        if (IsRecord || RelativePath.Length == 0)
        {
            return [];
        }

        var candidates = new List<string> { RelativePath };
        var absolute = Path.Combine(config.Root, RelativePath.Replace('/', Path.DirectorySeparatorChar));

        foreach (var root in config.RecordRoots)
        {
            if (Under(absolute, root) && Path.GetRelativePath(root, absolute) is { } relative && relative != ".")
            {
                candidates.Add(relative.Replace(Path.DirectorySeparatorChar, '/'));
            }
        }

        var name = RelativePath.Contains('/') ? RelativePath[(RelativePath.LastIndexOf('/') + 1)..] : RelativePath;

        if (name.Length > 0 && !candidates.Contains(name, StringComparer.Ordinal))
        {
            candidates.Add(name);
        }

        return candidates;
    }

    /// <summary>Derives the context of a file or folder path.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="backendStack">The selected backend stack id.</param>
    /// <param name="absolute">The absolute path.</param>
    /// <param name="isFolder">Whether the path is a folder.</param>
    /// <returns>The derived context.</returns>
    static PathContext FromPath(ConsumerConfig config, string backendStack, string absolute, bool isFolder)
    {
        var relative = config.Relative(absolute);
        var name = Path.GetFileName(absolute);
        var extension = Path.GetExtension(absolute).ToLowerInvariant();
        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var layer = "";
        var artifact = "";
        var stack = "";
        var text = "";

        if (!isFolder)
        {
            try
            {
                text = File.ReadAllText(absolute);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                text = "";
            }
        }

        if (IsTest(config, absolute, relative, name, extension))
        {
            layer = "tests";
            artifact = "test";
        }
        else if (UnderAny(absolute, config.RecordRoots))
        {
            layer = "records";
            artifact = RecordFileArtifact(text, name);
        }
        else if (UnderAny(absolute, config.Alerts) || UnderAny(absolute, config.Operations) || IsOperationsFile(name))
        {
            layer = "operations";
        }
        else if (FrontendOf(config, absolute) is { } frontend)
        {
            stack = frontend.Platform;
            (layer, artifact) = FrontendScope(config, frontend, absolute, name);
        }
        else if (config.ApiClientSrc.Length > 0 && Under(absolute, config.ApiClientSrc))
        {
            layer = "frontend-data";
        }
        else if (extension is ".svelte")
        {
            layer = "frontend-ui";
            artifact = name.Contains("form", StringComparison.OrdinalIgnoreCase) ? "form" : "component";
        }
        else if (extension is ".razor")
        {
            layer = "frontend-ui";
            artifact = HasSegment(segments, "Pages") ? "route" : HasSegment(segments, "Components") ? "component" : "";
        }
        else if (extension is ".kt")
        {
            layer = "frontend-ui";
            artifact = HasSegment(segments, "Screens") ? "route" : "";
        }
        else if (extension is ".csproj" or ".props" or ".targets" or ".sln" or ".slnx")
        {
            layer = "workspace";
        }
        else if (extension is ".cs")
        {
            stack = backendStack;
            (layer, artifact) = BackendScope(segments, name, text);
        }

        var marked = isFolder ? [] : ReadMarks(text);
        return new PathContext(relative, false, config.Root, relative, stack, layer, artifact, marked);
    }

    /// <summary>Derives the context of a record identifier.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="target">The record identifier or qualified identifier.</param>
    /// <returns>The derived context, or null when the target names no record type.</returns>
    static PathContext? FromRecord(ConsumerConfig config, string target)
    {
        var segments = target.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index + 1 < segments.Length; index++)
        {
            if (!RecordArtifacts.TryGetValue(segments[index], out var artifact))
            {
                continue;
            }

            var name = string.Join("/", segments[(index + 1)..]);

            if (name.Length == 0 || name.Contains(' ') || name.Contains('\\'))
            {
                return null;
            }

            return new PathContext(target, true, config.Root, "", "", "records", artifact, []);
        }

        return null;
    }

    /// <summary>Whether the path holds tests.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="absolute">The absolute path.</param>
    /// <param name="relative">The root-relative path with forward slashes.</param>
    /// <param name="name">The file name.</param>
    /// <param name="extension">The lowercase file extension.</param>
    /// <returns>True when the path holds tests.</returns>
    static bool IsTest(ConsumerConfig config, string absolute, string relative, string name, string extension)
    {
        if (UnderAny(absolute, config.TestRoots))
        {
            return true;
        }

        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment => segment is "tests" or "test" or "__tests__"))
        {
            return true;
        }

        return name.Contains(".Tests.", StringComparison.Ordinal)
            || name.EndsWith("Test" + extension, StringComparison.Ordinal)
            || name.EndsWith("Tests" + extension, StringComparison.Ordinal)
            || (extension is ".ts" or ".tsx" or ".js" or ".svelte" && name.Contains(".spec.", StringComparison.Ordinal));
    }

    /// <summary>Whether the file is an operations file by name.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>True for a Caddyfile or Dockerfile.</returns>
    static bool IsOperationsFile(string name) =>
        string.Equals(name, "Caddyfile", StringComparison.Ordinal)
        || name.StartsWith("Dockerfile", StringComparison.Ordinal);

    /// <summary>The frontend holding the path, or null when no frontend holds it.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="absolute">The absolute path.</param>
    /// <returns>The frontend, or null.</returns>
    static FrontendConfig? FrontendOf(ConsumerConfig config, string absolute) =>
        config.Frontends.FirstOrDefault(frontend => frontend.Path.Length > 0 && Under(absolute, frontend.Path));

    /// <summary>Derives the layer and artifact of a frontend path.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="frontend">The frontend holding the path.</param>
    /// <param name="absolute">The absolute path.</param>
    /// <param name="name">The file name.</param>
    /// <returns>The layer and artifact.</returns>
    static (string Layer, string Artifact) FrontendScope(
        ConsumerConfig config, FrontendConfig frontend, string absolute, string name)
    {
        if (frontend.Platform == "svelte" && Under(absolute, frontend.RoutesRoot))
        {
            return ("frontend-ui", name == "+page.svelte" ? "route" : "");
        }

        if (frontend.Platform == "react" && Under(absolute, Path.Combine(frontend.Path, "src", "routes")))
        {
            return ("frontend-ui", "route");
        }

        if (name.EndsWith(".svelte", StringComparison.Ordinal) || name.EndsWith(".tsx", StringComparison.Ordinal))
        {
            var relative = config.Relative(absolute);

            if (relative.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == "components"))
            {
                return ("frontend-ui", name.Contains("form", StringComparison.OrdinalIgnoreCase) ? "form" : "component");
            }
        }

        return ("frontend-ui", "");
    }

    /// <summary>Derives the layer and artifact of a backend C# path.</summary>
    /// <param name="segments">The root-relative path segments.</param>
    /// <param name="name">The file name.</param>
    /// <param name="text">The file text, or empty for a folder.</param>
    /// <returns>The layer and artifact.</returns>
    static (string Layer, string Artifact) BackendScope(string[] segments, string name, string text)
    {
        if (HasSegment(segments, "Migrations") || name.EndsWith("Migration.cs", StringComparison.Ordinal) || name.EndsWith("Snapshot.cs", StringComparison.Ordinal))
        {
            return ("infrastructure", "migration");
        }

        var layer = "";

        foreach (var segment in segments)
        {
            layer = segment switch
            {
                "Domain" => "domain",
                "Application" => "application",
                "Infrastructure" => "infrastructure",
                "WebApi" => "web-api",
                "Composition" or "Contracts" or "Jobs" => "application",
                "Migrations" => "infrastructure",
                _ => "",
            };

            if (layer.Length > 0)
            {
                break;
            }
        }

        if (layer.Length == 0)
        {
            foreach (var segment in segments)
            {
                if (segment.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase))
                {
                    layer = "domain";
                    break;
                }

                if (segment.EndsWith(".Application", StringComparison.OrdinalIgnoreCase)
                    || segment.EndsWith(".Worker", StringComparison.OrdinalIgnoreCase))
                {
                    layer = "application";
                    break;
                }

                if (segment.EndsWith(".Infrastructure", StringComparison.OrdinalIgnoreCase))
                {
                    layer = "infrastructure";
                    break;
                }

                if (segment.EndsWith(".WebApi", StringComparison.OrdinalIgnoreCase))
                {
                    layer = "web-api";
                    break;
                }
            }
        }

        if (layer.Length == 0)
        {
            layer = IsWebApiName(name) ? "web-api" : "application";
        }

        return (layer, BackendArtifact(segments, name, text));
    }

    /// <summary>Whether the file name carries a WebApi role suffix.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>True for an endpoint or transport model name.</returns>
    static bool IsWebApiName(string name) =>
        name.EndsWith("Endpoint.cs", StringComparison.Ordinal)
        || name.EndsWith("RequestModel.cs", StringComparison.Ordinal)
        || name.EndsWith("ResponseModel.cs", StringComparison.Ordinal)
        || name.EndsWith("ResponseItemModel.cs", StringComparison.Ordinal)
        || name.EndsWith("PaginationModel.cs", StringComparison.Ordinal)
        || (name.EndsWith("Model.cs", StringComparison.Ordinal) && !name.EndsWith("ViewModel.cs", StringComparison.Ordinal));

    /// <summary>Derives the artifact of a backend C# file.</summary>
    /// <param name="segments">The root-relative path segments.</param>
    /// <param name="name">The file name.</param>
    /// <param name="text">The file text, or empty for a folder.</param>
    /// <returns>The artifact, or empty when the file names none.</returns>
    static string BackendArtifact(string[] segments, string name, string text)
    {
        if (name.EndsWith("CommandHandler.cs", StringComparison.Ordinal))
        {
            return "command-handler";
        }

        if (name.EndsWith("QueryHandler.cs", StringComparison.Ordinal))
        {
            return "query-handler";
        }

        if (name.EndsWith("CommandValidator.cs", StringComparison.Ordinal)
            || name.EndsWith("QueryValidator.cs", StringComparison.Ordinal)
            || name.Contains("CommandResult", StringComparison.Ordinal)
            || name.Contains("QueryResult", StringComparison.Ordinal)
            || name.EndsWith("Command.cs", StringComparison.Ordinal)
            || name.EndsWith("Query.cs", StringComparison.Ordinal))
        {
            return "use-case";
        }

        if (IsWebApiName(name))
        {
            return "endpoint";
        }

        if (name.EndsWith("State.cs", StringComparison.Ordinal))
        {
            return "state-record";
        }

        if (name.EndsWith("Event.cs", StringComparison.Ordinal))
        {
            return "domain-event";
        }

        if (name.EndsWith("Exception.cs", StringComparison.Ordinal))
        {
            return "aggregate";
        }

        if (name.EndsWith("Repository.cs", StringComparison.Ordinal))
        {
            return "aggregate";
        }

        if (HasSegment(segments, "Jobs") && name.EndsWith("Job.cs", StringComparison.Ordinal))
        {
            return "job";
        }

        // A reaction folder is named On{Fact} for the fact it handles, in the module
        // UseCases folder or beside the aggregate it changes, so the parent folder
        // names the reaction whatever tree holds it.
        if (name.EndsWith("Handler.cs", StringComparison.Ordinal)
            && segments.Length >= 2
            && segments[^2].StartsWith("On", StringComparison.Ordinal)
            && segments[^2].Length > 2)
        {
            return "reaction";
        }

        if (HasSegment(segments, "Domain") || segments.Any(segment => segment.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase)))
        {
            if (text.Contains("AggregateRoot<", StringComparison.Ordinal))
            {
                return "aggregate";
            }

            if (name.Length > 2 && name[0] == 'I' && char.IsUpper(name[1]))
            {
                return "";
            }

            return "value-object";
        }

        return "";
    }

    /// <summary>Derives the artifact of a record file from its type line.</summary>
    /// <param name="text">The file text, or empty for a folder.</param>
    /// <param name="name">The file name.</param>
    /// <returns>The artifact, or empty when the file names none.</returns>
    static string RecordFileArtifact(string text, string name)
    {
        if (name == "glossary.yaml")
        {
            return "term";
        }

        foreach (var line in text.Split('\n'))
        {
            var match = TypeLine.Match(line);

            if (match.Success)
            {
                return RecordArtifacts.GetValueOrDefault(match.Groups[1].Value, "");
            }
        }

        return "";
    }

    /// <summary>Reads the marked identifiers from the file text.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The marked identifiers in file order, each once.</returns>
    static List<string> ReadMarks(string text)
    {
        var marked = new List<string>();

        void Add(string id)
        {
            var trimmed = id.Trim();

            if (trimmed.Length > 0 && !marked.Contains(trimmed, StringComparer.Ordinal))
            {
                marked.Add(trimmed);
            }
        }

        foreach (Match match in XmlMark.Matches(text))
        {
            Add(match.Groups[2].Value);
        }

        foreach (Match match in AttributeMark.Matches(text))
        {
            Add(match.Groups[2].Value);
        }

        foreach (Match match in ConstMark.Matches(text))
        {
            Add(match.Groups[2].Value);
        }

        foreach (Match match in ProvesCall.Matches(text))
        {
            Add(match.Groups[1].Value);
        }

        return marked;
    }

    /// <summary>The artifact the record type governs, or empty when it governs none.</summary>
    /// <param name="recordId">The record or provision identifier.</param>
    /// <returns>The artifact, or empty.</returns>
    public static string ArtifactOf(string recordId)
    {
        var slash = recordId.IndexOf('/');

        if (slash < 0)
        {
            return "";
        }

        return RecordArtifacts.GetValueOrDefault(recordId[..slash], "");
    }

    /// <summary>Whether the segments hold the folder name.</summary>
    /// <param name="segments">The path segments.</param>
    /// <param name="folder">The folder name.</param>
    /// <returns>True when a segment names the folder.</returns>
    static bool HasSegment(string[] segments, string folder) =>
        segments.Any(segment => segment == folder);

    /// <summary>Whether the path sits under one of the folders.</summary>
    /// <param name="absolute">The absolute path.</param>
    /// <param name="roots">The absolute folders.</param>
    /// <returns>True when a folder holds the path.</returns>
    static bool UnderAny(string absolute, IReadOnlyList<string> roots) =>
        roots.Any(root => root.Length > 0 && Under(absolute, root));

    /// <summary>Whether the folder holds the path.</summary>
    /// <param name="absolute">The absolute path.</param>
    /// <param name="folder">The absolute folder.</param>
    /// <returns>True when the folder holds the path.</returns>
    static bool Under(string absolute, string folder)
    {
        var full = Path.GetFullPath(absolute);
        var basePath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), basePath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }
}
