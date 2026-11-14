using System.Text.RegularExpressions;
using LiteSpec.Cli.Skill;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     Renders the agent-protocol generated region in a consumer's AGENTS.md from the release template and the project file.
/// </summary>
/// <follows>litespec/core/agent#keep-the-agent-protocol-current</follows>
static partial class AgentProtocol
{
    /// <summary>The region name inside its fences.</summary>
    internal const string RegionName = "agent-protocol";

    /// <summary>The agent context file name at the consumer root.</summary>
    internal const string FileName = "AGENTS.md";

    /// <summary>The entry file a Claude Code project holds beside the agent context.</summary>
    internal const string ClaudeFileName = "CLAUDE.md";

    /// <summary>The agents a project-scope skill copy is checked for, in name order.</summary>
    internal static readonly string[] SkillAgents = ["agents", "claude", "opencode"];

    /// <summary>
    ///     Writes the agent-protocol region: it rewrites the region in an AGENTS.md that holds one, inserts the region
    ///     into one that holds none, and writes a fresh AGENTS.md from the template when the file is missing.
    /// </summary>
    /// <param name="model">The built project model the region renders from.</param>
    /// <returns>The rewritten AGENTS.md path, relative to the root, or nothing when the file is current or the release carries no template.</returns>
    public static IReadOnlyList<string> Write(ProjectModel model)
    {
        if (ReleaseTemplate() is not { } release)
        {
            return [];
        }

        var config = model.Config;
        var file = Path.Combine(config.Root, FileName);

        if (!File.Exists(file))
        {
            var fresh = Fill(
                release.Template,
                release.Release,
                release.Package,
                config.Size,
                config.Shape,
                FormatStacks(config.Stacks),
                FormatExtensions(config.Extensions),
                FormatLayers(config.Layers),
                SelectionKey(config.Shape, config.Stacks),
                CommandFor(config.Root));
            File.WriteAllText(file, fresh);

            return [PathText.Relative(config.Root, file)];
        }

        if (TemplateBody(release.Template) is not { } body)
        {
            return [];
        }

        var text = File.ReadAllText(file);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var filled = Fill(
            body,
            release.Release,
            release.Package,
            config.Size,
            config.Shape,
            FormatStacks(config.Stacks),
            FormatExtensions(config.Extensions),
            FormatLayers(config.Layers),
            SelectionKey(config.Shape, config.Stacks),
            CommandFor(config.Root));
        var expected = BuildRegion(filled, newline);

        // The region keeps the page's own line ending, so a CRLF page never gains LF lines.
        var rewritten = FindRegion(text) is not null
            ? Region().Replace(text, _ => expected)
            : Insert(text, expected, newline);

        if (rewritten == text)
        {
            return [];
        }

        File.WriteAllText(file, rewritten);

        return [PathText.Relative(config.Root, file)];
    }

    /// <summary>Rewrites every marked project-scope skill copy that differs from what the carried release generates.</summary>
    /// <param name="model">The built project model the copies render from.</param>
    /// <returns>The rewritten skill files, relative to the root, or nothing when every copy is current or the release carries no skill.</returns>
    public static IReadOnlyList<string> WriteSkills(ProjectModel model)
    {
        if (ExpectedSkills() is not { } expected)
        {
            return [];
        }

        var changed = new List<string>();

        foreach (var agent in SkillAgents)
        {
            var folder = SkillFiles.ProjectFolder(model.Config.Root, agent);

            if (!Directory.Exists(folder) || !SkillFiles.CarriesMarker(folder) || !SkillDrifted(folder, expected))
            {
                continue;
            }

            Directory.Delete(folder, recursive: true);

            foreach (var (relative, text) in expected)
            {
                var target = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, text);
                changed.Add(PathText.Relative(model.Config.Root, target));
            }
        }

        return changed;
    }

    /// <summary>The expected project-scope skill files from the carried release, or null when the release carries none.</summary>
    /// <returns>Every file of the copy with its forward-slash path and text, or null.</returns>
    internal static IReadOnlyList<(string RelativePath, string Text)>? ExpectedSkills()
    {
        var home = LiteSpecHome.Root;
        var version = LiteSpecHome.Version;

        if (home is null || version is null)
        {
            return null;
        }

        try
        {
            return SkillFiles.Render(Path.Combine(home, "templates"), ReleaseVersion.PackageOf(version));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Whether a marked project-scope skill copy differs from the generated files.</summary>
    /// <param name="folder">The skill folder.</param>
    /// <param name="expected">Every generated file with its forward-slash path and text.</param>
    /// <returns>True when a file is missing, differs, or an extra file stands.</returns>
    internal static bool SkillDrifted(string folder, IReadOnlyList<(string RelativePath, string Text)> expected)
    {
        var wanted = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (relative, text) in expected)
        {
            wanted[relative] = text;
        }

        string[] actual;

        try
        {
            actual = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }

        if (actual.Length != wanted.Count)
        {
            return true;
        }

        foreach (var file in actual)
        {
            var relative = Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');

            if (!wanted.TryGetValue(relative, out var want))
            {
                return true;
            }

            string have;

            try
            {
                have = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return true;
            }

            if (!string.Equals(have, want, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a CLAUDE.md text imports the agent context.</summary>
    /// <param name="text">The CLAUDE.md text.</param>
    /// <returns>True when the text names @AGENTS.md.</returns>
    internal static bool ImportsAgents(string text) =>
        text.Contains("@AGENTS.md", StringComparison.Ordinal);

    /// <summary>The region text the check compares against: the template region filled for the project, with LF line endings.</summary>
    /// <param name="model">The built project model the region renders from.</param>
    /// <returns>The expected region including its fences, or null when the release carries no readable template.</returns>
    internal static string? ExpectedRegion(ProjectModel model)
    {
        if (ReleaseTemplate() is not { } release)
        {
            return null;
        }

        if (TemplateBody(release.Template) is not { } body)
        {
            return null;
        }

        var config = model.Config;
        var filled = Fill(
            body,
            release.Release,
            release.Package,
            config.Size,
            config.Shape,
            FormatStacks(config.Stacks),
            FormatExtensions(config.Extensions),
            FormatLayers(config.Layers),
            SelectionKey(config.Shape, config.Stacks),
            CommandFor(config.Root));

        return BuildRegion(filled, "\n");
    }

    /// <summary>The release version, the package version and the agent context template text, or null when the release carries no readable template.</summary>
    /// <returns>The release, the package version and the template text, or null.</returns>
    private static (string Release, string Package, string Template)? ReleaseTemplate()
    {
        var home = LiteSpecHome.Root;
        var release = LiteSpecHome.Version;

        if (home is null || release is null)
        {
            return null;
        }

        var templatePath = Path.Combine(home, "templates", "consumer", "project-agents.md");

        if (!File.Exists(templatePath))
        {
            return null;
        }

        try
        {
            return (release, ReleaseVersion.PackageOf(release), File.ReadAllText(templatePath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The template's region body without its fences and surrounding newlines, or null when the template holds no region.</summary>
    /// <param name="template">The agent context template text.</param>
    /// <returns>The region body, or null.</returns>
    internal static string? TemplateBody(string template) =>
        FindRegion(template) is { } match ? match.Groups["body"].Value.Trim('\r', '\n') : null;

    /// <summary>Builds the full region text from a filled body: the fences with the body between them.</summary>
    /// <param name="body">The filled region body without fences.</param>
    /// <param name="newline">The line ending the region uses.</param>
    /// <returns>The region including its fences.</returns>
    internal static string BuildRegion(string body, string newline) =>
        $"<!-- generated:{RegionName} -->{newline}{body.ReplaceLineEndings(newline)}{newline}<!-- /generated:{RegionName} -->";

    /// <summary>The region match in a text, or null when the text holds no agent-protocol region.</summary>
    /// <param name="text">The text to search.</param>
    /// <returns>The region match, or null.</returns>
    internal static Match? FindRegion(string text)
    {
        var match = Region().Match(text);

        return match.Success ? match : null;
    }

    /// <summary>Whether a text holds the agent-protocol region.</summary>
    /// <param name="text">The text to search.</param>
    /// <returns>True when the text holds the region.</returns>
    internal static bool ContainsRegion(string text) => FindRegion(text) is not null;

    /// <summary>Inserts a region into a text that holds none: after the first heading, or at the end when no heading exists.</summary>
    /// <param name="text">The text without a region.</param>
    /// <param name="region">The region including its fences.</param>
    /// <param name="newline">The line ending the inserted blank lines use.</param>
    /// <returns>The text with the region inserted, leaving every other byte untouched.</returns>
    internal static string Insert(string text, string region, string newline)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return region + newline;
        }

        if (HeadingEnd(text) is { } headingEnd)
        {
            var insertion = headingEnd;
            var prefix = newline;

            if (insertion == text.Length && !text.EndsWith('\n'))
            {
                // The heading is the last line with no trailing newline; end it and leave a blank line.
                prefix = newline + newline;
            }
            else if (insertion < text.Length && text.AsSpan(insertion).StartsWith(newline.AsSpan()))
            {
                // A blank line after the heading already separates it from what follows, so the region starts after it.
                insertion += newline.Length;
                prefix = "";
            }

            // At the end the region closes the file; before content a blank line separates it from what follows.
            var suffix = insertion >= text.Length ? newline : newline + newline;

            return text[..insertion] + prefix + region + suffix + text[insertion..];
        }

        var end = text;

        if (!end.EndsWith('\n'))
        {
            end += newline;
        }

        // A blank line separates the region from the text above it.
        var separator = end.EndsWith(newline + newline) ? "" : newline;

        return end + separator + region + newline;
    }

    /// <summary>The offset after the first heading line's newline, or null when no line starts with a hash.</summary>
    /// <param name="text">The text to search.</param>
    /// <returns>The insertion offset after the first heading, or null.</returns>
    private static int? HeadingEnd(string text)
    {
        var offset = 0;

        foreach (var line in text.Split('\n'))
        {
            var stripped = line.EndsWith('\r') ? line[..^1] : line;
            var lineEnd = offset + line.Length;

            if (stripped.StartsWith('#'))
            {
                return lineEnd < text.Length && text[lineEnd] == '\n' ? lineEnd + 1 : lineEnd;
            }

            if (lineEnd >= text.Length)
            {
                break;
            }

            offset = lineEnd + 1;
        }

        return null;
    }

    /// <summary>Replaces every region placeholder in a text with its value.</summary>
    /// <param name="text">The template text with placeholders.</param>
    /// <param name="version">The LiteSpec release version, which names the schemas, selection indexes and links.</param>
    /// <param name="packageVersion">The LiteSpec package version the project follows, with a release candidate's suffix.</param>
    /// <param name="size">The project size, small or large.</param>
    /// <param name="shape">The project shape, server or client-only.</param>
    /// <param name="stacks">The selected stack keys, comma and space separated, or none.</param>
    /// <param name="extensions">The selected extension keys, comma and space separated, or none.</param>
    /// <param name="layers">The claimed layers with their practices.</param>
    /// <param name="selectionKey">The site's selection key for the selection page URL.</param>
    /// <param name="command">The command that runs the CLI in the project: dotnet litespec or litespec.</param>
    /// <returns>The text with each placeholder replaced by its value.</returns>
    internal static string Fill(string text, string version, string packageVersion, string size, string shape, string stacks, string extensions, string layers, string selectionKey, string command) =>
        text.Replace("__LITESPEC_PACKAGE_VERSION__", packageVersion, StringComparison.Ordinal)
            .Replace("__LITESPEC_VERSION__", version, StringComparison.Ordinal)
            .Replace("__SIZE__", size, StringComparison.Ordinal)
            .Replace("__SHAPE__", shape, StringComparison.Ordinal)
            .Replace("__STACKS__", stacks, StringComparison.Ordinal)
            .Replace("__EXTENSIONS__", extensions, StringComparison.Ordinal)
            .Replace("__LAYERS__", layers, StringComparison.Ordinal)
            .Replace("__SELECTION_KEY__", selectionKey, StringComparison.Ordinal)
            .Replace("__LITESPEC_COMMAND__", command, StringComparison.Ordinal);

    /// <summary>The stacks as the region names them: none, or the keys comma and space separated.</summary>
    /// <param name="stacks">The stack keys in list order.</param>
    /// <returns>The region form of the list.</returns>
    internal static string FormatStacks(IReadOnlyList<string> stacks) =>
        stacks.Count == 0 ? "none" : string.Join(", ", stacks);

    /// <summary>The extensions as the region names them: none, or the keys comma and space separated.</summary>
    /// <param name="extensions">The extension keys in declaration order.</param>
    /// <returns>The region form of the list.</returns>
    internal static string FormatExtensions(IReadOnlyList<string> extensions) =>
        extensions.Count == 0 ? "none" : string.Join(", ", extensions);

    /// <summary>The layers as the region names them: each claimed pair with its practice, such as intent and behavior: litespec.</summary>
    /// <param name="layers">The claimed layers with their practices.</param>
    /// <returns>The region form of the claim, or none when no layer is claimed.</returns>
    internal static string FormatLayers(LayerClaims layers)
    {
        var groups = new List<string>();

        if (FirstLayerGroup(layers) is { } first)
        {
            groups.Add(first);
        }

        if (SecondLayerGroup(layers) is { } second)
        {
            groups.Add(second);
        }

        return groups.Count == 0 ? "none" : string.Join(", ", groups);
    }

    /// <summary>The intent and behavior pair as the region names it, or null when neither is claimed.</summary>
    /// <param name="layers">The claimed layers with their practices.</param>
    /// <returns>The pair form, or null.</returns>
    private static string? FirstLayerGroup(LayerClaims layers) => (layers.Intent, layers.Behavior) switch
    {
        (true, true) => "intent and behavior: litespec",
        (true, false) => "intent: litespec",
        (false, true) => "behavior: litespec",
        _ => null,
    };

    /// <summary>The implementation and proof pair as the region names it, or null when neither is claimed.</summary>
    /// <param name="layers">The claimed layers with their practices.</param>
    /// <returns>The pair form, or null.</returns>
    private static string? SecondLayerGroup(LayerClaims layers)
    {
        if (layers.Implementation is { } implementation && layers.Proof is { } proof && implementation == proof)
        {
            return $"implementation and proof: {LayerClaims.NameOf(implementation)}";
        }

        var parts = new List<string>();

        if (layers.Implementation is { } claimedImplementation)
        {
            parts.Add($"implementation: {LayerClaims.NameOf(claimedImplementation)}");
        }

        if (layers.Proof is { } claimedProof)
        {
            parts.Add($"proof: {LayerClaims.NameOf(claimedProof)}");
        }

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    /// <summary>The site's selection key: the shape alone when no stack is selected, else the shape and the stacks joined by dots.</summary>
    /// <param name="shape">The project shape, server or client-only.</param>
    /// <param name="stacks">The stack keys in list order.</param>
    /// <returns>The selection key for the selection page URL.</returns>
    internal static string SelectionKey(string shape, IReadOnlyList<string> stacks) =>
        stacks.Count == 0 ? shape : $"{shape}.{string.Join(".", stacks)}";

    /// <summary>The command that runs the CLI in the project: dotnet litespec with a tool manifest, else litespec.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The command form the region names.</returns>
    internal static string CommandFor(string root) =>
        File.Exists(Path.Combine(root, ".config", "dotnet-tools.json")) ? "dotnet litespec" : "litespec";

    [GeneratedRegex("<!-- generated:agent-protocol -->(?<body>.*?)<!-- /generated:agent-protocol -->", RegexOptions.Singleline)]
    private static partial Regex Region();
}
