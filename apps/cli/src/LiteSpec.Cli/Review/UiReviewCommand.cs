using System.Text;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Review;

/// <summary>
///     The <c>litespec ui review</c> command: lists the screen routes whose files
///     changed since a revision, pairs each with its golden baseline PNGs when
///     present, and writes a review packet an independent reviewer answers. The
///     command reads files only, so it calls no model and no network.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class UiReviewCommand
{
    /// <summary>The usage text for the ui review command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec ui review --since <ref> [--root <folder>]
          litespec ui review --help

        Lists the screen routes whose files changed since the revision, pairs
        each with its golden baseline PNGs under .agents/ui-baseline/ when
        present, and writes a review packet to .litespec/review/ui-<timestamp>.md
        with the geometry checklist an independent reviewer answers.

          --root              The repository to read. Defaults to the working directory.
          --since <ref>       The git revision to diff the working tree against, such as HEAD~1.
          --help              Print this text and exit.

        Exit codes: 0 every changed route has a baseline, 1 a changed route has no baseline, 2 usage error or unreadable changes.
        """;

    /// <summary>The folder holding the golden baseline PNGs, relative to the repository root.</summary>
    const string BaselineFolder = ".agents/ui-baseline";

    /// <summary>Runs the ui review: lists the changed routes and writes the review packet.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 when every route has a baseline, 1 when a route has none, 2 for a usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--since"], maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (string.IsNullOrEmpty(parsed.Value("--since")))
        {
            return CommandFailure.UsageError("Option --since needs a value.", Usage, json: false);
        }

        var root = FullPath(parsed.Value("--root") ?? CliHost.WorkingDirectory);

        if (!Directory.Exists(root))
        {
            return CommandFailure.Fail($"The root folder '{root}' does not exist.", json: false, code: 2);
        }

        var exit = Execute(root, parsed.Value("--since")!, CliHost.Out, CliHost.Error, out _);

        return exit;
    }

    /// <summary>A changed route: the route address, the changed file it comes from, and its golden baselines.</summary>
    /// <param name="Route">The route address, such as /expenses.</param>
    /// <param name="File">The changed file path relative to the repository root.</param>
    /// <param name="Baselines">The baseline PNG paths relative to the repository root, in order.</param>
    sealed record ChangedRoute(string Route, string File, List<string> Baselines);

    /// <summary>Runs the review on explicit writers, so tests prove the packet and the exit code without the process console.</summary>
    /// <param name="root">The repository folder to read.</param>
    /// <param name="sinceRef">The git revision to diff the working tree against.</param>
    /// <param name="out">The writer receiving the summary lines.</param>
    /// <param name="error">The writer receiving the failure line.</param>
    /// <param name="packetPath">The absolute packet path written, or empty when nothing was written.</param>
    /// <returns>The exit code: 0 when every route has a baseline, 1 when a route has none, 2 when the changes cannot be read or written.</returns>
    internal static int Execute(string root, string sinceRef, TextWriter @out, TextWriter error, out string packetPath)
    {
        packetPath = "";
        var (files, gitError) = ReviewGit.ChangedFiles(root, sinceRef);

        if (files is null)
        {
            error.WriteLine(gitError);
            return 2;
        }

        var changed = files.OrderBy(path => path, StringComparer.Ordinal).ToList();
        var routes = changed.Select(path => RouteFor(root, path)).ToList();
        PairBaselines(root, routes);

        if (!WritePacket(root, sinceRef, routes, out packetPath, out var writeError))
        {
            error.WriteLine(writeError);
            return 2;
        }

        foreach (var route in routes)
        {
            var baselines = route.Baselines.Count > 0
                ? string.Join(", ", route.Baselines)
                : "no baseline PNG";

            @out.WriteLine($"{route.Route} (from {route.File}): {baselines}");
        }

        var missing = routes.Count(route => route.Baselines.Count == 0);

        @out.WriteLine($"Wrote {Path.GetRelativePath(root, packetPath)} ({routes.Count} routes, {missing} without baselines).");

        return missing > 0 ? 1 : 0;
    }

    /// <summary>Reads the route of one changed file: the route a screen record declares, or the route guessed from the file name.</summary>
    /// <param name="root">The repository folder the file sits under.</param>
    /// <param name="path">The absolute changed file path.</param>
    /// <returns>The changed route with no baselines paired yet.</returns>
    static ChangedRoute RouteFor(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);

        return new ChangedRoute(ReadDeclaredRoute(path) ?? GuessRoute(path), relative, []);
    }

    /// <summary>Reads the route a screen record declares on its route line, or null when the file declares none.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <returns>The declared route, or null.</returns>
    static string? ReadDeclaredRoute(string path)
    {
        string[] lines;

        try
        {
            if (new FileInfo(path).Length > 1024 * 1024)
            {
                return null;
            }

            lines = File.ReadAllLines(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("route:", StringComparison.Ordinal))
            {
                var route = trimmed["route:".Length..].Trim().Trim('\'', '"');

                if (route.Length > 0)
                {
                    return route;
                }
            }
        }

        return null;
    }

    /// <summary>Guesses the route from the file name: the lowercased base name with separators as dashes under the root slash.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <returns>The guessed route address.</returns>
    static string GuessRoute(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        foreach (var glyph in new[] { ' ', '_', '.' })
        {
            name = name.Replace(glyph, '-');
        }

        return "/" + name.Trim('-');
    }

    /// <summary>Pairs each changed route with its golden baseline PNGs under the baseline folder, in file order.</summary>
    /// <param name="root">The repository folder the baseline folder sits under.</param>
    /// <param name="routes">The changed routes to pair.</param>
    static void PairBaselines(string root, List<ChangedRoute> routes)
    {
        var folder = Path.Combine(root, BaselineFolder);
        string[] baselines;

        try
        {
            baselines = Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories)
                : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        for (var index = 0; index < routes.Count; index++)
        {
            var slug = Slug(routes[index].Route);
            var matches = baselines
                .Where(candidate => Path.GetFileNameWithoutExtension(candidate).StartsWith(slug, StringComparison.OrdinalIgnoreCase))
                .OrderBy(candidate => candidate, StringComparer.Ordinal)
                .Select(candidate => Path.GetRelativePath(root, candidate))
                .ToList();

            routes[index] = routes[index] with { Baselines = matches };
        }
    }

    /// <summary>The baseline slug of a route: the address without slashes, dashes between segments, and home for the root route.</summary>
    /// <param name="route">The route address.</param>
    /// <returns>The slug the baseline file name starts with.</returns>
    static string Slug(string route)
    {
        var slug = route.Trim('/').Replace('/', '-');

        return slug.Length > 0 ? slug : "home";
    }

    /// <summary>Writes the review packet: the changed routes, their baselines, the geometry checklist, and their diffs.</summary>
    /// <param name="root">The repository folder the packet is written under.</param>
    /// <param name="sinceRef">The git revision the change set was read from.</param>
    /// <param name="routes">The changed routes in file order.</param>
    /// <param name="packetPath">The absolute packet path written, or empty when nothing was written.</param>
    /// <param name="error">The write error, or empty when the packet was written.</param>
    /// <returns>True when the packet was written.</returns>
    static bool WritePacket(string root, string sinceRef, List<ChangedRoute> routes, out string packetPath, out string error)
    {
        packetPath = "";
        error = "";

        try
        {
            var folder = Path.Combine(root, ".litespec", "review");
            Directory.CreateDirectory(folder);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var candidate = Path.Combine(folder, $"ui-{stamp}.md");
            var attempt = 1;

            while (File.Exists(candidate))
            {
                attempt++;
                candidate = Path.Combine(folder, $"ui-{stamp}-{attempt}.md");
            }

            var text = new StringBuilder();

            text.AppendLine("# Ui Review Packet");
            text.AppendLine();
            text.AppendLine($"Base revision: {sinceRef}");
            text.AppendLine($"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Changed routes: {routes.Count}");
            text.AppendLine($"Routes without baselines: {routes.Count(route => route.Baselines.Count == 0)}");
            text.AppendLine();
            text.AppendLine("## Changed Routes");
            text.AppendLine();

            if (routes.Count == 0)
            {
                text.AppendLine("No files changed since the base revision.");
            }
            else
            {
                foreach (var route in routes)
                {
                    text.AppendLine($"- {route.Route} (from {route.File})");
                }
            }

            text.AppendLine();
            text.AppendLine("## Baselines");
            text.AppendLine();

            if (routes.Count == 0)
            {
                text.AppendLine("No baselines: no files changed.");
            }
            else
            {
                foreach (var route in routes)
                {
                    var baselines = route.Baselines.Count > 0
                        ? string.Join(", ", route.Baselines)
                        : "no baseline PNG under .agents/ui-baseline/";

                    text.AppendLine($"- {route.Route}: {baselines}");
                }
            }

            text.AppendLine();
            text.AppendLine("## Geometry Checklist");
            text.AppendLine();
            text.AppendLine("The independent reviewer checks each changed route at phone, laptop, and wide widths against these points:");
            text.AppendLine();
            text.AppendLine("- [ ] Edge alignment: every control on the route starts and ends on the same grid lines as its neighbors.");
            text.AppendLine("- [ ] Control heights: controls on one row share one height, and the primary action keeps that height.");
            text.AppendLine("- [ ] Symmetric grids: columns and gaps mirror across the center, with no stranded control on a wrapped row.");
            text.AppendLine("- [ ] Form width: short fields size to their value, and related short fields share one row instead of spanning the form.");
            text.AppendLine("- [ ] Nested surfaces: no surface draws its own border and fill inside another surface; regions split with spacing.");
            text.AppendLine("- [ ] Overflow: no table, tab list, or action group clips or scrolls the page at phone width.");
            text.AppendLine();
            text.AppendLine("## Diffs");
            text.AppendLine();

            if (routes.Count == 0)
            {
                text.AppendLine("No diffs: no files changed.");
            }
            else
            {
                foreach (var route in routes)
                {
                    text.AppendLine($"### {route.Route} ({route.File})");
                    text.AppendLine();
                    text.AppendLine("```diff");
                    text.AppendLine(ReviewGit.DiffFor(root, sinceRef, Path.Combine(root, route.File)).TrimEnd());
                    text.AppendLine("```");
                    text.AppendLine();
                }
            }

            File.WriteAllText(candidate, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            packetPath = candidate;

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = $"Cannot write the review packet: {exception.Message}";
            return false;
        }
    }

    /// <summary>The absolute path of an option value: as written when absolute, else under the working directory.</summary>
    /// <param name="path">The option value.</param>
    /// <returns>The absolute path.</returns>
    static string FullPath(string path) =>
        Path.IsPathFullyQualified(path) ? path : Path.Combine(CliHost.WorkingDirectory, path);
}
