using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Schemas;

namespace LiteSpec.Ui;

/// <summary>One in-process validation run: the errors, digests, and manifest it collects.</summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/ui#use-the-product-profiles</follows>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
sealed class UiSession
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UiSession"/> class.
    /// </summary>
    /// <param name="root">The consumer root to validate.</param>
    public UiSession(string root)
    {
        Root = Path.GetFullPath(root);
    }

    /// <summary>Gets the consumer root the run checks.</summary>
    public string Root { get; }

    /// <summary>Gets the problems the checks reported.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Gets the source digests computed so far, by file.</summary>
    public Dictionary<string, string> Digests { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the parsed LiteSpec manifest.</summary>
    public JsonNode? Manifest { get; private set; }

    /// <summary>The LiteSpec pages the UI checks own exceptions against.</summary>
    static readonly HashSet<string> UiExceptionPages = new(StringComparer.Ordinal)
    {
        "frontend/ui", "frontend/components", "frontend/patterns",
        "frontend/web/ui", "frontend/web/components", "frontend/web/patterns",
    };

    /// <summary>An ISO calendar date.</summary>
    static readonly Regex ReviewDate = new("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.Compiled);

    /// <summary>A line ending.</summary>
    static readonly Regex Newline = new("\\r\\n?", RegexOptions.Compiled);

    /// <summary>Spaces at a line end.</summary>
    static readonly Regex TrailingSpaces = new("[ \\t]+$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    ///     The platform a frontend declares, or null when it declares none.
    /// </summary>
    /// <param name="frontend">The declared frontend.</param>
    /// <returns>The declared platform.</returns>
    public static string? PlatformOf(JsonNode? frontend) =>
        JsonRead.Child(frontend, "platform") is { Node: JsonValue value } && value.TryGetValue<string>(out var platform)
            ? platform
            : null;

    /// <summary>
    ///     Runs every check and collects the report.
    /// </summary>
    /// <returns>The collected report.</returns>
    public UiReport Execute()
    {
        var projectFile = Path.Combine(Root, "litespec.project.json");
        if (!File.Exists(projectFile))
        {
            return new UiReport(2, new[] { $"No litespec.project.json at {Root}" }, Root, null, 0, Array.Empty<UiFrontend>(), false, Array.Empty<UiFrontend>());
        }

        var project = ReadJson(projectFile, "litespec.project.json");
        // The manifest comes from the release the tool carries, the way every other command finds it.
        var manifestFile = LiteSpecHome.Root is { } home ? Path.Combine(home, LiteSpecHome.ManifestFile) : null;
        Manifest = manifestFile is not null && File.Exists(manifestFile) ? ReadJson(manifestFile, RelativeToRoot(manifestFile)) : null;

        var frontends = JsonRead.Chain(project, "paths", "frontends").Node is JsonArray array
            ? array.ToList()
            : new List<JsonNode?>();
        var controlled = frontends.Where(frontend => PlatformOf(frontend) is "react" or "svelte").ToList();
        var skipped = frontends.Where(frontend => JsonRead.Truthy(JsonRead.Child(frontend, "platform")) && PlatformOf(frontend) is not ("react" or "svelte")).ToList();
        var checkedCount = 0;

        var packagePlatform = LockPlatform(project);
        var baselineKey = packagePlatform == "svelte" ? "uiBaselineSvelte" : "uiBaseline";
        var baseline = JsonRead.Child(Manifest, baselineKey);
        var baselineNode = baseline.Found ? baseline.Node : null;
        var hasBaseline = baselineNode is not null && JsonValues.IsTruthy(baselineNode);

        var uiExceptions = ReadUiExceptions(project);

        // An exception carries an expiry date and stops being valid once that date
        // passes. This check is time dependent by design; every other check in this
        // validator depends only on repository files. Only exceptions against the UI
        // pages are read here; the rest belong to the checks that enforce them.
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (var exception in uiExceptions)
        {
            // A provision ID is litespec/<page>#<slug>; the page decides whether the UI checks own it.
            var page = exception.Provision.StartsWith("litespec/", StringComparison.Ordinal)
                ? exception.Provision["litespec/".Length..].Split('#')[0]
                : "";
            if (!UiExceptionPages.Contains(page))
            {
                continue;
            }

            if (exception.ExpiresOn is not JsonValue expiresValue || !expiresValue.TryGetValue<string>(out var expiresOn) || !ReviewDate.IsMatch(expiresOn))
            {
                continue;
            }

            if (string.CompareOrdinal(expiresOn, today) < 0)
            {
                Errors.Add($"[{exception.Provision}] {exception.File}: expiresOn {expiresOn} has passed; renew the exception");
            }
        }

        // A controlled frontend composes the shared package, so the workspace declares
        // it once. A frontend that composes no shared component carries no UI block.
        // (litespec/frontend/ui#select-one-visual-authority)
        // An additional registry passes both registry gates under an exception against
        // the pinned baseline provision. The expiry date on that record is checked
        // above; the gates below check its presence.
        var baselineOverride = uiExceptions.Any(exception => exception.Provision == "litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package");
        var pkgValue = JsonRead.Chain(project, "paths", "uiPackage");
        var pkg = pkgValue.Found ? pkgValue.Node : null;
        var hasPkg = pkg is not null && JsonValues.IsTruthy(pkg);
        if (controlled.Count == 0 && hasPkg)
        {
            Errors.Add("litespec.project.json: paths.uiPackage is declared but no frontend declares a controlled platform (react, svelte)");
        }

        if (controlled.Count > 0 && !hasPkg)
        {
            Errors.Add($"[litespec/frontend/ui#select-one-visual-authority] litespec.project.json: {controlled.Count} controlled frontend(s) and no paths.uiPackage");
        }

        UiSharedPackage? packageSummary = null;
        if (hasPkg)
        {
            var package = pkg!;
            var label = $"shared package '{JsonRead.ShowOr(JsonRead.Child(package, "name"), "(unnamed)")}'";
            if (!hasBaseline)
            {
                Errors.Add($"{label}: the LiteSpec manifest has no {baselineKey}");
            }

            foreach (var key in new[] { "name", "path", "componentsJson", "primitives", "tokens", "sourceLock", "designContract" })
            {
                Required(package, key, label);
            }

            var exports = ReadArray(package, "publicExports", label);
            if (exports.Count == 0)
            {
                Errors.Add($"{label}: publicExports must contain at least one entry point");
            }

            Unique(exports.Select(entry => new JsValue(true, entry)), $"{label}.publicExports");

            var pathValue = JsonRead.Child(package, "path");
            var packageRoot = pathValue is { Node: JsonValue pathText } && pathText.TryGetValue<string>(out var pathRaw) && pathRaw.Length > 0
                ? FilePath(pathRaw)
                : null;
            if (packageRoot is not null && !FileWalk.Exists(packageRoot))
            {
                Errors.Add($"{label}: path does not exist '{JsonRead.Show(pathValue)}'");
            }

            foreach (var key in new[] { "componentsJson", "sourceLock", "designContract" })
            {
                var value = JsonRead.Child(package, key);
                if (!JsonRead.Truthy(value))
                {
                    continue;
                }

                if (value.Node is not JsonValue textValue || !textValue.TryGetValue<string>(out var relative))
                {
                    continue;
                }

                var candidate = FilePath(relative);
                if (packageRoot is not null && !FileWalk.Within(packageRoot, candidate))
                {
                    Errors.Add($"{label}: {key} must live inside the shared package root '{JsonRead.Show(pathValue)}'");
                }
                else if (!FileWalk.Exists(candidate))
                {
                    Errors.Add($"{label}: {key} path does not exist '{JsonRead.Show(value)}'");
                }
            }

            if (packageRoot is not null)
            {
                foreach (var key in new[] { "primitives", "tokens" })
                {
                    var value = JsonRead.Child(package, key);
                    if (!JsonRead.Truthy(value))
                    {
                        continue;
                    }

                    if (value.Node is not JsonValue textValue || !textValue.TryGetValue<string>(out var relative))
                    {
                        continue;
                    }

                    var candidate = FilePath(relative);
                    if (!FileWalk.Within(packageRoot, candidate))
                    {
                        Errors.Add($"{label}: {key} must live inside the shared package root '{JsonRead.Show(pathValue)}'");
                    }
                    else if (!FileWalk.Exists(candidate))
                    {
                        Errors.Add($"{label}: {key} path does not exist '{JsonRead.Show(value)}'");
                    }
                }

                if (hasBaseline)
                {
                    var componentsJson = JsonRead.Child(package, "componentsJson");
                    if (componentsJson.Node is JsonValue componentsValue && componentsValue.TryGetValue<string>(out var componentsRelative) && FileWalk.Exists(FilePath(componentsRelative)))
                    {
                        new PackageChecks(this).ValidateComponentsJson(FilePath(componentsRelative), baselineNode, baselineOverride, packagePlatform);
                    }

                    var sourceLock = JsonRead.Child(package, "sourceLock");
                    if (sourceLock.Node is JsonValue lockValue && lockValue.TryGetValue<string>(out var lockRelative) && FileWalk.Exists(FilePath(lockRelative)))
                    {
                        new SourceLockChecks(this).ValidateSourceLock(FilePath(lockRelative), baselineNode, packageRoot, Manifest, baselineOverride, packagePlatform);
                    }

                    if (JsonRead.Truthy(JsonRead.Child(package, "designContract")))
                    {
                        new DesignContractChecks(this).ValidateDesignContract(package, packagePlatform);
                    }

                    var tokens = JsonRead.Child(package, "tokens");
                    if (tokens.Node is JsonValue tokensValue && tokensValue.TryGetValue<string>(out var tokensRelative) && FileWalk.Exists(FilePath(tokensRelative)))
                    {
                        new StylesheetChecks(this).ValidateGlobalCss(FilePath(tokensRelative), packagePlatform);
                    }

                    if (tokens.Node is JsonValue sheetValue && sheetValue.TryGetValue<string>(out var sheetRelative) && sheetRelative.Length > 0)
                    {
                        new StylesheetChecks(this).ValidateSharedStylesheets(packageRoot, FilePath(sheetRelative), packagePlatform);
                    }

                    var primitives = JsonRead.Child(package, "primitives");
                    new SourceScanChecks(this).ValidateSourceScan(packageRoot, primitives.Node is JsonValue primitivesValue && primitivesValue.TryGetValue<string>(out var primitivesRelative) && primitivesRelative.Length > 0 ? Path.GetFullPath(FilePath(primitivesRelative)) : null, packagePlatform);
                }

                var name = JsonRead.Child(package, "name");
                var path = JsonRead.Child(package, "path");
                packageSummary = new UiSharedPackage(name.Found, name.Node, path.Found, path.Node, exports);
            }

            new DependencyBoundaryChecks(this).ValidateDependencyBoundary();
        }

        foreach (var frontend in controlled)
        {
            var label = $"frontend '{JsonRead.Show(JsonRead.Child(frontend, "name"))}'";
            var uiBlock = JsonRead.Child(frontend, "ui");
            if (!JsonRead.Truthy(uiBlock))
            {
                Errors.Add($"[litespec/frontend/ui#use-the-product-profiles] {label}: a controlled frontend requires a 'ui' block declaring its density profile");
                continue;
            }

            Required(uiBlock.Node, "profile", label);
            var profile = uiBlock.Node is JsonObject ? JsonRead.Child(uiBlock.Node, "profile") : new JsValue(false, null);
            if (profile.Node is not JsonValue profileValue || !profileValue.TryGetValue<string>(out var profileText) || (profileText != "public-light" && profileText != "application-balanced" && profileText != "admin-dense"))
            {
                Errors.Add($"[litespec/frontend/ui#use-the-product-profiles] {label}: profile must be one of public-light, application-balanced, admin-dense");
            }

            var frontendPath = JsonRead.Child(frontend, "path");
            var frontendRoot = frontendPath is { Node: JsonValue rootValue } && rootValue.TryGetValue<string>(out var rootText) && rootText.Length > 0
                ? FilePath(rootText)
                : null;
            if (frontendRoot is not null && !FileWalk.Exists(frontendRoot))
            {
                Errors.Add($"{label}: frontend path does not exist '{JsonRead.Show(frontendPath)}'");
                continue;
            }

            checkedCount++;
            var globalCss = uiBlock.Node is JsonObject ? JsonRead.Child(uiBlock.Node, "globalCss") : new JsValue(false, null);
            if (JsonRead.Truthy(globalCss) && globalCss.Node is JsonValue cssValue && cssValue.TryGetValue<string>(out var cssRelative))
            {
                var stylesheet = FilePath(cssRelative);
                if (!FileWalk.Exists(stylesheet))
                {
                    Errors.Add($"{label}: globalCss path does not exist '{JsonRead.Show(globalCss)}'");
                }
                else if (hasPkg)
                {
                    var packagePath = JsonRead.Child(pkg, "path");
                    new StylesheetChecks(this).ValidateFrontendStylesheet(stylesheet, packagePath.Node is JsonValue packageValue && packageValue.TryGetValue<string>(out var packageRelative) && packageRelative.Length > 0 ? FilePath(packageRelative) : null, pkg, PlatformOf(frontend)!);
                }
            }

            if (frontendRoot is not null)
            {
                var platform = PlatformOf(frontend)!;
                new ApplicationImportChecks(this).ValidateApplicationImports(frontendRoot, pkg is null ? "(undeclared)" : JsonRead.Show(JsonRead.Child(pkg, "name")), platform);
                var sources = FileWalk.Walk(frontendRoot, file => SourceFiles.SourceFile.IsMatch(file));
                foreach (var source in sources)
                {
                    var relative = RelativeToRoot(source);
                    if (SourceFiles.TestFile.IsMatch(source))
                    {
                        continue;
                    }

                    if (SourceFiles.TestDir.IsMatch(source))
                    {
                        continue;
                    }

                    if (!SourceFiles.VisualFile.IsMatch(source))
                    {
                        continue;
                    }

                    // A visual file lays itself out, and it is the one place in an application
                    // where class names belong. The utility rule still applies, so the drawing
                    // stays on semantic tokens and on no raw palette value.
                    var text = File.ReadAllText(source);
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var value in ClassStringChecks.ClassStrings(text))
                    {
                        ClassStringChecks.InspectClassString(value, (kind, token) =>
                        {
                            if (!seen.Add(kind + ":" + token))
                            {
                                return;
                            }

                            Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {relative}: {ClassStringChecks.UtilityMessages[kind]} '{token}'");
                        });
                    }
                }

                foreach (var source in sources)
                {
                    var relative = RelativeToRoot(source);
                    if (SourceFiles.TestFile.IsMatch(source))
                    {
                        continue;
                    }

                    if (SourceFiles.TestDir.IsMatch(source))
                    {
                        continue;
                    }

                    // A visual file lays itself out. Its class names are read above.
                    if (SourceFiles.VisualFile.IsMatch(source))
                    {
                        continue;
                    }

                    // React writes an inline style as an object, Svelte as an attribute, an
                    // expression, or a style directive. A `<style>` block opening tag carries
                    // none of those after its name, so element styles never match this test.
                    var inline = source.EndsWith(".svelte", StringComparison.Ordinal) ? SourceFiles.SvelteInlineStyle : SourceFiles.ReactInlineStyle;
                    if (inline.IsMatch(File.ReadAllText(source)))
                    {
                        Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {relative}: an inline style belongs in a shared component");
                    }
                }

                new RouteCoverageChecks(this).ValidateRouteSuites(label, frontendRoot, platform);
            }
        }

        // A workspace that records pattern decisions keeps one catalog beside its
        // design contract. A workspace without one reports nothing here.
        // (litespec/frontend/patterns#keep-one-pattern-catalog)
        var uiPatterns = JsonRead.Chain(project, "paths", "uiPatterns");
        if (JsonRead.Truthy(uiPatterns))
        {
            new CatalogChecks(this).ValidatePatternCatalog(JsonRead.Show(uiPatterns));
        }

        UiFrontend Describe(JsonNode? frontend)
        {
            var name = JsonRead.Child(frontend, "name");
            var platform = JsonRead.Child(frontend, "platform");
            return new UiFrontend(name.Found, name.Node, platform.Found, platform.Node);
        }

        return new UiReport(
            Errors.Count > 0 ? 1 : 0,
            Errors,
            Root,
            packageSummary,
            checkedCount,
            skipped.Select(Describe).ToList(),
            hasPkg,
            frontends.Select(Describe).ToList());
    }

    // The source lock declares which CLI installed the package, so the flavor is
    // read from the lock rather than inferred from the frontends. A workspace
    // mid-migration holds frontends on both platforms beside one lock, and the
    // package is validated against the baseline its own lock names. An unreadable
    // lock reports in its own gate; this read stays silent and assumes React.
    /// <summary>
    ///     The platform the source lock names, or React when it cannot be read.
    /// </summary>
    /// <param name="project">The parsed project record.</param>
    /// <returns>The platform name.</returns>
    string LockPlatform(JsonNode? project)
    {
        var sourceLock = JsonRead.Chain(project, "paths", "uiPackage", "sourceLock");
        if (!JsonRead.Truthy(sourceLock))
        {
            return "react";
        }

        try
        {
            if (sourceLock.Node is not JsonValue value || !value.TryGetValue<string>(out var relative))
            {
                return "react";
            }

            var lockNode = JsonNode.Parse(File.ReadAllText(FilePath(relative)));
            return JsonRead.Child(lockNode, "generator") is { Node: JsonValue generator } && generator.TryGetValue<string>(out var name) && name == "shadcn-svelte"
                ? "svelte"
                : "react";
        }
        catch
        {
            return "react";
        }
    }

    // A rule override is an Exception record: docs/governance/exceptions/<name>.md
    // carrying the provision it departs from, the reason, and the expiry date.
    // The consumer validator owns the record shape; this validator reads
    // the departure its own gates need and enforces the expiry date. The folder
    // moves with paths.records.exception when the consumer sets it.
    /// <summary>
    ///     Every UI exception record the project carries.
    /// </summary>
    /// <param name="project">The parsed project record.</param>
    /// <returns>The exception records.</returns>
    List<UiException> ReadUiExceptions(JsonNode? project)
    {
        var folder = JsonRead.Chain(project, "paths", "records", "exception");
        var folderName = folder is { Node: JsonValue value } && value.TryGetValue<string>(out var name) ? name : "docs/governance/exceptions";
        var directory = FilePath(folderName);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var found = new List<UiException>();
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if ((entry.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                continue;
            }

            if (!entry.Name.EndsWith(".md", StringComparison.Ordinal))
            {
                continue;
            }

            var raw = File.ReadAllText(entry.FullName);
            if (!raw.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var end = raw.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (end < 0)
            {
                continue;
            }

            JsonNode? meta;
            try
            {
                meta = JsonNode.Parse(raw.Substring(3, end - 3).Trim());
            }
            catch (JsonException)
            {
                continue;
            }

            if (meta is not JsonObject)
            {
                continue;
            }

            if (JsonRead.Child(meta, "type").Node is not JsonValue typeValue || !typeValue.TryGetValue<string>(out var type) || type != "exception")
            {
                continue;
            }

            if (JsonRead.Child(meta, "covers").Node is not JsonValue provisionValue || !provisionValue.TryGetValue<string>(out var provision))
            {
                continue;
            }

            found.Add(new UiException(RelativeToRoot(entry.FullName), provision, JsonRead.Child(meta, "expiresOn").Node));
        }

        return found;
    }

    /// <summary>
    ///     Reads and parses a JSON file, recording a problem when it cannot be parsed.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="label">The label the problem names.</param>
    /// <returns>The parsed JSON value, or null.</returns>
    public JsonNode? ReadJson(string file, string label)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(file));
        }
        catch (Exception cause)
        {
            Errors.Add($"{label}: cannot parse JSON ({cause.Message})");
            return null;
        }
    }

    /// <summary>
    ///     Requires one property of a value, recording a problem when it is absent.
    /// </summary>
    /// <param name="node">The JSON value holding the property.</param>
    /// <param name="key">The property name.</param>
    /// <param name="label">The label the problem names.</param>
    /// <returns>True when the property is present.</returns>
    public bool Required(JsonNode? node, string key, string label)
    {
        if (node is null || !JsonValues.IsTruthy(node))
        {
            Errors.Add($"{label}: missing '{key}'");
            return false;
        }

        var value = JsonRead.Child(node, key);
        if (!value.Found || value.Node is null || (value.Node is JsonValue text && text.TryGetValue<string>(out var raw) && raw.Length == 0))
        {
            Errors.Add($"{label}: missing '{key}'");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Reads one array property, recording a problem when it is no array.
    /// </summary>
    /// <param name="node">The JSON value holding the property.</param>
    /// <param name="key">The property name.</param>
    /// <param name="label">The label the problem names.</param>
    /// <returns>The array entries.</returns>
    public List<JsonNode?> ReadArray(JsonNode? node, string key, string label)
    {
        if (node is JsonObject found && found.TryGetPropertyValue(key, out var value) && value is JsonArray array)
        {
            return array.ToList();
        }

        Errors.Add($"{label}: '{key}' must be an array");
        return [];
    }

    /// <summary>
    ///     Records a problem for every repeated value.
    /// </summary>
    /// <param name="values">The values to check.</param>
    /// <param name="label">The label the problem names.</param>
    public void Unique(IEnumerable<JsValue> values, string label)
    {
        var seen = new List<JsValue>();
        foreach (var value in values)
        {
            if (seen.Any(candidate => JsonRead.StrictEqual(candidate, value)))
            {
                Errors.Add($"{label}: duplicate '{JsonRead.Show(value)}'");
            }

            seen.Add(value);
        }
    }

    /// <summary>
    ///     A path inside the consumer, from a recorded relative path.
    /// </summary>
    /// <param name="relative">The recorded relative path.</param>
    /// <returns>The full path.</returns>
    public string FilePath(string relative) => Path.GetFullPath(Path.Combine(Root, relative));

    /// <summary>
    ///     The path of a file relative to the consumer root.
    /// </summary>
    /// <param name="file">The file to make relative.</param>
    /// <returns>The forward-slash relative path.</returns>
    public string RelativeToRoot(string file) =>
        Path.GetRelativePath(Root, Path.GetFullPath(file)).Replace(Path.DirectorySeparatorChar, '/');

    // A source lock names one file in several component entries, and the walk that
    // collects source files reads each one again. Hashing is the expensive step, so
    // the digest is computed once per absolute path.
    /// <summary>
    ///     The recorded digest of one source file, or null when it cannot be read.
    /// </summary>
    /// <param name="file">The source file.</param>
    /// <returns>The digest, or null.</returns>
    public string? Digest(string file)
    {
        var key = Path.GetFullPath(file);
        if (Digests.TryGetValue(key, out var cached))
        {
            return cached;
        }

        string value;
        try
        {
            value = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeSource(File.ReadAllText(key)))));
        }
        catch (Exception cause) when (cause is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        Digests[key] = value;
        return value;
    }

    /// <summary>
    ///     Source text with uniform line endings and no trailing spaces.
    /// </summary>
    /// <param name="contents">The source text.</param>
    /// <returns>The normalized source.</returns>
    static string NormalizeSource(string contents) =>
        TrailingSpaces.Replace(Newline.Replace(contents, "\n"), "").TrimEnd() + "\n";

    // A value read against one of the shipped schemas. The keyword gate runs first,
    // so a schema carrying a keyword this evaluator does not implement fails rather
    // than passing unread.
    /// <summary>
    ///     Validates one value against a shipped schema.
    /// </summary>
    /// <param name="schemaName">The schema file name.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="label">The label the problems name.</param>
    /// <param name="provisionId">The provision the problems cite.</param>
    /// <returns>True when the value is valid.</returns>
    public bool SchemaCheck(string schemaName, JsonNode? value, string label, string provisionId)
    {
        var file = LiteSpecHome.PathTo($"schemas/{schemaName}");
        if (!File.Exists(file))
        {
            Errors.Add($"{label}: schema '{schemaName}' is missing from the LiteSpec release");
            return false;
        }

        var schema = ReadJson(file, schemaName);
        if (schema is null)
        {
            return false;
        }

        var problems = new List<string>();
        JsonSchema.UnsupportedKeywords(schema, schemaName, "#", (_, _, _, message) => problems.Add(message));
        try
        {
            JsonSchema.Validate(value, schema, schema, label, problems);
        }
        catch (Exception cause)
        {
            problems.Add($"{label}: {cause.Message}");
        }

        foreach (var problem in problems)
        {
            Errors.Add($"[{provisionId}] {problem}");
        }

        return problems.Count == 0;
    }

    /// <summary>
    ///     The metadata block of a Markdown file, or null when it carries none.
    /// </summary>
    /// <param name="file">The Markdown file.</param>
    /// <returns>The parsed metadata, or null.</returns>
    public JsonNode? ParseMetadata(string file)
    {
        var raw = File.ReadAllText(file);
        if (!raw.StartsWith("---", StringComparison.Ordinal))
        {
            return null;
        }

        var end = raw.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            Errors.Add($"{RelativeToRoot(file)}: unterminated metadata block");
            return null;
        }

        try
        {
            return JsonNode.Parse(raw.Substring(3, end - 3).Trim());
        }
        catch (JsonException cause)
        {
            Errors.Add($"{RelativeToRoot(file)}: metadata is not JSON ({cause.Message})");
            return null;
        }
    }
}
