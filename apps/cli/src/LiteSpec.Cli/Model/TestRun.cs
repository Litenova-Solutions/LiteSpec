namespace LiteSpec.Model;

/// <summary>The latest test run, read from result files.</summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class TestRun
{
    /// <summary>Gets the files read, relative to the root.</summary>
    public List<string> Files { get; } = [];

    /// <summary>Gets outcome by test name, exactly as the result file writes it.</summary>
    public Dictionary<string, string> OutcomeByTest { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets outcome by fully qualified test name: the namespace, the containing types
    ///     joined with dots, and the method, without any data-row arguments. A renamed
    ///     test matches nothing instead of matching another test's same-named method.
    /// </summary>
    public Dictionary<string, string> OutcomeByMethod { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets outcome by browser test title, exactly as the test declares it. A Playwright
    ///     result answers to the bare title of its test, without any describe chain or
    ///     project prefix, so a scenario proved in the browser can be passing.
    /// </summary>
    public Dictionary<string, string> OutcomeByTitle { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets the tests that both passed and failed within the runs read, by fully
    ///     qualified method name or browser title, as the proves links name them.
    /// </summary>
    public HashSet<string> Flaky { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the total number of tests the run reports.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets the number of tests the run reports as passed.</summary>
    public int Passed { get; set; }

    /// <summary>Gets or sets the number of tests the run reports as failed.</summary>
    public int Failed { get; set; }

    /// <summary>Gets or sets the run date, yyyy-MM-dd, or null when no result file names one.</summary>
    public string? Date { get; set; }

    /// <summary>
    ///     Whether the browser test with the title passed in this run: true when its
    ///     outcome is Passed, false when it ran and did not pass, null when the run
    ///     holds no outcome for the title.
    /// </summary>
    /// <param name="title">The browser test title, exactly as the test declares it.</param>
    /// <returns>True for a passed outcome, false for a run outcome that is not Passed, or null when the run holds no outcome for the title.</returns>
    public bool? PassingOfTitle(string title) =>
        OutcomeByTitle.TryGetValue(title, out var outcome) ? outcome == "Passed" : null;

    /// <summary>
    ///     Merges one outcome into the map: a failure wins over any earlier outcome,
    ///     and a test seen both passed and failed is recorded as flaky. A skipped
    ///     outcome never marks a test flaky.
    /// </summary>
    /// <param name="map">The outcome map to merge into, keyed by test name or title.</param>
    /// <param name="key">The test name or title the outcome belongs to.</param>
    /// <param name="outcome">The outcome to merge, as the result file writes it.</param>
    public void MergeOutcome(Dictionary<string, string> map, string key, string outcome)
    {
        if (map.TryGetValue(key, out var kept))
        {
            if ((kept == "Passed" && outcome == "Failed") || (kept == "Failed" && outcome == "Passed"))
            {
                Flaky.Add(key);
            }

            if (kept == "Failed")
            {
                return;
            }
        }

        map[key] = outcome;
    }

    /// <summary>
    ///     The browser title a JUnit case answers to, or null when it is no browser
    ///     case: its class names no test file and its name carries no describe chain.
    ///     Playwright writes the test file as the class and the title, with any
    ///     describe chain and any project prefix, as the name, so "lists the
    ///     expenses", "expenses list › lists the expenses" and
    ///     "[chromium] expenses list › lists the expenses" all answer to
    ///     "lists the expenses".
    /// </summary>
    /// <param name="classname">The JUnit class name of the case.</param>
    /// <param name="name">The JUnit case name, carrying any describe chain and project prefix.</param>
    /// <returns>The bare browser title, or null when the case is no browser case or yields no title.</returns>
    public static string? BrowserTitleOf(string classname, string name)
    {
        if (name.Length == 0)
        {
            return null;
        }

        if (!name.Contains(" › ", StringComparison.Ordinal) && !TestFile(classname))
        {
            return null;
        }

        var text = name;

        if (text.StartsWith('['))
        {
            var close = text.IndexOf(']');

            if (close > 0 && close + 1 < text.Length && text[close + 1] == ' ')
            {
                text = text[(close + 1)..].TrimStart();
            }
        }

        var chain = text.LastIndexOf(" › ", StringComparison.Ordinal);
        var title = chain >= 0 ? text[(chain + 3)..] : text;

        return title.Length > 0 ? title : null;
    }

    /// <summary>Whether the JUnit class names a Playwright test file: a path or a JavaScript file.</summary>
    /// <param name="classname">The JUnit class name to test.</param>
    /// <returns>True when the class name is a path or carries a JavaScript or TypeScript extension.</returns>
    private static bool TestFile(string classname) =>
        classname.Contains('/') || classname.Contains('\\')
        || classname.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".cjs", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".mts", StringComparison.OrdinalIgnoreCase)
        || classname.EndsWith(".cts", StringComparison.OrdinalIgnoreCase);
}
