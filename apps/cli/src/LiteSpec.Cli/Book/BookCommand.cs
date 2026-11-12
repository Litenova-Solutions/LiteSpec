using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Consumer;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The <c>litespec book</c> command: generate the project book, one offline
///     shell with sharded data, from a consumer's records.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec book [--root <folder>] [--out <folder>] [--editor <template>]

        Options:
          --root <folder>      The consumer root to read. Defaults to the consumer found from the working directory.
          --out <folder>       The folder to write the static site to. Defaults to `.litespec/book` under the consumer root.
          --editor <template>  The editor link template with {root}, {path} and {line}, or "none" to suppress links. Defaults to vscode://file/{root}/{path}:{line}.
          --help               Print this text and exit.

        Exit codes: 0 written, 1 unreadable input or unwritable output, 2 usage error, a missing carried release, or a missing or broken lexicon page.
        """;

    /// <summary>The default editor link template, with its three placeholders.</summary>
    public const string DefaultEditor = "vscode://file/{root}/{path}:{line}";

    /// <summary>The UTF-8 encoding without a byte order mark, used for the written book files.</summary>
    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>Runs the command with the arguments after <c>book</c>; returns the exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the root, output and editor options.</param>
    /// <returns>The process exit code: 0 when the book is written, 1 on unreadable input or unwritable output, and 2 on a usage error or a missing carried release or lexicon.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The book command is a last-resort command handler that reports any build, release lookup or write failure as an error message and an exit code instead of crashing.")]
    public static int Run(string[] args)
    {
        var parsed = ParseArgs(args);
        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Root, json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;

        string liteSpecRoot;
        try
        {
            liteSpecRoot = LiteSpecHome.RequireRoot();
        }
        catch (Exception exception)
        {
            return CommandFailure.Fail($"Cannot find the carried LiteSpec release: {exception.Message}", json: false, code: 2);
        }

        BookLexicon lexicon;
        try
        {
            lexicon = BookLexicon.Load(liteSpecRoot);
        }
        catch (BookLexiconException exception)
        {
            return CommandFailure.Fail($"Cannot load the book words from '{exception.File}': {exception.Message}", json: false, code: 2);
        }

        ProjectModel model;
        try
        {
            model = ModelBuilder.Build(config);
        }
        catch (Exception exception)
        {
            return CommandFailure.Fail($"Cannot build the model from '{root}': {exception.Message}", json: false, code: 1);
        }

        // The book takes the findings that model check takes, under the layer claims, so the two never disagree.
        List<Finding> findings;
        IReadOnlyList<Finding> covered;

        try
        {
            (findings, covered) = ModelCommand.Collect(model, root);
        }
        catch (FailFast fail)
        {
            return CommandFailure.Fail(fail.Message, json: false, code: fail.Code);
        }

        model.FindingsJson = new JsonArray([.. findings.Select(finding => (JsonNode?)ModelCommand.AsJson(config, finding, ModelCommand.Decide(model, config, finding, release: false)))]);
        model.CoveredJson = new JsonArray([.. covered.Select(finding => (JsonNode?)ModelCommand.AsCoveredJson(config, finding))]);

        if (JsonNode.Parse(model.ToJson()) is not JsonObject store || store["records"] is not JsonArray)
        {
            return CommandFailure.Fail("The built model holds no records array.", json: false, code: 1);
        }

        store["intentCoverage"] = BookIntentCoverage.ToJson(IntentCoverage.Compute(model));

        // The book groups decisions by the concerns the one classification gives
        // their followers, computed once here so the client reads one field.
        BookDecisionDetail.AddDecisionConcerns(store, config);

        var editor = parsed.Editor ?? DefaultEditor;
        var outDir = parsed.Out ?? Path.Combine(root, ConsumerRoot.FolderName, "book");

        try
        {
            var line = WriteBook(store, root, lexicon, liteSpecRoot, editor, outDir, config.Layers);
            CliHost.Out.Write(line + "\n");
            return 0;
        }
        catch (Exception exception)
        {
            return CommandFailure.Fail($"Cannot write to '{outDir}': {exception.Message}", json: false, code: 1);
        }
    }

    /// <summary>
    ///     Writes the book for a parsed model with every layer claimed: one
    ///     shell with sharded data under the output folder.
    ///     The command calls it with the model it built in memory; tests call it with a frozen model.
    /// </summary>
    /// <param name="store">The model object that holds the records array.</param>
    /// <param name="root">The absolute consumer root for source links and captures.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="liteSpecRoot">The absolute path of the carried LiteSpec release.</param>
    /// <param name="editor">The editor link template with its root, path and line placeholders.</param>
    /// <param name="outDir">The folder to write the static site to.</param>
    /// <returns>The console line naming the output folder, the record count, the shard count and the unplaced ids.</returns>
    internal static string WriteBook(JsonObject store, string root, BookLexicon lexicon, string liteSpecRoot, string editor, string outDir) =>
        WriteBook(store, root, lexicon, liteSpecRoot, editor, outDir, LayerClaims.All);

    /// <summary>
    ///     Writes the book for a parsed model under the layers the project claims: one
    ///     shell with sharded data under the output folder. A layer the project does
    ///     not claim shows as not claimed, and the book leaves out its code, proof and
    ///     level S findings. At every claim the manifest and the Intent part carry the
    ///     computed Intent coverage.
    /// </summary>
    /// <param name="store">The model object that holds the records array.</param>
    /// <param name="root">The absolute consumer root for source links and captures.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="liteSpecRoot">The absolute path of the carried LiteSpec release.</param>
    /// <param name="editor">The editor link template with its root, path and line placeholders.</param>
    /// <param name="outDir">The folder to write the static site to.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <returns>The console line naming the output folder, the record count, the shard count and the unplaced ids.</returns>
    internal static string WriteBook(JsonObject store, string root, BookLexicon lexicon, string liteSpecRoot, string editor, string outDir, LayerClaims claims)
    {
        // One pass over the one index: the outline and the details are each
        // built once, then packed into shards. Nothing is recomputed per record.
        var coverage = BookIntentCoverage.Of(store);
        var index = BookIndexBuilder.BuildIndex(store);
        var outline = BookOutline.Build(store, claims);
        var details = BookRecordDetails.Build(store, outline, claims);
        var generatedOn = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        var dataDir = Path.Combine(outDir, "data");
        var shards = 0;

        CopyCaptures(outDir, root, details);

        // Records first: the outline nodes and the manifest map carry the shard ids.
        var shardOf = BookShards.WriteRecords(dataDir, details);
        shards += shardOf.Values.Distinct(StringComparer.Ordinal).Count();

        var layers = new JsonArray();
        foreach (var layerRoot in outline.Layers)
        {
            var keys = BookShards.WriteLayer(dataDir, layerRoot.Id, layerRoot.Children, shardOf);
            shards += keys.Count;
            layers.Add((JsonNode)new JsonObject
            {
                ["key"] = layerRoot.Id,
                ["label"] = LayerLabel(lexicon, layerRoot.Id),
                ["outline"] = new JsonArray([.. keys.Select(key => (JsonNode?)key)]),
            });
        }

        var searchKeys = BookShards.WriteSearch(dataDir, SearchRows(index, details));
        shards += searchKeys.Count;

        BookShards.WriteShard(dataDir, "lexicon", lexicon.ToJson());
        shards += 1;
        BookShards.WriteShard(dataDir, "security", SecurityJson(details));
        shards += 1;
        BookShards.WriteShard(
            dataDir,
            "manifest",
            ManifestJson(store, outline, layers, shardOf, searchKeys, root, liteSpecRoot, editor, generatedOn, claims, coverage));
        shards += 1;

        var project = ProjectOf(store);
        var totals = TotalsOf(store);
        var title = project.Title.Length > 0 ? project.Title + " Book" : "Project book";
        var html = BookShell.Render(title, BookShell.Noscript(project.Title, totals, coverage));
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "index.html"), html + "\n", Utf8NoBom);

        Directory.CreateDirectory(Path.Combine(outDir, "assets"));
        WriteAsset(outDir, "book.css");
        WriteAsset(outDir, "book.js");

        var line = $"{outDir}: {details.Count} records, {shards} shards.";
        if (outline.Unplaced.Count > 0)
        {
            line += $" Unplaced (record-parent-missing): {string.Join(", ", outline.Unplaced)}.";
        }

        return line;
    }

    /// <summary>The parsed book command arguments: the help flag, the usage error, and the output, root and editor values, each null when absent.</summary>
    sealed record Parsed(bool Help, string? Error, string? Out, string? Root, string? Editor);

    /// <summary>Parses the book command arguments into their named values and any usage error.</summary>
    /// <param name="argv">The command arguments after the command name.</param>
    /// <returns>The parsed arguments, with the help flag or the usage error set when the input calls for it.</returns>
    static Parsed ParseArgs(string[] argv)
    {
        var parsed = CliArgs.Parse(argv, [], ["--out", "--root", "--editor"]);

        if (parsed.Help)
        {
            return new Parsed(true, null, null, null, null);
        }

        if (parsed.Error is not null)
        {
            return new Parsed(false, parsed.Error, null, null, null);
        }

        return new Parsed(false, null, parsed.Value("--out"), parsed.Value("--root"), parsed.Value("--editor"));
    }

    /// <summary>One layer's display label from the lexicon, else the layer key itself.</summary>
    /// <param name="lexicon">The book lexicon that holds the layer labels.</param>
    /// <param name="key">The layer key whose label is sought.</param>
    /// <returns>The layer display label from the lexicon, or the layer key itself when the lexicon holds no label for it.</returns>
    static string LayerLabel(BookLexicon lexicon, string key) =>
        lexicon.Layers.TryGetValue(key, out var layer) ? layer.Label : key;

    /// <summary>The product id and title, or empty strings when the model holds no product.</summary>
    /// <param name="store">The model object that holds the records array.</param>
    /// <returns>The product identifier and title, or empty strings when the model holds no product record.</returns>
    static (string Id, string Title) ProjectOf(JsonObject store)
    {
        foreach (var record in store["records"] as JsonArray ?? [])
        {
            if (record is JsonObject entry
                && string.Equals(StrProp(entry, "type"), "product", StringComparison.Ordinal))
            {
                return (StrProp(entry, "id") ?? "", StrProp(entry, "title") ?? "");
            }
        }

        return ("", "");
    }

    /// <summary>The record count per record type.</summary>
    /// <param name="store">The model object that holds the records array.</param>
    /// <returns>The record count for each record type present in the model.</returns>
    static Dictionary<string, int> TotalsOf(JsonObject store)
    {
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var record in store["records"] as JsonArray ?? [])
        {
            if (record is JsonObject entry && StrProp(entry, "type") is { Length: > 0 } type)
            {
                totals[type] = totals.TryGetValue(type, out var count) ? count + 1 : 1;
            }
        }

        return totals;
    }

    /// <summary>The search rows in identifier order: id, title, type and excerpt each, with the folder path and module appended when present.</summary>
    /// <param name="index">The model index that holds the raw record objects.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <returns>The search rows in identifier order, each holding identifier, title, type and excerpt.</returns>
    static JsonArray SearchRows(BookIndex index, IReadOnlyDictionary<string, JsonObject> details)
    {
        var rows = new JsonArray();
        foreach (var id in details.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var detail = details[id];
            var raw = index.ById.TryGetValue(id, out var found) ? found : detail;
            var excerpt = BookShards.SearchExcerpt(raw);
            var placement = string.Join(" ", new[] { StrProp(detail, "folder"), StrProp(detail, "module") }
                .Where(part => part is { Length: > 0 }));
            var text = placement.Length > 0
                ? (excerpt.Length > 0 ? excerpt + " " + placement : placement)
                : excerpt;
            rows.Add((JsonNode)new JsonArray(
                id,
                StrProp(detail, "title") ?? id,
                StrProp(detail, "type") ?? "",
                text));
        }

        return rows;
    }

    /// <summary>The layer claims as the project file writes them: one entry per claimed layer, in layer order.</summary>
    /// <param name="claims">The record layers the project claims.</param>
    /// <returns>The entries, each with its layer and its practice.</returns>
    static JsonArray ClaimsJson(LayerClaims claims) =>
        new JsonArray([.. claims.Entries().Select(entry => (JsonNode?)new JsonObject
        {
            ["layer"] = entry.Key,
            ["practice"] = entry.Value,
        })]);

    /// <summary>
    ///     The manifest shard value: project, roots, editor template, test run,
    ///     totals, layers with their outline shards, the shard map, the search
    ///     shards, the unplaced ids, the generation date, the layer
    ///     claims and the Intent coverage. Without a Proof claim the test run
    ///     is null, because the book leaves out proof.
    /// </summary>
    /// <param name="store">The model object that holds the records and the test run.</param>
    /// <param name="outline">The book outline that holds the unplaced identifiers.</param>
    /// <param name="layers">The layer entries for the manifest, each holding its outline shard keys.</param>
    /// <param name="shardOf">The shard identifier for each record identifier.</param>
    /// <param name="searchKeys">The search shard keys in order.</param>
    /// <param name="root">The absolute consumer root for source links.</param>
    /// <param name="liteSpecRoot">The absolute path of the carried LiteSpec release.</param>
    /// <param name="editor">The editor link template with its root, path and line placeholders.</param>
    /// <param name="generatedOn">The generation timestamp text in UTC.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <param name="coverage">The computed Intent coverage status.</param>
    /// <returns>The manifest shard value.</returns>
    static JsonObject ManifestJson(
        JsonObject store,
        BookOutlineResult outline,
        JsonArray layers,
        IReadOnlyDictionary<string, string> shardOf,
        IReadOnlyList<string> searchKeys,
        string root,
        string liteSpecRoot,
        string editor,
        string generatedOn,
        LayerClaims claims,
        IntentCoverage coverage)
    {
        var project = ProjectOf(store);
        var counts = TotalsOf(store);
        var totals = new JsonObject();
        foreach (var type in counts.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            totals[type] = counts[type];
        }

        var map = new JsonObject();
        foreach (var id in shardOf.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            map[id] = shardOf[id];
        }

        return new JsonObject
        {
            ["project"] = new JsonObject
            {
                ["id"] = project.Id,
                ["title"] = project.Title,
            },
            ["roots"] = new JsonObject
            {
                ["consumer"] = root,
                ["litespec"] = liteSpecRoot,
            },
            ["editor"] = editor,
            ["claims"] = ClaimsJson(claims),
            ["intentCoverage"] = BookIntentCoverage.ToJson(coverage),
            ["testRun"] = claims.Proof is null ? JsonNode.Parse("null")! : store["testRun"]?.DeepClone() ?? JsonNode.Parse("null")!,
            ["totals"] = totals,
            ["layers"] = layers,
            ["shardOf"] = map,
            ["search"] = new JsonArray([.. searchKeys.Select(key => (JsonNode?)key)]),
            ["unplaced"] = new JsonArray([.. outline.Unplaced.Select(id => (JsonNode?)id)]),
            ["generatedOn"] = generatedOn,
        };
    }

    /// <summary>
    ///     The security items the Security view lists: cross-tenant access,
    ///     outside calls and personal or sensitive data, in the client's words.
    /// </summary>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <returns>The security items in identifier order, covering cross-tenant access, outside calls and personal or sensitive data.</returns>
    static JsonArray SecurityJson(IReadOnlyDictionary<string, JsonObject> details)
    {
        var items = new JsonArray();
        foreach (var id in details.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var fields = details[id]["fields"] as JsonObject;
            if (fields is null)
            {
                continue;
            }

            if (fields["access"] is JsonObject access
                && access["tenant"] is JsonValue tenant
                && tenant.TryGetValue<string>(out var scope)
                && scope == "cross")
            {
                var grant = access["grant"] is JsonValue giver && giver.TryGetValue<string>(out var target)
                    ? target
                    : null;
                var text = "Acts across tenants"
                    + (grant is { Length: > 0 } ? ", through " + TitleOf(details, grant) : "");
                items.Add((JsonNode)new JsonObject
                {
                    ["kind"] = "cross",
                    ["id"] = id,
                    ["text"] = text,
                });
            }

            foreach (var effect in fields["effects"] as JsonArray ?? [])
            {
                if (effect is JsonObject entry
                    && entry["calls"] is JsonValue call
                    && call.TryGetValue<string>(out var to)
                    && to.Length > 0)
                {
                    items.Add((JsonNode)new JsonObject
                    {
                        ["kind"] = "calls",
                        ["id"] = id,
                        ["text"] = CallsText(details, to),
                    });
                }
            }

            foreach (var data in fields["dataClasses"] as JsonArray ?? [])
            {
                if (data is JsonObject held
                    && held["classification"] is JsonValue rank
                    && rank.TryGetValue<string>(out var level)
                    && level is "personal" or "sensitive")
                {
                    var name = held["name"] is JsonValue label && label.TryGetValue<string>(out var words)
                        ? words
                        : "";
                    var text = Cap(level) + " data: " + name.Replace('-', ' ');
                    if (held["onErasure"] is JsonValue erasure
                        && erasure.TryGetValue<string>(out var fate)
                        && fate.Length > 0)
                    {
                        text += ", on erasure " + fate;
                    }

                    items.Add((JsonNode)new JsonObject
                    {
                        ["kind"] = "data",
                        ["id"] = id,
                        ["text"] = text,
                    });
                }
            }
        }

        return items;
    }

    /// <summary>One outside call in words: the called record's title and the called step, if any.</summary>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <param name="to">The called record identifier, optionally with a fragment that names the called step.</param>
    /// <returns>The outside call sentence with the called record title and the called step when a fragment is present.</returns>
    static string CallsText(IReadOnlyDictionary<string, JsonObject> details, string to)
    {
        var hash = to.IndexOf('#');
        var text = "Calls " + TitleOf(details, hash < 0 ? to : to[..hash]);
        if (hash > 0)
        {
            var fragment = to[(hash + 1)..];
            var cut = fragment.IndexOf('#');
            text += ", " + (cut < 0 ? fragment : fragment[..cut]).Replace('-', ' ');
        }

        return text;
    }

    /// <summary>One record's title for a security sentence, else the id itself.</summary>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <param name="id">The record identifier whose title is sought.</param>
    /// <returns>The record title, or the identifier itself when no detail or no title is present.</returns>
    static string TitleOf(IReadOnlyDictionary<string, JsonObject> details, string id) =>
        details.TryGetValue(id, out var detail) && StrProp(detail, "title") is { Length: > 0 } title
            ? title
            : id;

    /// <summary>One word with its first letter turned, such as Personal.</summary>
    /// <param name="text">The word to capitalize, possibly empty.</param>
    /// <returns>The word with its first letter uppercased, or the empty text unchanged.</returns>
    static string Cap(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>
    ///     Copies each screen capture the model lists into the book output under
    ///     <c>assets/captures/&lt;screen&gt;/</c>, and records the book-relative path on
    ///     the capture as <c>bookPath</c>. A capture whose file is missing keeps
    ///     only its model path, so the page states the capture state instead.
    /// </summary>
    /// <param name="outDir">The book output folder.</param>
    /// <param name="root">The absolute consumer root the capture paths resolve against.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    static void CopyCaptures(string outDir, string root, IReadOnlyDictionary<string, JsonObject> details)
    {
        foreach (var (id, detail) in details)
        {
            if (StrProp(detail, "type") != "screen" || detail["captures"] is not JsonArray captures)
            {
                continue;
            }

            var folder = id.Replace('/', '-').Replace('.', '-');
            foreach (var capture in captures.OfType<JsonObject>())
            {
                var path = StrProp(capture, "path");
                if (path is not { Length: > 0 })
                {
                    continue;
                }

                var source = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                if (!File.Exists(source))
                {
                    continue;
                }

                var name = Path.GetFileName(source);
                var targetDir = Path.Combine(outDir, "assets", "captures", folder);
                Directory.CreateDirectory(targetDir);
                File.Copy(source, Path.Combine(targetDir, name), overwrite: true);
                capture["bookPath"] = $"assets/captures/{folder}/{name}";
            }
        }
    }

    /// <summary>Writes one embedded book asset into the output assets folder.</summary>
    /// <param name="outDir">The output folder that holds the assets folder.</param>
    /// <param name="name">The asset file name, which also names the embedded resource suffix.</param>
    static void WriteAsset(string outDir, string name)
    {
        using var stream = typeof(BookCommand).Assembly.GetManifestResourceStream($"LiteSpec.Book.Assets.{name}")
            ?? throw new InvalidOperationException($"Missing embedded asset '{name}'; build the book client in apps/cli/book first.");
        using var file = File.Create(Path.Combine(outDir, "assets", name));
        stream.CopyTo(file);
    }

    /// <summary>One string property, or null when it is missing or not a string.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);
}
