using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.RecordEditing;

/// <summary>
///     The <c>litespec rename</c> command: renames one record identifier across
///     the consumer repository, moving its record file and explanation file and
///     rewriting every citation.
/// </summary>
/// <follows>litespec/core/model#rewrite-every-link-on-a-rename</follows>
static class RenameCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec rename <old> <new> [--root <path>] [--help]

        Renames one record identifier across the consumer repository. It moves
        the record file and its explanation file to the path the new identifier
        owns, updates the record file, and rewrites every citation of the old
        identifier, so no link points at the old name after the rename. Both
        arguments are full record identifiers, such as use-case/lending.borrow-copy.
        A use-case file that already sits beside its aggregate stays in its
        folder and takes the new file name. The command refuses a new identifier
        a record already carries and an old identifier no record carries.

          --root    The consumer repository to change. Defaults to the working directory.
          --help    Print this text and exit.

        Exit codes: 0 renamed, 1 the run failed after the checks passed, 2 the old identifier names no record, the new identifier is taken, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the old and new identifiers and the options.</param>
    /// <returns>The process exit code: 0 when the rename completes, 1 when the run fails after the checks pass, and 2 on invalid input, an unknown old identifier or a taken new identifier.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root"], maxPositionals: 2);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        if (parsed.Positionals is not [var oldId, var newId])
        {
            return CommandFailure.UsageError("Rename needs an old and a new identifier.", Usage, json: false);
        }

        if (!TryRecordId(oldId, out var oldType, out var oldHome, out _))
        {
            CliHost.Error.WriteLine($"'{oldId}' is not a record identifier.");
            return 2;
        }

        if (!TryRecordId(newId, out var newType, out var newHome, out var newName))
        {
            CliHost.Error.WriteLine($"'{newId}' is not a record identifier.");
            return 2;
        }

        if (oldType != newType)
        {
            CliHost.Error.WriteLine($"Cannot rename '{oldId}' to '{newId}': a rename keeps the record type.");
            return 2;
        }

        if (oldId == newId)
        {
            CliHost.Error.WriteLine($"'{oldId}' already names the record.");
            return 2;
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;
        IReadOnlyList<Record> records;

        try
        {
            records = RecordReader.Read(config).Records;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            CliHost.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var old = records.FirstOrDefault(record => record.Id == oldId && !record.Derived);

        if (old is null)
        {
            CliHost.Error.WriteLine($"No record holds '{oldId}'.");
            return 2;
        }

        var clash = records.FirstOrDefault(record => record.Id == newId);

        if (clash is not null)
        {
            CliHost.Error.WriteLine($"Identifier '{newId}' is already taken by {PathText.Relative(config.Root, clash.File)}.");
            return 2;
        }

        // The move runs before any content changes, so a blocked move leaves
        // the repository untouched.
        if (!old.IsEntry && MovePage(config, oldType, oldHome, newHome, newName, old.File) is { } moved)
        {
            if (!moved.Moved)
            {
                CliHost.Error.WriteLine(moved.Error);
                return 1;
            }

            CliHost.Out.WriteLine($"Moved {PathText.Relative(config.Root, moved.From)} -> {PathText.Relative(config.Root, moved.To)}.");

            if (moved.ExplanationFrom is not null && moved.ExplanationTo is not null)
            {
                CliHost.Out.WriteLine($"Moved {PathText.Relative(config.Root, moved.ExplanationFrom)} -> {PathText.Relative(config.Root, moved.ExplanationTo)}.");
            }
        }

        var files = 0;
        var citations = 0;
        var markFiles = 0;
        var marks = 0;

        foreach (var file in RecordPages.ProjectFiles(root))
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to read '{PathText.Relative(config.Root, file)}': {exception.Message}");
                return 1;
            }

            var code = IsCodeFile(Path.GetExtension(file));
            int count;
            string rewritten;

            if (!code && IsRecordCandidate(config, file) && TryRewriteRecord(text, oldId, newId, out var recordText, out var recordCount))
            {
                rewritten = recordText;
                count = recordCount;
            }
            else
            {
                rewritten = code
                    ? ReplaceMarks(text, oldId, newId, Path.GetExtension(file), out count)
                    : RecordPages.ReplaceCitations(text, oldId, newId, out count);
            }

            if (count == 0)
            {
                continue;
            }

            try
            {
                File.WriteAllText(file, rewritten);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CliHost.Error.WriteLine($"Failed to write '{PathText.Relative(config.Root, file)}': {exception.Message}");
                return 1;
            }

            CliHost.Out.WriteLine($"Updated {PathText.Relative(config.Root, file)} ({count} {(code ? "marks" : "citations")}).");

            if (code)
            {
                markFiles += 1;
                marks += count;
            }
            else
            {
                files += 1;
                citations += count;
            }
        }

        CliHost.Out.WriteLine($"Rewrote {citations} citations in {files} files.");

        if (marks > 0)
        {
            CliHost.Out.WriteLine($"Rewrote {marks} marks in {markFiles} files.");
        }

        return 0;
    }

    /// <summary>Whether the extension holds code marks rather than record citations.</summary>
    /// <param name="extension">The file extension including the dot, in any casing.</param>
    /// <returns>True when the extension marks a code file that holds marks rather than citations; otherwise false.</returns>
    static bool IsCodeFile(string extension) =>
        extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".ts", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".tsx", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mjs", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".svelte", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Whether the file is a candidate for a parsed record rewrite: a record
    ///     file under a record root. The glossary is rewritten as text instead,
    ///     since it is not a record file.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the record roots and the glossary path.</param>
    /// <param name="file">The absolute path of the file.</param>
    /// <returns>True when the file is a record file under a record root and not the glossary; otherwise false.</returns>
    static bool IsRecordCandidate(ConsumerConfig config, string file)
    {
        if (!RecordFiles.IsRecordFile(file))
        {
            return false;
        }

        var full = Path.GetFullPath(file);

        if (config.GlossaryPath.Length > 0
            && string.Equals(full, Path.GetFullPath(config.GlossaryPath), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return config.RecordRoots.Any(root => full.StartsWith(
            root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Rewrites every citation of <paramref name="oldId"/> as
    ///     <paramref name="newId"/> in the record file's parsed values and writes
    ///     the file back in canonical form.
    /// </summary>
    /// <param name="text">The record file text to rewrite.</param>
    /// <param name="oldId">The identifier to replace.</param>
    /// <param name="newId">The identifier that replaces each standalone citation.</param>
    /// <param name="rewritten">When the result is true, the rewritten file in canonical form.</param>
    /// <param name="count">When the result is true, the number of citations rewritten.</param>
    /// <returns>True when the text parses as a record file; otherwise false, and the caller rewrites it as text.</returns>
    static bool TryRewriteRecord(string text, string oldId, string newId, out string rewritten, out int count)
    {
        rewritten = text;
        count = 0;

        if (!RecordFile.TryParse(text, out var root, out var tree, out _))
        {
            return false;
        }

        var type = root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("type", out var style)
            && style.ValueKind == JsonValueKind.String
                ? style.GetString()
                : null;

        if (type is null || (!RecordTypes.All.Contains(type) && !RecordTypes.ContainerStyles.Contains(type)))
        {
            return false;
        }

        if (RecordFile.NodeOf(tree) is not JsonObject node)
        {
            return false;
        }

        var total = 0;
        RewriteValues(node, oldId, newId, ref total);

        if (total == 0)
        {
            return true;
        }

        rewritten = RecordFile.Write(node);
        count = total;

        return true;
    }

    /// <summary>Rewrites the identifier inside every string value of the record, counting the rewrites.</summary>
    /// <param name="node">The record node whose string values are rewritten in place, or null when the subtree holds no value.</param>
    /// <param name="oldId">The identifier to replace inside each value.</param>
    /// <param name="newId">The identifier that replaces the old one inside each value.</param>
    /// <param name="count">The running rewrite count, increased by the citations rewritten in this subtree.</param>
    static void RewriteValues(JsonNode? node, string oldId, string newId, ref int count)
    {
        switch (node)
        {
            case JsonObject record:
                foreach (var key in record.Select(entry => entry.Key).ToList())
                {
                    if (record[key] is JsonValue value && value.TryGetValue<string>(out var current))
                    {
                        var replaced = RecordPages.ReplaceCitations(current, oldId, newId, out var found);

                        if (found > 0)
                        {
                            record[key] = replaced;
                            count += found;
                        }
                    }
                    else
                    {
                        RewriteValues(record[key], oldId, newId, ref count);
                    }
                }

                break;
            case JsonArray items:
                for (var index = 0; index < items.Count; index++)
                {
                    if (items[index] is JsonValue item && item.TryGetValue<string>(out var current))
                    {
                        var replaced = RecordPages.ReplaceCitations(current, oldId, newId, out var found);

                        if (found > 0)
                        {
                            items[index] = replaced;
                            count += found;
                        }
                    }
                    else
                    {
                        RewriteValues(items[index], oldId, newId, ref count);
                    }
                }

                break;
        }
    }

    /// <summary>
    ///     Rewrites the identifier inside marks only: doc tags and attributes in C#,
    ///     frontend constants in TypeScript. Any other string is left alone.
    /// </summary>
    /// <param name="text">The code file text to rewrite.</param>
    /// <param name="oldId">The identifier to replace inside marks.</param>
    /// <param name="newId">The identifier that replaces the old one inside marks.</param>
    /// <param name="extension">The file extension including the dot, which selects the mark forms to rewrite.</param>
    /// <param name="count">The number of marks rewritten.</param>
    /// <returns>The text with the identifier rewritten inside marks only, unchanged when it holds no citation.</returns>
    static string ReplaceMarks(string text, string oldId, string newId, string extension, out int count)
    {
        count = 0;

        if (text.IndexOf(oldId, StringComparison.Ordinal) < 0)
        {
            return text;
        }

        var escaped = System.Text.RegularExpressions.Regex.Escape(oldId);

        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".razor", StringComparison.OrdinalIgnoreCase))
        {
            text = ReplacePattern(text, $"<(implements|proves|follows)>\\s*{escaped}\\s*</\\1>", oldId, newId, ref count);
            text = ReplacePattern(text, $"\\b(Implements|Proves|Follows)(Attribute)?\\(\\s*\"{escaped}\"", oldId, newId, ref count);
        }

        if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // A route mark and a module mark are frontend constants, and a
            // browser test names its proof in a proves(...) call or a raw
            // annotation: a screen or scenario rename rewrites all three.
            text = ReplacePattern(text, $"\\bimplements\\s*:\\s*(?<quote>[\"']){escaped}\\k<quote>", oldId, newId, ref count);
            text = ReplacePattern(text, $"\\bproves\\s*\\(\\s*(?<quote>[\"']){escaped}\\k<quote>", oldId, newId, ref count);
            text = ReplacePattern(text, $"\\bdescription\\s*:\\s*(?<quote>[\"']){escaped}\\k<quote>", oldId, newId, ref count);
        }

        return text;
    }

    /// <summary>Rewrites the identifier inside each match of the pattern, counting the rewrites.</summary>
    /// <param name="text">The code file text to rewrite.</param>
    /// <param name="pattern">The regular expression that matches one mark form.</param>
    /// <param name="oldId">The identifier to replace inside each match.</param>
    /// <param name="newId">The identifier that replaces the old one inside each match.</param>
    /// <param name="count">The running rewrite count, increased by the number of matches.</param>
    /// <returns>The text with the identifier rewritten inside each match of the pattern.</returns>
    static string ReplacePattern(string text, string pattern, string oldId, string newId, ref int count)
    {
        var found = 0;

        var rewritten = new System.Text.RegularExpressions.Regex(pattern).Replace(
            text,
            match =>
            {
                found++;
                return match.Value.Replace(oldId, newId, StringComparison.Ordinal);
            });

        count += found;

        return rewritten;
    }

    /// <summary>
    ///     Splits a full record identifier into its type, home, and name, following
    ///     the record identifier grammar.
    /// </summary>
    /// <param name="full">The full candidate record identifier.</param>
    /// <param name="type">When the result is true, the record type segment; otherwise an empty string.</param>
    /// <param name="home">When the result is true, the home segment, or null for a type without a home.</param>
    /// <param name="name">When the result is true, the name segment; otherwise an empty string.</param>
    /// <returns>True when the text is a grammatically valid identifier of a known record type; otherwise false.</returns>
    static bool TryRecordId(string full, out string type, out string? home, out string name)
    {
        type = "";
        home = null;
        name = "";

        if (!Identifiers.TrySplit(full, out var prefix, out var splitHome, out var splitName)
            || !RecordTypes.All.Contains(prefix)
            || RecordPages.GrammarError(prefix, splitHome, splitName, full) is not null)
        {
            return false;
        }

        type = prefix;
        home = splitHome;
        name = splitName;

        return true;
    }

    /// <summary>The outcome of moving the renamed file: moved, stayed, or blocked.</summary>
    sealed record Move(bool Moved, string From, string To, string Error, string? ExplanationFrom = null, string? ExplanationTo = null);

    /// <summary>
    ///     Moves the renamed file to the path the new identifier owns, with its
    ///     explanation file beside it. A module or an aggregate moves with its
    ///     folder; any other file moves as one file with its explanation.
    ///     Returns null when the file stays where it is.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <param name="oldFile">The current absolute path of the file.</param>
    /// <returns>The move outcome, or null when the file stays at its current path.</returns>
    static Move? MovePage(
        ConsumerConfig config, string type, string? oldHome, string? newHome, string newName, string oldFile)
    {
        if (type is "module" or "aggregate")
        {
            var oldFolder = Path.GetDirectoryName(oldFile)!;
            var newFolder = RecordPages.RenameFolderTarget(config, type, newHome, newName);

            if (string.Equals(oldFolder, newFolder, StringComparison.Ordinal))
            {
                return null;
            }

            if (!Directory.Exists(oldFolder))
            {
                return new Move(false, oldFolder, newFolder, $"Cannot move '{oldFolder}': the folder does not exist.");
            }

            if (Directory.Exists(newFolder) || File.Exists(newFolder))
            {
                return new Move(false, oldFolder, newFolder, $"Cannot move '{oldFolder}': '{newFolder}' already exists.");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFolder)!);
                Directory.Move(oldFolder, newFolder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return new Move(false, oldFolder, newFolder, $"Cannot move '{oldFolder}': {exception.Message}");
            }

            return new Move(true, oldFile, Path.Combine(newFolder, Path.GetFileName(oldFile)), "");
        }

        var target = RecordPages.RenameTarget(config, type, oldHome, newHome, newName, oldFile);

        if (target is null)
        {
            return null;
        }

        if (File.Exists(target) || Directory.Exists(target))
        {
            return new Move(false, oldFile, target, $"Cannot move '{oldFile}': '{target}' already exists.");
        }

        var explanationFrom = RecordFile.ExplanationPathOf(oldFile);
        var explanationTo = RecordFile.ExplanationPathOf(target);
        var hasExplanation = !string.Equals(explanationFrom, oldFile, StringComparison.OrdinalIgnoreCase)
            && File.Exists(explanationFrom);

        if (hasExplanation && (File.Exists(explanationTo) || Directory.Exists(explanationTo)))
        {
            return new Move(false, oldFile, target, $"Cannot move '{explanationFrom}': '{explanationTo}' already exists.");
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(oldFile, target);

            if (hasExplanation)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(explanationTo)!);
                File.Move(explanationFrom, explanationTo);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new Move(false, oldFile, target, $"Cannot move '{oldFile}': {exception.Message}");
        }

        return hasExplanation
            ? new Move(true, oldFile, target, "", explanationFrom, explanationTo)
            : new Move(true, oldFile, target, "");
    }
}
