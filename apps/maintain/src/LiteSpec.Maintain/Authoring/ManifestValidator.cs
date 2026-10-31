using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Schemas;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The manifest contract: references, folder-based stacks, shapes, extensions, and schema consumers.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class ManifestValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ManifestValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public ManifestValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    /// <summary>Reads a JSON node as a string, or null when it holds no string.</summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The string value, or null.</returns>
    public static string? JsonString(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Reads a JSON array as its string values.</summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The string values.</returns>
    public static List<string> JsonStrings(JsonNode? node) => node is JsonArray array
        ? array.Select(JsonString).Where(text => text is not null).Select(text => text!).ToList()
        : [];

    /// <summary>Renders a JSON node as the scalar text a message prints.</summary>
    /// <param name="node">The JSON node.</param>
    /// <returns>The rendered scalar.</returns>
    public static string RenderJsonScalar(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject => "[object Object]",
        _ => JsonString(node) ?? node.ToJsonString(),
    };

    /// <summary>Lists the backticked code spans in one line.</summary>
    /// <param name="line">The line to scan.</param>
    /// <returns>The code spans in the line.</returns>
    public static List<string> InlineCodes(string line)
    {
        var codes = new List<string>();
        var search = 0;
        while (true)
        {
            var open = line.IndexOf('`', search);
            if (open < 0)
            {
                return codes;
            }

            var close = line.IndexOf('`', open + 1);
            if (close < 0)
            {
                return codes;
            }

            if (close > open + 1)
            {
                codes.Add(line.Substring(open + 1, close - open - 1));
            }

            search = close + 1;
        }
    }

    /// <summary>Checks the manifest references, stacks, shapes, and extensions.</summary>
    /// <param name="activeIds">The active provision identifiers.</param>
    /// <param name="provisionsByPage">The provisions of each page, by relative path.</param>
    internal void CheckManifest(HashSet<string> activeIds, Dictionary<string, List<Provision>> provisionsByPage)
    {
        var root = Context.Root;
        var add = Context.Add;
        var manifestFile = Path.Combine(root, "litespec.manifest.json");
        if (!File.Exists(manifestFile))
        {
            return;
        }

        JsonNode? manifest;
        try
        {
            manifest = JsonNode.Parse(TrimBom(ReadText(manifestFile)));
        }
        catch (Exception exception)
        {
            add("litespec.manifest.json", 1, "MANIFEST_JSON", exception.Message);
            return;
        }

        var obj = manifest as JsonObject;
        var references = new List<string>();
        var agentsEntry = JsonString(obj?["agentsEntry"]);
        if (agentsEntry is not null)
        {
            references.Add(agentsEntry);
        }

        var stacks = obj?["stacks"] as JsonObject;
        var shapes = obj?["shapes"] as JsonObject;
        var extensions = obj?["extensions"] as JsonObject;
        if (extensions is not null)
        {
            foreach (var (_, extensionNode) in extensions)
            {
                var path = JsonString((extensionNode as JsonObject)?["path"]);
                if (path is not null)
                {
                    references.Add(path);
                }
            }
        }

        if (shapes is not null)
        {
            foreach (var (_, shapeNode) in shapes)
            {
                references.AddRange(JsonStrings((shapeNode as JsonObject)?["backendPages"] as JsonArray));
            }
        }

        foreach (var reference in references)
        {
            if (reference.Length == 0)
            {
                continue;
            }

            var parts = reference.Split('#');
            var relative = parts[0];
            var anchor = parts.Length > 1 ? parts[1] : "";
            var target = JoinRoot(root, relative);
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                add("litespec.manifest.json", 1, "MANIFEST_PATH", $"path '{relative}' does not exist");
                continue;
            }

            if (anchor.Length > 0 && File.Exists(target) && !PageReader.Parse(ReadText(target)).Anchors.Contains(anchor))
            {
                add("litespec.manifest.json", 1, "MANIFEST_ANCHOR", $"anchor '#{anchor}' does not exist in '{relative}'");
            }
        }

        if (obj is not null)
        {
            CheckStackFolders(stacks);
            CheckStackReferences(obj);
            CheckShapePages(shapes);
            CheckExtensionLocations(obj);
            CheckUnlistedExtensions(extensions);
            CheckLayerCoverage(stacks);
            CheckStrayPages();
        }

        CheckManifestExtensions(extensions, activeIds, provisionsByPage);
    }

    // A stack ships a folder of pages, so the folder names the stack's area and holds
    // its entry page. Anything else selects nothing.
    /// <summary>Checks that every stack folder holds its entry page.</summary>
    /// <param name="stacks">The manifest stacks object.</param>
    private void CheckStackFolders(JsonObject? stacks)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (stacks is null)
        {
            return;
        }

        foreach (var (id, stackNode) in stacks)
        {
            var folder = JsonString((stackNode as JsonObject)?["folder"]);
            if (folder is null || folder.Length == 0)
            {
                continue;
            }

            if (Layout.KindFor(folder).Length == 0)
            {
                add("litespec.manifest.json", 1, "STACK_FOLDER", $"stack '{id}' folder '{folder}' is not under 'docs/backend/' or 'docs/frontend/'");
                continue;
            }

            var trimmed = folder.TrimEnd('/');
            if (!Directory.Exists(JoinRoot(root, trimmed)))
            {
                add("litespec.manifest.json", 1, "STACK_FOLDER", $"stack '{id}' folder '{folder}' does not exist");
                continue;
            }

            if (!File.Exists(JoinRoot(root, trimmed + "/stack.md")))
            {
                add("litespec.manifest.json", 1, "STACK_FOLDER", $"stack '{id}' folder '{folder}' holds no 'stack.md'");
            }
        }
    }

    // requires and pairsWith name other shipped stacks, and a frontend stack only
    // pairs with a backend one. An unknown name selects nothing.
    /// <summary>Checks that stack requires and pairsWith references name shipped stacks.</summary>
    /// <param name="manifest">The manifest object.</param>
    private void CheckStackReferences(JsonObject manifest)
    {
        var add = Context.Add;
        var stacks = manifest["stacks"] as JsonObject;
        if (stacks is null)
        {
            return;
        }

        var kinds = Layout.Stacks(manifest).ToDictionary(stack => stack.Id, stack => stack.Kind, StringComparer.Ordinal);
        foreach (var (id, stackNode) in stacks)
        {
            var stack = stackNode as JsonObject;
            foreach (var required in JsonStrings(stack?["requires"]))
            {
                if (!stacks.ContainsKey(required))
                {
                    add("litespec.manifest.json", 1, "STACK_REFERENCE", $"stack '{id}' requires '{required}', which the manifest does not ship");
                }
            }

            foreach (var partner in JsonStrings(stack?["pairsWith"]))
            {
                if (!stacks.ContainsKey(partner))
                {
                    add("litespec.manifest.json", 1, "STACK_REFERENCE", $"stack '{id}' pairs with '{partner}', which the manifest does not ship");
                }
                else if (kinds.GetValueOrDefault(partner) == "frontend")
                {
                    add("litespec.manifest.json", 1, "STACK_REFERENCE", $"stack '{id}' pairs with '{partner}', which is a frontend stack");
                }
            }
        }
    }

    // A shape lists backend pages for projects without a backend stack. Only a page
    // directly in docs/backend/ can stand in for the backend layer.
    /// <summary>Checks that shape backend pages sit directly in docs/backend.</summary>
    /// <param name="shapes">The manifest shapes object.</param>
    private void CheckShapePages(JsonObject? shapes)
    {
        var add = Context.Add;
        if (shapes is null)
        {
            return;
        }

        const string backendPrefix = "docs/backend/";
        foreach (var (id, shapeNode) in shapes)
        {
            var pagesNode = (shapeNode as JsonObject)?["backendPages"] as JsonArray;
            if (pagesNode is null)
            {
                continue;
            }

            var pages = JsonStrings(pagesNode);
            if (pagesNode.All(node => JsonString(node) is not null)
                && new HashSet<string>(pages, StringComparer.Ordinal).Count != pagesNode.Count)
            {
                add("litespec.manifest.json", 1, "MANIFEST_DUPLICATE_PATH", $"shape '{id}' contains a duplicate backend page path");
            }

            foreach (var page in pages)
            {
                var rest = page.StartsWith(backendPrefix, StringComparison.Ordinal) ? page[backendPrefix.Length..] : null;
                if (rest is null || rest.Length <= ".md".Length || rest.Contains('/') || !rest.EndsWith(".md", StringComparison.Ordinal))
                {
                    add("litespec.manifest.json", 1, "SHAPE_PAGE", $"shape '{id}' backend page '{page}' is not a page directly in 'docs/backend/'");
                }
            }
        }
    }

    // An extension's path is its scope: docs/ext/ supports every stack, a stack
    // folder's ext/ supports that stack only. The file name is the extension id.
    /// <summary>Checks that extension paths name their scope folder.</summary>
    /// <param name="manifest">The manifest object.</param>
    private void CheckExtensionLocations(JsonObject manifest)
    {
        var add = Context.Add;
        var extensions = manifest["extensions"] as JsonObject;
        if (extensions is null)
        {
            return;
        }

        var folders = Layout.Stacks(manifest).Select(stack => stack.Folder.Replace('\\', '/').TrimEnd('/')).ToList();
        foreach (var (id, extensionNode) in extensions)
        {
            var path = JsonString((extensionNode as JsonObject)?["path"]);
            if (path is null || path.Length == 0)
            {
                continue;
            }

            var file = $"ext/{id}.md";
            if (path != $"docs/{file}" && !folders.Any(folder => folder.Length > 0 && path == $"{folder}/{file}"))
            {
                add("litespec.manifest.json", 1, "EXTENSION_LOCATION", $"extension '{id}' path '{path}' is not 'docs/ext/{id}.md' or '<stack folder>/ext/{id}.md'");
            }
        }
    }

    // Every page in an ext/ folder is selected through the manifest. A page no
    // extension lists applies to no project.
    /// <summary>Checks that every extension page is listed in the manifest.</summary>
    /// <param name="extensions">The manifest extensions object.</param>
    private void CheckUnlistedExtensions(JsonObject? extensions)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (extensions is null)
        {
            return;
        }

        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, extensionNode) in extensions)
        {
            if (JsonString((extensionNode as JsonObject)?["path"]) is { } path)
            {
                listed.Add(path);
            }
        }

        var docs = Path.Combine(root, "docs");
        if (!Directory.Exists(docs))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(docs, "*.md", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
        {
            var relative = Slash(Path.GetRelativePath(root, file));
            if (Path.GetFileName(relative) != "README.md"
                && relative.Contains("/ext/", StringComparison.Ordinal)
                && !listed.Contains(relative))
            {
                add(relative, 1, "EXTENSION_UNLISTED", "page is in an ext/ folder but no extension in litespec.manifest.json lists it");
            }
        }
    }

    // A backend or frontend page applies through a stack folder: the page's folder is
    // a stack folder, an ancestor of one, or a folder holding no stack below one of
    // those. Anything else no selection reaches.
    /// <summary>Checks that every layer page is reached by a stack selection.</summary>
    /// <param name="stacks">The manifest stacks object.</param>
    private void CheckLayerCoverage(JsonObject? stacks)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (stacks is null)
        {
            return;
        }

        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, stackNode) in stacks)
        {
            var folder = JsonString((stackNode as JsonObject)?["folder"]);
            var kind = folder is null ? "" : Layout.KindFor(folder);
            if (folder is null || folder.Length == 0 || kind.Length == 0)
            {
                continue;
            }

            var area = kind == "backend" ? "docs/backend" : "docs/frontend";
            var current = folder.Replace('\\', '/').TrimEnd('/');
            while (current.Length >= area.Length
                && current.StartsWith(area, StringComparison.Ordinal)
                && (current.Length == area.Length || current[area.Length] == '/'))
            {
                covered.Add(current);
                if (current.Length == area.Length)
                {
                    break;
                }

                current = current[..current.LastIndexOf('/')];
            }
        }

        foreach (var folder in covered.ToList())
        {
            covered.UnionWith(Layout.CompanionFolders(root, folder));
        }

        foreach (var area in new[] { "docs/backend", "docs/frontend" })
        {
            var directory = JoinRoot(root, area);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
            {
                var relative = Slash(Path.GetRelativePath(root, file));
                if (Path.GetFileName(relative) == "README.md" || relative.Contains("/ext/", StringComparison.Ordinal))
                {
                    continue;
                }

                var folder = Slash(Path.GetDirectoryName(relative) ?? "");
                if (!covered.Contains(folder))
                {
                    add(relative, 1, "LAYER_UNREACHABLE", "page is applied by no stack selection in litespec.manifest.json");
                }
            }
        }
    }

    // Every page lives in a known area: the always-applied areas, a stack area, an
    // ext/ folder, or the guides and references no selection applies. Anything else
    // is a leftover path outside the tree.
    /// <summary>Gets the known documentation areas.</summary>
    private static readonly string[] KnownAreas =
    [
        "docs/core/", "docs/workspace/", "docs/quality/", "docs/ext/", "docs/backend/",
        "docs/frontend/", "docs/guide/", "docs/learn/", "docs/reference/", "docs/tools/", "docs/maintain/", "docs/tutorial/",
        "docs/analyzers/", "docs/example/", "docs/intent/", "docs/implementation/",
    ];

    /// <summary>Checks that every page lives in a known documentation area.</summary>
    private void CheckStrayPages()
    {
        var root = Context.Root;
        var add = Context.Add;
        var docs = Path.Combine(root, "docs");
        if (!Directory.Exists(docs))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(docs, "*.md", SearchOption.AllDirectories).OrderBy(name => name, StringComparer.Ordinal))
        {
            var relative = Slash(Path.GetRelativePath(root, file));
            if (Path.GetFileName(relative) == "README.md" || KnownAreas.Any(area => relative.StartsWith(area, StringComparison.Ordinal)))
            {
                continue;
            }

            add(relative, 1, "LAYER_UNLISTED", "page is in none of the known docs areas");
        }
    }

    /// <summary>Checks the activation, baseline, dependencies, and replacements of every manifest extension.</summary>
    /// <param name="extensions">The manifest extensions object.</param>
    /// <param name="activeIds">The active provision identifiers.</param>
    /// <param name="provisionsByPage">The provisions of each page, by relative path.</param>
    private void CheckManifestExtensions(JsonObject? extensions, HashSet<string> activeIds, Dictionary<string, List<Provision>> provisionsByPage)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (extensions is null)
        {
            return;
        }

        foreach (var (id, extensionNode) in extensions)
        {
            var ext = extensionNode as JsonObject;
            var extensionPath = JsonString(ext?["path"]);
            if (extensionPath is null)
            {
                continue;
            }

            var file = JoinRoot(root, extensionPath);
            if (!File.Exists(file))
            {
                continue;
            }

            var reader = PageReader.Parse(ReadText(file));
            var activation = PageValidator.SectionBody(reader, "Activation");
            var baseline = PageValidator.SectionBody(reader, "Baseline Relationship");
            var dependencies = PageValidator.SectionBody(reader, "Dependencies");
            var scope = JsonString(ext?["activationScope"]) ?? "undefined";
            if (!activation.Contains($"Activation scope: `{scope}`.", StringComparison.Ordinal))
            {
                add(extensionPath, 1, "EXTENSION_SCOPE", $"extension '{id}' does not state activation scope '{scope}'");
            }

            var kindLine = SplitLines(activation).FirstOrDefault(line => line.StartsWith("Applicable specification kinds:", StringComparison.Ordinal)) ?? "";
            var actualKinds = InlineCodes(kindLine);
            var expectedKinds = JsonStrings(ext?["applicableKinds"]);
            if (actualKinds.Count != expectedKinds.Count || actualKinds.Any(kind => !expectedKinds.Contains(kind)))
            {
                add(extensionPath, 1, "EXTENSION_KIND", $"extension '{id}' applicable kinds do not match the manifest");
            }

            var activationDetail = SplitLines(activation).Where(line =>
            {
                var trimmed = line.Trim();
                return trimmed.Length > 0
                    && !trimmed.StartsWith("Activation scope:", StringComparison.Ordinal)
                    && !trimmed.StartsWith("Applicable specification kinds:", StringComparison.Ordinal);
            }).ToList();
            if (activationDetail.Count == 0)
            {
                add(extensionPath, 1, "EXTENSION_ACTIVATION", $"extension '{id}' has no activation criterion");
            }

            if (baseline.Length == 0)
            {
                add(extensionPath, 1, "EXTENSION_BASELINE", $"extension '{id}' has an empty Baseline Relationship");
            }

            if (dependencies.Length == 0)
            {
                add(extensionPath, 1, "EXTENSION_DEPENDENCIES", $"extension '{id}' has an empty Dependencies section");
            }

            // A replacement is machine-readable: every replaced ID resolves to an
            // active provision, and the replacing extension states its own
            // provisions instead, so it owns no provision under a replaced ID.
            var replaces = ext?["replaces"] is JsonArray replacesNode
                ? replacesNode.Select(RenderJsonScalar).ToList()
                : new List<string>();
            var own = provisionsByPage.TryGetValue(extensionPath, out var owned) ? owned : new List<Provision>();
            foreach (var replaced in replaces)
            {
                if (!activeIds.Contains(replaced))
                {
                    add("litespec.manifest.json", 1, "EXTENSION_REPLACES_UNKNOWN", $"extension '{id}' replaces unknown provision '{replaced}'");
                }

                var provision = own.FirstOrDefault(item => item.Id == replaced);
                if (provision is not null)
                {
                    add(extensionPath, provision.Line, "EXTENSION_REPLACES_ENFORCED", $"extension '{id}' replaces '{replaced}' and still states it as its own provision");
                }
            }
        }
    }

    /// <summary>The published home every schema id names, followed by the release version.</summary>
    public const string SchemaHome = "https://litespec.dev/";

    // A schema id is the URL a consumer's editor fetches, so it names the release that
    // ships the schema: a pinned consumer keeps resolving the shape it reviewed.
    /// <summary>Checks that every schema id names its published home and release.</summary>
    internal void CheckSchemaIds()
    {
        var root = Context.Root;
        var add = Context.Add;
        var schemas = Path.Combine(root, "schemas");
        var manifest = Path.Combine(root, "litespec.manifest.json");
        if (!Directory.Exists(schemas) || !File.Exists(manifest))
        {
            return;
        }

        string? version;
        try
        {
            version = JsonNode.Parse(TrimBom(ReadText(manifest)))?["version"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
        }
        catch (Exception)
        {
            return;
        }

        if (version is null)
        {
            return;
        }

        foreach (var file in Directory.GetFiles(schemas, "*.json").OrderBy(name => name, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(file);
            var relative = $"schemas/{name}";
            var expected = $"{SchemaHome}{version}/schemas/{name}";
            string? id;
            try
            {
                id = JsonNode.Parse(TrimBom(ReadText(file)))?["$id"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
            }
            catch (Exception exception)
            {
                add(relative, 1, "SCHEMA_JSON", exception.Message);
                continue;
            }

            if (id is null)
            {
                add(relative, 1, "SCHEMA_ID", $"schema has no $id; it must be '{expected}'");
            }
            else if (id != expected)
            {
                add(relative, 1, "SCHEMA_ID", $"schema $id '{id}' must be '{expected}'");
            }
        }
    }

    /// <summary>Validates one shipped value against one shipped schema.</summary>
    /// <param name="schemaRelative">The schema path, relative to the root.</param>
    /// <param name="valueRelative">The value path, relative to the root.</param>
    internal void ValidateSchemaConsumer(string schemaRelative, string valueRelative)
    {
        var root = Context.Root;
        var add = Context.Add;
        var schemaFile = JoinRoot(root, schemaRelative);
        var valueFile = JoinRoot(root, valueRelative);
        if (!File.Exists(schemaFile) || !File.Exists(valueFile))
        {
            return;
        }

        JsonNode? schema;
        JsonNode? value;
        try
        {
            schema = JsonNode.Parse(TrimBom(ReadText(schemaFile)));
            value = JsonNode.Parse(TrimBom(ReadText(valueFile)));
        }
        catch (Exception exception)
        {
            add(valueRelative, 1, "SCHEMA_JSON", exception.Message);
            return;
        }

        JsonSchema.UnsupportedKeywords(schema, schemaRelative, "#", add);
        var errors = new List<string>();
        try
        {
            JsonSchema.Validate(value, schema, schema!, valueRelative, errors);
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        foreach (var error in errors)
        {
            add(valueRelative, 1, "SCHEMA_INVALID", error);
        }
    }

    // The manifest pins the analyzer package a consumer installs, so the pin names
    // the release the stack pages promise. A pin behind the project ships none of
    // the rules the pages cite, and a citation of a rule the pinned release does
    // not ship promises a check the consumer never runs.
    /// <summary>Checks the analyzer pin against the project version and the shipped rules.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckAnalyzerRelease(List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        var pin = ReadAnalyzerPin();
        if (pin is null)
        {
            return;
        }

        var projectVersion = ReadAnalyzerProjectVersion();
        if (projectVersion is not null && pin != projectVersion)
        {
            add("litespec.manifest.json", 1, "MANIFEST_ANALYZER_PIN", $"analyzer pin '{pin}' differs from analyzer project version '{projectVersion}'");
        }

        var shipped = ReadShippedAnalyzerRules(pin);
        if (shipped is null)
        {
            return;
        }

        foreach (var page in parsedPages)
        {
            var prose = string.Join('\n', page.Reader.Lines.Select((line, index) =>
                page.Reader.FencedLines.Contains(index + 1) || page.Reader.GeneratedLines.Contains(index + 1) ? "" : line));
            foreach (Match match in AnalyzerRulePattern().Matches(prose))
            {
                if (!shipped.Contains(match.Value))
                {
                    add(page.Relative, LineNumber(prose, match.Index), "MANIFEST_ANALYZER_RULE", $"'{match.Value}' is cited but the pinned analyzer release '{pin}' does not ship it");
                }
            }
        }
    }

    /// <summary>Reads the pinned analyzer package version from the manifest, or null when there is none.</summary>
    /// <returns>The pinned version, or null.</returns>
    internal string? ReadAnalyzerPin()
    {
        var root = Context.Root;
        var manifestFile = JoinRoot(root, "litespec.manifest.json");
        if (!File.Exists(manifestFile))
        {
            return null;
        }

        try
        {
            var manifest = JsonNode.Parse(TrimBom(ReadText(manifestFile))) as JsonObject;
            var packages = manifest?["packages"] as JsonObject;
            var nuget = packages?["nuget"] as JsonObject;
            return JsonString(nuget?["LiteSpec.Analyzers"]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Reads the analyzer project version, or null when there is none.</summary>
    /// <returns>The project version, or null.</returns>
    private string? ReadAnalyzerProjectVersion()
    {
        var root = Context.Root;
        var projectFile = JoinRoot(root, "apps/analyzers/src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj");
        if (!File.Exists(projectFile))
        {
            return null;
        }

        var match = AnalyzerVersionPattern().Match(ReadText(projectFile));
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    // A shipped release lists its new rules and its removals, so the pinned
    // release ships every new rule at or before its version minus every removal
    // at or before it. A version that parses compares numerically; anything else
    // matches only its own release heading.
    /// <summary>Reads the analyzer rules shipped at or before a pinned release.</summary>
    /// <param name="pin">The pinned release version.</param>
    /// <returns>The shipped rule codes, or null when no shipped list exists.</returns>
    internal HashSet<string>? ReadShippedAnalyzerRules(string pin)
    {
        var root = Context.Root;
        var shippedFile = JoinRoot(root, "apps/analyzers/src/LiteSpec.Analyzers/AnalyzerReleases.Shipped.md");
        if (!File.Exists(shippedFile))
        {
            return null;
        }

        var added = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var removed = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var current = "";
        var mode = "";
        foreach (var line in SplitLines(ReadText(shippedFile)))
        {
            var release = AnalyzerReleasePattern().Match(line);
            if (release.Success)
            {
                current = release.Groups[1].Value.Trim();
                mode = "";
                continue;
            }

            if (line.StartsWith("### New Rules", StringComparison.Ordinal))
            {
                mode = "new";
                continue;
            }

            if (line.StartsWith("### Removed Rules", StringComparison.Ordinal))
            {
                mode = "removed";
                continue;
            }

            if (current.Length == 0 || mode.Length == 0)
            {
                continue;
            }

            var row = AnalyzerRuleRowPattern().Match(line);
            if (row.Success)
            {
                var table = mode == "new" ? added : removed;
                if (!table.TryGetValue(current, out var rules))
                {
                    table[current] = rules = new HashSet<string>(StringComparer.Ordinal);
                }

                rules.Add(row.Groups[1].Value);
            }
        }

        var shipped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (version, rules) in added)
        {
            if (ReleaseAtOrBefore(version, pin))
            {
                shipped.UnionWith(rules);
            }
        }

        foreach (var (version, rules) in removed)
        {
            if (ReleaseAtOrBefore(version, pin))
            {
                shipped.ExceptWith(rules);
            }
        }

        return shipped;
    }

    /// <summary>Reports whether a release version is at or before a pinned version.</summary>
    /// <param name="release">The release version.</param>
    /// <param name="pin">The pinned version.</param>
    /// <returns>True when the release is at or before the pin.</returns>
    private bool ReleaseAtOrBefore(string release, string pin)
    {
        if (System.Version.TryParse(release, out var releaseVersion) && System.Version.TryParse(pin, out var pinVersion))
        {
            return releaseVersion <= pinVersion;
        }

        return release == pin;
    }

    /// <summary>Gets the pattern that matches an analyzer rule citation.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\bLS\d{4}\b")] private static partial Regex AnalyzerRulePattern();

    /// <summary>Gets the pattern that captures a project version.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"<Version>([^<]+)</Version>")] private static partial Regex AnalyzerVersionPattern();

    /// <summary>Gets the pattern that captures a shipped release heading.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^## Release (\S+)")] private static partial Regex AnalyzerReleasePattern();

    /// <summary>Gets the pattern that captures a shipped rule row.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(LS\d{4})\s*\|")] private static partial Regex AnalyzerRuleRowPattern();
}
