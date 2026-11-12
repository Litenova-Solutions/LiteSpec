using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The <c>litespec rules</c> command: lists the provisions that govern a path or
///     record identifier, grouped by strength, with project conventions and departures
///     merged in.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
static class RulesCommand
{
    /// <summary>The usage text for the rules command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec rules [--root <consumer>] <path or record id> [--full]
          litespec rules [--root <consumer>] --since <ref>
          litespec rules [--root <consumer>] <provision id or page id>
          litespec rules --help

        Exit codes: 0 the provisions are listed, 1 the target names no file, record, provision or page, 2 usage error or an unreadable consumer or carried release.
        """;

    /// <summary>Runs the rules command.</summary>
    /// <param name="args">The command arguments, the target first.</param>
    /// <returns>The exit code: 0 listed, 1 unknown target, 2 usage error or unreadable input.</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var parsed = CliArgs.Parse(args, ["--full"], ["--root", "--since"], maxPositionals: 1);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        string home;

        try
        {
            home = LiteSpecHome.RequireRoot();
        }
        catch (InvalidOperationException exception)
        {
            return CommandFailure.Fail($"Cannot find the carried LiteSpec release: {exception.Message}", json: false, code: 2);
        }

        if (LoadManifest(home) is not { } manifest)
        {
            return CommandFailure.Fail($"The LiteSpec manifest '{LiteSpecHome.ManifestFile}' could not be read.", json: false, code: 2);
        }

        var backendStack = StackSelection.Stacks(manifest)
            .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal))
            ?.Id ?? "";

        if (parsed.Value("--since") is { } sinceRef)
        {
            if (parsed.Positionals.Count > 0)
            {
                return CommandFailure.UsageError("Rules --since takes no path or record id.", Usage, json: false);
            }

            return RunSince(config, home, manifest, backendStack, sinceRef);
        }

        if (parsed.Positionals is not [var target])
        {
            return CommandFailure.UsageError("Rules needs a path, record id, provision id or page id.", Usage, json: false);
        }

        var context = PathContext.Resolve(config, backendStack, target);

        if (context is not null)
        {
            var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
            var conventions = ConventionFiles.Load(config);
            var departures = DepartureFiles.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
            var results = ProvisionRetriever.Retrieve(catalog, conventions, departures, config, context);

            Print(context, results, parsed.Has("--full"), config.Layers);
            return 0;
        }

        return RunLookup(home, target);
    }

    /// <summary>Runs the change-set mode: the union of judgment provisions over the files changed since the revision.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <param name="manifest">The release manifest object.</param>
    /// <param name="backendStack">The selected backend stack id, or empty.</param>
    /// <param name="sinceRef">The git revision to diff the working tree against.</param>
    /// <returns>The exit code: 0 listed, 2 when the changes cannot be read.</returns>
    static int RunSince(ConsumerConfig config, string home, JsonObject manifest, string backendStack, string sinceRef)
    {
        var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
        var conventions = ConventionFiles.Load(config);
        var departures = DepartureFiles.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
        var (files, error) = RulesSince.ChangedFiles(config.Root, sinceRef);

        if (files is null)
        {
            return CommandFailure.Fail(error, json: false, code: 2);
        }

        var union = new Dictionary<string, ProvisionMatch>(StringComparer.Ordinal);
        var queried = 0;

        foreach (var file in files.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!IsUnder(config.Root, file) || (!File.Exists(file) && !Directory.Exists(file)))
            {
                continue;
            }

            var context = PathContext.Resolve(config, backendStack, file);

            if (context is null)
            {
                continue;
            }

            queried++;

            foreach (var match in ProvisionRetriever.Retrieve(catalog, conventions, departures, config, context))
            {
                if (!match.IsChecked && !match.IsConcernWide && !union.ContainsKey(match.Entry.Id))
                {
                    union[match.Entry.Id] = match;
                }
            }
        }

        var groups = ProvisionEntry.GroupOrder.ToList();
        var ordered = union.Values.ToList();

        ordered.Sort((left, right) =>
        {
            var rank = groups.IndexOf(left.Entry.Group).CompareTo(groups.IndexOf(right.Entry.Group));
            return rank != 0 ? rank : string.CompareOrdinal(left.Entry.Id, right.Entry.Id);
        });

        PrintSince(sinceRef, queried, ordered);
        return 0;
    }

    /// <summary>Runs identifier lookup: one provision in full, or one page's provision list.</summary>
    /// <param name="home">The carried LiteSpec release root.</param>
    /// <param name="target">The provision identifier or page identifier.</param>
    /// <returns>The exit code: 0 listed, 1 when the identifier names nothing.</returns>
    static int RunLookup(string home, string target)
    {
        var trimmed = target.Trim();

        if (Provisions.IsProvisionId(trimmed))
        {
            var catalog = ProvisionCatalog.LoadAll(home);
            var entry = catalog.Entries.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, trimmed, StringComparison.Ordinal));

            if (entry is null)
            {
                return CommandFailure.Fail($"No provision with ID '{trimmed}'.", json: false, code: 1);
            }

            RulesLookup.PrintProvision(entry);
            return 0;
        }

        if (RulesLookup.IsPageId(trimmed))
        {
            var catalog = ProvisionCatalog.LoadAll(home);
            var page = RulesLookup.PagePathOf(trimmed);
            var entries = catalog.Entries
                .Where(candidate => string.Equals(candidate.Page, page, StringComparison.Ordinal))
                .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
                .ToList();

            if (entries.Count == 0)
            {
                return CommandFailure.Fail($"No provisions on page '{trimmed}'.", json: false, code: 1);
            }

            RulesLookup.PrintPage(trimmed, entries);
            return 0;
        }

        return CommandFailure.Fail($"No file or record '{target}'.", json: false, code: 1);
    }

    /// <summary>Whether the folder holds the path.</summary>
    /// <param name="folder">The absolute folder.</param>
    /// <param name="path">The absolute path.</param>
    /// <returns>True when the folder holds the path.</returns>
    static bool IsUnder(string folder, string path)
    {
        var full = Path.GetFullPath(path);
        var basePath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), basePath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
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
    ///     judgment and match the file kind print as statements grouped by strength,
    ///     the provisions matched only through a marked record collapse into one line
    ///     per record, the checked provisions print as one line each with their
    ///     finding codes and analyzer IDs, and the layer-wide provisions print by
    ///     identifier under their own heading. With full, every provision prints as
    ///     a statement with its rationale and example, with collapsed records after.
    /// </summary>
    /// <param name="context">The rules target.</param>
    /// <param name="results">The retrieved provisions in print order.</param>
    /// <param name="full">Whether to print every provision with rationale and example.</param>
    /// <param name="claims">The record layers the project claims.</param>
    static void Print(PathContext context, IReadOnlyList<ProvisionMatch> results, bool full, LayerClaims claims)
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

        CliHost.Out.WriteLine(parts.Count > 0
            ? $"Rules for {context.Display} ({string.Join(", ", parts)})"
            : $"Rules for {context.Display}");

        var collapsed = context.IsCodePath
            ? results.Where(result => result.MatchesOnlyThroughMarkedRecord).ToList()
            : [];
        var rest = context.IsCodePath
            ? results.Where(result => !result.MatchesOnlyThroughMarkedRecord).ToList()
            : results.ToList();

        if (rest.Count == 0 && collapsed.Count == 0)
        {
            CliHost.Out.WriteLine(context.IsCodePath && claims.Implementation is null && claims.Proof is null
                ? "No provisions apply to a code path: the project claims neither the Implementation nor the Proof layer, so provisions about code do not apply. Query a record id for the provisions about records."
                : "No provisions apply.");
            return;
        }

        if (full)
        {
            PrintFull(rest);
            PrintCollapsed(collapsed);
            return;
        }

        foreach (var group in ProvisionEntry.GroupOrder)
        {
            var items = rest
                .Where(result => result.Entry.Group == group && !result.IsChecked && !result.IsConcernWide)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            CliHost.Out.WriteLine($"{group}:");

            foreach (var item in items)
            {
                CliHost.Out.WriteLine($"- {item.Entry.Id}: {item.Statement} [{string.Join("; ", item.Reasons)}]");
            }
        }

        PrintCollapsed(collapsed);

        var checkedItems = rest
            .Where(result => result.IsChecked)
            .OrderBy(result => result.Entry.Id, StringComparer.Ordinal)
            .ToList();

        if (checkedItems.Count > 0)
        {
            CliHost.Out.WriteLine("Checked by a tool:");

            foreach (var item in checkedItems)
            {
                CliHost.Out.WriteLine($"- {item.Entry.Id}: checked by {CheckedDisplay(item.Entry)} [{string.Join("; ", item.Reasons)}]");
            }
        }

        var layerItems = rest
            .Where(result => result.IsConcernWide)
            .OrderBy(result => result.Entry.Id, StringComparer.Ordinal)
            .ToList();

        if (layerItems.Count > 0)
        {
            CliHost.Out.WriteLine("Concern-wide:");

            foreach (var item in layerItems)
            {
                CliHost.Out.WriteLine($"- {item.Entry.Id}");
            }
        }
    }

    /// <summary>Prints one collapsed line per marked record holding provisions about the record.</summary>
    /// <param name="collapsed">The provisions matched only through marked records.</param>
    static void PrintCollapsed(IReadOnlyList<ProvisionMatch> collapsed)
    {
        var records = collapsed
            .SelectMany(match => match.RecordIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        foreach (var record in records)
        {
            var about = collapsed.Where(match => match.RecordIds.Contains(record, StringComparer.Ordinal)).ToList();
            var checkedCount = about.Count(match => match.IsChecked);

            CliHost.Out.WriteLine($"{record}: {about.Count} provisions about the record; model check checks {checkedCount}; run rules {record} for the rest");
        }
    }

    /// <summary>Prints the change-set report skeleton: one judgment provision per entry with an empty How judged line.</summary>
    /// <param name="sinceRef">The git revision the change set was read from.</param>
    /// <param name="files">How many changed files were queried.</param>
    /// <param name="union">The judgment provisions in print order.</param>
    static void PrintSince(string sinceRef, int files, List<ProvisionMatch> union)
    {
        CliHost.Out.WriteLine($"Rules since {sinceRef} ({files} files, {union.Count} judgment provisions):");

        if (union.Count == 0)
        {
            CliHost.Out.WriteLine("No judgment provisions apply.");
            return;
        }

        foreach (var match in union)
        {
            CliHost.Out.WriteLine($"- {match.Entry.Id}: {match.Entry.ReviewedFor}");
            CliHost.Out.WriteLine("  How judged:");
        }
    }

    /// <summary>Prints every retrieved provision as a statement grouped by strength, with rationale and example.</summary>
    /// <param name="results">The retrieved provisions in print order.</param>
    static void PrintFull(IReadOnlyList<ProvisionMatch> results)
    {
        foreach (var group in ProvisionEntry.GroupOrder)
        {
            var items = results.Where(result => result.Entry.Group == group).ToList();

            if (items.Count == 0)
            {
                continue;
            }

            CliHost.Out.WriteLine($"{group}:");

            foreach (var item in items)
            {
                CliHost.Out.WriteLine($"- {item.Entry.Id}: {item.Statement} [{string.Join("; ", item.Reasons)}]");

                if (item.Departure is not null)
                {
                    if (item.Rationale.Length > 0)
                    {
                        CliHost.Out.WriteLine($"    Consequences: {item.Rationale}");
                    }
                }
                else
                {
                    if (item.Rationale.Length > 0)
                    {
                        CliHost.Out.WriteLine($"    Rationale: {item.Rationale}");
                    }

                    if (item.Example.Length > 0)
                    {
                        CliHost.Out.WriteLine($"    Example: {item.Example}");
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
