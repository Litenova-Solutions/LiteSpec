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
        Usage: litespec rename <old> <new> [--move] [--root <path>] [--help]

        Renames one record identifier across the consumer repository. A plain
        rename keeps the record at its level and in its owner's folder: the
        record's own folder takes the new name, and the record file and its
        explanation file take the new stem inside it. A named record folder
        moves whole, so every supporting file the folder holds moves with it.
        A module or aggregate folder moves whole with every record in it, and a
        feature folder moves whole with its requirement folders. A requirement
        stays in its feature's requirements folder. A plain rename refuses a new
        identifier that names another owner, such as a use case under a different
        module or a requirement under a different feature. --move accepts that
        change and moves the record into the folder its new owner holds. The
        command updates the record file and rewrites every citation of the old
        identifier, so no link points at the old name after the rename. Both
        arguments are full record identifiers, such as use-case/lending.borrow-copy.
        The command checks every destination before it moves anything. A move
        that fails part way is reported, and the command does not roll it back.
        It refuses a new identifier a record already carries and an old
        identifier no record carries.

          --move    Move the record to the owner its new identifier names.
          --root    The consumer repository to change. Defaults to the working directory.
          --help    Print this text and exit.

        Exit codes: 0 renamed, 1 the run failed after the checks passed, 2 the old identifier names no record, the new identifier is taken or names another owner without --move, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the old and new identifiers and the options.</param>
    /// <returns>The process exit code: 0 when the rename completes, 1 when the run fails after the checks pass, and 2 on invalid input, an unknown old identifier or a taken new identifier.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--move"], ["--root"], maxPositionals: 2);

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

        var move = parsed.Has("--move");

        if (!move && !string.Equals(oldHome, newHome, StringComparison.Ordinal))
        {
            CliHost.Error.WriteLine($"Cannot rename '{oldId}' to '{newId}' without --move: the new identifier names another owner. A plain rename keeps the record in its folder; add --move to move it there.");
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

        if (oldType == RecordTypes.Requirement && newHome != oldHome && RecordPages.FeatureFolder(records, newHome!) is null)
        {
            CliHost.Error.WriteLine($"No feature '{newHome}' holds the requirement; write feature/{newHome} first.");
            return 2;
        }

        // The move runs before any content changes, so a blocked move leaves
        // the repository untouched. The requirements of a moved feature are
        // renamed in the citations below, with the feature.
        var renames = new List<(string From, string To)> { (oldId, newId) };
        renames.AddRange(FeatureRequirementRenames(records, old, newName));

        if (!old.IsEntry && MovePage(config, records, oldType, oldHome, newHome, newName, old.File, move) is { } moved)
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
            var rewritten = text;
            var count = 0;

            foreach (var (from, to) in renames)
            {
                int found;

                if (!code && IsRecordCandidate(config, file) && TryRewriteRecord(rewritten, from, to, out var recordText, out var recordCount))
                {
                    rewritten = recordText;
                    found = recordCount;
                }
                else
                {
                    rewritten = code
                        ? ReplaceMarks(rewritten, from, to, Path.GetExtension(file), out found)
                        : RecordPages.ReplaceCitations(rewritten, from, to, out found);
                }

                count += found;
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
    ///     Moves the renamed record to the folder its new identifier gives it. A plain rename keeps the folder the
    ///     record sits in and takes the new name; a rename with <paramref name="move"/> places the record in the
    ///     folder its new owner holds. A named record's folder moves whole, so its supporting files come with it.
    ///     Every destination is checked before anything moves. Returns null when the record stays where it is.
    /// </summary>
    /// <param name="config">The consumer configuration that holds the documentation roots.</param>
    /// <param name="records">Every record the model read, which finds the feature a requirement moves into.</param>
    /// <param name="type">The record type that owns the file.</param>
    /// <param name="oldHome">The identifier home before the rename, or null for a type without a home.</param>
    /// <param name="newHome">The identifier home after the rename, or null for a type without a home.</param>
    /// <param name="newName">The identifier name after the rename.</param>
    /// <param name="oldFile">The current absolute path of the file.</param>
    /// <param name="move">Whether the rename moves the record to the owner its new identifier names.</param>
    /// <returns>The move outcome, or null when the record stays at its current path.</returns>
    static Move? MovePage(
        ConsumerConfig config, IReadOnlyList<Record> records, string type, string? oldHome, string? newHome, string newName, string oldFile, bool move)
    {
        var oldFolder = Path.GetDirectoryName(oldFile)!;

        if (type == RecordTypes.Feature)
        {
            return MoveFolder(oldFolder, Path.Combine(Path.GetDirectoryName(oldFolder)!, newName), oldFile, renameFile: true);
        }

        // A module and an aggregate keep their file names, module.yaml and aggregate.yaml, inside the folder that takes the new name.
        if (type == RecordTypes.Module)
        {
            return MoveFolder(oldFolder, Path.Combine(Path.GetDirectoryName(oldFolder)!, newName), oldFile, renameFile: false);
        }

        if (type == RecordTypes.Aggregate)
        {
            var aggregates = move
                ? Path.Combine(config.BehaviorDocs, "modules", newHome!, "aggregates")
                : Path.GetDirectoryName(oldFolder)!;

            return MoveFolder(oldFolder, Path.Combine(aggregates, newName), oldFile, renameFile: false);
        }

        string? container;

        if (type == RecordTypes.Requirement)
        {
            container = move ? RecordPages.FeatureFolder(records, newHome!) : RecordPages.ContainerOf(oldFile);
        }
        else
        {
            container = move ? RecordPages.OwnerContainer(config, type, newHome, newName, oldFile) : RecordPages.ContainerOf(oldFile);
        }

        if (container is null)
        {
            return null;
        }

        var stem = type == RecordTypes.Requirement ? newName : RecordPages.StemOf(config, type, newHome, newName);

        // A named record sits in its own folder, which moves whole. A record file with no folder of its own moves alone.
        if (!string.Equals(Path.GetFileName(oldFolder), Path.GetFileNameWithoutExtension(oldFile), StringComparison.Ordinal))
        {
            var flat = Path.Combine(container, stem + Path.GetExtension(oldFile));

            return string.Equals(flat, oldFile, StringComparison.Ordinal)
                ? null
                : MoveFlat(oldFile, flat);
        }

        return MoveFolder(oldFolder, Path.Combine(container, stem), oldFile, renameFile: true);
    }

    /// <summary>
    ///     Moves a record folder to the new folder, checking every destination before anything moves. With
    ///     <paramref name="renameFile"/> the record file and its explanation take the new folder's name as their stem.
    /// </summary>
    /// <param name="oldFolder">The folder that holds the record now.</param>
    /// <param name="newFolder">The folder the record moves to.</param>
    /// <param name="oldFile">The current absolute path of the record file.</param>
    /// <param name="renameFile">Whether the record file and its explanation take the new folder's name.</param>
    /// <returns>The move outcome, or null when the folder already sits at the new path.</returns>
    static Move? MoveFolder(string oldFolder, string newFolder, string oldFile, bool renameFile)
    {
        if (string.Equals(oldFolder, newFolder, StringComparison.Ordinal))
        {
            return null;
        }

        var extension = Path.GetExtension(oldFile);
        var stem = Path.GetFileNameWithoutExtension(oldFile);
        var newStem = renameFile ? Path.GetFileName(newFolder) : stem;
        var target = Path.Combine(newFolder, newStem + extension);
        var explanationFrom = RecordFile.ExplanationPathOf(oldFile);
        var hasExplanation = !string.Equals(explanationFrom, oldFile, StringComparison.OrdinalIgnoreCase) && File.Exists(explanationFrom);
        var explanationTo = Path.Combine(newFolder, newStem + RecordFile.ExplanationExtension);

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

            if (renameFile)
            {
                File.Move(Path.Combine(newFolder, stem + extension), target);
            }

            if (hasExplanation)
            {
                File.Move(Path.Combine(newFolder, stem + RecordFile.ExplanationExtension), explanationTo);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new Move(false, oldFolder, newFolder, $"Cannot move '{oldFolder}': {exception.Message}");
        }

        return hasExplanation
            ? new Move(true, oldFile, target, "", explanationFrom, explanationTo)
            : new Move(true, oldFile, target, "");
    }

    /// <summary>
    ///     Moves a record file that has no folder of its own, with its explanation beside it, checking both destinations first.
    /// </summary>
    /// <param name="oldFile">The current absolute path of the record file.</param>
    /// <param name="target">The path the record file moves to.</param>
    /// <returns>The move outcome.</returns>
    static Move MoveFlat(string oldFile, string target)
    {
        var explanationFrom = RecordFile.ExplanationPathOf(oldFile);
        var hasExplanation = !string.Equals(explanationFrom, oldFile, StringComparison.OrdinalIgnoreCase) && File.Exists(explanationFrom);
        var explanationTo = RecordFile.ExplanationPathOf(target);

        if (File.Exists(target) || Directory.Exists(target))
        {
            return new Move(false, oldFile, target, $"Cannot move '{oldFile}': '{target}' already exists.");
        }

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

    /// <summary>
    ///     The identifier renames a feature rename carries to its requirements: each requirement in the feature's
    ///     folder takes the new feature name in its identifier, keeping its own name.
    /// </summary>
    /// <param name="records">Every record the model read, before the move.</param>
    /// <param name="feature">The record the rename names, which is a feature or any other record.</param>
    /// <param name="newName">The feature name after the rename.</param>
    /// <returns>The old and new identifier of each requirement the feature holds; empty for any other record.</returns>
    static List<(string From, string To)> FeatureRequirementRenames(IReadOnlyList<Record> records, Record feature, string newName)
    {
        var renames = new List<(string From, string To)>();

        if (feature.Type != RecordTypes.Feature)
        {
            return renames;
        }

        var folder = Path.GetDirectoryName(feature.File);

        // A requirement's folder sits in the requirements folder, which sits in the feature's folder.
        foreach (var requirement in records.Where(record =>
            record.Type == RecordTypes.Requirement
            && !record.Derived
            && Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(record.File))) == folder))
        {
            if (Identifiers.TrySplit(requirement.Id, out _, out _, out var requirementName))
            {
                renames.Add((requirement.Id, $"requirement/{newName}.{requirementName}"));
            }
        }

        return renames;
    }
}
