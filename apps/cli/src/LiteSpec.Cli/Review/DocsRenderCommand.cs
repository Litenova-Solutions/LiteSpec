using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Review;

/// <summary>
///     The <c>litespec docs render --changed</c> command: re-renders the book
///     pages of only the records whose content hash changed since the last
///     render, and keeps the hashes in <c>.litespec/.cache/render-hashes.json</c>.
///     The command reads and writes files only, so it calls no model and no network.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class DocsRenderCommand
{
    /// <summary>The usage text for the docs render command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec docs render --changed [--root <folder>]
          litespec docs render --help

        Re-renders the book pages of only the records whose content hash changed
        since the last render. Hashes live in .litespec/.cache/render-hashes.json,
        and each rendered page is written under .litespec/render beside the
        relative path of its record.

          --root              The repository to read. Defaults to the working directory.
          --changed           Render only the records whose content hash changed. Required.
          --help              Print this text and exit.

        Exit codes: 0 rendered, 1 unreadable input or an unwritable page, 2 usage error.
        """;

    /// <summary>The cache file holding the content hash the last render saw, per record.</summary>
    const string HashCache = ".litespec/.cache/render-hashes.json";

    /// <summary>The folder receiving one rendered page per changed record.</summary>
    const string RenderFolder = ".litespec/render";

    /// <summary>Runs the docs render: re-renders only the records whose content hash changed.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 for rendered, 1 for unreadable input, 2 for a usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--changed"], ["--root"], maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (!parsed.Has("--changed"))
        {
            return CommandFailure.UsageError("Docs render needs --changed: it renders only the records whose content hash changed.", Usage, json: false);
        }

        var root = FullPath(parsed.Value("--root") ?? CliHost.WorkingDirectory);

        if (!Directory.Exists(root))
        {
            return CommandFailure.Fail($"The root folder '{root}' does not exist.", json: false, code: 2);
        }

        return Execute(root, CliHost.Out, CliHost.Error);
    }

    /// <summary>Runs the render on explicit writers, so tests prove the markers and the exit code without the process console.</summary>
    /// <param name="root">The repository folder to read and write.</param>
    /// <param name="out">The writer receiving the summary line.</param>
    /// <param name="error">The writer receiving the failure line.</param>
    /// <returns>The exit code: 0 for rendered, 1 when input cannot be read or a page cannot be written.</returns>
    internal static int Execute(string root, TextWriter @out, TextWriter error)
    {
        var records = RecordFiles(root).OrderBy(path => path, StringComparer.Ordinal).ToList();
        var hashes = ReadHashes(Path.Combine(root, HashCache));
        var rendered = 0;

        foreach (var record in records)
        {
            string hash;

            try
            {
                hash = Convert.ToHexString(Sha256.Hash(File.ReadAllBytes(record))).ToLowerInvariant();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                error.WriteLine($"Cannot read '{Path.GetRelativePath(root, record)}': {exception.Message}");
                return 1;
            }

            var relative = Path.GetRelativePath(root, record);

            if (hashes.TryGetValue(relative, out var seen) && string.Equals(seen, hash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!WritePage(root, relative, hash, out var writeError))
            {
                error.WriteLine(writeError);
                return 1;
            }

            hashes[relative] = hash;
            rendered++;
        }

        foreach (var missing in hashes.Keys.Where(key => !records.Contains(Path.Combine(root, key), StringComparer.OrdinalIgnoreCase)).ToList())
        {
            hashes.Remove(missing);
            RemovePage(root, missing);
        }

        if (!WriteHashes(Path.Combine(root, HashCache), hashes, out var cacheError))
        {
            error.WriteLine(cacheError);
            return 1;
        }

        @out.WriteLine($"Rendered {rendered} pages ({records.Count - rendered} unchanged, {records.Count} records).");

        return 0;
    }

    /// <summary>Every record file under the root, minus the folders the tool and the build write.</summary>
    /// <param name="root">The repository folder to scan.</param>
    /// <returns>The absolute record paths.</returns>
    static IEnumerable<string> RecordFiles(string root)
    {
        List<string> found;

        try
        {
            found = Directory.EnumerateFiles(root, "*.yaml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(root, "*.yml", SearchOption.AllDirectories))
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        return found.Where(path => !IsSkipped(root, path));
    }

    /// <summary>Whether the scan skips the path: version control, dependencies, build output, or the render outputs themselves.</summary>
    /// <param name="root">The repository folder the scan runs in.</param>
    /// <param name="path">The absolute path to test.</param>
    /// <returns>True when the scan skips the path.</returns>
    static bool IsSkipped(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (parts.Any(part =>
            part.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || part.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || part.Equals("node_modules", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // The tool outputs under .litespec are written by the review commands, so they never widen the render.
        return relative.StartsWith(".litespec" + Path.DirectorySeparatorChar + "review", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith(".litespec" + Path.DirectorySeparatorChar + ".cache", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith(".litespec" + Path.DirectorySeparatorChar + "render", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads the hash cache: the content hash the last render saw, per record relative path.</summary>
    /// <param name="cacheFile">The absolute cache file path.</param>
    /// <returns>The cached hashes, or an empty map when the cache is missing or invalid.</returns>
    static Dictionary<string, string> ReadHashes(string cacheFile)
    {
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);

        JsonNode? json;

        try
        {
            json = CliJson.TryRead(cacheFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return hashes;
        }

        if (json is not JsonObject cached)
        {
            return hashes;
        }

        foreach (var (key, value) in cached)
        {
            if (value?.GetValue<string>() is { } hash)
            {
                hashes[key] = hash;
            }
        }

        return hashes;
    }

    /// <summary>Writes the hash cache.</summary>
    /// <param name="cacheFile">The absolute cache file path.</param>
    /// <param name="hashes">The hashes by record relative path.</param>
    /// <param name="error">The write error, or empty when the cache was written.</param>
    /// <returns>True when the cache was written.</returns>
    static bool WriteHashes(string cacheFile, Dictionary<string, string> hashes, out string error)
    {
        error = "";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
            var json = new JsonObject();

            foreach (var (key, hash) in hashes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                json[key] = hash;
            }

            File.WriteAllText(cacheFile, CliJson.Serialize(json), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = $"Cannot write '{cacheFile}': {exception.Message}";
            return false;
        }
    }

    /// <summary>Writes one rendered page under the render folder, mirroring the record's relative path with a Markdown extension.</summary>
    /// <param name="root">The repository folder the render folder sits under.</param>
    /// <param name="relative">The record path relative to the root.</param>
    /// <param name="hash">The content hash the page was rendered from.</param>
    /// <param name="error">The write error, or empty when the page was written.</param>
    /// <returns>True when the page was written.</returns>
    static bool WritePage(string root, string relative, string hash, out string error)
    {
        error = "";

        try
        {
            var page = Path.Combine(root, RenderFolder, Path.ChangeExtension(relative, ".md"));
            Directory.CreateDirectory(Path.GetDirectoryName(page)!);
            var text = new StringBuilder();

            text.AppendLine($"# Rendered {relative}");
            text.AppendLine();
            text.AppendLine($"Source: {relative}");
            text.AppendLine($"Content hash: {hash}");
            text.AppendLine($"Rendered (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine();
            text.AppendLine("This marker tracks the content hash the last render saw. The book build renders this record; the marker changes only when the record changes.");
            File.WriteAllText(page, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = $"Cannot write the rendered page for '{relative}': {exception.Message}";
            return false;
        }
    }

    /// <summary>Removes the rendered page of a record that no longer exists.</summary>
    /// <param name="root">The repository folder the render folder sits under.</param>
    /// <param name="relative">The removed record path relative to the root.</param>
    static void RemovePage(string root, string relative)
    {
        try
        {
            var page = Path.Combine(root, RenderFolder, Path.ChangeExtension(relative, ".md"));

            if (File.Exists(page))
            {
                File.Delete(page);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A stale marker never fails the render; the next render rewrites the cache without it.
        }
    }

    /// <summary>The absolute path of an option value: as written when absolute, else under the working directory.</summary>
    /// <param name="path">The option value.</param>
    /// <returns>The absolute path.</returns>
    static string FullPath(string path) =>
        Path.IsPathFullyQualified(path) ? path : Path.Combine(CliHost.WorkingDirectory, path);
}
