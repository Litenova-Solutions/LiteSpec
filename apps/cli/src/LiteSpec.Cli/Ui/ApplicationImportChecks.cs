using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The application import checks of the UI validator: an application
///     composes the shared package's exported entry points only.
/// </summary>
/// <follows>litespec/frontend/components#compose-composites-in-page-code</follows>
sealed class ApplicationImportChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    // An application composes the shared package's exported entry points. It reaches
    // no primitive library and no internal package path.
    /// <summary>The imports a React application boundary refuses.</summary>
    static readonly ImportBoundary[] ReactImportBoundaries =
    [
        new(new Regex(@"['""](@base-ui/react|radix-ui|@radix-ui/)", RegexOptions.Compiled), "imports the primitive library directly; import the shared package's component"),
        new(new Regex(@"['""]@(?:[a-z0-9-]+/)?ui/(?:lib|hooks)/", RegexOptions.Compiled), "imports an internal shared package path; import a documented entry point"),
        new(new Regex(@"['""]@tanstack/react-table['""]", RegexOptions.Compiled), "imports the table library directly; import the shared package's data table"),
    ];

    /// <summary>The imports a Svelte application boundary refuses.</summary>
    static readonly ImportBoundary[] SvelteImportBoundaries =
    [
        new(new Regex(@"['""](bits-ui|@melt-ui/svelte)['""]", RegexOptions.Compiled), "imports the primitive library directly; import the shared package's component"),
        new(new Regex(@"['""]@(?:[a-z0-9-]+/)?ui/(?:lib|hooks)/", RegexOptions.Compiled), "imports an internal shared package path; import a documented entry point"),
        new(new Regex(@"['""]@tanstack/svelte-table['""]", RegexOptions.Compiled), "imports the table library directly; import the shared package's data table"),
    ];

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationImportChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public ApplicationImportChecks(UiSession session)
    {
        this.session = session;
    }

    /// <summary>
    ///     Holds one frontend's imports to the shared package boundary.
    /// </summary>
    /// <param name="frontendRoot">The frontend root.</param>
    /// <param name="packageName">The shared package name.</param>
    /// <param name="platform">The frontend platform.</param>
    public void ValidateApplicationImports(string frontendRoot, string packageName, string platform)
    {
        var boundaries = platform == "svelte" ? SvelteImportBoundaries : ReactImportBoundaries;
        foreach (var source in FileWalk.Walk(frontendRoot, file => SourceFiles.SourceFile.IsMatch(file)))
        {
            var relative = session.RelativeToRoot(source);
            if (SourceFiles.TestFile.IsMatch(source))
            {
                continue;
            }

            if (SourceFiles.TestDir.IsMatch(source))
            {
                continue;
            }

            var text = File.ReadAllText(source);
            foreach (var boundary in boundaries)
            {
                if (!boundary.Pattern.IsMatch(text))
                {
                    continue;
                }

                // The scanner is a reference check, not a linter. A file that composes the
                // shared package's components may quote a library name in a doc comment, so
                // only a real import statement counts.
                var statements = SourceFiles.ImportStatement.Matches(text).Cast<Match>().Select(match => match.Value).ToList();
                if (!statements.Any(statement => boundary.Pattern.IsMatch(statement)))
                {
                    continue;
                }

                session.Errors.Add($"[litespec/frontend/components#compose-composites-in-page-code] {relative}: {boundary.Message} from shared package '{packageName}'");
            }
        }
    }
}
