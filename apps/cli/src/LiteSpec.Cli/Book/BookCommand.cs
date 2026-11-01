using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>
///     The <c>litespec book</c> command: generate the project book, one offline
///     shell with sharded data, from a consumer's model.json.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage =
        "Usage: litespec book --model <model.json> --out <folder> [--root <folder>] [--editor <template>]\n"
        + "\n"
        + "Options:\n"
        + "  --model <file>       The model.json written by the model builder (required).\n"
        + "  --out <folder>       The folder to write the static site to (required).\n"
        + "  --root <folder>      The consumer root for source links. Defaults to the parent of the model's folder when the model sits in a .model folder, else the model's folder.\n"
        + "  --editor <template>  The editor link template with {root}, {path} and {line}, or \"none\" to suppress links. Defaults to vscode://file/{root}/{path}:{line}.\n"
        + "  --help               Print this text and exit.\n"
        + "\n"
        + "Exit codes: 0 written, 1 unreadable input or unwritable output, 2 usage error, a missing carried release, or a missing or broken lexicon page.\n";

    /// <summary>The default editor link template, with its three placeholders.</summary>
    public const string DefaultEditor = "vscode://file/{root}/{path}:{line}";

    /// <summary>The UTF-8 encoding without a byte order mark, used for the written book files.</summary>
    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>Runs the command with the arguments after <c>book</c>; returns the exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the model, output, root and editor options.</param>
    /// <returns>The process exit code: 0 when the book is written, 1 on unreadable input or unwritable output, and 2 on a usage error or a missing carried release or lexicon.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The book command is a last-resort command handler that reports any read, release lookup or write failure as an error message and an exit code instead of crashing.")]
    public static int Run(string[] args)
    {
        var parsed = ParseArgs(args);
        if (parsed.Help)
        {
            Console.Out.Write(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            Console.Error.Write($"{parsed.Error}\n{Usage}");
            return 2;
        }

        JsonNode? model;
        try
        {
            model = ReadJson(parsed.Model!);
        }
        catch (Exception exception)
        {
            Console.Error.Write($"Cannot read model '{parsed.Model}': {exception.Message}\n");
            return 1;
        }

        if (model is not JsonObject store || store["records"] is not JsonArray)
        {
            Console.Error.Write($"Model '{parsed.Model}' holds no records array.\n");
            return 1;
        }

        string liteSpecRoot;
        try
        {
            liteSpecRoot = LiteSpecHome.RequireRoot();
        }
        catch (Exception exception)
        {
            Console.Error.Write($"Cannot find the carried LiteSpec release: {exception.Message}\n");
            return 2;
        }

        BookLexicon lexicon;
        try
        {
            lexicon = BookLexicon.Load(liteSpecRoot);
        }
        catch (BookLexiconException exception)
        {
            Console.Error.Write($"Cannot load the book words from '{exception.File}': {exception.Message}\n");
            return 2;
        }

        var root = ResolveRoot(parsed.Root, parsed.Model!);
        var editor = parsed.Editor ?? DefaultEditor;

        // The check result reaches the book only through the model: `litespec model build`
        // writes findings and known into model.json, and the model itself holds that shape.
        JsonNode? findingsFile = null;
        if (store["findings"] is JsonArray || store["known"] is JsonArray)
        {
            findingsFile = store;
        }
        else if (store["findings"] is JsonObject embedded && embedded["findings"] is JsonArray)
        {
            findingsFile = embedded;
        }

        // One pass over the one index: outline, details and review batches are each
        // built once, then packed into shards. Nothing is recomputed per record.
        var index = BookIndexBuilder.BuildIndex(store);
        var outline = BookOutline.Build(store);
        var details = BookEnrich.Build(store, outline);
        var generatedOn = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        var review = BookReviewQueries.ReviewData(index, findingsFile, root);

        try
        {
            var outDir = parsed.Out!;
            var dataDir = Path.Combine(outDir, "data");
            var shards = 0;

            // Records first: the outline nodes and the manifest map carry the shard ids.
            var shardOf = BookShards.WriteRecords(dataDir, details);
            shards += shardOf.Values.Distinct(StringComparer.Ordinal).Count();

            var layers = new JsonArray();
            foreach (var layerRoot in outline.Layers)
            {
                var keys = BookShards.WriteLayer(dataDir, layerRoot.Id, layerRoot.Children, shardOf);
                shards += keys.Count;
                layers.Add(new JsonObject
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
            BookShards.WriteShard(dataDir, "queue", QueueJson(index, root, review, store, details));
            shards += 1;
            BookShards.WriteShard(
                dataDir,
                "manifest",
                ManifestJson(store, outline, layers, shardOf, searchKeys, root, liteSpecRoot, editor, generatedOn));
            shards += 1;

            var project = ProjectOf(store);
            var totals = TotalsOf(store);
            var title = project.Title.Length > 0 ? project.Title + " Book" : "Project book";
            var html = BookShell.Render(title, BookShell.Noscript(project.Title, totals));
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "index.html"), html + "\n", Utf8NoBom);

            Directory.CreateDirectory(Path.Combine(outDir, "assets"));
            WriteAsset(outDir, "book.css");
            WriteAsset(outDir, "book-extra.css");
            WriteAsset(outDir, "book-fonts.css");
            WriteAsset(outDir, "book.js");

            var line = $"{outDir}: {details.Count} records, {shards} shards.";
            if (outline.Unplaced.Count > 0)
            {
                line += $" Unplaced (record-parent-missing): {string.Join(", ", outline.Unplaced)}.";
            }

            Console.Out.Write(line + "\n");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.Write($"Cannot write to '{parsed.Out}': {exception.Message}\n");
            return 1;
        }
    }

    /// <summary>The parsed book command arguments: the help flag, the usage error, and the model, output, root and editor values, each null when absent.</summary>
    sealed record Parsed(bool Help, string? Error, string? Model, string? Out, string? Root, string? Editor);

    /// <summary>Parses the book command arguments into their named values and any usage error.</summary>
    /// <param name="argv">The command arguments after the command name.</param>
    /// <returns>The parsed arguments, with the help flag or the usage error set when the input calls for it.</returns>
    static Parsed ParseArgs(string[] argv)
    {
        var parsed = CliArgs.Parse(argv, [], ["--model", "--out", "--root", "--editor"]);

        if (parsed.Help)
        {
            return new Parsed(true, null, null, null, null, null);
        }

        if (parsed.Error is not null)
        {
            return new Parsed(false, parsed.Error, null, null, null, null);
        }

        if (parsed.Value("--model") is not { Length: > 0 } model)
        {
            return new Parsed(false, "Missing --model <model.json>.", null, null, null, null);
        }

        if (parsed.Value("--out") is not { Length: > 0 } output)
        {
            return new Parsed(false, "Missing --out <folder>.", null, null, null, null);
        }

        return new Parsed(false, null, model, output, parsed.Value("--root"), parsed.Value("--editor"));
    }

    /// <summary>
    ///     The consumer root for source links: the --root value, else the parent of
    ///     the model's folder when the model sits in a <c>.model</c> folder, else
    ///     the model's folder.
    /// </summary>
    /// <param name="root">The root option value, or null or empty when the option is absent.</param>
    /// <param name="model">The path of the model file, whose folder supplies the default root.</param>
    /// <returns>The absolute consumer root used for source links.</returns>
    static string ResolveRoot(string? root, string model)
    {
        if (root is { Length: > 0 })
        {
            return Path.GetFullPath(root);
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(model)) ?? ".";
        return string.Equals(Path.GetFileName(folder), ".model", StringComparison.Ordinal)
            ? Path.GetDirectoryName(folder) ?? folder
            : folder;
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

    /// <summary>The search rows in identifier order: id, title, type and excerpt each.</summary>
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
            rows.Add(new JsonArray(
                id,
                StrProp(detail, "title") ?? id,
                StrProp(detail, "type") ?? "",
                BookShards.SearchExcerpt(raw)));
        }

        return rows;
    }

    /// <summary>
    ///     The manifest shard value: project, roots, editor template, test run,
    ///     totals, layers with their outline shards, the shard map, the search
    ///     shards, the unplaced ids and the generation date.
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
        string generatedOn)
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
            ["testRun"] = store["testRun"]?.DeepClone() ?? JsonNode.Parse("null")!,
            ["totals"] = totals,
            ["layers"] = layers,
            ["shardOf"] = map,
            ["search"] = new JsonArray([.. searchKeys.Select(key => (JsonNode?)key)]),
            ["unplaced"] = new JsonArray([.. outline.Unplaced.Select(id => (JsonNode?)id)]),
            ["generatedOn"] = generatedOn,
        };
    }

    /// <summary>
    ///     The queue shard value: the review batches keyed by selector with their
    ///     order, the open ballots, the security items and the review settings.
    /// </summary>
    /// <param name="index">The model index that holds the review settings.</param>
    /// <param name="root">The absolute consumer root, used to load review settings the index lacks.</param>
    /// <param name="review">The review data that holds the batches in order.</param>
    /// <param name="store">The model object that holds the records with their open questions.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <returns>The queue shard value with batches, ballots, security items and review settings.</returns>
    static JsonObject QueueJson(
        BookIndex index,
        string root,
        ReviewBundle review,
        JsonObject store,
        IReadOnlyDictionary<string, JsonObject> details)
    {
        var batches = new JsonObject();
        var order = new JsonArray();
        foreach (var batch in review.Batches)
        {
            var records = new JsonArray();
            foreach (var entry in batch.Records)
            {
                records.Add(entry);
            }

            batches[batch.Selector] = new JsonObject
            {
                ["selector"] = batch.Selector,
                ["kind"] = batch.Kind,
                ["tier"] = batch.Tier,
                ["deciders"] = new JsonArray([.. batch.Deciders.Select(decider => (JsonNode?)decider)]),
                ["decideBy"] = StringOrNull(batch.DecideBy),
                ["awaiting"] = batch.Awaiting,
                ["n"] = batch.Records.Count,
                ["records"] = records,
            };
            order.Add(batch.Selector);
        }

        return new JsonObject
        {
            ["batches"] = batches,
            ["order"] = order,
            ["ballots"] = BallotJson(store),
            ["security"] = SecurityJson(details),
            ["review"] = ReviewJson(index, root),
        };
    }

    /// <summary>
    ///     Every open question with its record id and its index in the record's
    ///     questions, so the client renders ballots without reading every record.
    /// </summary>
    /// <param name="store">The model object that holds the records with their open questions.</param>
    /// <returns>Every open question with its record identifier and its index in the record questions.</returns>
    static JsonArray BallotJson(JsonObject store)
    {
        var ballots = new JsonArray();
        foreach (var record in store["records"] as JsonArray ?? [])
        {
            if (record is not JsonObject entry || StrProp(entry, "id") is not { Length: > 0 } id)
            {
                continue;
            }

            var at = 0;
            foreach (var item in (entry["fields"] as JsonObject)?["questions"] as JsonArray ?? [])
            {
                if (item is JsonObject question && StrProp(question, "question") is { Length: > 0 } text)
                {
                    ballots.Add(new JsonObject
                    {
                        ["id"] = id,
                        ["i"] = at,
                        ["q"] = new JsonObject
                        {
                            ["question"] = text,
                            ["tier"] = BookReviewQueries.TierOf(entry),
                            ["decider"] = StringOrNull(StrProp(question, "decider")),
                            ["decideBy"] = StringOrNull(StrProp(question, "decideBy")),
                            ["ballot"] = StringOrNull(StrProp(question, "ballot")),
                            ["recommendation"] = StringOrNull(StrProp(question, "recommendation")),
                        },
                    });
                }

                at++;
            }
        }

        return ballots;
    }

    /// <summary>
    ///     The security items the security reviewer reads: cross-tenant access,
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
                items.Add(new JsonObject
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
                    items.Add(new JsonObject
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

                    items.Add(new JsonObject
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

    /// <summary>The review settings the client reads: the veto window and the security reviewer.</summary>
    /// <param name="index">The model index that holds the veto window and possibly the security reviewer.</param>
    /// <param name="root">The absolute consumer root, used to load the security reviewer when the index lacks one.</param>
    /// <returns>The review settings value with the veto window in days and the security reviewer, or JSON null for the reviewer when neither source names one.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loading the consumer configuration for the fallback security reviewer must report no failure; any failure leaves the reviewer absent so the book still builds.")]
    static JsonObject ReviewJson(BookIndex index, string root)
    {
        var reviewer = index.SecurityReviewer;
        if (reviewer is null)
        {
            try
            {
                reviewer = ConsumerConfig.Load(root).SecurityReviewer;
            }
            catch (Exception)
            {
                reviewer = null;
            }
        }

        return new JsonObject
        {
            ["vetoWindowDays"] = index.VetoWindowDays,
            ["securityReviewer"] = StringOrNull(reviewer),
        };
    }

    /// <summary>Reads and parses one JSON file.</summary>
    /// <param name="path">The path of the JSON file to read.</param>
    /// <returns>The parsed JSON node, or null when the file holds the JSON null value.</returns>
    static JsonNode? ReadJson(string path) => JsonNode.Parse(File.ReadAllText(path));

    /// <summary>Writes one embedded book asset into the output assets folder.</summary>
    /// <param name="outDir">The output folder that holds the assets folder.</param>
    /// <param name="name">The asset file name, which also names the embedded resource suffix.</param>
    static void WriteAsset(string outDir, string name)
    {
        using var stream = typeof(BookCommand).Assembly.GetManifestResourceStream($"LiteSpec.Book.Assets.{name}")
            ?? throw new InvalidOperationException($"Missing embedded asset '{name}'.");
        using var file = File.Create(Path.Combine(outDir, "assets", name));
        stream.CopyTo(file);
    }

    /// <summary>One string property, or null when it is missing or not a string.</summary>
    /// <param name="obj">The JSON object to read, or null.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The string value of the property, or null when the object or property is missing or the value is not a string.</returns>
    static string? StrProp(JsonObject? obj, string name) => BookRender.StrProp(obj, name);

    /// <summary>A JSON string, or JSON null when the text is absent.</summary>
    /// <param name="value">The text to convert, or null.</param>
    /// <returns>A JSON string node for the text, or null when the text is null.</returns>
    static JsonNode? StringOrNull(string? value) => BookRender.StrNode(value);
}
