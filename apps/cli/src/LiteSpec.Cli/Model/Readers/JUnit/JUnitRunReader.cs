using System.Globalization;
using System.Xml.Linq;

namespace LiteSpec.Model.Readers.JUnit;

/// <summary>
///     Reads test outcomes from JUnit XML files. Absent when the folder holds none.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class JUnitRunReader : ITestRunReader
{
    /// <summary>Reads the run, or null when no JUnit file exists.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The test run, or null when no result file exists.</returns>
    public TestRun? Read(ConsumerConfig config)
    {
        var files = SourceTree.Files(config.TestResultsDir, ".xml");

        TestRun? run = null;

        foreach (var file in files)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(file);
            }
            catch (Exception exception) when (exception is System.Xml.XmlException or IOException)
            {
                continue;
            }

            if (document.Root is null
                || (document.Root.Name.LocalName != "testsuites" && document.Root.Name.LocalName != "testsuite"))
            {
                continue;
            }

            run ??= new TestRun();
            run.Files.Add(config.Relative(file));

            if (RunDateOf(document, file) is { } day
                && (run.Date is null || string.CompareOrdinal(day, run.Date) > 0))
            {
                run.Date = day;
            }

            var cases = document.Descendants().Where(element => element.Name.LocalName == "testcase");

            foreach (var testcase in cases)
            {
                var name = testcase.Attribute("name")?.Value ?? "";

                if (name.Length == 0)
                {
                    continue;
                }

                var classname = testcase.Attribute("classname")?.Value ?? "";
                var testName = classname.Length > 0 ? $"{classname}.{name}" : name;
                var outcome = testcase.Elements().Any(element => element.Name.LocalName is "failure" or "error")
                    ? "Failed"
                    : testcase.Elements().Any(element => element.Name.LocalName == "skipped")
                        ? "Skipped"
                        : "Passed";

                run.OutcomeByTest[testName] = outcome;

                if (TestRun.BrowserTitleOf(classname, name) is { } title)
                {
                    run.MergeOutcome(run.OutcomeByTitle, title, outcome);
                }

                foreach (var method in NamesOf(testName))
                {
                    run.MergeOutcome(run.OutcomeByMethod, method, outcome);
                }
            }
        }

        if (run is not null)
        {
            run.Total = run.OutcomeByTest.Count;
            run.Passed = run.OutcomeByTest.Values.Count(outcome => outcome == "Passed");
            run.Failed = run.OutcomeByTest.Values.Count(outcome => outcome == "Failed");
        }

        return run;
    }

    /// <summary>
    ///     The fully qualified names a case answers to: its class and name without any
    ///     data-row arguments, plus the dotted form when nested types join with a plus.
    /// </summary>
    /// <param name="testName">The recorded test name, with any data-row arguments it carries.</param>
    /// <returns>The names a case answers to, without data-row arguments.</returns>
    private static IReadOnlyList<string> NamesOf(string testName)
    {
        var bare = testName;

        var paren = bare.IndexOf('(');

        if (paren >= 0)
        {
            bare = bare[..paren].Trim();
        }

        if (bare.Length == 0)
        {
            return [];
        }

        var dotted = bare.Replace('+', '.');

        return dotted == bare ? [bare] : [bare, dotted];
    }

    /// <summary>The file's run date, yyyy-MM-dd: the latest suite timestamp, else the file date.</summary>
    /// <param name="document">The parsed run document to read dates from.</param>
    /// <param name="file">The run file path, whose write day is the last fallback.</param>
    /// <returns>The yyyy-MM-dd run date, or null when the document and the file state none.</returns>
    private static string? RunDateOf(XDocument document, string file)
    {
        string? latest = null;

        foreach (var suite in document.Descendants().Where(element =>
                     element.Name.LocalName is "testsuite" or "testsuites"))
        {
            if (DateOf(suite.Attribute("timestamp")?.Value) is { } day
                && (latest is null || string.CompareOrdinal(day, latest) > 0))
            {
                latest = day;
            }
        }

        return latest ?? FileDate(file);
    }

    /// <summary>The yyyy-MM-dd day the timestamp starts with, or null when it names none.</summary>
    /// <param name="stamp">The timestamp text, or null when the run states none.</param>
    /// <returns>The yyyy-MM-dd day, or null when the timestamp starts with no valid date.</returns>
    private static string? DateOf(string? stamp)
    {
        if (stamp is not { Length: >= 10 })
        {
            return null;
        }

        var day = stamp[..10];

        return DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? day
            : null;
    }

    /// <summary>The file last write day in UTC, as yyyy-MM-dd.</summary>
    /// <param name="file">The run file path whose last write day is wanted.</param>
    /// <returns>A yyyy-MM-dd text built from the file last write time in UTC.</returns>
    private static string FileDate(string file) =>
        File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
