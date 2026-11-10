using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Markdig.Syntax;

namespace LiteSpec.Authoring;

/// <summary>
///     The folder-based stack layout: the stacks a manifest ships and the pages a
///     selection applies. Pure computation over the manifest and the docs tree, so the
///     consumer validator and the model builder can call it without console output.
/// </summary>
/// <follows>litespec/core/stacks#apply-the-complete-stack</follows>
public static class Layout
{
    /// <summary>One shipped stack: its id, folder, kind, and pairing rules.</summary>
    /// <param name="Id">The stack id a project names.</param>
    /// <param name="Folder">The stack folder, such as <c>docs/backend/dotnet</c>.</param>
    /// <param name="Kind"><c>backend</c> or <c>frontend</c>, from the folder's area.</param>
    /// <param name="Requires">Stacks a project must also select with this one.</param>
    /// <param name="PairsWith">The only backend stacks a frontend stack works beside.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Layout.Stack names one shipped stack in the manifest model; renaming the public nested type breaks every host that reads the stack list.")]
    public sealed record Stack(string Id, string Folder, string Kind, IReadOnlyList<string> Requires, IReadOnlyList<string> PairsWith);

    /// <summary>One page of the page index: its file, route, style, area, title, intent and H2 sections.</summary>
    /// <param name="FilePath">The docs-relative file path, such as <c>docs/core/scope.md</c>.</param>
    /// <param name="Route">The path under docs without the extension, such as <c>core/scope</c>.</param>
    /// <param name="Style">The page style: standard, stack, extension, pattern, index and the rest.</param>
    /// <param name="Area">The first folder under docs, such as <c>core</c>.</param>
    /// <param name="Title">The first H1 text, or the route when the page has none.</param>
    /// <param name="Intent">The first paragraph of the Intent, Purpose or Name section.</param>
    /// <param name="Sections">The H2 section titles, in order.</param>
    private sealed record IndexedPage(string FilePath, string Route, string Style, string Area, string Title, string Intent, IReadOnlyList<string> Sections);

    /// <summary>One layer the site publishes: its key, its title and the pages it holds.</summary>
    /// <param name="Key">The layer key, such as <c>core</c> or <c>backend/dotnet</c>.</param>
    /// <param name="Title">The layer title.</param>
    /// <param name="Files">The docs-relative pages in the layer, in reading order.</param>
    private sealed record PublishedLayer(string Key, string Title, IReadOnlyList<string> Files);

    /// <summary>One selection the site publishes: a shape and the stacks it names.</summary>
    /// <param name="Shape">The project shape, such as <c>server</c>.</param>
    /// <param name="Stacks">The stacks the selection names, backend first.</param>
    private sealed record SelectionChoice(string Shape, List<string> Stacks);

    /// <summary>Reads the stacks a manifest ships, in manifest order.</summary>
    /// <param name="manifest">The manifest object.</param>
    /// <returns>The stacks the manifest ships, in manifest order.</returns>
    public static IReadOnlyList<Stack> Stacks(JsonObject manifest)
    {
        var result = new List<Stack>();
        if ((manifest["stacks"] as JsonObject) is not { } stacks)
        {
            return result;
        }

        foreach (var (id, node) in stacks)
        {
            var stack = node as JsonObject;
            var folder = Text(stack?["folder"]) ?? "";
            result.Add(new Stack(id, folder, KindFor(folder), Strings(stack?["requires"]), Strings(stack?["pairsWith"])));
        }

        return result;
    }

    /// <summary>
    ///     Computes the pages a selection applies: the always-applied areas, each selected
    ///     stack's folder chain, the named shape's backend pages, and each selected
    ///     extension's page. Returns <c>docs/...</c> relative paths sorted ordinally.
    ///     Pages stay applied while their replaced provisions drop out: read the applied
    ///     provisions through <see cref="AppliedProvisions"/>.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="shape">The shape the project names.</param>
    /// <param name="stacks">The stacks the project selects.</param>
    /// <param name="extensions">The extensions the project selects.</param>
    /// <returns>The applied pages as docs-relative paths, sorted ordinally.</returns>
    public static IReadOnlyList<string> AppliedPages(string root, JsonObject manifest, string shape, IReadOnlyList<string> stacks, IReadOnlyList<string> extensions)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var area in new[] { "docs/core", "docs/workspace", "docs/quality" })
        {
            foreach (var page in PagesDirectlyIn(root, area))
            {
                applied.Add(page);
            }
        }

        var shipped = Stacks(manifest).ToDictionary(stack => stack.Id, StringComparer.Ordinal);
        foreach (var id in stacks)
        {
            if (!shipped.TryGetValue(id, out var stack) || stack.Kind.Length == 0)
            {
                continue;
            }

            var area = stack.Kind == "backend" ? "docs/backend" : "docs/frontend";
            foreach (var folder in ChainFolders(stack.Folder, area).SelectMany(folder => CompanionFolders(root, folder).Prepend(folder)))
            {
                foreach (var page in PagesDirectlyIn(root, folder))
                {
                    applied.Add(page);
                }
            }
        }

        if ((manifest["shapes"] as JsonObject)?[shape] as JsonObject is { } named)
        {
            foreach (var page in Strings(named["backendPages"]))
            {
                applied.Add(page);
            }
        }

        var shippedExtensions = manifest["extensions"] as JsonObject;
        foreach (var id in extensions)
        {
            if (Text((shippedExtensions?[id] as JsonObject)?["path"]) is { Length: > 0 } path)
            {
                applied.Add(path);
            }
        }

        return applied.OrderBy(page => page, StringComparer.Ordinal).ToList();
    }

    /// <summary>Reads the provisions a selection replaces: every provision id the selected extensions name in replaces.</summary>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="extensions">The extensions the project selects.</param>
    /// <returns>The replaced provision ids, sorted ordinally.</returns>
    public static IReadOnlyList<string> ReplacedProvisions(JsonObject manifest, IReadOnlyList<string> extensions)
    {
        var replaced = new HashSet<string>(StringComparer.Ordinal);
        var shipped = manifest["extensions"] as JsonObject;

        foreach (var id in extensions)
        {
            foreach (var provision in Strings((shipped?[id] as JsonObject)?["replaces"]))
            {
                replaced.Add(provision);
            }
        }

        return replaced.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    ///     Computes the provisions a selection applies: every provision on an applied page except the
    ///     provisions a selected extension replaces. Returns provision ids sorted ordinally.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="shape">The shape the project names.</param>
    /// <param name="stacks">The stacks the project selects.</param>
    /// <param name="extensions">The extensions the project selects.</param>
    /// <returns>The applied provision ids, sorted ordinally.</returns>
    public static IReadOnlyList<string> AppliedProvisions(string root, JsonObject manifest, string shape, IReadOnlyList<string> stacks, IReadOnlyList<string> extensions)
    {
        var applied = new HashSet<string>(AppliedPages(root, manifest, shape, stacks, extensions), StringComparer.Ordinal);
        var replaced = new HashSet<string>(ReplacedProvisions(manifest, extensions), StringComparer.Ordinal);
        var provisions = new List<string>();

        foreach (var row in Provisions.Collect(root))
        {
            if (applied.Contains(row.Page) && !replaced.Contains(row.Id))
            {
                provisions.Add(row.Id);
            }
        }

        provisions.Sort(StringComparer.Ordinal);

        return provisions;
    }

    /// <summary>Derives a stack's kind from its folder's area, or empty when outside both areas.</summary>
    /// <param name="folder">The stack folder.</param>
    /// <returns>The stack kind, or empty when the folder sits outside both areas.</returns>
    public static string KindFor(string folder) =>
        folder == "docs/backend" || folder.StartsWith("docs/backend/", StringComparison.Ordinal) ? "backend"
        : folder == "docs/frontend" || folder.StartsWith("docs/frontend/", StringComparison.Ordinal) ? "frontend"
        : "";

    // A stack applies its folder and every folder up to its area root, so a page
    // directly in any of those folders is reached and anything deeper is not.
    /// <summary>Lists a stack folder and every folder up to its area root.</summary>
    /// <param name="folder">The stack folder.</param>
    /// <param name="area">The area root folder.</param>
    /// <returns>The folder chain from the stack folder up to the area root.</returns>
    static IEnumerable<string> ChainFolders(string folder, string area)
    {
        var current = folder.Replace('\\', '/').TrimEnd('/');
        while (current.Length >= area.Length
            && current.StartsWith(area, StringComparison.Ordinal)
            && (current.Length == area.Length || current[area.Length] == '/'))
        {
            yield return current;
            if (current.Length == area.Length)
            {
                yield break;
            }

            current = current[..current.LastIndexOf('/')];
        }
    }

    /// <summary>
    ///     The folders that apply with <paramref name="folder"/>: each subfolder, at any
    ///     depth, that is not an <c>ext/</c> folder and holds no stack. A stack folder is
    ///     one with a <c>stack.md</c> page, so <c>docs/frontend/patterns</c> applies with
    ///     <c>docs/frontend</c> while <c>docs/frontend/web</c>, which holds stacks, does not.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="folder">The folder that applies.</param>
    /// <returns>The companion folders, in ordinal order.</returns>
    public static IEnumerable<string> CompanionFolders(string root, string folder)
    {
        var directory = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var sub in Directory.GetDirectories(directory).OrderBy(name => name, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(sub);
            if (name == "ext" || HoldsStack(sub))
            {
                continue;
            }

            var relative = $"{folder.TrimEnd('/')}/{name}";
            yield return relative;

            foreach (var deeper in CompanionFolders(root, relative))
            {
                yield return deeper;
            }
        }
    }

    /// <summary>Whether the folder holds a stack, directly or below it.</summary>
    /// <param name="directory">The folder to test.</param>
    /// <returns>True when the folder holds a stack.</returns>
    static bool HoldsStack(string directory) =>
        File.Exists(Path.Combine(directory, "stack.md"))
        || Directory.GetDirectories(directory).Any(sub => Path.GetFileName(sub) != "ext" && HoldsStack(sub));

    /// <summary>Lists the pages directly in one folder.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="folder">The folder to read.</param>
    /// <returns>The pages directly in the folder, as docs-relative paths.</returns>
    static IEnumerable<string> PagesDirectlyIn(string root, string folder)
    {
        var directory = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            if (!file.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file) == "README.md")
            {
                continue;
            }

            yield return Slash(Path.GetRelativePath(root, file));
        }
    }

    /// <summary>
    ///     Builds the page index the site reads: every docs page with its style, area, title, intent and H2 sections,
    ///     the stacks and extensions the manifest ships, the layers the site publishes, and each selection with the
    ///     pages and layers it applies. The site reads this one file and derives none of it.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="manifest">The manifest object.</param>
    /// <returns>The page index as JSON.</returns>
    public static JsonObject PageIndex(string root, JsonObject manifest)
    {
        var pages = ReadingOrder(root, AllPages(root).Select(file => ReadPage(root, file)).ToList());
        var stacks = Stacks(manifest);
        var layers = PublishedLayers(root, manifest, pages, stacks);
        var selections = new JsonArray();
        foreach (var choice in SelectionChoices(manifest, stacks))
        {
            var applied = AppliedPages(root, manifest, choice.Shape, choice.Stacks, []);
            var appliedSet = new HashSet<string>(applied, StringComparer.Ordinal);
            var selectionLayers = layers
                .Where(layer => !layer.Key.StartsWith("ext/", StringComparison.Ordinal) && !layer.Key.StartsWith("patterns/", StringComparison.Ordinal))
                .Where(layer => layer.Files.Any(file => appliedSet.Contains(file)))
                .Select(layer => layer.Key);
            Append(selections, new JsonObject
            {
                ["key"] = string.Join('.', new[] { choice.Shape }.Concat(choice.Stacks)),
                ["shape"] = choice.Shape,
                ["stacks"] = StringArray(choice.Stacks),
                ["pages"] = StringArray(applied),
                ["layers"] = StringArray(selectionLayers),
            });
        }

        var pageArray = new JsonArray();
        foreach (var page in pages)
        {
            Append(pageArray, new JsonObject
            {
                ["file"] = page.FilePath,
                ["path"] = page.Route,
                ["style"] = page.Style,
                ["area"] = page.Area,
                ["title"] = page.Title,
                ["intent"] = page.Intent,
                ["sections"] = StringArray(page.Sections),
            });
        }

        var stackArray = new JsonArray();
        foreach (var stack in stacks)
        {
            Append(stackArray, new JsonObject { ["id"] = stack.Id, ["folder"] = stack.Folder, ["kind"] = stack.Kind });
        }

        var extensionArray = new JsonArray();
        foreach (var (id, node) in manifest["extensions"] as JsonObject ?? new JsonObject())
        {
            var file = Text((node as JsonObject)?["path"]) ?? "";
            var owner = stacks.FirstOrDefault(stack => file.StartsWith(stack.Folder + "/ext/", StringComparison.Ordinal))?.Id;
            Append(extensionArray, new JsonObject { ["id"] = id, ["path"] = file, ["stack"] = owner is null ? null : JsonValue.Create(owner) });
        }

        var layerArray = new JsonArray();
        foreach (var layer in layers)
        {
            Append(layerArray, new JsonObject { ["key"] = layer.Key, ["title"] = layer.Title, ["files"] = StringArray(layer.Files) });
        }

        return new JsonObject
        {
            ["version"] = Text(manifest["version"]) ?? "",
            ["pages"] = pageArray,
            ["stacks"] = stackArray,
            ["extensions"] = extensionArray,
            ["layers"] = layerArray,
            ["selections"] = selections,
        };
    }

    /// <summary>Lists the docs Markdown files, in ordinal path order.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <returns>The docs-relative file paths.</returns>
    static List<string> AllPages(string root)
    {
        var docs = Path.Combine(root, "docs");
        if (!Directory.Exists(docs))
        {
            return new List<string>();
        }

        return Directory.GetFiles(docs, "*.md", SearchOption.AllDirectories)
            .Select(file => Slash(Path.GetRelativePath(root, file)))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Reads one file under the release root as text with LF line endings.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="file">The root-relative file path.</param>
    /// <returns>The file text.</returns>
    static string ReadText(string root, string file) =>
        File.ReadAllText(Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar))).Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>Reads one page for the index: its title, sections and intent from its Markdown, with its frontmatter blanked.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="file">The docs-relative file path.</param>
    /// <returns>The indexed page.</returns>
    static IndexedPage ReadPage(string root, string file)
    {
        var blanked = BlankFrontmatter(ReadText(root, file));
        var reader = PageReader.Parse(blanked);
        var route = file["docs/".Length..^3];
        var intent = FirstParagraph(reader, blanked, "Intent");
        if (intent.Length == 0)
        {
            intent = FirstParagraph(reader, blanked, "Purpose");
        }

        if (intent.Length == 0)
        {
            intent = FirstParagraph(reader, blanked, "Name");
        }

        var title = reader.Headings.FirstOrDefault(heading => heading.Level == 1)?.Text ?? route;
        var sections = reader.Headings.Where(heading => heading.Level == 2).Select(heading => SectionTitle(heading.Text)).ToList();
        return new IndexedPage(file, route, StyleOf(file), route.Split('/')[0], title, intent, sections);
    }

    /// <summary>Classifies a docs page by its path: its style, as the site and the authoring standard name it.</summary>
    /// <param name="rel">The docs-relative file path.</param>
    /// <returns>The page style.</returns>
    static string StyleOf(string rel)
    {
        var segments = rel.Split('/');
        if (rel.EndsWith("/README.md", StringComparison.Ordinal))
        {
            return "index";
        }

        if (rel == "docs/reference/glossary.md")
        {
            return "glossary";
        }

        if (rel.Contains("/ext/", StringComparison.Ordinal))
        {
            return "extension";
        }

        if (rel.StartsWith("docs/intent/research/", StringComparison.Ordinal))
        {
            return "research";
        }

        if (rel.StartsWith("docs/intent/decisions/", StringComparison.Ordinal) || rel.StartsWith("docs/implementation/decisions/", StringComparison.Ordinal))
        {
            return "decision";
        }

        if (rel.StartsWith("docs/frontend/patterns/", StringComparison.Ordinal))
        {
            return "pattern";
        }

        return segments[1] switch
        {
            "tools" or "maintain" => "command",
            "reference" => "reference",
            "tutorial" => "tutorial",
            "guide" => "how-to",
            "analyzers" => segments.Length > 3 && segments[2] == "rules" ? "reference" : "how-to",
            "learn" or "example" => "explanation",
            _ => segments[^1] == "stack.md" ? "stack" : "standard",
        };
    }

    /// <summary>Orders the pages: the learn and example pages follow their index's links, and the rest keep path order.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="all">The pages in ordinal path order.</param>
    /// <returns>The pages in reading order.</returns>
    static List<IndexedPage> ReadingOrder(string root, List<IndexedPage> all)
    {
        var output = all;
        foreach (var area in new[] { "learn", "example" })
        {
            var indexFile = $"docs/{area}/README.md";
            if (!output.Any(page => page.FilePath == indexFile))
            {
                continue;
            }

            var listed = Regex.Matches(ReadText(root, indexFile), @"\]\(([a-z0-9-]+)\.md\)")
                .Select(match => $"docs/{area}/{match.Groups[1].Value}.md")
                .ToList();
            int Rank(IndexedPage page)
            {
                if (page.FilePath == indexFile)
                {
                    return -1;
                }

                var at = listed.IndexOf(page.FilePath);
                return at < 0 ? listed.Count : at;
            }

            var first = output.FindIndex(page => page.Area == area);
            var inArea = output.Where(page => page.Area == area).OrderBy(Rank).ToList();
            var rest = output.Where(page => page.Area != area).ToList();
            output = [.. rest.Take(first), .. inArea, .. rest.Skip(first)];
        }

        return output;
    }

    /// <summary>
    ///     Lists the layers the site publishes, in order: the always-applied areas, each shared folder and stack folder,
    ///     each pattern page, and each extension.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="pages">The pages in reading order.</param>
    /// <param name="stacks">The stacks the manifest ships.</param>
    /// <returns>The layers that hold at least one page.</returns>
    static List<PublishedLayer> PublishedLayers(string root, JsonObject manifest, IReadOnlyList<IndexedPage> pages, IReadOnlyList<Stack> stacks)
    {
        var coreAreas = new[] { "docs/core", "docs/workspace", "docs/quality" };
        IReadOnlyList<string> Direct(string folder) =>
            pages.Where(page => page.Style != "index" && DirectoryOf(page.FilePath) == folder).Select(page => page.FilePath).ToList();

        var layers = new List<PublishedLayer>
        {
            new("core", "Core, Workspace and Quality", coreAreas.SelectMany(Direct).ToList()),
            new("backend", "Backend Principles", Direct("docs/backend")),
            new("frontend", "Frontend Principles", Direct("docs/frontend")),
        };
        var groups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stack in stacks)
        {
            // The folders between the area root and the stack folder, such as docs/frontend/web.
            foreach (var folder in ChainFolders(stack.Folder, $"docs/{stack.Kind}").Reverse().Skip(1).SkipLast(1))
            {
                var key = folder["docs/".Length..];
                if (key is "backend" or "frontend" || !groups.Add(key))
                {
                    continue;
                }

                layers.Add(new PublishedLayer(key, $"{Capitalize(key.Split('/')[^1])} Shared Pages", Direct(folder)));
            }

            var stackFiles = new[] { stack.Folder }.Concat(CompanionFolders(root, stack.Folder)).SelectMany(Direct).ToList();
            layers.Add(new PublishedLayer(stack.Folder["docs/".Length..], $"{Capitalize(stack.Id)} Stack", stackFiles));
        }

        foreach (var page in pages.Where(page => page.Style == "pattern"))
        {
            layers.Add(new PublishedLayer($"patterns/{page.Route.Split('/')[^1]}", page.Title, [page.FilePath]));
        }

        foreach (var (id, node) in manifest["extensions"] as JsonObject ?? new JsonObject())
        {
            var file = Text((node as JsonObject)?["path"]) ?? "";
            var title = pages.FirstOrDefault(page => page.FilePath == file)?.Title ?? id;
            layers.Add(new PublishedLayer($"ext/{id}", title, [file]));
        }

        return layers.Where(layer => layer.Files.Count > 0).ToList();
    }

    /// <summary>
    ///     Lists the selections the site publishes: each shape with one backend stack when it names backend stacks and
    ///     zero or one frontend stack, kept when every stack's requires and pairsWith rules hold.
    /// </summary>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="stacks">The stacks the manifest ships.</param>
    /// <returns>The selections, in manifest order.</returns>
    static List<SelectionChoice> SelectionChoices(JsonObject manifest, IReadOnlyList<Stack> stacks)
    {
        var backends = stacks.Where(stack => stack.Kind == "backend").Select(stack => stack.Id).ToList();
        var frontends = stacks.Where(stack => stack.Kind == "frontend").Select(stack => stack.Id).ToList();
        var choices = new List<SelectionChoice>();
        foreach (var (shape, node) in manifest["shapes"] as JsonObject ?? new JsonObject())
        {
            var backendCount = (node as JsonObject)?["backendStacks"]?.GetValue<int>() ?? 0;
            var backendChoices = backendCount > 0 ? backends.Cast<string?>().ToList() : new List<string?> { null };
            foreach (var backend in backendChoices)
            {
                foreach (var frontend in new List<string?> { null }.Concat(frontends))
                {
                    var chosen = new List<string>();
                    if (backend is not null)
                    {
                        chosen.Add(backend);
                    }

                    if (frontend is not null)
                    {
                        chosen.Add(frontend);
                    }

                    if (chosen.Count == 0 || (shape != "server" && frontend is null))
                    {
                        continue;
                    }

                    if (chosen.All(id => StackFits(manifest, id, chosen, backend)))
                    {
                        choices.Add(new SelectionChoice(shape, chosen));
                    }
                }
            }
        }

        return choices;
    }

    /// <summary>Whether a selected stack's requires and pairsWith rules hold for the selection.</summary>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="id">The stack id.</param>
    /// <param name="chosen">The stacks the selection names.</param>
    /// <param name="backend">The selection's backend stack, or null.</param>
    /// <returns>True when the stack fits the selection.</returns>
    static bool StackFits(JsonObject manifest, string id, List<string> chosen, string? backend)
    {
        var entry = (manifest["stacks"] as JsonObject)?[id] as JsonObject;
        var pairs = entry?["pairsWith"];
        return Strings(entry?["requires"]).All(required => chosen.Contains(required))
            && (backend is null || pairs is null || Strings(pairs).Contains(backend));
    }

    /// <summary>Finds the first paragraph under one H2 section of a page.</summary>
    /// <param name="reader">The page read with Markdig.</param>
    /// <param name="text">The page text the reader parsed.</param>
    /// <param name="section">The section title to find.</param>
    /// <returns>The paragraph text on one line, or an empty string when the section has none.</returns>
    static string FirstParagraph(PageReader reader, string text, string section)
    {
        var inside = false;
        foreach (var block in reader.Document)
        {
            if (block is HeadingBlock { Level: 2 } heading)
            {
                if (inside)
                {
                    break;
                }

                inside = SectionTitle(HeadingText(reader.Lines[heading.Line])) == section;
            }
            else if (inside && block is ParagraphBlock)
            {
                var end = Math.Min(block.Span.End + 1, text.Length);
                return string.Join(" ", text[block.Span.Start..end].Split('\n').Select(line => line.Trim())).Trim();
            }
        }

        return "";
    }

    /// <summary>Blanks a leading frontmatter block, keeping its line breaks so line numbers do not move.</summary>
    /// <param name="text">The page text with LF line endings.</param>
    /// <returns>The text with the frontmatter replaced by line breaks.</returns>
    static string BlankFrontmatter(string text)
    {
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            return text;
        }

        var end = text.IndexOf("\n---\n", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return text;
        }

        var block = text[..(end + "\n---".Length)];
        return new string('\n', block.Count(character => character == '\n')) + text[block.Length..];
    }

    /// <summary>The text of an ATX heading line after its opening hashes.</summary>
    /// <param name="line">The heading line.</param>
    /// <returns>The heading text, trimmed.</returns>
    static string HeadingText(string line) => line.Trim().TrimStart('#').Trim();

    /// <summary>An H2 section title with a trailing <c>{#id}</c> dropped.</summary>
    /// <param name="raw">The heading text.</param>
    /// <returns>The section title.</returns>
    static string SectionTitle(string raw)
    {
        var id = raw.IndexOf("{#", StringComparison.Ordinal);
        return id > 0 && raw.EndsWith('}') ? raw[..id].Trim() : raw;
    }

    /// <summary>The folder part of a docs-relative file path.</summary>
    /// <param name="file">The docs-relative file path.</param>
    /// <returns>The folder, without a trailing slash.</returns>
    static string DirectoryOf(string file) => file[..file.LastIndexOf('/')];

    /// <summary>Capitalizes the first character of a word.</summary>
    /// <param name="value">The word.</param>
    /// <returns>The word with its first character upper case.</returns>
    static string Capitalize(string value) => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    /// <summary>Appends one node to a JSON array through the untyped overload, which keeps the trimming and AOT analyzers quiet.</summary>
    /// <param name="array">The array to append to.</param>
    /// <param name="node">The node to append.</param>
    static void Append(JsonArray array, JsonNode? node) => array.Add(node);

    /// <summary>Builds a JSON array of strings.</summary>
    /// <param name="values">The strings.</param>
    /// <returns>The JSON array, in the order given.</returns>
    static JsonArray StringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            Append(array, JsonValue.Create(value));
        }

        return array;
    }

    /// <summary>Reads a JSON node as text.</summary>
    /// <param name="node">The node to read.</param>
    /// <returns>The text, or null when the node holds no string.</returns>
    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Reads a JSON array as a list of strings.</summary>
    /// <param name="node">The node to read.</param>
    /// <returns>The strings in array order; empty when the node holds no array.</returns>
    static List<string> Strings(JsonNode? node) => node is JsonArray array
        ? array.Select(Text).Where(text => text is not null).Select(text => text!).ToList()
        : [];

    /// <summary>Writes a path with forward slashes.</summary>
    /// <param name="value">The path as the operating system writes it.</param>
    /// <returns>The path with forward slashes.</returns>
    static string Slash(string value) => value.Replace('\\', '/');
}
