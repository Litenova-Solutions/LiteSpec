using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec-maintain changelog</c> command: writes the Added, Removed, and
///     Renamed provision IDs since a git ref, with the added and removed extensions,
///     into the marked section of <c>CHANGELOG.md</c>.
/// </summary>
/// <follows>litespec/core/authoring#generate-the-provision-diff-for-each-release</follows>
static class ProvisionDiffCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain changelog --from <ref> [--root <path>] [--check] [--help]

        Writes the Added, Removed, and Renamed provision IDs since the ref, with the added
        and removed extensions, into the marked section of CHANGELOG.md. The ref is a git
        tag or commit; the old index is read with git show, and the extensions are the ext
        pages each index names.

          --from    The git tag or commit to compare with. Required.
          --root    The repository root. Defaults to the LiteSpec checkout.
          --check   Compare the marked section with the computed lists and write nothing.
          --help    Print this text and exit.

        Exit codes: 0 written or current, 1 stale under --check, 2 usage error or unreadable input.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written or current, 1 stale under check, 2 usage error or unreadable input.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--check"], ["--from", "--root"]);

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

        var fromRef = parsed.Value("--from");

        if (fromRef is null)
        {
            Console.Error.WriteLine("Missing --from; name the git tag or commit to compare with.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (fromRef.Length == 0)
        {
            Console.Error.WriteLine("Option --from needs a value.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;
        var configured = parsed.Value("--root");

        if (configured is not null)
        {
            try
            {
                root = Path.GetFullPath(configured);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
            {
                Console.Error.WriteLine($"Invalid root '{configured}'.");
                Console.Error.WriteLine(Usage);
                return 2;
            }
        }
        else
        {
            try
            {
                root = LiteSpecHome.RequireRoot();
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine(Usage);
                return 2;
            }
        }

        // A mistyped root would otherwise yield an empty comparison written into a new folder.
        if (!Directory.Exists(Path.Combine(root, "docs")))
        {
            Console.Error.WriteLine($"Root '{root}' holds no docs folder.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string changelog;
        ProvisionDiff.Result diff;

        try
        {
            var newJson = ReadInput(root, Provisions.JsonPath, " Run: litespec-maintain provisions");
            changelog = ReadInput(root, ProvisionDiff.ChangelogPath, "");
            var oldRows = ProvisionDiff.RowsAtRef(root, fromRef);
            var newRows = ProvisionDiff.ReadRows(newJson, Provisions.JsonPath);
            diff = ProvisionDiff.Compare(oldRows, newRows, ProvisionDiff.ExtensionsOf(oldRows), ProvisionDiff.ExtensionsOf(newRows));
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        var target = Path.Combine(root, ProvisionDiff.ChangelogPath);
        var expected = ProvisionDiff.Apply(changelog, ProvisionDiff.Render(diff, fromRef));
        var actual = changelog.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!actual.EndsWith('\n'))
        {
            actual += "\n";
        }

        if (parsed.Has("--check"))
        {
            if (actual == expected)
            {
                Console.WriteLine($"{ProvisionDiff.ChangelogPath} is current.");
                return 0;
            }

            Console.Error.WriteLine($"{ProvisionDiff.ChangelogPath} is stale. Run: litespec-maintain changelog --from {fromRef}");
            return 1;
        }

        File.WriteAllText(target, expected);
        Console.WriteLine($"Wrote the provision changes since {fromRef} into {ProvisionDiff.ChangelogPath}.");
        return 0;
    }

    /// <summary>Reads one input file under the repository root.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="relative">The file path under the root, with forward-slash segments.</param>
    /// <param name="hint">The fix named when the file is missing, or empty for none.</param>
    /// <returns>The file text.</returns>
    static string ReadInput(string root, string relative, string hint)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidOperationException($"{relative} is missing.{hint}", exception);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"{relative} is unreadable: {exception.Message}", exception);
        }
    }
}
