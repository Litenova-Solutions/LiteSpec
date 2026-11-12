using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.ProvisionIndex;

/// <summary>
///     The <c>litespec-maintain provisions</c> command: writes <c>docs/reference/provisions.md</c>,
///     <c>docs/reference/provisions.json</c> and the site's <c>docs/reference/pages.json</c> from the active standards, with each provision's
///     inline verification mark, scope, record layer, trace, and strength, and the checked share
///     of rules for each page in the index.
/// </summary>
/// <follows>litespec/core/authoring#regenerate-the-provision-index</follows>
static class ProvisionsCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain provisions [--root <path>] [--check] [--help]

        Writes docs/reference/provisions.md, docs/reference/provisions.json and the site's page
        index docs/reference/pages.json from the active standards in root, which defaults to the
        LiteSpec checkout this tool belongs to. Each provision carries its record layer and trace,
        and the index states the checked share of rules for each page. It also writes the finding
        code table docs/reference/finding-codes.md, the record folder table docs/reference/folders.md,
        and the pin table inside docs/reference/manifest.md.

          --root    The repository root. Defaults to the LiteSpec checkout.
          --check   Compare the tracked files with the generated ones and write nothing.
          --help    Print this text and exit.

        Exit codes: 0 written or current, 1 stale under --check, 2 usage error.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 written or current, 1 stale under check, 2 usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--check"], ["--root"]);

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

        // A mistyped root would otherwise yield an empty index written into a new folder.
        if (!Directory.Exists(Path.Combine(root, "docs")))
        {
            Console.Error.WriteLine($"Root '{root}' holds no docs folder.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        (string Path, string Text)[] files;
        try
        {
            files =
            [
                (Provisions.IndexPath, Provisions.BuildIndex(root) + "\n"),
                (Provisions.JsonPath, Provisions.BuildJson(root) + "\n"),
                (Provisions.PagesPath, Provisions.BuildPages(root) + "\n"),
                .. ReferenceTables.Outputs(root),
            ];
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        if (parsed.Has("--check"))
        {
            var stale = false;
            foreach (var (path, text) in files)
            {
                var file = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
                var actual = File.Exists(file) ? File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
                if (actual == text)
                {
                    Console.WriteLine($"{path} is current.");
                }
                else
                {
                    Console.Error.WriteLine($"{path} is stale. Run: litespec-maintain provisions");
                    stale = true;
                }
            }

            return stale ? 1 : 0;
        }

        foreach (var (path, text) in files)
        {
            var file = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        Console.WriteLine($"Wrote {string.Join(", ", files.Select(file => file.Path))}.");
        return 0;
    }
}
