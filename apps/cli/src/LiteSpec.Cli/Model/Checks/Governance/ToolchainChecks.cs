using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.ProseText;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The toolchain check: the <c>litespec</c> tool the project's tool manifest pins, the
///     LiteSpec.Analyzers package its build references, and the CLI running the check name one LiteSpec build.
/// </summary>
/// <follows>litespec/core/release#run-one-litespec-build</follows>
internal static class ToolchainChecks
{
    /// <summary>The finding code for toolchain builds that differ.</summary>
    internal const string DriftCode = "toolchain-drift";

    /// <summary>An MSBuild package element that can carry the analyzer version.</summary>
    static readonly Regex PackageElement = new(
        @"<\s*(PackageVersion|PackageReference)\b([^>]*?)(/?)>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>The Include or Update attribute naming the analyzer package.</summary>
    static readonly Regex AnalyzerName = new(
        """\b(Include|Update)\s*=\s*["']LiteSpec\.Analyzers["']""",
        RegexOptions.Compiled);

    /// <summary>The Version attribute of a package element.</summary>
    static readonly Regex VersionAttribute = new(
        """\bVersion\s*=\s*["']([^"']+)["']""",
        RegexOptions.Compiled);

    /// <summary>The Version child element of a package reference.</summary>
    static readonly Regex VersionElement = new(
        @"<\s*Version\s*>([^<]+)<\s*/\s*Version\s*>",
        RegexOptions.Compiled);

    /// <summary>One pinned version: the version text with the file and line that state it.</summary>
    /// <param name="Version">The pinned version text.</param>
    /// <param name="File">The absolute path of the file stating it.</param>
    /// <param name="Line">The 1-based line stating it.</param>
    internal sealed record Pin(string Version, string File, int Line);

    /// <summary>Reports toolchain-drift when the pinned tool, the referenced analyzers, and the running CLI name different builds.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var root = model.Config.Root;

        // Inside the LiteSpec repository itself the checkout's own pins are the maintainer's
        // business, and the example projects hold no tool manifest of their own.
        if (UnderLiteSpecRoot(root))
        {
            return;
        }

        var tool = ReadToolPin(root);
        var analyzer = ReadAnalyzerPin(root);

        if (tool is null && analyzer is null)
        {
            return;
        }

        var running = RunningVersion();
        var versions = new List<string>();

        if (tool is not null)
        {
            versions.Add(tool.Version);
        }

        if (analyzer is not null)
        {
            versions.Add(analyzer.Version);
        }

        if (running is not null)
        {
            versions.Add(running);
        }

        if (versions.Distinct(StringComparer.Ordinal).Count() < 2)
        {
            return;
        }

        var place = (tool ?? analyzer)!;
        findings.Add(new Finding(DriftCode, Message(tool, analyzer, running), place.File, place.Line, null));
    }

    /// <summary>The finding message naming the tool, analyzer, and running versions.</summary>
    /// <param name="tool">The pinned tool version, or null when no tool manifest pins it.</param>
    /// <param name="analyzer">The referenced analyzer version, or null when no build file references it.</param>
    /// <param name="running">The running CLI version, or null when it names none.</param>
    /// <returns>The one-sentence message with the fix.</returns>
    static string Message(Pin? tool, Pin? analyzer, string? running)
    {
        var toolText = tool is null ? "no tool manifest pins LiteSpec.Cli" : $"the tool manifest pins LiteSpec.Cli {tool.Version}";
        var analyzerText = analyzer is null
            ? "no build file references LiteSpec.Analyzers"
            : $"the build references LiteSpec.Analyzers {analyzer.Version}";
        var runningText = running is null ? "the running CLI names no version" : $"the running CLI is {running}";

        return $"The toolchain names different LiteSpec builds: {toolText}, {analyzerText}, and {runningText}; move all three to one LiteSpec build.";
    }

    /// <summary>Whether the consumer root sits inside the LiteSpec checkout the running tool belongs to.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>True when the root is the LiteSpec release root or sits under it.</returns>
    static bool UnderLiteSpecRoot(string root)
    {
        if (LiteSpecHome.Root is not { } home)
        {
            return false;
        }

        var full = Path.GetFullPath(root);
        home = Path.GetFullPath(home);

        return full.Equals(home, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(home + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads the LiteSpec.Cli version the tool manifest pins.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The pin, or null when no manifest pins the tool or the manifest cannot be read.</returns>
    internal static Pin? ReadToolPin(string root)
    {
        var file = Path.Combine(root, ".config", "dotnet-tools.json");

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        JsonNode? manifest;

        try
        {
            manifest = JsonNode.Parse(text);
        }
        catch (Exception exception) when (exception is JsonException)
        {
            return null;
        }

        var version = (manifest as JsonObject)?["tools"] is JsonObject tools
            && tools["LiteSpec.Cli"] is JsonObject entry
            && entry["version"] is JsonValue stated
            && stated.TryGetValue<string>(out var pinned)
            && pinned.Length > 0
            ? pinned
            : null;

        if (version is null)
        {
            return null;
        }

        var key = text.IndexOf("\"LiteSpec.Cli\"", StringComparison.Ordinal);
        var value = key < 0 ? -1 : text.IndexOf($"\"{version}\"", key, StringComparison.Ordinal);
        var line = value < 0 ? 1 : LineOf(LineStarts(text), value);

        return new Pin(version, file, line);
    }

    /// <summary>Reads the LiteSpec.Analyzers version the build references.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The first pin in central package files, then in projects, or null when nothing references the analyzers.</returns>
    internal static Pin? ReadAnalyzerPin(string root)
    {
        var central = new List<string>();
        var projects = new List<string>();

        foreach (var file in ProjectFiles(root))
        {
            if (Path.GetFileName(file).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            {
                central.Add(file);
            }
            else
            {
                projects.Add(file);
            }
        }

        // Central package management is the declared home of versions; a direct project
        // reference is the fallback. Each group reads in path order, so the pin is stable.
        foreach (var file in central.OrderBy(path => path, StringComparer.Ordinal)
            .Concat(projects.OrderBy(path => path, StringComparer.Ordinal)))
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (AnalyzerVersionIn(text) is { } pin)
            {
                return new Pin(pin.Version, file, LineOf(LineStarts(text), pin.Offset));
            }
        }

        return null;
    }

    /// <summary>Every central package file and every project under the root.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The absolute paths of the files found; folders that cannot be read are skipped.</returns>
    static IEnumerable<string> ProjectFiles(string root)
    {
        var waiting = new Stack<string>();
        waiting.Push(root);

        while (waiting.Count > 0)
        {
            var folder = waiting.Pop();
            string[] entries;

            try
            {
                entries = Directory.GetFileSystemEntries(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                bool folderEntry;

                try
                {
                    folderEntry = Directory.Exists(entry);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (folderEntry)
                {
                    waiting.Push(entry);
                    continue;
                }

                var name = Path.GetFileName(entry);

                if (name.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    yield return entry;
                }
            }
        }
    }

    /// <summary>Reads the analyzer version from one MSBuild file.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The version with its offset, or null when the file names no analyzer version.</returns>
    static (string Version, int Offset)? AnalyzerVersionIn(string text)
    {
        foreach (Match element in PackageElement.Matches(text))
        {
            var attributes = element.Groups[2].Value;

            if (!AnalyzerName.IsMatch(attributes))
            {
                continue;
            }

            var version = VersionAttribute.Match(element.Value);

            if (version.Success)
            {
                return (version.Groups[1].Value, element.Index + version.Groups[1].Index);
            }

            // A direct project reference can carry its version as a child element instead.
            if (element.Groups[3].Value.Length == 0 && element.Groups[1].Value == "PackageReference")
            {
                var body = text.Substring(element.Index, Math.Min(2000, text.Length - element.Index));
                var end = body.IndexOf("</PackageReference", StringComparison.Ordinal);

                if (end >= 0)
                {
                    var child = VersionElement.Match(body[..end]);

                    if (child.Success)
                    {
                        return (child.Groups[1].Value.Trim(), element.Index + child.Index);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>The version of the CLI running the check, from its informational version without build metadata.</summary>
    /// <returns>The running version, or null when the build stamped none.</returns>
    internal static string? RunningVersion()
    {
        var informational = typeof(ToolchainChecks).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
        {
            return null;
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        var version = plus < 0 ? informational : informational[..plus];

        return version.Length > 0 ? version : null;
    }
}
