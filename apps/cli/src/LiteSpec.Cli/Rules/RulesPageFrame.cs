using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The Page frame block of <c>litespec rules</c> on a route file: the screen record
///     serving the file's route address, with the page-frame pattern it names printed in
///     full, followed by the screen's task, primary action and offers. The file's address
///     is derived the way the frontend readers derive it: from the folder under the web
///     routes root, from the Blazor page template, or from the screen the file marks.
///     When no screen serves the route, one line says so.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RulesPageFrame
{
    /// <summary>One screen record's fields the Page frame block prints.</summary>
    /// <param name="Id">The screen identifier.</param>
    /// <param name="Route">The route address the screen serves, or empty for a hosted screen.</param>
    /// <param name="Task">The page job in one to three sentences, or empty when the record names none.</param>
    /// <param name="PrimaryAction">The offered use case the view treats as primary, or empty when the record names none.</param>
    /// <param name="Offers">The use cases the screen offers.</param>
    /// <param name="PageFramePattern">The page-frame pattern slug, or empty when the record names none.</param>
    /// <param name="PageFrameOption">The chosen pattern option, or empty when the record names none.</param>
    internal sealed record ScreenFields(
        string Id,
        string Route,
        string Task,
        string PrimaryAction,
        IReadOnlyList<string> Offers,
        string PageFramePattern,
        string PageFrameOption);

    /// <summary>One page-frame pattern's fields the Page frame block prints.</summary>
    /// <param name="Title">The pattern heading.</param>
    /// <param name="Id">The pattern provision identifier.</param>
    /// <param name="Question">The layout question the pattern answers.</param>
    /// <param name="Default">The default option slug.</param>
    /// <param name="Wireframes">The wireframe lines by option slug.</param>
    internal sealed record PatternFields(
        string Title,
        string Id,
        string Question,
        string Default,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Wireframes);

    /// <summary>A SvelteKit route group segment, which carries no address.</summary>
    static readonly Regex GroupSegment = new(@"^\(.*\)$", RegexOptions.Compiled);

    /// <summary>A React <c>createFileRoute</c> call with its address argument.</summary>
    static readonly Regex CreateFileRoute = new(@"createFileRoute\(\s*['""]([^'""]+)['""]", RegexOptions.Compiled);

    /// <summary>A Blazor <c>@page</c> directive with its template.</summary>
    static readonly Regex PageDirective = new(@"@page\s+""([^""]+)""", RegexOptions.Compiled);

    /// <summary>One backticked span, holding an option slug.</summary>
    static readonly Regex BacktickSpan = new("`([^`]+)`", RegexOptions.Compiled);

    /// <summary>The page-frame pattern page, relative to the release root.</summary>
    const string PageFramesPage = "docs/frontend/patterns/page-frames.md";

    /// <summary>Prints the Page frame block for a route file, or nothing for any other target.</summary>
    /// <param name="config">The consumer configuration holding the record roots and frontends.</param>
    /// <param name="home">The carried LiteSpec release root holding the pattern pages.</param>
    /// <param name="target">The rules target.</param>
    public static void Print(ConsumerConfig config, string home, PathContext target)
    {
        if (target.IsRecord || target.Artifact != "route" || target.RelativePath.Length == 0)
        {
            return;
        }

        var absolute = Path.Combine(config.Root, target.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(absolute))
        {
            return;
        }

        var screen = FindScreen(config, target, absolute);

        if (screen is null)
        {
            CliHost.Out.WriteLine("No screen record serves this route.");
            return;
        }

        PrintScreen(home, screen);
    }

    /// <summary>Finds the screen serving the route file: first by route address, then by mark.</summary>
    /// <param name="config">The consumer configuration holding the record roots and frontends.</param>
    /// <param name="target">The rules target with its marked identifiers.</param>
    /// <param name="absolute">The absolute route file path.</param>
    /// <returns>The serving screen, or null when no screen serves the route.</returns>
    static ScreenFields? FindScreen(ConsumerConfig config, PathContext target, string absolute)
    {
        var screens = LoadScreens(config);

        if (screens.Count == 0)
        {
            return null;
        }

        var addresses = FileAddresses(config, absolute);

        if (addresses.Count > 0)
        {
            var byRoute = screens
                .Where(screen => screen.Route.Length > 0
                    && addresses.Any(address => FrontendFactsRules.AddressMatches(screen.Route, address)))
                .OrderBy(screen => screen.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (byRoute is not null)
            {
                return byRoute;
            }
        }

        foreach (var marked in target.Marked)
        {
            if (marked.StartsWith("screen/", StringComparison.Ordinal)
                && screens.FirstOrDefault(screen => screen.Id == marked) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Loads every screen record under the consumer's record roots, in identifier order.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The screen records found.</returns>
    static List<ScreenFields> LoadScreens(ConsumerConfig config)
    {
        var found = new Dictionary<string, ScreenFields>(StringComparer.Ordinal);

        foreach (var root in config.RecordRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.OrderBy(name => name, StringComparer.Ordinal))
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

                if (!RecordFile.TryParse(text, out var parsed, out _, out _)
                    || parsed.ValueKind != JsonValueKind.Object
                    || !string.Equals(Text(parsed, "type"), "screen", StringComparison.Ordinal))
                {
                    continue;
                }

                var id = Text(parsed, "id");

                if (id.Length == 0 || found.ContainsKey(id))
                {
                    continue;
                }

                found[id] = new ScreenFields(
                    id,
                    Text(parsed, "route"),
                    Text(parsed, "task"),
                    Text(parsed, "primaryAction"),
                    OffersOf(parsed),
                    PageFrameOf(parsed, "pattern"),
                    PageFrameOf(parsed, "option"));
            }
        }

        return found.Values.OrderBy(screen => screen.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>The route addresses the file serves, or none when it serves none by path.</summary>
    /// <param name="config">The consumer configuration holding the frontends.</param>
    /// <param name="absolute">The absolute route file path.</param>
    /// <returns>The addresses the file serves.</returns>
    static IReadOnlyList<string> FileAddresses(ConsumerConfig config, string absolute)
    {
        var frontend = config.Frontends.FirstOrDefault(candidate =>
            candidate.Path.Length > 0 && Under(absolute, candidate.Path));

        if (frontend is null)
        {
            return absolute.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                ? RazorAddresses(ReadText(absolute))
                : [];
        }

        return frontend.Platform.ToLowerInvariant() switch
        {
            "svelte" => SvelteAddresses(frontend, absolute),
            "react" => ReactAddresses(frontend, absolute),
            "blazor" => RazorAddresses(ReadText(absolute)),
            _ => [],
        };
    }

    /// <summary>The address a Svelte route file serves: its folder under the routes root without group segments.</summary>
    /// <param name="frontend">The frontend holding the file.</param>
    /// <param name="absolute">The absolute route file path.</param>
    /// <returns>The one address, or none when the file is no route entry.</returns>
    static IReadOnlyList<string> SvelteAddresses(FrontendConfig frontend, string absolute)
    {
        if (!string.Equals(Path.GetFileName(absolute), "+page.svelte", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var folder = Path.GetDirectoryName(absolute);

        if (folder is null || !Under(folder, frontend.RoutesRoot))
        {
            return [];
        }

        var relative = PathText.Relative(frontend.RoutesRoot, folder);

        if (relative is "" or ".")
        {
            return ["/"];
        }

        var kept = relative.Split('/')
            .Where(segment => segment.Length > 0 && !GroupSegment.IsMatch(segment))
            .ToList();

        return kept.Count == 0 ? ["/"] : [$"/{string.Join("/", kept)}"];
    }

    /// <summary>The address a React route file serves: its declared route, or the address its path derives.</summary>
    /// <param name="frontend">The frontend holding the file.</param>
    /// <param name="absolute">The absolute route file path.</param>
    /// <returns>The one address, or none when the file sits outside the routes root.</returns>
    static IReadOnlyList<string> ReactAddresses(FrontendConfig frontend, string absolute)
    {
        if (!Under(absolute, frontend.RoutesRoot))
        {
            return [];
        }

        var declared = CreateFileRoute.Match(ReadText(absolute));

        if (declared.Success && declared.Groups[1].Value.Trim().Length > 0)
        {
            return [VisibleAddress(declared.Groups[1].Value.Trim())];
        }

        return [DerivedAddress(frontend.RoutesRoot, absolute)];
    }

    /// <summary>The addresses a Blazor component serves: every page template it declares.</summary>
    /// <param name="text">The component text.</param>
    /// <returns>The declared templates, without the escaped literals.</returns>
    static List<string> RazorAddresses(string text)
    {
        var templates = new List<string>();

        foreach (Match match in PageDirective.Matches(text))
        {
            if (match.Index > 0 && text[match.Index - 1] == '@')
            {
                continue;
            }

            var template = match.Groups[1].Value.Trim();

            if (template.Length > 0 && !templates.Contains(template, StringComparer.Ordinal))
            {
                templates.Add(template);
            }
        }

        return templates;
    }

    /// <summary>The visible address of a declared React route: without groups, pathless segments and index files.</summary>
    /// <param name="route">The declared route.</param>
    /// <returns>The visible address.</returns>
    static string VisibleAddress(string route)
    {
        var kept = new List<string>();

        foreach (var segment in route.Split('/'))
        {
            if (segment.Length == 0 || GroupSegment.IsMatch(segment) || segment.StartsWith('_') || segment == "index")
            {
                continue;
            }

            kept.Add(segment);
        }

        return kept.Count == 0 ? "/" : $"/{string.Join("/", kept)}";
    }

    /// <summary>The address a React route path derives: without groups, pathless segments and index files, with parameters in brackets.</summary>
    /// <param name="routes">The routes root.</param>
    /// <param name="absolute">The absolute route file path.</param>
    /// <returns>The derived address.</returns>
    static string DerivedAddress(string routes, string absolute)
    {
        var relative = PathText.Relative(routes, absolute);
        var segments = relative.Split('/').ToList();
        var last = segments.Count > 0 ? segments[^1] : "";
        segments.RemoveAt(segments.Count - 1);
        segments.Add(WithoutExtension(last));
        var kept = new List<string>();

        foreach (var segment in segments)
        {
            if (segment.Length == 0 || GroupSegment.IsMatch(segment) || segment.StartsWith('_') || segment == "index")
            {
                continue;
            }

            kept.Add(segment == "$" ? "[...rest]" : segment.StartsWith('$') ? $"[{segment[1..]}]" : segment);
        }

        return kept.Count == 0 ? "/" : $"/{string.Join("/", kept)}";
    }

    /// <summary>The file name without a web route extension.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>The stem without its extension.</returns>
    static string WithoutExtension(string name) =>
        name.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ? name[..^4]
        : name.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ? name[..^3]
        : name.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase) ? name[..^4]
        : name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ? name[..^3]
        : name;

    /// <summary>Prints the matched screen with its page-frame pattern in full.</summary>
    /// <param name="home">The carried LiteSpec release root holding the pattern pages.</param>
    /// <param name="screen">The screen serving the route.</param>
    static void PrintScreen(string home, ScreenFields screen)
    {
        if (screen.PageFramePattern.Length == 0)
        {
            CliHost.Out.WriteLine("Page frame: none recorded.");
            CliHost.Out.WriteLine($"Screen: {screen.Id}");
            PrintTask(screen);
            return;
        }

        var pattern = LoadPattern(home, screen.PageFramePattern);

        if (pattern is null)
        {
            CliHost.Out.WriteLine($"Page frame: {screen.PageFramePattern} names no page-frame pattern.");
            CliHost.Out.WriteLine($"Screen: {screen.Id}");
            PrintTask(screen);
            return;
        }

        CliHost.Out.WriteLine($"Page frame: {pattern.Id} ({pattern.Title})");
        CliHost.Out.WriteLine($"Screen: {screen.Id}");
        CliHost.Out.WriteLine($"Question: {pattern.Question}");
        CliHost.Out.WriteLine($"Default: {pattern.Default}");

        if (screen.PageFrameOption.Length > 0)
        {
            CliHost.Out.WriteLine($"Chosen option: {screen.PageFrameOption}");
        }

        var wireframeOption = screen.PageFrameOption.Length > 0 && pattern.Wireframes.ContainsKey(screen.PageFrameOption)
            ? screen.PageFrameOption
            : pattern.Default;

        if (pattern.Wireframes.TryGetValue(wireframeOption, out var wireframe) && wireframe.Count > 0)
        {
            CliHost.Out.WriteLine($"Wireframe ({wireframeOption}):");

            foreach (var line in wireframe)
            {
                CliHost.Out.WriteLine(line);
            }
        }
        else
        {
            CliHost.Out.WriteLine("Wireframe: none recorded on the pattern.");
        }

        PrintTask(screen);
    }

    /// <summary>Prints the screen's task, primary action and offers.</summary>
    /// <param name="screen">The screen serving the route.</param>
    static void PrintTask(ScreenFields screen)
    {
        CliHost.Out.WriteLine(screen.Task.Length > 0 ? $"Task: {screen.Task}" : "Task: none recorded.");
        CliHost.Out.WriteLine(screen.PrimaryAction.Length > 0 ? $"Primary action: {screen.PrimaryAction}" : "Primary action: none recorded.");
        CliHost.Out.WriteLine(screen.Offers.Count > 0 ? $"Offers: {string.Join(", ", screen.Offers)}" : "Offers: none.");
    }

    /// <summary>Reads one page-frame pattern from the pattern page in the carried release.</summary>
    /// <param name="home">The carried LiteSpec release root holding the pattern pages.</param>
    /// <param name="slug">The pattern slug the screen names.</param>
    /// <returns>The pattern, or null when the page holds no pattern with the slug.</returns>
    static PatternFields? LoadPattern(string home, string slug)
    {
        var file = Path.Combine(home, PageFramesPage.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(file))
        {
            return null;
        }

        string[] lines;

        try
        {
            lines = File.ReadAllLines(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var fenced = FencedLines(lines);

        for (var index = 0; index < lines.Length; index++)
        {
            if (fenced.Contains(index) || !lines[index].StartsWith("### ", StringComparison.Ordinal))
            {
                continue;
            }

            var title = lines[index]["### ".Length..].Trim();
            var id = StatedId(lines, fenced, index);

            if (id.Length == 0 || SlugOf(id) != slug)
            {
                continue;
            }

            var end = SectionEnd(lines, fenced, index);
            return new PatternFields(
                title,
                id,
                LabelText(lines, fenced, index, end, "Question"),
                Unquote(LabelText(lines, fenced, index, end, "Default")),
                WireframesOf(lines, fenced, index, end));
        }

        return null;
    }

    /// <summary>The lines inside fenced blocks, which hold no headings or labels.</summary>
    /// <param name="lines">The page lines.</param>
    /// <returns>The fenced line numbers.</returns>
    static HashSet<int> FencedLines(string[] lines)
    {
        var fenced = new HashSet<int>();
        var open = false;

        for (var number = 0; number < lines.Length; number++)
        {
            if (lines[number].StartsWith("```", StringComparison.Ordinal))
            {
                open = !open;
            }

            if (open)
            {
                fenced.Add(number);
            }
        }

        return fenced;
    }

    /// <summary>The identifier the ID line under the heading states, or empty when it states none.</summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="fenced">The fenced line numbers.</param>
    /// <param name="heading">The heading line number.</param>
    /// <returns>The stated identifier, or empty.</returns>
    static string StatedId(string[] lines, HashSet<int> fenced, int heading)
    {
        var cursor = heading + 1;

        while (cursor < lines.Length && (fenced.Contains(cursor) || lines[cursor].Trim().Length == 0))
        {
            cursor++;
        }

        return cursor < lines.Length && PageReader.TryParseIdLine(lines[cursor].Trim(), out var stated)
            ? stated
            : "";
    }

    /// <summary>The slug of a provision identifier: the part after the number sign.</summary>
    /// <param name="id">The provision identifier.</param>
    /// <returns>The slug, or empty when the identifier names none.</returns>
    static string SlugOf(string id)
    {
        var hash = id.IndexOf('#');
        return hash >= 0 && hash + 1 < id.Length ? id[(hash + 1)..] : "";
    }

    /// <summary>The first line after the pattern section: the next second or third level heading, or the page end.</summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="fenced">The fenced line numbers.</param>
    /// <param name="heading">The pattern heading line number.</param>
    /// <returns>The first line after the section.</returns>
    static int SectionEnd(string[] lines, HashSet<int> fenced, int heading)
    {
        for (var cursor = heading + 1; cursor < lines.Length; cursor++)
        {
            if (fenced.Contains(cursor))
            {
                continue;
            }

            if (lines[cursor].StartsWith("### ", StringComparison.Ordinal)
                || lines[cursor].StartsWith("## ", StringComparison.Ordinal)
                || lines[cursor].StartsWith("# ", StringComparison.Ordinal))
            {
                return cursor;
            }
        }

        return lines.Length;
    }

    /// <summary>One label's text in the pattern section: its line rest with the plain lines below it.</summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="fenced">The fenced line numbers.</param>
    /// <param name="heading">The pattern heading line number.</param>
    /// <param name="end">The first line after the section.</param>
    /// <param name="label">The label name, such as Question or Default.</param>
    /// <returns>The label text with single spaces.</returns>
    static string LabelText(string[] lines, HashSet<int> fenced, int heading, int end, string label)
    {
        var prefix = $"**{label}:**";

        for (var cursor = heading + 1; cursor < end; cursor++)
        {
            if (fenced.Contains(cursor) || !lines[cursor].StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var parts = new List<string>();
            var rest = lines[cursor][prefix.Length..].Trim();

            if (rest.Length > 0)
            {
                parts.Add(rest);
            }

            for (var next = cursor + 1; next < end; next++)
            {
                if (fenced.Contains(next))
                {
                    continue;
                }

                var text = lines[next].Trim();

                if (text.Length == 0 || text.StartsWith("**", StringComparison.Ordinal) || text.StartsWith('#'))
                {
                    break;
                }

                parts.Add(text);
            }

            return string.Join(" ", parts);
        }

        return "";
    }

    /// <summary>Every option wireframe in the pattern section, by option slug.</summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="fenced">The fenced line numbers.</param>
    /// <param name="heading">The pattern heading line number.</param>
    /// <param name="end">The first line after the section.</param>
    /// <returns>The wireframe lines by option slug.</returns>
    static Dictionary<string, IReadOnlyList<string>> WireframesOf(string[] lines, HashSet<int> fenced, int heading, int end)
    {
        var wireframes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        for (var cursor = heading + 1; cursor < end; cursor++)
        {
            if (fenced.Contains(cursor) || !lines[cursor].StartsWith("#### ", StringComparison.Ordinal))
            {
                continue;
            }

            var option = BacktickSpan.Match(lines[cursor]);

            if (!option.Success)
            {
                continue;
            }

            var slug = option.Groups[1].Value.Trim();
            var optionEnd = cursor + 1;

            while (optionEnd < end
                && (fenced.Contains(optionEnd) || !lines[optionEnd].StartsWith("#### ", StringComparison.Ordinal)))
            {
                optionEnd++;
            }

            var wireframe = WireframeBetween(lines, cursor, optionEnd);

            if (slug.Length > 0 && wireframe.Count > 0 && !wireframes.ContainsKey(slug))
            {
                wireframes[slug] = wireframe;
            }
        }

        return wireframes;
    }

    /// <summary>The first wireframe block between the option heading and the next option heading.</summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="start">The option heading line number.</param>
    /// <param name="end">The next option heading line number, or the section end.</param>
    /// <returns>The wireframe lines without the fences.</returns>
    static List<string> WireframeBetween(string[] lines, int start, int end)
    {
        for (var cursor = start + 1; cursor < end; cursor++)
        {
            if (!lines[cursor].StartsWith("```wireframe", StringComparison.Ordinal))
            {
                continue;
            }

            var wireframe = new List<string>();

            for (var next = cursor + 1; next < end; next++)
            {
                if (lines[next].StartsWith("```", StringComparison.Ordinal))
                {
                    return wireframe;
                }

                wireframe.Add(lines[next].TrimEnd('\r'));
            }

            return wireframe;
        }

        return [];
    }

    /// <summary>Removes one surrounding backtick pair from a value, with the spaces around it.</summary>
    /// <param name="value">The value as written.</param>
    /// <returns>The value without its backticks.</returns>
    static string Unquote(string value)
    {
        var text = value.Trim();
        return text.Length >= 2 && text.StartsWith('`') && text.EndsWith('`') ? text[1..^1].Trim() : text;
    }

    /// <summary>The offered use cases the screen record names.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The offered identifiers.</returns>
    static List<string> OffersOf(JsonElement root)
    {
        if (!root.TryGetProperty("offers", out var offers) || offers.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return offers.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.String)
            .Select(entry => entry.GetString() ?? "")
            .Where(id => id.Length > 0)
            .ToList();
    }

    /// <summary>One page-frame field the screen record names.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <param name="name">The field name: pattern or option.</param>
    /// <returns>The field text, or empty.</returns>
    static string PageFrameOf(JsonElement root, string name) =>
        root.TryGetProperty("pageFrame", out var frame) && frame.ValueKind == JsonValueKind.Object
            ? Text(frame, name)
            : "";

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>Reads the file text, or empty when it cannot be read.</summary>
    /// <param name="absolute">The absolute file path.</param>
    /// <returns>The file text, or empty.</returns>
    static string ReadText(string absolute)
    {
        try
        {
            return File.ReadAllText(absolute);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }

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
