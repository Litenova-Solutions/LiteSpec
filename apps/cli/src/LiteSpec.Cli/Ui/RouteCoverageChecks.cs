using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The route coverage checks of the UI validator: every route a frontend
///     discovers is visited by its route suite.
/// </summary>
/// <follows>litespec/frontend/web/ui#check-every-route-in-a-browser</follows>
sealed class RouteCoverageChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>A React route file extension.</summary>
    static readonly Regex ReactRouteExtension = new(@"\.(?:tsx?|jsx?)$", RegexOptions.Compiled);

    /// <summary>
    ///     Initializes a new instance of the <see cref="RouteCoverageChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public RouteCoverageChecks(UiSession session)
    {
        this.session = session;
    }

    // A React route is a file under src/routes with one address each, except the
    // files the router itself does not treat as addresses: the root layout, the
    // lazy half of a route, the generated route tree, and router-ignored
    // segments. The model reader states the same exclusions for the root, the
    // lazy half, and dash-prefixed segments; the generated tree stays untracked,
    // and a test colocated under routes is a test, not an address.
    /// <summary>
    ///     Every React route file under a routes directory.
    /// </summary>
    /// <param name="routes">The routes directory.</param>
    /// <returns>The route files.</returns>
    static List<string> ReactRouteFiles(string routes) =>
        FileWalk.Walk(routes, file =>
        {
            if (SourceFiles.TestDir.IsMatch(file) || SourceFiles.TestFile.IsMatch(file))
            {
                return false;
            }

            var name = Path.GetFileName(file);
            if (name.StartsWith("__root", StringComparison.Ordinal)
                || name.Contains(".lazy.", StringComparison.Ordinal)
                || name.StartsWith("routeTree.gen.", StringComparison.Ordinal)
                || IgnoredRouteSegment(Path.GetRelativePath(routes, file)))
            {
                return false;
            }

            return ReactRouteExtension.IsMatch(name);
        });

    // A Svelte route is a folder holding +page.svelte. A server-only endpoint
    // renders no page, so the browser suite has nothing to visit there.
    /// <summary>
    ///     Every Svelte route file under a routes directory.
    /// </summary>
    /// <param name="routes">The routes directory.</param>
    /// <returns>The route files.</returns>
    static List<string> SvelteRouteFiles(string routes) =>
        FileWalk.Walk(routes, file =>
        {
            if (SourceFiles.TestDir.IsMatch(file))
            {
                return false;
            }

            if (!Path.GetFileName(file).Equals("+page.svelte", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !IgnoredRouteSegment(Path.GetRelativePath(routes, file));
        });

    /// <summary>
    ///     Whether a route path carries a router-ignored segment.
    /// </summary>
    /// <param name="relative">The route path relative to its routes directory.</param>
    /// <returns>True when a segment is router-ignored.</returns>
    static bool IgnoredRouteSegment(string relative) =>
        relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.StartsWith("-", StringComparison.Ordinal));

    // The route suite is one specification per frontend that visits every
    // route the route tree discovers. Every browser verification row names
    // its file, so the file is the fixed contract. A route without
    // tests/routes/every-route.spec.ts under its frontend root is a route
    // the browser checks never visit, and each one is reported by name.
    // (litespec/frontend/web/ui#check-every-route-in-a-browser)
    /// <summary>
    ///     Requires the route suite that visits every route of one frontend.
    /// </summary>
    /// <param name="label">The frontend label the findings name.</param>
    /// <param name="frontendRoot">The frontend root.</param>
    /// <param name="platform">The frontend platform.</param>
    public void ValidateRouteSuites(string label, string frontendRoot, string platform)
    {
        var routes = Path.Combine(frontendRoot, "src", "routes");
        var files = platform == "svelte" ? SvelteRouteFiles(routes) : ReactRouteFiles(routes);
        if (files.Count == 0)
        {
            return;
        }

        var suite = Path.Combine(frontendRoot, "tests", "routes", "every-route.spec.ts");
        if (File.Exists(suite))
        {
            return;
        }

        var expected = session.RelativeToRoot(suite);
        foreach (var file in files)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#check-every-route-in-a-browser] {label}: route '{session.RelativeToRoot(file)}' has no route suite at '{expected}'");
        }
    }
}
