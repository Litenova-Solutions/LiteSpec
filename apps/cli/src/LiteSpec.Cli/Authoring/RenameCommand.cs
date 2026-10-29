using System.Text.Json;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec rename</c> command: renames one record identifier across
///     the consumer repository, moving its page and rewriting every link.
/// </summary>
static partial class RenameCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec rename <old> <new> [--root <path>] [--help]

        Renames one record identifier across the consumer repository. It moves
        the page to the path the new identifier owns, updates the opening
        specification metadata block, and rewrites every citation of the old
        identifier, so no link points at the old name after the rename. Both
        arguments are full record identifiers, such as use-case/lending.borrow-copy.
        A use-case page that already sits beside its aggregate stays in its
        folder and takes the new file name. The command refuses a new identifier
        a record already carries and an old identifier no record carries.

          --root    The consumer repository to change. Defaults to the working directory.
          --help    Print this text and exit.

        Exit codes: 0 renamed, 1 the run failed after the checks passed, 2 the old identifier names no record, the new identifier is taken, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--home"], maxPositionals: 2);

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (parsed.Positionals is not [var oldId, var newId])
        {
            Console.Error.WriteLine("Rename needs an old and a new identifier.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (!TryRecordId(oldId, out var oldType, out var oldHome, out _))
        {
            Console.Error.WriteLine($"'{oldId}' is not a record identifier.");
            return 2;
        }

        if (!TryRecordId(newId, out var newType, out var newHome, out var newName))
        {
            Console.Error.WriteLine($"'{newId}' is not a record identifier.");
            return 2;
        }

        if (oldType != newType)
        {
            Console.Error.WriteLine($"Cannot rename '{oldId}' to '{newId}': a rename keeps the record type.");
            return 2;
        }

        if (oldId == newId)
        {
            Console.Error.WriteLine($"'{oldId}' already names the record.");
            return 2;
        }

        string root;

        try
        {
            root = Path.GetFullPath(parsed.Value("--root") ?? parsed.Value("--home") ?? Directory.GetCurrentDirectory());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root") ?? parsed.Value("--home")}': {exception.Message}");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        ConsumerConfig config;
        IReadOnlyList<Record> records;

        try
        {
            config = ConsumerConfig.Load(root);
            records = RecordReader.Read(config).Records;
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($"litespec.project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            Console.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var old = records.FirstOrDefault(record => record.Id == oldId && !record.Derived);

        if (old is null)
        {
            Console.Error.WriteLine($"No record holds '{oldId}'.");
            return 2;
        }

        var clash = records.FirstOrDefault(record => record.Id == newId);

        if (clash is not null)
        {
            Console.Error.WriteLine($"Identifier '{newId}' is already taken by {config.Relative(clash.File)}.");
            return 2;
        }

        // The move runs before any content changes, so a blocked move leaves
        // the repository untouched.
        Move? movedPage = null;

        if (!old.IsEntry && MovePage(config, oldType, oldHome, newHome, newName, old.File) is { } moved)
        {
            movedPage = moved;

            if (!moved.Moved)
            {
                Console.Error.WriteLine(moved.Error);
                return 1;
            }

            Console.WriteLine($"Moved {config.Relative(moved.From)} -> {config.Relative(moved.To)}.");
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
                Console.Error.WriteLine($"Failed to read '{config.Relative(file)}': {exception.Message}");
                return 1;
            }

            var code = IsCodeFile(Path.GetExtension(file));
            int count;
            var rewritten = code
                ? ReplaceMarks(text, oldId, newId, Path.GetExtension(file), out count)
                : RecordPages.ReplaceCitations(text, oldId, newId, out count);

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
                Console.Error.WriteLine($"Failed to write '{config.Relative(file)}': {exception.Message}");
                return 1;
            }

            Console.WriteLine($"Updated {config.Relative(file)} ({count} {(code ? "marks" : "citations")}).");

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

        Console.WriteLine($"Rewrote {citations} citations in {files} files.");

        if (marks > 0)
        {
            Console.WriteLine($"Rewrote {marks} marks in {markFiles} files.");
        }

        if (movedPage is { Moved: true } page)
        {
            var links = RewriteLinks(root, page.From, page.To);
            Console.WriteLine($"Rewrote {links} links to the moved page.");
        }

        return 0;
    }

    /// <summary>Whether the extension holds code marks rather than record citations.</summary>
    static bool IsCodeFile(string extension) =>
        extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".ts", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".tsx", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mjs", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".svelte", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Rewrites the identifier inside marks only: doc tags and attributes in C#,
    ///     frontend constants in TypeScript. Any other string is left alone.
    /// </summary>
    static string ReplaceMarks(string text, string oldId, string newId, string extension, out int count)
    {
        count = 0;

        if (text.IndexOf(oldId, StringComparison.Ordinal) < 0)
        {
            return text;
        }

        var escaped = System.Text.RegularExpressions.Regex.Escape(oldId);

        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            text = ReplacePattern(text, $"<(implements|proves|follows)>\\s*{escaped}\\s*</\\1>", oldId, newId, ref count);
            text = ReplacePattern(text, $"\\b(Implements|Proves|Follows)(Attribute)?\\(\\s*\"{escaped}\"", oldId, newId, ref count);
        }
        else
        {
            text = ReplacePattern(text, $"\\bimplements\\s*:\\s*(?<quote>[\"']){escaped}\\k<quote>", oldId, newId, ref count);
        }

        return text;
    }

    /// <summary>Rewrites the identifier inside each match of the pattern, counting the rewrites.</summary>
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
    ///     Points every relative Markdown link at the page's new place: links from other pages to the
    ///     moved file or folder, and the moved page's own links when it changed folder.
    /// </summary>
    static int RewriteLinks(string root, string from, string to)
    {
        var fromFull = Path.GetFullPath(from);
        var toFull = Path.GetFullPath(to);
        var rewritten = 0;

        foreach (var file in RecordPages.ProjectFiles(root).Where(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var text = File.ReadAllText(file);
            var here = Path.GetDirectoryName(Path.GetFullPath(file))!;
            var movedHere = Under(Path.GetFullPath(file), toFull);

            // A page that moved resolves its old links from where it used to sit.
            var before = movedHere ? Path.GetDirectoryName(Remap(Path.GetFullPath(file), toFull, fromFull))! : here;
            var count = 0;

            var updated = MarkdownLink().Replace(text, match =>
            {
                var target = match.Groups["target"].Value;

                if (target.Length == 0 || target.StartsWith('#') || target.Contains("://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    return match.Value;
                }

                var resolved = Path.GetFullPath(Path.Combine(before, target.Replace('/', Path.DirectorySeparatorChar)));
                var pointsAtMoved = Under(resolved, fromFull);

                if (!pointsAtMoved && !movedHere)
                {
                    return match.Value;
                }

                var destination = pointsAtMoved ? Remap(resolved, fromFull, toFull) : resolved;
                var relative = Path.GetRelativePath(here, destination).Replace(Path.DirectorySeparatorChar, '/');

                if (relative == target)
                {
                    return match.Value;
                }

                count += 1;

                return match.Value.Replace("(" + target, "(" + relative, StringComparison.Ordinal);
            });

            if (count > 0)
            {
                File.WriteAllText(file, updated);
                rewritten += count;
            }
        }

        return rewritten;
    }

    /// <summary>Whether the path is the root itself or sits inside it.</summary>
    static bool Under(string path, string root) =>
        string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>The path with its leading root swapped for another.</summary>
    static string Remap(string path, string root, string replacement) =>
        replacement + path[root.Length..];

    [System.Text.RegularExpressions.GeneratedRegex(@"\]\((?<target>[^)\s#?]*)(?<rest>[^)]*)\)")]
    private static partial System.Text.RegularExpressions.Regex MarkdownLink();

    /// <summary>
    ///     Splits a full record identifier into its type, home, and name, following
    ///     the record identifier grammar.
    /// </summary>
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

    /// <summary>The outcome of moving the renamed page: moved, stayed, or blocked.</summary>
    sealed record Move(bool Moved, string From, string To, string Error);

    /// <summary>
    ///     Moves the renamed page to the path the new identifier owns. A module or
    ///     an aggregate moves with its folder; any other page moves as one file.
    ///     Returns null when the page stays where it is.
    /// </summary>
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

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(oldFile, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new Move(false, oldFile, target, $"Cannot move '{oldFile}': {exception.Message}");
        }

        return new Move(true, oldFile, target, "");
    }
}
