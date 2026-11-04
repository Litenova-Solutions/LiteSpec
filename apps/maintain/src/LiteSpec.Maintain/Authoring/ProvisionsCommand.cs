namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec-maintain provisions</c> command: writes <c>docs/reference/provisions.md</c>
///     and <c>docs/reference/provisions.json</c> from the active standards, with each provision's
///     inline verification mark, scope, and strength.
/// </summary>
/// <follows>litespec/core/authoring#regenerate-the-provision-index</follows>
static class ProvisionsCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain provisions [--root <path>] [--check] [--help]

        Writes docs/reference/provisions.md and docs/reference/provisions.json from the active
        standards in root, which defaults to the LiteSpec checkout this tool belongs to.

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

        var target = Path.Combine(root, Provisions.IndexPath.Replace('/', Path.DirectorySeparatorChar));
        var jsonTarget = Path.Combine(root, Provisions.JsonPath.Replace('/', Path.DirectorySeparatorChar));
        var expected = Provisions.BuildIndex(root) + "\n";
        var expectedJson = Provisions.BuildJson(root) + "\n";
        var actual = File.Exists(target) ? File.ReadAllText(target).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
        var actualJson = File.Exists(jsonTarget) ? File.ReadAllText(jsonTarget).Replace("\r\n", "\n", StringComparison.Ordinal) : null;

        if (parsed.Has("--check"))
        {
            var stale = false;
            if (actual == expected)
            {
                Console.WriteLine($"{Provisions.IndexPath} is current.");
            }
            else
            {
                Console.Error.WriteLine($"{Provisions.IndexPath} is stale. Run: litespec-maintain provisions");
                stale = true;
            }

            if (actualJson == expectedJson)
            {
                Console.WriteLine($"{Provisions.JsonPath} is current.");
            }
            else
            {
                Console.Error.WriteLine($"{Provisions.JsonPath} is stale. Run: litespec-maintain provisions");
                stale = true;
            }

            return stale ? 1 : 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, expected);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonTarget)!);
        File.WriteAllText(jsonTarget, expectedJson);
        Console.WriteLine($"Wrote {Provisions.IndexPath} and {Provisions.JsonPath}.");
        return 0;
    }
}
