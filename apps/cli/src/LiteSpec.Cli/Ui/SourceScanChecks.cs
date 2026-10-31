namespace LiteSpec.Ui;

/// <summary>
///     The source-scan checks of the UI validator: the shared package's own
///     source against the Tailwind utility rules.
/// </summary>
/// <follows>litespec/frontend/web/ui#restrict-css-decisions</follows>
sealed class SourceScanChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SourceScanChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public SourceScanChecks(UiSession session)
    {
        this.session = session;
    }

    // One pass over the shared package. The Tailwind utility rules apply to every
    // authored file outside the copied primitive boundary and the tests. The shared
    // package owns the primitive library, so its composites import it deliberately;
    // an application does not, and `ValidateApplicationImports` refuses it there.
    // (litespec/frontend/web/ui#restrict-css-decisions)
    /// <summary>
    ///     Scans the shared package's source against the utility rules.
    /// </summary>
    /// <param name="target">The directory to scan.</param>
    /// <param name="primitives">The copied primitive boundary, or null.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateSourceScan(string target, string? primitives, string platform)
    {
        _ = platform;
        foreach (var source in FileWalk.Walk(target, file => SourceFiles.SourceFile.IsMatch(file)))
        {
            var relative = session.RelativeToRoot(source);
            var text = File.ReadAllText(source);
            var inPrimitives = primitives is not null && FileWalk.Within(primitives, source);
            var inTests = SourceFiles.TestDir.IsMatch(source) || SourceFiles.TestFile.IsMatch(source);
            if (inPrimitives || inTests)
            {
                continue;
            }

            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in ClassStringChecks.ClassStrings(text))
            {
                ClassStringChecks.InspectClassString(value, (kind, token) =>
                {
                    if (!reported.Add(kind + ":" + token))
                    {
                        return;
                    }

                    session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {relative}: {ClassStringChecks.UtilityMessages[kind]} '{token}'");
                }, variants: false);
            }

        }
    }
}
