using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Book;

/// <summary>
///     Turns the enriched model into the book's files: the shell, the app assets, the screen
///     captures and every data shard, all as bytes in memory. The export writes them to a
///     folder and the live server answers from them, so both carry the same book.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookSiteBuilder
{
    /// <summary>The UTF-8 encoding without a byte order mark, used for the book's text files.</summary>
    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>Builds the book for a model object with the layers the project claims.</summary>
    /// <param name="store">The model object that holds the records array and the findings.</param>
    /// <param name="root">The absolute consumer root that the captures resolve against.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="links">The source link template with {root}, {commit}, {path} and {line} already named, or null for no source links.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <param name="generatedOn">The build time in UTC, in ISO 8601 form.</param>
    /// <returns>The built book.</returns>
    internal static BookSiteBuild Build(
        JsonObject store,
        string root,
        BookLexicon lexicon,
        string? links,
        LayerClaims claims,
        string generatedOn)
    {
        // One pass over the one index: the outline and the details are each built once,
        // then packed into shards. Nothing is recomputed per record.
        var coverage = BookIntentCoverage.Of(store);
        var index = BookIndexBuilder.BuildIndex(store);
        var outline = BookOutline.Build(store, claims);
        var details = BookRecordDetails.Build(store, outline, claims);
        var files = new List<BookFile>();

        AddCaptures(files, root, details);

        // Records first in outline order, so parents pack with children: the outline
        // nodes and the manifest map carry the shard ids.
        var shardOf = BookShards.AddRecords(files, details, BookShards.OutlineOrder(outline));

        var layers = new JsonArray();
        foreach (var layerRoot in outline.Layers)
        {
            var keys = BookShards.AddLayer(files, layerRoot.Id, layerRoot.Children, shardOf);
            layers.Add((JsonNode)new JsonObject
            {
                ["key"] = layerRoot.Id,
                ["label"] = LayerLabel(lexicon, layerRoot.Id),
                ["outline"] = new JsonArray([.. keys.Select(key => (JsonNode?)key)]),
            });
        }

        var searchKeys = BookShards.AddSearch(files, SearchRows(index, details));

        BookShards.AddShard(files, "lexicon", lexicon.ToJson());
        BookShards.AddShard(files, "security", SecurityJson(details));
        BookShards.AddShard(files, "coverage", BookIntentCoverage.EntriesJson(coverage));
        BookShards.AddShard(
            files,
            "manifest",
            ManifestJson(store, outline, layers, shardOf, searchKeys, links, generatedOn, claims, coverage));

        var project = ProjectOf(store);
        var totals = TotalsOf(store);
        var title = project.Title.Length > 0 ? project.Title + " Book" : "Project book";
        var html = BookShell.Render(title, BookShell.Noscript(project.Title, totals, coverage));
        files.Add(BookFile.Of("index.html", Utf8NoBom.GetBytes(html + "\n")));
        files.Add(BookFile.Of("assets/book.css", ReadAsset("book.css")));
        files.Add(BookFile.Of("assets/book.js", ReadAsset("book.js")));

        var shards = files.Count(file => file.Path.StartsWith("data/", StringComparison.Ordinal));
        return new BookSiteBuild(new BookSite(files), details.Count, shards, outline.Unplaced, generatedOn);
    }

    /// <summary>One line for the console: the record count, the shard count and the unplaced identifiers.</summary>
    /// <param name="build">The built book.</param>
    /// <param name="target">The folder written, or the address served.</param>
    /// <returns>The console line.</returns>
    internal static string Summary(BookSiteBuild build, string target)
    {
        var line = $"{target}: {build.Records} records, {build.Shards} shards.";
        if (build.Unplaced.Count > 0)
        {
            line += $" Unplaced (record-parent-missing): {string.Join(", ", build.Unplaced)}.";
        }

        return line;
    }

    /// <summary>The label of one layer from the lexicon, else the layer key itself.</summary>
    /// <param name="lexicon">The book lexicon that holds the layer labels.</param>
    /// <param name="key">The layer key whose label is sought.</param>
    /// <returns>The layer label from the lexicon, or the layer key itself when the lexicon holds no label for it.</returns>
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
    ///     The manifest shard value: project, the product id, source links, the LiteSpec release,
    ///     the build time, the layer claims, the Intent coverage, the test run, the totals, the
    ///     layers with their outline shards, the shard map, the search shards and the unplaced ids.
    ///     Without a Proof claim the test run is null, because the book leaves out proof.
    /// </summary>
    /// <param name="store">The model object that holds the records and the test run.</param>
    /// <param name="outline">The book outline that holds the unplaced identifiers.</param>
    /// <param name="layers">The layer entries for the manifest, each holding its outline shard keys.</param>
    /// <param name="shardOf">The shard identifier for each record identifier.</param>
    /// <param name="searchKeys">The search shard keys in order.</param>
    /// <param name="links">The source link template with {path} and {line} left for the reader, or null for none.</param>
    /// <param name="generatedOn">The build time in UTC, in ISO 8601 form.</param>
    /// <param name="claims">The record layers the project claims.</param>
    /// <param name="coverage">The computed Intent coverage status.</param>
    /// <returns>The manifest shard value.</returns>
    static JsonObject ManifestJson(
        JsonObject store,
        BookOutlineResult outline,
        JsonArray layers,
        IReadOnlyDictionary<string, string> shardOf,
        IReadOnlyList<string> searchKeys,
        string? links,
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
            ["productId"] = project.Id,
            ["links"] = BookRender.StrNode(links),
            ["release"] = LiteSpecHome.Version ?? "",
            ["generatedOn"] = generatedOn,
            ["claims"] = ClaimsJson(claims),
            ["intentCoverage"] = BookIntentCoverage.SummaryJson(coverage),
            ["testRun"] = claims.Proof is null ? JsonNode.Parse("null")! : store["testRun"]?.DeepClone() ?? JsonNode.Parse("null")!,
            ["totals"] = totals,
            ["layers"] = layers,
            ["shardOf"] = map,
            ["search"] = new JsonArray([.. searchKeys.Select(key => (JsonNode?)key)]),
            ["unplaced"] = new JsonArray([.. outline.Unplaced.Select(id => (JsonNode?)id)]),
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
    ///     Adds each screen capture the model lists to the book under
    ///     <c>assets/captures/&lt;screen&gt;/</c>, and records the book-relative path on
    ///     the capture as <c>bookPath</c>. A capture whose file is missing keeps
    ///     only its model path, so the page states the capture state instead.
    /// </summary>
    /// <param name="files">The book files the captures join.</param>
    /// <param name="root">The absolute consumer root the capture paths resolve against.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    static void AddCaptures(List<BookFile> files, string root, IReadOnlyDictionary<string, JsonObject> details)
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
                var bookPath = $"assets/captures/{folder}/{name}";
                files.Add(BookFile.Of(bookPath, File.ReadAllBytes(source)));
                capture["bookPath"] = bookPath;
            }
        }
    }

    /// <summary>Reads one embedded book asset: the shell's script or stylesheet, as bytes.</summary>
    /// <param name="name">The asset file name, which also names the embedded resource suffix.</param>
    /// <returns>The asset bytes.</returns>
    static byte[] ReadAsset(string name)
    {
        using var stream = typeof(BookSiteBuilder).Assembly.GetManifestResourceStream($"LiteSpec.Book.Assets.{name}")
            ?? throw new InvalidOperationException($"Missing embedded asset '{name}'; build the book client in apps/cli/book first.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>One string property, or null when it is missing or not a string.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);
}
