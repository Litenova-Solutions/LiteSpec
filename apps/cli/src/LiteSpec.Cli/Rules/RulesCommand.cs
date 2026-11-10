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
            return LastResort.UsageError(error, Usage, json: false);
        }

        if (parsed.Positionals is not [var target])
        {
            return LastResort.UsageError("Rules needs a path or record id.", Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            return LastResort.Fail($"Cannot find the carried LiteSpec release: {exception.Message}", json: false, code: 2);
        }

        if (LoadManifest(home) is not { } manifest)
        {
            return LastResort.Fail($"The LiteSpec manifest '{LiteSpecHome.ManifestFile}' could not be read.", json: false, code: 2);
        }

        var backendStack = LiteSpec.Authoring.Layout.Stacks(manifest)
            .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal))
            ?.Id ?? "";

        var context = PathContext.Resolve(config, backendStack, target);

        if (context is null)
        {
            return LastResort.Fail($"No file or record '{target}'.", json: false, code: 1);
        }

        var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
        var conventions = ConventionStore.Load(config);
        var departures = DepartureStore.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
        var results = RuleRetriever.Retrieve(catalog, conventions, departures, config, context);

        Print(context, results, parsed.Has("--full"), config.Layers);
        return 0;
    }

    /// <summary>Reads the release manifest object, or null when it cannot be read.</summary>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <returns>The manifest object, or null.</returns>
    static JsonObject? LoadManifest(string home)
    {
        try
        {
            return LiteSpecHome.ReadManifest(home);
        }
        catch (Exception exception) when (exception is IOException or JsonTextException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Prints the retrieved provisions. Without full, the provisions that need
    ///     judgment and match the file kind or its record print as statements grouped
    ///     by strength, the checked provisions print as one line each with their
    ///     finding codes and analyzer IDs, and the layer-wide provisions print by
    ///     identifier under their own heading. With full, every provision prints as
    ///     a statement with its rationale and example.
    /// </summary>
    /// <param name="context">The rules target.</param>
    /// <param name="results">The retrieved provisions in print order.</param>
    /// <param name="full">Whether to print every provision with rationale and example.</param>
    /// <param name="claims">The record layers the project claims.</param>
    static void Print(PathContext context, IReadOnlyList<RuleResult> results, bool full, LayerClaims claims)
    {
        var parts = new List<string>();

        if (context.Stack.Length > 0)
        {
            parts.Add($"stack: {context.Stack}");
        }

        if (context.Concern.Length > 0)
        {
            parts.Add($"concern: {context.Concern}");
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
            Console.WriteLine(context.IsCodePath && claims.Implementation is null && claims.Proof is null
                ? "No provisions apply to a code path: the project claims neither the Implementation nor the Proof layer, so provisions about code do not apply. Query a record id for the provisions about records."
                : "No provisions apply.");
            return;
        }

        if (full)
        {
            PrintFull(results);
            return;
        }

        foreach (var group in ProvisionEntry.GroupOrder)
        {
            var items = results
                .Where(result => result.Entry.Group == group && !result.IsChecked && !result.IsConcernWide)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"{group}:");

            foreach (var item in items)
            {
                Console.WriteLine($"- {item.Entry.Id}: {item.Statement} [{string.Join("; ", item.Reasons)}]");
            }
        }

        var checkedItems = results
            .Where(result => result.IsChecked)
            .OrderBy(result => result.Entry.Id, StringComparer.Ordinal)
            .ToList();

        if (checkedItems.Count > 0)
        {
            Console.WriteLine("Checked by a tool:");

            foreach (var item in checkedItems)
            {
                Console.WriteLine($"- {item.Entry.Id}: checked by {CheckedDisplay(item.Entry)} [{string.Join("; ", item.Reasons)}]");
            }
        }

        var layerItems = results
            .Where(result => result.IsConcernWide)
            .OrderBy(result => result.Entry.Id, StringComparer.Ordinal)
            .ToList();

        if (layerItems.Count > 0)
        {
            Console.WriteLine("Concern-wide:");

            foreach (var item in layerItems)
            {
                Console.WriteLine($"- {item.Entry.Id}");
            }
        }
    }

    /// <summary>Prints every retrieved provision as a statement grouped by strength, with rationale and example.</summary>
    /// <param name="results">The retrieved provisions in print order.</param>
    static void PrintFull(IReadOnlyList<RuleResult> results)
    {
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

    /// <summary>The checked display of one provision: its finding codes and analyzer IDs, or its checking command when it names no code.</summary>
    /// <param name="entry">The checked provision.</param>
    /// <returns>The checked display.</returns>
    static string CheckedDisplay(ProvisionEntry entry)
    {
        if (entry.CheckedCodes.Count > 0)
        {
            return string.Join(", ", entry.CheckedCodes.Select(code => $"`{code}`"));
        }

        return entry.CheckedCommand.Length > 0 ? $"`{entry.CheckedCommand}`" : "a tool";
    }
}
