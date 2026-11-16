using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Context;

/// <summary>
///     The <c>litespec context</c> command: writes the consumer's records
///     as one compact plain-text file for an AI chat.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class ContextCommand
{
    /// <summary>The file name the command writes when --out names a folder.</summary>
    public const string DefaultFileName = "litespec-context.txt";

    /// <summary>The record layers in outline order.</summary>
    static readonly string[] LayerOrder = ["intent", "behavior", "implementation", "proof", "governance"];

    /// <summary>The usage text for the context command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec context [--root <folder>] [--out <file-or-folder>] [--layers <list>] [--modules <list>] [--types <list>] [--include records|explanations|both] [--full] [--help]

        Writes the consumer's records as one compact
        plain-text file for an AI chat: a short header, then one block per
        file between '=== BEGIN FILE: <relative path> ===' and
        '=== END FILE: <relative path> ===', and a closing
        '=== END OF CONTEXT: <n> files ===' line. The product record comes first and
        is always included. The command never edits, moves or deletes a
        LiteSpec file.

          --root      The consumer repository to read. Defaults to the working directory.
          --out       The file to write, or a folder receiving litespec-context.txt. Defaults to litespec-context.txt in the working directory.
          --layers    Comma list of intent, behavior, implementation, proof, governance. Defaults to all.
          --modules   Comma list of module names; limits Behavior records to those modules.
          --types     Comma list of record types to keep, such as use-case,rule.
          --include   One of records, explanations, or both. Defaults to records.
          --full      Keep the files as written instead of compacting them.
          --help      Print this text and exit.

        Exit codes: 0 the file is written, 1 the records cannot be read or the file cannot be written, 2 the options are invalid or the project file is missing.
        """;

    /// <summary>Runs the context command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written, 1 unreadable records or unwritable file, 2 usage error or unreadable consumer.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--full"],
            ["--root", "--out", "--layers", "--modules", "--types", "--include"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (!TrySplit(parsed.Value("--layers"), "--layers", out var layers, out var layersError))
        {
            return CommandFailure.UsageError(layersError, Usage, json: false);
        }

        if (layers is not null)
        {
            foreach (var layer in layers)
            {
                if (!LayerOrder.Contains(layer, StringComparer.Ordinal))
                {
                    return CommandFailure.UsageError(
                        $"Unknown layer '{layer}'. Use {string.Join(", ", LayerOrder[..^1])}, or {LayerOrder[^1]}.",
                        Usage,
                        json: false);
                }
            }
        }

        if (!TrySplit(parsed.Value("--modules"), "--modules", out var modules, out var modulesError))
        {
            return CommandFailure.UsageError(modulesError, Usage, json: false);
        }

        if (!TrySplit(parsed.Value("--types"), "--types", out var types, out var typesError))
        {
            return CommandFailure.UsageError(typesError, Usage, json: false);
        }

        if (types is not null)
        {
            foreach (var type in types)
            {
                if (!RecordTypes.All.Contains(type))
                {
                    return CommandFailure.UsageError(
                        $"Unknown record type '{type}'. Use one of: {string.Join(", ", RecordTypes.All.OrderBy(name => name, StringComparer.Ordinal))}.",
                        Usage,
                        json: false);
                }
            }
        }

        var include = parsed.Value("--include") ?? "records";

        if (include.Length == 0)
        {
            return CommandFailure.UsageError("Option --include needs a value.", Usage, json: false);
        }

        if (include is not ("records" or "explanations" or "both"))
        {
            return CommandFailure.UsageError(
                $"Unknown include '{include}'. Use records, explanations, or both.",
                Usage,
                json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var read = RecordReader.Read(config);

        if (read.Problems is [var first, ..])
        {
            return CommandFailure.Fail($"Cannot read the records: {first.Message}", json: false, code: 1);
        }

        if (modules is not null)
        {
            var known = KnownModules(read.Records);

            foreach (var module in modules)
            {
                if (!known.Contains(module))
                {
                    return CommandFailure.UsageError(
                        known.Count == 0
                            ? $"Unknown module '{module}': this project declares no modules."
                            : $"Unknown module '{module}'. Use one of: {string.Join(", ", known.OrderBy(name => name, StringComparer.Ordinal))}.",
                        Usage,
                        json: false);
                }
            }
        }

        var product = read.Records.FirstOrDefault(record => !record.Derived && record.Type == RecordTypes.Product);

        if (product is null)
        {
            return CommandFailure.Fail("The consumer holds no product record.", json: false, code: 1);
        }

        var files = SelectFiles(config, read.Records, product.File, layers, modules, types);
        var full = parsed.Has("--full");
        var text = Render(config, product, files, layers, modules, types, include, full);
        var target = ResolveOut(parsed.Value("--out"));

        try
        {
            if (Path.GetDirectoryName(target) is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(target, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{target}': {exception.Message}", json: false, code: 1);
        }

        CliHost.Out.WriteLine($"Wrote {target} ({text.Length} characters, about {text.Length / 4} tokens).");
        return 0;
    }

    /// <summary>Splits a comma-list option into its names.</summary>
    /// <param name="raw">The option value, or null when it was not passed.</param>
    /// <param name="option">The option name, with its dashes.</param>
    /// <param name="names">The names in order, or null when the option was not passed.</param>
    /// <param name="error">The usage error when the value holds no name.</param>
    /// <returns>True when the value parsed; otherwise false.</returns>
    static bool TrySplit(string? raw, string option, out IReadOnlyList<string>? names, out string error)
    {
        names = null;
        error = "";

        if (raw is null)
        {
            return true;
        }

        var split = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (split.Length == 0)
        {
            error = $"Option {option} needs a value.";
            return false;
        }

        names = split;
        return true;
    }

    /// <summary>The module names the module records declare.</summary>
    /// <param name="records">Every written record.</param>
    /// <returns>The declared module names.</returns>
    static HashSet<string> KnownModules(IReadOnlyList<Record> records)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record is { Type: RecordTypes.Module, Derived: false } && record.Id.Contains('/', StringComparison.Ordinal))
            {
                known.Add(record.Id[(record.Id.IndexOf('/') + 1)..]);
            }
        }

        return known;
    }

    /// <summary>
    ///     The record files the filters keep: the product file first, then the
    ///     book's outline order by record layer, then by path. The product file
    ///     passes every filter.
    /// </summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="records">Every written record.</param>
    /// <param name="productFile">The product record's file.</param>
    /// <param name="layers">The kept layers, or null for all.</param>
    /// <param name="modules">The kept modules, or null for all.</param>
    /// <param name="types">The kept record types, or null for all.</param>
    /// <returns>The kept files in output order.</returns>
    static List<ContextFile> SelectFiles(
        ConsumerConfig config,
        IReadOnlyList<Record> records,
        string productFile,
        IReadOnlyList<string>? layers,
        IReadOnlyList<string>? modules,
        IReadOnlyList<string>? types)
    {
        var kept = new List<ContextFile>();

        foreach (var group in records.Where(record => !record.Derived && IsRecordFile(config, record.File)).GroupBy(record => record.File, StringComparer.Ordinal))
        {
            var page = group.FirstOrDefault(record => !record.IsEntry) ?? group.First();
            var fileTypes = group.Select(record => record.Type).Distinct(StringComparer.Ordinal).ToList();
            var layer = fileTypes.Select(LayerOf).FirstOrDefault(layer => layer is not null) ?? "intent";
            string? explanation = RecordFile.ExplanationPathOf(group.Key);

            if (!File.Exists(explanation))
            {
                explanation = null;
            }

            var file = new ContextFile(
                group.Key,
                layer,
                page.Module,
                fileTypes,
                page.Title,
                group.Count(),
                explanation)
            {
                IsProduct = string.Equals(group.Key, productFile, StringComparison.Ordinal),
            };

            if (!file.IsProduct
                && layers is not null
                && !layers.Contains(file.Layer, StringComparer.Ordinal))
            {
                continue;
            }

            if (!file.IsProduct
                && modules is not null
                && string.Equals(file.Layer, "behavior", StringComparison.Ordinal)
                && (file.Module is null || !modules.Contains(file.Module, StringComparer.Ordinal)))
            {
                continue;
            }

            if (!file.IsProduct
                && types is not null
                && !file.Types.Any(type => types.Contains(type, StringComparer.Ordinal)))
            {
                continue;
            }

            kept.Add(file);
        }

        kept.Sort((left, right) =>
        {
            if (left.IsProduct != right.IsProduct)
            {
                return left.IsProduct ? -1 : 1;
            }

            var layer = LayerRank(left.Layer).CompareTo(LayerRank(right.Layer));

            return layer != 0
                ? layer
                : string.Compare(Relative(config, left.File), Relative(config, right.File), StringComparison.Ordinal);
        });

        return kept;
    }

    /// <summary>Whether the file is a record file the output reads: YAML that holds records.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="file">The file to test.</param>
    /// <returns>True for a record file; otherwise false.</returns>
    static bool IsRecordFile(ConsumerConfig config, string file) =>
        file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Path.GetFileName(file), "refusals.yaml", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(file, config.GlossaryPath, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(file, ConsumerRoot.ProjectFile(config.Root), StringComparison.OrdinalIgnoreCase)
        && !string.Equals(file, config.UiPatternCatalog, StringComparison.OrdinalIgnoreCase);

    /// <summary>The record layer of one record type, from the model.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The layer, or null when the type names none.</returns>
    static string? LayerOf(string type) =>
        RecordTypes.LayerByType.TryGetValue(type, out var layer) ? layer : null;

    /// <summary>The layer's rank in outline order.</summary>
    /// <param name="layer">The record layer.</param>
    /// <returns>The outline rank.</returns>
    static int LayerRank(string layer) => layer switch
    {
        "intent" => 0,
        "behavior" => 1,
        "implementation" => 2,
        "proof" => 3,
        _ => 4,
    };

    /// <summary>The file path relative to the consumer root, with forward slashes.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The relative path.</returns>
    static string Relative(ConsumerConfig config, string file) =>
        Path.GetRelativePath(config.Root, file).Replace('\\', '/');

    /// <summary>
    ///     Renders the context file: the header, then one block per record
    ///     file with its explanation after it.
    /// </summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="product">The product record.</param>
    /// <param name="files">The kept files in output order.</param>
    /// <param name="layers">The kept layers, or null for all.</param>
    /// <param name="modules">The kept modules, or null for all.</param>
    /// <param name="types">The kept record types, or null for all.</param>
    /// <param name="include">Which files to emit: records, explanations, or both.</param>
    /// <param name="full">Whether to keep the files as written.</param>
    /// <returns>The context file text with LF line endings.</returns>
    static string Render(
        ConsumerConfig config,
        Record product,
        IReadOnlyList<ContextFile> files,
        IReadOnlyList<string>? layers,
        IReadOnlyList<string>? modules,
        IReadOnlyList<string>? types,
        string include,
        bool full)
    {
        var header = string.Join("\n", new[]
        {
            $"LiteSpec context for {product.Title} ({product.Id})",
            $"Reviewed LiteSpec version: {ReviewedVersion(config.Root)}",
            $"Filters: layers={Echo(layers)}; modules={Echo(modules)}; types={Echo(types)}; include={include}; full={(full ? "yes" : "no")}",
            $"Records: {files.Sum(file => file.RecordCount)} in {files.Count} files",
            $"Generated: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}",
        });
        var blocks = new List<string>();

        foreach (var file in files)
        {
            if (include is not "explanations")
            {
                blocks.Add(Block(Relative(config, file.File), BlockText(SourceTree.Read(file.File), full, yaml: true, title: "")));
            }

            if (include is not "records" && file.ExplanationFile is { } explanation)
            {
                blocks.Add(Block(Relative(config, explanation), BlockText(SourceTree.Read(explanation), full, yaml: false, file.Title)));
            }
        }

        // The file count and the closing line let a reader tell a complete
        // context from one an upload or a chat cut short.
        var guide = string.Join("\n", new[]
        {
            $"Files: {blocks.Count}",
            "Format: each file stands between '=== BEGIN FILE: <path> ===' and '=== END FILE: <path> ===', and the context ends with '=== END OF CONTEXT: <n> files ==='. A context without that last line is incomplete.",
        });

        return header + "\n" + guide + "\n\n" + string.Join("\n\n", blocks) + $"\n\n=== END OF CONTEXT: {blocks.Count} files ===\n";
    }

    /// <summary>One file's block between its begin and end markers.</summary>
    /// <param name="path">The file path relative to the consumer root.</param>
    /// <param name="text">The file's block text.</param>
    /// <returns>The block with its markers.</returns>
    static string Block(string path, string text) =>
        $"=== BEGIN FILE: {path} ===\n{text}\n=== END FILE: {path} ===";

    /// <summary>The block text of one file: as written with LF endings, or compacted.</summary>
    /// <param name="text">The file text.</param>
    /// <param name="full">Whether to keep the file as written.</param>
    /// <param name="yaml">Whether the file is a YAML record file rather than a Markdown explanation.</param>
    /// <param name="title">The title of the record the explanation file explains.</param>
    /// <returns>The block text without trailing blank lines.</returns>
    static string BlockText(string text, bool full, bool yaml, string title)
    {
        var normalized = ContextCompact.Normalize(text);
        var body = full ? normalized : yaml ? ContextCompact.Yaml(normalized) : ContextCompact.Markdown(normalized, title);

        return body.TrimEnd('\n');
    }

    /// <summary>How the header echoes one list filter: its names, or all.</summary>
    /// <param name="names">The kept names, or null for all.</param>
    /// <returns>The echo text.</returns>
    static string Echo(IReadOnlyList<string>? names) => names is null ? "all" : string.Join(",", names);

    /// <summary>The LiteSpec version the project reviewed, or unknown.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The reviewed version.</returns>
    static string ReviewedVersion(string root)
    {
        try
        {
            if (RecordText.Read(ConsumerRoot.ProjectFile(root)) is JsonObject project
                && JsonValues.AsString(project["reviewedLiteSpecVersion"]) is { Length: > 0 } version)
            {
                return version;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
        }

        return "unknown";
    }

    /// <summary>The output file: --out as written, a folder receiving the default name, or the default in the working directory.</summary>
    /// <param name="option">The --out value, or null when it was not passed.</param>
    /// <returns>The absolute output file path.</returns>
    static string ResolveOut(string? option)
    {
        var raw = option ?? DefaultFileName;
        var full = Path.IsPathFullyQualified(raw) ? raw : Path.Combine(CliHost.WorkingDirectory, raw);

        if (raw.EndsWith('/') || raw.EndsWith('\\') || Directory.Exists(full))
        {
            full = Path.Combine(full, DefaultFileName);
        }

        return full;
    }

    /// <summary>One record file kept for the output, with the facts the filters and the header need.</summary>
    /// <param name="File">The absolute record file path.</param>
    /// <param name="Layer">The record layer from the file's record types.</param>
    /// <param name="Module">The module folder holding the file, or null.</param>
    /// <param name="Types">The record types the file holds.</param>
    /// <param name="Title">The page record's title.</param>
    /// <param name="RecordCount">How many written records the file holds.</param>
    /// <param name="ExplanationFile">The explanation file beside it, or null.</param>
    sealed record ContextFile(
        string File,
        string Layer,
        string? Module,
        IReadOnlyList<string> Types,
        string Title,
        int RecordCount,
        string? ExplanationFile)
    {
        /// <summary>Gets a value indicating whether the file holds the product record.</summary>
        public bool IsProduct { get; init; }
    }
}
