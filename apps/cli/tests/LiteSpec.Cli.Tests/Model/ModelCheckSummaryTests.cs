using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check</c> prints blocking findings first, counts each code, and rejects dead options.</summary>
public sealed partial class ModelCheckSummaryTests
{
    /// <summary>One count line under "Findings by code:", such as <c>     12  paging-undeclared (level G)</c>.</summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"^\s+(?<count>\d+)\s+(?<code>[a-z-]+) \(level (?<level>[GSA])\)$")]
    private static partial System.Text.RegularExpressions.Regex CountLine();

    /// <summary>The per-code count lines of one output, in printed order, as code, level and count.</summary>
    /// <param name="output">The check command output text.</param>
    /// <returns>The parsed count lines in printed order.</returns>
    static List<(string Code, string Level, int Count)> CountLines(string output)
    {
        var counts = new List<(string, string, int)>();

        foreach (var line in Lines(output))
        {
            if (CountLine().Match(line) is { Success: true } match)
            {
                counts.Add((match.Groups["code"].Value, match.Groups["level"].Value, int.Parse(match.Groups["count"].Value, System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        return counts;
    }

    /// <summary>The output lines without their carriage returns.</summary>
    /// <param name="output">The check command output text.</param>
    /// <returns>The output split into lines.</returns>
    static string[] Lines(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    /// <summary>The sort rank of a level: G first, then S, then A.</summary>
    /// <param name="level">The level letter to rank.</param>
    /// <returns>The sort rank, lower first.</returns>
    static int Rank(string level) => level switch
    {
        "G" => 0,
        "S" => 1,
        _ => 2,
    };

    /// <summary>
    ///     With --all each finding prints once, the blocking ones first; the counts per code follow the
    ///     findings, and the totals line, with the blocking count, ends the output.
    /// </summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckPrintsBlockingFirstThenCountsThenTotals()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteScenario(fixture.Path);
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Equal(1, result.Status);

        var lines = Lines(result.Output);
        var blocking = Array.FindIndex(lines, line => line.Contains("[paging-undeclared]", StringComparison.Ordinal));
        var advisory = Array.FindIndex(lines, line => line.Contains("[use-case-unverified]", StringComparison.Ordinal));
        var heading = Array.IndexOf(lines, "Findings by code:");
        var totals = Array.FindIndex(lines, line => line.Contains(" findings, ", StringComparison.Ordinal) && line.EndsWith(" covered.", StringComparison.Ordinal));
        var counts = CountLines(result.Output);

        Assert.Single(lines, line => line.Contains("[paging-undeclared]", StringComparison.Ordinal));
        Assert.True(blocking >= 0 && advisory >= 0 && heading >= 0 && totals >= 0);
        Assert.True(blocking < advisory, "The blocking findings print before the advisory ones.");
        Assert.True(advisory < heading, "The findings print before the counts.");
        Assert.True(heading < totals, "The counts print before the totals line.");
        Assert.Contains(" blocking, ", lines[totals], StringComparison.Ordinal);
        Assert.NotEmpty(counts);
        Assert.Equal("G", counts[0].Level);
        Assert.Contains(counts, count => count.Code == "paging-undeclared" && count.Level == "G");
        Assert.Contains(counts, count => count.Code == "use-case-unverified" && count.Level == "A");

        for (var index = 1; index < counts.Count; index++)
        {
            Assert.True(Rank(counts[index].Level) >= Rank(counts[index - 1].Level), "The counts order by level G, S, A.");

            if (counts[index].Level == counts[index - 1].Level)
            {
                Assert.True(counts[index].Count <= counts[index - 1].Count, "The counts order by count within one level.");
            }
        }
    }

    /// <summary>By default the level A findings are summarized by code and not listed; the counts still name them.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckByDefaultSummarizesAdvisoryByCode()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        WriteScenario(fixture.Path);
        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[paging-undeclared]", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[use-case-unverified]", result.Output, StringComparison.Ordinal);
        Assert.Contains("advisory findings summarized by code; run with --all to list them.", result.Output, StringComparison.Ordinal);
        Assert.Contains(CountLines(result.Output), count => count.Code == "use-case-unverified" && count.Level == "A");

        var all = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Contains("[use-case-unverified]", all.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("advisory findings summarized by code", all.Output, StringComparison.Ordinal);
    }

    /// <summary>A dead check option is rejected as an unknown option.</summary>
    /// <param name="option">The dead option to pass.</param>
    /// <param name="value">The value to pass with the option.</param>
    [Theory]
    [InlineData("--level", "G")]
    [InlineData("--out", "model.json")]
    public void CheckDeadOptionIsRejected(string option, string value)
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, option, value);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Unknown option '{option}'.", result.Output);
    }

    /// <summary>A format other than json or text is a usage error, not human text.</summary>
    /// <param name="format">The unsupported format value to pass.</param>
    [Theory]
    [InlineData("xml")]
    [InlineData("yaml")]
    public void CheckUnsupportedFormatIsAUsageError(string format)
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", format);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Unknown format '{format}'; use json or text.", result.Output);
    }

    /// <summary>Writes a scenario for borrow-copy with no proving test, so an advisory finding prints.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteScenario(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/lending/loans/borrow-copy-refused-scenario.yaml", """
            {
              "type": "scenario",
              "id": "scenario/borrow-copy.borrowing-an-unavailable-copy-is-refused",
              "title": "Borrowing an unavailable copy is refused",
              "given": "A copy sits on loan to another reader.",
              "when": "The reader borrows the copy.",
              "result": "result/borrow-copy.copy-unavailable",
              "then": "The loan is refused."
            }
            """);
        TestRecords.Explain(root, ".litespec/behavior/modules/lending/loans/borrow-copy-refused-scenario.md", """
            # Borrowing an unavailable copy is refused

            A reader borrows a copy that is on loan.
            """);
    }
}
