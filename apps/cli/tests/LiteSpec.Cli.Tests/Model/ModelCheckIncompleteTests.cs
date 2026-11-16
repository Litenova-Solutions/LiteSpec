using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check</c> names each missing reader on its last line and exits 3 when nothing blocks.</summary>
public sealed class ModelCheckIncompleteTests
{
    /// <summary>Reads one finding's outcome and reason from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <param name="code">The finding code to read.</param>
    /// <returns>The finding outcome and reason, each null when absent.</returns>
    static (string? Outcome, string? Reason) OutcomeOf(string stdout, string code)
    {
        using var document = System.Text.Json.JsonDocument.Parse(stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == code)
            {
                var reason = finding.GetProperty("reason");

                return (
                    finding.GetProperty("outcome").GetString(),
                    reason.ValueKind == System.Text.Json.JsonValueKind.Null ? null : reason.GetString());
            }
        }

        throw new InvalidOperationException($"No finding carries '{code}'.");
    }

    /// <summary>How many JSON findings report a level S finding.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <returns>The level S reported findings.</returns>
    static int ReportedLevelS(string stdout)
    {
        using var document = System.Text.Json.JsonDocument.Parse(stdout);
        var waiting = 0;

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("level").GetString() == "S"
                && finding.GetProperty("outcome").GetString() == "reported")
            {
                waiting++;
            }
        }

        return waiting;
    }

    /// <summary>The last non-empty line of the output.</summary>
    /// <param name="output">The command output text.</param>
    /// <returns>The last line that holds text.</returns>
    static string LastLine(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Last(line => line.Length > 0);

    /// <summary>The process PATH without the folders holding Node, so the TypeScript reader is unavailable.</summary>
    /// <returns>The PATH value with every Node folder removed.</returns>
    static string PathWithoutNode()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        return string.Join(
            Path.PathSeparator,
            path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(folder => !File.Exists(Path.Combine(folder.Trim(), "node.exe"))
                    && !File.Exists(Path.Combine(folder.Trim(), "node"))));
    }

    /// <summary>A missing frontend reader exits 3 with the incomplete line naming the frontend last.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckWithMissingReaderExits3AndNamesTheFrontend()
    {
        using var fixture = ModelFixture.Prepare("large");
        var hidden = new Dictionary<string, string>
        {
            ["CI"] = "",
            ["PATH"] = PathWithoutNode(),
        };

        var text = TestCli.Run(["model", "check", "--root", fixture.Path], environment: hidden);

        Assert.Equal(3, text.Status);
        Assert.Contains(" covered.", text.Stdout, StringComparison.Ordinal);

        var json = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"], environment: hidden);
        var (outcome, _) = OutcomeOf(json.Stdout, "reader-missing");
        var waiting = ReportedLevelS(json.Stdout);

        Assert.Equal(3, json.Status);
        Assert.Equal("reported", outcome);
        Assert.True(waiting >= 1);
        Assert.Equal($"inspection incomplete: reader for catalog missing, {waiting} findings not blocking", LastLine(text.Stdout));
    }

    /// <summary>A check whose readers all ran prints no incomplete line and exits 0.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckWithReadersPresentPrintsNoIncompleteLine()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("inspection incomplete", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A missing reader with a blocking finding exits 1 and still names the missing reader.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckWithMissingReaderAndBlockingFindingExits1()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        var result = TestCli.Run(
            ["model", "check", "--root", fixture.Path],
            environment: new Dictionary<string, string>
            {
                ["CI"] = "",
                ["PATH"] = PathWithoutNode(),
            });

        Assert.Equal(1, result.Status);
        Assert.Contains("inspection incomplete: reader for catalog missing, ", result.Output, StringComparison.Ordinal);
    }

    /// <summary>At release a missing reader blocks instead of reporting.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ReleaseWithMissingReaderBlocks()
    {
        using var fixture = ModelFixture.Prepare("large");

        var json = TestCli.Run(
            ["model", "check", "--root", fixture.Path, "--format", "json", "--release"],
            environment: new Dictionary<string, string>
            {
                ["CI"] = "",
                ["PATH"] = PathWithoutNode(),
            });
        var (outcome, _) = OutcomeOf(json.Stdout, "reader-missing");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
    }

    /// <summary>One unread frontend renders the incomplete line the specification states.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void IncompleteLineNamesOneFrontend()
    {
        Assert.Equal(
            "inspection incomplete: reader for catalog missing, 2 findings not blocking",
            ModelCommand.IncompleteLine(["catalog"], 2));
    }

    /// <summary>Two unread frontends render one clause per frontend on one line.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void IncompleteLineJoinsOneClausePerFrontend()
    {
        var line = ModelCommand.IncompleteLine(["admin", "catalog"], 1);

        Assert.NotNull(line);
        Assert.Contains("inspection incomplete: ", line!, StringComparison.Ordinal);
        Assert.Contains("reader for admin missing, 1 findings not blocking", line!, StringComparison.Ordinal);
        Assert.Contains("reader for catalog missing, 1 findings not blocking", line!, StringComparison.Ordinal);
    }

    /// <summary>No unread frontend renders no incomplete line.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void IncompleteLineWithNoFrontendIsNull()
    {
        Assert.Null(ModelCommand.IncompleteLine([], 0));
    }
}
