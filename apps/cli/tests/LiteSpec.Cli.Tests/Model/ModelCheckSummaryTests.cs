using System.Text.Json;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves <c>litespec model check</c> prints blocking findings first, counts each code, and filters by level.</summary>
public sealed partial class ModelCheckSummaryTests
{
    /// <summary>One count line under "Findings by code:", such as <c>     12  value-invalid (level A)</c>.</summary>
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
    ///     Each finding prints once, the blocking ones first; the counts per code follow the
    ///     findings, and the totals line, with the blocking count, ends the output.
    /// </summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Check_Prints_Blocking_First_Then_Counts_Then_Totals()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);

        var lines = Lines(result.Output);
        var blocking = Array.FindIndex(lines, line => line.Contains("[paging-undeclared]", StringComparison.Ordinal));
        var advisory = Array.FindIndex(lines, line => line.Contains("[value-invalid]", StringComparison.Ordinal));
        var heading = Array.IndexOf(lines, "Findings by code:");
        var totals = Array.FindIndex(lines, line => line.Contains(" findings, ", StringComparison.Ordinal) && line.EndsWith(" known.", StringComparison.Ordinal));
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
        Assert.Contains(counts, count => count.Code == "value-invalid" && count.Level == "A");

        for (var index = 1; index < counts.Count; index++)
        {
            Assert.True(Rank(counts[index].Level) >= Rank(counts[index - 1].Level), "The counts order by level G, S, A.");

            if (counts[index].Level == counts[index - 1].Level)
            {
                Assert.True(counts[index].Count <= counts[index - 1].Count, "The counts order by count within one level.");
            }
        }
    }

    /// <summary>The level option G,S keeps the blocking findings and omits the advisory findings.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Check_Level_GS_Omits_Advisory_Findings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");

        var full = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, full.Status);
        Assert.Contains("value-invalid", full.Output);

        var filtered = Cli.Run("model", "check", "--root", fixture.Path, "--level", "G,S");

        Assert.Equal(1, filtered.Status);
        Assert.Contains("paging-undeclared", filtered.Output);
        Assert.DoesNotContain("value-invalid", filtered.Output);
        Assert.All(CountLines(filtered.Output), count => Assert.True(count.Level is "G" or "S"));
    }

    /// <summary>The level option filters the JSON findings to the named levels and keeps the shape.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Check_Level_Filters_Json_Findings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--level", "G");

        Assert.Equal(1, result.Status);

        using var document = JsonDocument.Parse(result.Stdout);
        var findings = document.RootElement.GetProperty("findings");

        Assert.True(findings.GetArrayLength() > 0);
        Assert.Contains(findings.EnumerateArray(), finding => finding.GetProperty("code").GetString() == "paging-undeclared");

        foreach (var finding in findings.EnumerateArray())
        {
            Assert.Equal("G", finding.GetProperty("tier").GetString());
        }
    }

    /// <summary>An unknown level is a usage error.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Check_Unknown_Level_Exits_2()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--level", "X");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown level 'X'", result.Output);
    }
}
