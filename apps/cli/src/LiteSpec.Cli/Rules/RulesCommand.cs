using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Authoring;
using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     The <c>litespec rules</c> command: lists the provisions that govern a path or
///     record identifier, grouped by strength, with project conventions and departure
///     exceptions merged in.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
static class RulesCommand
{
    /// <summary>The usage text for the rules command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec rules [--root <consumer>] <path or record id> [--full]
          litespec rules --help

        Exit codes: 0 the provisions are listed, 1 the target names no file or record, 2 usage error or an unreadable consumer or carried release.
        """;

    /// <summary>Runs the rules command.</summary>
    /// <param name="args">The command arguments, the target first.</param>
    /// <returns>The exit code: 0 listed, 1 unknown target, 2 usage error or unreadable input.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Run is the last-resort command handler: every failure becomes a Failed message with exit code 1, so no exception type is excluded.")]
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var parsed = CliArgs.Parse(args, ["--full"], ["--root"], maxPositionals: 1);

        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (parsed.Positionals is not [var target])
        {
            Console.Error.WriteLine("Rules needs a path or record id.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;

        try
        {
            root = ConsumerRoot.Resolve(parsed.Value("--root"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            return 2;
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Root '{root}' does not exist.");
            return 2;
        }

        // Without a project file the selection is unknown, and the command
        // would list provisions for stacks the consumer never selected.
        if (!File.Exists(ConsumerRoot.ProjectFile(root)))
        {
            Console.Error.WriteLine($"Root '{root}' holds no .litespec/project.json.");
            return 2;
        }

        ConsumerConfig config;

        try
        {
            config = ConsumerConfig.Load(root);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($".litespec/project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine($"Cannot find the carried LiteSpec release: {exception.Message}");
            return 2;
        }

        if (LoadManifest(home) is not { } manifest)
        {
            Console.Error.WriteLine($"The LiteSpec manifest '{LiteSpecHome.ManifestFile}' could not be read.");
            return 2;
        }

        var backendStack = LiteSpec.Authoring.Layout.Stacks(manifest)
            .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal))
            ?.Id ?? "";

        try
        {
            var context = PathContext.Resolve(config, backendStack, target);

            if (context is null)
            {
                Console.Error.WriteLine($"No file or record '{target}'.");
                return 1;
            }

            var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
            var conventions = ConventionStore.Load(config);
            var departures = DepartureStore.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
            var results = RuleRetriever.Retrieve(catalog, conventions, departures, config, context);

            Print(context, results, parsed.Has("--full"));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed: {exception.Message}");
            return 1;
        }
    }

    /// <summary>Reads the release manifest object, or null when it cannot be read.</summary>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <returns>The manifest object, or null.</returns>
    static JsonObject? LoadManifest(string home)
    {
        try
        {
            var file = Path.Combine(home, LiteSpecHome.ManifestFile);
            return JsonNode.Parse(File.ReadAllText(file)) as JsonObject;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Prints the retrieved provisions grouped by strength, one line each.</summary>
    /// <param name="context">The rules target.</param>
    /// <param name="results">The retrieved provisions in print order.</param>
    /// <param name="full">Whether to print rationale and example under each item.</param>
    static void Print(PathContext context, IReadOnlyList<RuleResult> results, bool full)
    {
        var parts = new List<string>();

        if (context.Stack.Length > 0)
        {
            parts.Add($"stack: {context.Stack}");
        }

        if (context.Layer.Length > 0)
        {
            parts.Add($"layer: {context.Layer}");
        }

        if (context.Artifact.Length > 0)
        {
            parts.Add($"artifact: {context.Artifact}");
        }

        Console.WriteLine(parts.Count > 0
            ? $"Rules for {context.Display} ({string.Join(", ", parts)})"
            : $"Rules for {context.Display}");

        if (results.Count == 0)
        {
            Console.WriteLine("No provisions apply.");
            return;
        }

        foreach (var group in ProvisionEntry.GroupOrder)
        {
            var items = results.Where(result => result.Entry.Group == group).ToList();

            if (items.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"{group}:");

            foreach (var item in items)
            {
                Console.WriteLine($"- {item.Entry.Id}: {item.Statement} [{string.Join("; ", item.Reasons)}]");

                if (full)
                {
                    if (item.Departure is not null)
                    {
                        if (item.Rationale.Length > 0)
                        {
                            Console.WriteLine($"    Consequences: {item.Rationale}");
                        }
                    }
                    else
                    {
                        if (item.Rationale.Length > 0)
                        {
                            Console.WriteLine($"    Rationale: {item.Rationale}");
                        }

                        if (item.Example.Length > 0)
                        {
                            Console.WriteLine($"    Example: {item.Example}");
                        }
                    }
                }
            }
        }
    }
}
