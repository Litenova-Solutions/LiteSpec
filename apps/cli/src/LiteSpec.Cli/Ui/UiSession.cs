using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Schemas;

namespace LiteSpec.Ui;

/// <summary>One in-process validation run: the errors, digests, and manifest it collects.</summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/ui#use-the-product-profiles</follows>
/// <follows>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</follows>
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

    /// <summary>Gets the findings the checks reported.</summary>
    public List<Finding> Errors { get; } = [];

    /// <summary>Gets the source digests computed so far, by file.</summary>
    public Dictionary<string, string> Digests { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the parsed LiteSpec manifest.</summary>
    public JsonNode? Manifest { get; private set; }

    /// <summary>The LiteSpec pages the UI checks own departures against.</summary>
    static readonly HashSet<string> UiDeparturePages = new(StringComparer.Ordinal)
    {
        "frontend/ui", "frontend/components", "frontend/patterns",
        "frontend/web/ui", "frontend/web/components", "frontend/web/patterns",
    };

    /// <summary>An ISO calendar date.</summary>
    static readonly Regex ExpiryDate = new("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.Compiled);

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
    ///     Records one finding.
    /// </summary>
    /// <param name="code">The finding code.</param>
    /// <param name="message">What is wrong, in one sentence.</param>
    /// <param name="file">The file as a full path.</param>
    /// <param name="line">The 1-based line.</param>
    /// <param name="subject">The record id the finding is about, when there is one.</param>
    public void Add(string code, string message, string file, int line = 1, string? subject = null) =>
        Errors.Add(new Finding(code, message, file, line, subject));

    /// <summary>
    ///     Runs every check and collects the report.
    /// </summary>
    /// <returns>The collected report.</returns>
    public UiReport Execute()
    {
        var projectFile = ConsumerRoot.ProjectFile(Root);
        if (!File.Exists(projectFile))
        {
            return new UiReport(2, [new Finding("project-invalid", $"No .litespec/project.yaml at {Root}", projectFile, 1, null)], Root, null, 0, Array.Empty<UiFrontend>(), false, Array.Empty<UiFrontend>());
        }

        var project = ReadData(projectFile, "project-invalid");
        // The manifest comes from the release the tool carries, the way every other command finds it.
        var manifestFile = LiteSpecHome.Root is { } home ? Path.Combine(home, LiteSpecHome.ManifestFile) : null;
        Manifest = manifestFile is not null && File.Exists(manifestFile) ? ReadData(manifestFile, "ui-baseline-missing") : null;

        var frontends = JsonRead.Chain(project, "frontends").Node is JsonArray array
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

        var uiDepartures = ReadUiDepartures();

        // A departure carries an expiry date and stops being live once that date
        // passes. A departure with no expiry date is lasting and never expires. This
        // check is time dependent by design; every other check in this validator
        // depends only on repository files. Only departures whose covers name a UI
        // page are read here; the rest belong to the checks that enforce them.
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (var departure in uiDepartures)
        {
            if (!departure.Covers.Any(IsUiCover))
            {
                continue;
            }

            if (departure.ExpiresOn is not JsonValue expiresValue || !expiresValue.TryGetValue<string>(out var expiresOn) || !ExpiryDate.IsMatch(expiresOn))
            {
                continue;
            }

            if (string.CompareOrdinal(expiresOn, today) < 0)
            {
                Add("departure-expired", $"[{string.Join(", ", departure.Covers)}] expiresOn {expiresOn} has passed; renew the departure or close the gap", FilePath(departure.File));
            }
        }

        // A controlled frontend composes the shared package, so the workspace declares
        // it once. A frontend that composes no shared component carries no UI block.
        // (litespec/frontend/ui#select-one-visual-authority)
        // An additional registry passes both registry gates under a departure against
        // the pinned baseline provision. The expiry date on that record is checked
        // above; the gates below check its presence.
        var baselineOverride = uiDepartures.Any(departure => departure.Covers.Contains("litespec/frontend/ui#pin-the-baseline-in-the-shared-package"));
        var pkgValue = JsonRead.Chain(project, "ui", "package");
        var pkg = pkgValue.Found ? pkgValue.Node : null;
        var hasPkg = pkg is not null && JsonValues.IsTruthy(pkg);
        if (controlled.Count == 0 && hasPkg)
        {
            Add("ui-package-invalid", "ui.package is declared but no frontend declares a controlled platform (react, svelte)", projectFile);
        }
        UiSharedPackage? packageSummary = null;
        if (hasPkg)
        {
            var package = pkg!;
            var label = $"shared package '{JsonRead.ShowOr(JsonRead.Child(package, "name"), "(unnamed)")}'";
            if (!hasBaseline)
            {
                Add("ui-baseline-missing", $"{label}: the LiteSpec manifest has no {baselineKey}", projectFile);
            }

            var exports = ReadArray(package, "publicExports", label, "ui-package-invalid", projectFile);

            Unique(exports.Select(entry => new JsValue(true, entry)), $"{label}.publicExports", "ui-package-invalid", projectFile);

            var pathValue = JsonRead.Child(package, "path");
            var packageRoot = pathValue is { Node: JsonValue pathText } && pathText.TryGetValue<string>(out var pathRaw) && pathRaw.Length > 0
                ? FilePath(pathRaw)
                : null;
            if (packageRoot is not null && !FileWalk.Exists(packageRoot))
            {
                Add("ui-package-invalid", $"{label}: path does not exist '{JsonRead.Show(pathValue)}'", projectFile);
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
                    Add("ui-package-invalid", $"{label}: {key} must live inside the shared package root '{JsonRead.Show(pathValue)}'", projectFile);
                }
                else if (!FileWalk.Exists(candidate))
                {
                    Add("ui-package-invalid", $"{label}: {key} path does not exist '{JsonRead.Show(value)}'", projectFile);
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
                        Add("ui-package-invalid", $"{label}: {key} must live inside the shared package root '{JsonRead.Show(pathValue)}'", projectFile);
                    }
                    else if (!FileWalk.Exists(candidate))
                    {
                        Add("ui-package-invalid", $"{label}: {key} path does not exist '{JsonRead.Show(value)}'", projectFile);
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
                        new DesignContractChecks(this).ValidateDesignContract(package, packagePlatform, projectFile);
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

            var frontendPath = JsonRead.Child(frontend, "path");
            var frontendRoot = frontendPath is { Node: JsonValue rootValue } && rootValue.TryGetValue<string>(out var rootText) && rootText.Length > 0
                ? FilePath(rootText)
                : null;
            if (frontendRoot is not null && !FileWalk.Exists(frontendRoot))
            {
                Add("ui-frontend-invalid", $"{label}: frontend path does not exist '{JsonRead.Show(frontendPath)}'", projectFile);
                continue;
            }

            checkedCount++;
            var uiBlock = JsonRead.Child(frontend, "ui");
            var globalCss = uiBlock.Node is JsonObject ? JsonRead.Child(uiBlock.Node, "globalCss") : new JsValue(false, null);
            if (JsonRead.Truthy(globalCss) && globalCss.Node is JsonValue cssValue && cssValue.TryGetValue<string>(out var cssRelative))
            {
                var stylesheet = FilePath(cssRelative);
                if (!FileWalk.Exists(stylesheet))
                {
                    Add("ui-frontend-invalid", $"{label}: globalCss path does not exist '{JsonRead.Show(globalCss)}'", projectFile);
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

                            Add("ui-class-invalid", $"[litespec/frontend/web/ui#restrict-css-decisions] {ClassStringChecks.UtilityMessages[kind]} '{token}'", source);
                        });
                    }
                }

                foreach (var source in sources)
                {
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
                        Add("ui-stylesheet-invalid", "[litespec/frontend/web/ui#restrict-css-decisions] an inline style belongs in a shared component", source);
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
        else if (File.Exists(FilePath(CatalogChecks.DefaultCatalog)))
        {
            new CatalogChecks(this).ValidatePatternCatalog(CatalogChecks.DefaultCatalog);
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "LockPlatform reads an optional source lock and assumes React on any read or parse failure, so an unreadable lock selects the React baseline instead of crashing validation.")]
    string LockPlatform(JsonNode? project)
    {
        var sourceLock = JsonRead.Chain(project, "ui", "package", "sourceLock");
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

            var lockNode = DataFile.Read(FilePath(relative));
            return JsonRead.Child(lockNode, "generator") is { Node: JsonValue generator } && generator.TryGetValue<string>(out var name) && name == "shadcn-svelte"
                ? "svelte"
                : "react";
        }
        catch
        {
            return "react";
        }
    }

    /// <summary>Whether a covers item names a UI page: a provision on it, or the page itself.</summary>
    /// <param name="cover">The covers item: a provision ID, a page or a finding code.</param>
    /// <returns>True when the item names one of the UI pages.</returns>
    static bool IsUiCover(string cover)
    {
        if (!cover.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return false;
        }

        return UiDeparturePages.Contains(cover["litespec/".Length..].Split('#')[0]);
    }

    // A rule override is a departure record: .litespec/governance/departures/<name>.yaml
    // whose covers list names the provision it departs from, with the reason and the
    // expiry date. The consumer validator owns the record shape; this validator reads
    // the departures its own gates need and enforces the expiry date.
    /// <summary>
    ///     Every departure record the project carries.
    /// </summary>
    /// <returns>The departure records.</returns>
    List<UiDeparture> ReadUiDepartures()
    {
        var directory = FilePath(".litespec/governance/departures");
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var found = new List<UiDeparture>();
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if ((entry.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            {
                continue;
            }

            if (!RecordFile.IsRecordFile(entry.Name))
            {
                continue;
            }

            var raw = File.ReadAllText(entry.FullName);
            if (!RecordFile.TryParse(raw, out var root, out _, out _))
            {
                continue;
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || type.GetString() != "departure")
            {
                continue;
            }

            if (!root.TryGetProperty("covers", out var covers) || covers.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var names = covers.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!)
                .ToList();

            JsonNode? expiresOn = null;
            if (root.TryGetProperty("expiresOn", out var expires))
            {
                expiresOn = JsonNode.Parse(expires.GetRawText());
            }

            found.Add(new UiDeparture(PathText.Relative(Root, Path.GetFullPath(entry.FullName)), names, expiresOn));
        }

        return found;
    }

    /// <summary>
    ///     Reads and parses a YAML or JSON data file, recording a finding when it cannot be parsed.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="code">The finding code.</param>
    /// <returns>The parsed value, or null.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "ReadData records any read or parse failure of the file as a finding and returns null, so malformed consumer input reports instead of crashing validation.")]
    public JsonNode? ReadData(string file, string code)
    {
        try
        {
            return DataFile.Read(file);
        }
        catch (Exception cause)
        {
            Add(code, $"cannot parse YAML or JSON ({cause.Message})", file);
            return null;
        }
    }

    /// <summary>
    ///     How a label reads inside a message: a label naming the finding's own file
    ///     is dropped, because the finding carries its path already.
    /// </summary>
    /// <param name="label">The label the message would name.</param>
    /// <param name="file">The finding file as a full path.</param>
    /// <param name="message">The message without its label.</param>
    /// <returns>The message with its label, unless the label is the file.</returns>
    string Labelled(string label, string file, string message) =>
        label == PathText.Relative(Root, Path.GetFullPath(file)) ? message : $"{label}: {message}";

    /// <summary>
    ///     Requires one property of a value, recording a finding when it is absent.
    /// </summary>
    /// <param name="node">The JSON value holding the property.</param>
    /// <param name="key">The property name.</param>
    /// <param name="label">The label the finding names.</param>
    /// <param name="code">The finding code.</param>
    /// <param name="file">The finding file as a full path.</param>
    /// <returns>True when the property is present.</returns>
    public bool Required(JsonNode? node, string key, string label, string code, string file)
    {
        if (node is null || !JsonValues.IsTruthy(node))
        {
            Add(code, Labelled(label, file, $"missing '{key}'"), file);
            return false;
        }

        var value = JsonRead.Child(node, key);
        if (!value.Found || value.Node is null || (value.Node is JsonValue text && text.TryGetValue<string>(out var raw) && raw.Length == 0))
        {
            Add(code, Labelled(label, file, $"missing '{key}'"), file);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Reads one array property, recording a finding when it is no array.
    /// </summary>
    /// <param name="node">The JSON value holding the property.</param>
    /// <param name="key">The property name.</param>
    /// <param name="label">The label the finding names.</param>
    /// <param name="code">The finding code.</param>
    /// <param name="file">The finding file as a full path.</param>
    /// <returns>The array entries.</returns>
    public List<JsonNode?> ReadArray(JsonNode? node, string key, string label, string code, string file)
    {
        if (node is JsonObject found && found.TryGetPropertyValue(key, out var value) && value is JsonArray array)
        {
            return array.ToList();
        }

        Add(code, Labelled(label, file, $"'{key}' must be an array"), file);
        return [];
    }

    /// <summary>
    ///     Records a finding for every repeated value.
    /// </summary>
    /// <param name="values">The values to check.</param>
    /// <param name="label">The label the finding names.</param>
    /// <param name="code">The finding code.</param>
    /// <param name="file">The finding file as a full path.</param>
    public void Unique(IEnumerable<JsValue> values, string label, string code, string file)
    {
        var seen = new List<JsValue>();
        foreach (var value in values)
        {
            if (seen.Any(candidate => JsonRead.StrictEqual(candidate, value)))
            {
                Add(code, Labelled(label, file, $"duplicate '{JsonRead.Show(value)}'"), file);
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

        var value = SourceDigest.Of(key);
        if (value is null)
        {
            return null;
        }

        Digests[key] = value;
        return value;
    }

    // A value read against one of the shipped schemas. The keyword gate runs first,
    // so a schema carrying a keyword outside the draft 2020-12 vocabulary fails
    // rather than passing unread.
    /// <summary>
    ///     Validates one value against a shipped schema.
    /// </summary>
    /// <param name="schemaName">The schema file name.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="label">The validated file, relative to the consumer root.</param>
    /// <param name="provisionId">The provision the findings cite.</param>
    /// <param name="code">The finding code.</param>
    /// <returns>True when the value is valid.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "SchemaCheck records any failure of schema validation as a finding for the value, so an unsupported schema or malformed value reports instead of crashing validation.")]
    public bool SchemaCheck(string schemaName, JsonNode? value, string label, string provisionId, string code)
    {
        var file = LiteSpecHome.PathTo($"schemas/{schemaName}");
        if (!File.Exists(file))
        {
            Add(code, $"schema '{schemaName}' is missing from the LiteSpec release", FilePath(label));
            return false;
        }

        var schema = ReadData(file, code);
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
            var prefix = label + ": ";
            var message = problem.StartsWith(prefix, StringComparison.Ordinal) ? problem[prefix.Length..] : problem;
            Add(code, $"[{provisionId}] {message}", FilePath(label));
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
            Add("ui-design-contract-invalid", "unterminated metadata block", file);
            return null;
        }

        try
        {
            return RecordFile.NodeOf(YamlText.Parse(raw.Substring(3, end - 3).Trim('\r', '\n')));
        }
        catch (JsonTextException cause)
        {
            Add("ui-design-contract-invalid", $"metadata is not YAML ({cause.Message})", file);
            return null;
        }
    }
}
