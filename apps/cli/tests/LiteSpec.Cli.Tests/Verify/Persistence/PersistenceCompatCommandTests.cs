using LiteSpec.Cli.Tests.Support;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Verify.Persistence;

namespace LiteSpec.Cli.Tests.Verify.Persistence;

/// <summary>Proves the persistence compatibility check runs the consumer harness and reports unreadable snapshots.</summary>
public sealed class PersistenceCompatCommandTests
{
    /// <summary>Check passes when every stored snapshot reads.</summary>
    [Fact]
    public void CheckPassesWhenEverySnapshotReads()
    {
        using var consumer = ModelFixture.Prepare("small");
        const string file = ".litespec/proof/persistence-snapshots/v1/expense.json";
        WriteSnapshot(consumer.Path, "v1");
        AppendPersistence(consumer.Path, WriteCheckHarness(consumer.Path, file, "ok", string.Empty));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("snapshot-", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(ReportFindings(consumer.Path));
        Assert.Contains("check .litespec/proof/persistence-snapshots/v1", CallsLog(consumer.Path), StringComparison.Ordinal);
    }

    /// <summary>Check reports an incompatible snapshot when the harness marks a line failed.</summary>
    [Fact]
    public void CheckReportsIncompatibleSnapshot()
    {
        using var consumer = ModelFixture.Prepare("small");
        const string file = ".litespec/proof/persistence-snapshots/v1/expense.json";
        WriteSnapshot(consumer.Path, "v1");
        AppendPersistence(consumer.Path, WriteCheckHarness(consumer.Path, file, "failed", "Amount reads as text."));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("snapshot-read-failed", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("expense.json", result.Stdout, StringComparison.Ordinal);
        var finding = Assert.Single(ReportFindings(consumer.Path));
        Assert.Equal("snapshot-read-failed", finding!["code"]!.GetValue<string>());
    }

    /// <summary>Check reports a missing snapshot when a release folder holds no snapshot.</summary>
    [Fact]
    public void CheckReportsMissingSnapshotForEmptyRelease()
    {
        using var consumer = ModelFixture.Prepare("small");
        Directory.CreateDirectory(Path.Combine(consumer.Path, ".litespec", "proof", "persistence-snapshots", "v1"));
        AppendPersistence(consumer.Path, WriteQuietHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("snapshot-missing", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("v1", result.Stdout, StringComparison.Ordinal);
        var finding = Assert.Single(ReportFindings(consumer.Path));
        Assert.Equal("snapshot-missing", finding!["code"]!.GetValue<string>());
    }

    /// <summary>Check runs the harness once per release folder.</summary>
    [Fact]
    public void CheckRunsOncePerReleaseFolder()
    {
        using var consumer = ModelFixture.Prepare("small");
        const string file = ".litespec/proof/persistence-snapshots/v1/expense.json";
        WriteSnapshot(consumer.Path, "v1");
        Directory.CreateDirectory(Path.Combine(consumer.Path, ".litespec", "proof", "persistence-snapshots", "v2"));
        AppendPersistence(consumer.Path, WriteCheckHarness(consumer.Path, file, "ok", string.Empty));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(1, result.Status);
        var log = CallsLog(consumer.Path);
        Assert.Contains("check .litespec/proof/persistence-snapshots/v1", log, StringComparison.Ordinal);
        Assert.Contains("check .litespec/proof/persistence-snapshots/v2", log, StringComparison.Ordinal);
        Assert.Contains("snapshot-missing", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Check reports an incompatible snapshot when a harness line is not a snapshot result.</summary>
    [Fact]
    public void CheckReportsMalformedHarnessLine()
    {
        using var consumer = ModelFixture.Prepare("small");
        WriteSnapshot(consumer.Path, "v1");
        AppendPersistence(consumer.Path, WriteBrokenHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("snapshot-read-failed", result.Stdout, StringComparison.Ordinal);
        var finding = Assert.Single(ReportFindings(consumer.Path));
        Assert.Equal("snapshot-read-failed", finding!["code"]!.GetValue<string>());
    }

    /// <summary>Check passes when no release folder exists yet.</summary>
    [Fact]
    public void CheckPassesWhenNoReleaseExists()
    {
        using var consumer = ModelFixture.Prepare("small");
        AppendPersistence(consumer.Path, WriteQuietHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("snapshot-", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(ReportFindings(consumer.Path));
    }

    /// <summary>Record creates the release folder and runs the harness in record mode.</summary>
    [Fact]
    public void RecordCreatesFolderAndRunsHarness()
    {
        using var consumer = ModelFixture.Prepare("small");
        AppendPersistence(consumer.Path, WriteRecordHarness(consumer.Path, ".litespec/proof/persistence-snapshots/v2/expense.json"));

        var result = RunCommand("--root", consumer.Path, "--record", "--release", "v2");

        Assert.Equal(0, result.Status);
        Assert.True(Directory.Exists(Path.Combine(consumer.Path, ".litespec", "proof", "persistence-snapshots", "v2")), "release folder");
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec", "proof", "persistence-snapshots", "v2", "expense.json")), "snapshot file");
        Assert.Contains("record .litespec/proof/persistence-snapshots/v2", CallsLog(consumer.Path), StringComparison.Ordinal);
        Assert.Empty(ReportFindings(consumer.Path));
    }

    /// <summary>Record without a release is a usage error.</summary>
    [Fact]
    public void RecordWithoutReleaseIsUsageError()
    {
        using var consumer = ModelFixture.Prepare("small");
        AppendPersistence(consumer.Path, WriteQuietHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path, "--record");

        Assert.Equal(2, result.Status);
    }

    /// <summary>A release without record is a usage error.</summary>
    [Fact]
    public void ReleaseWithoutRecordIsUsageError()
    {
        using var consumer = ModelFixture.Prepare("small");
        AppendPersistence(consumer.Path, WriteQuietHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path, "--release", "v2");

        Assert.Equal(2, result.Status);
    }

    /// <summary>Record rejects a release that escapes the snapshots folder.</summary>
    [Fact]
    public void RecordRejectsReleaseOutsideSnapshots()
    {
        using var consumer = ModelFixture.Prepare("small");
        AppendPersistence(consumer.Path, WriteQuietHarness(consumer.Path));

        var result = RunCommand("--root", consumer.Path, "--record", "--release", "../outside");

        Assert.Equal(2, result.Status);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, "outside")), "no folder outside the consumer");
    }

    /// <summary>A project file without a persistence harness is a usage error.</summary>
    [Fact]
    public void MissingPersistenceBlockIsUsageError()
    {
        using var consumer = ModelFixture.Prepare("small");

        var result = RunCommand("--root", consumer.Path);

        Assert.Equal(2, result.Status);
    }

    /// <summary>Help prints the usage text.</summary>
    [Fact]
    public void HelpPrintsUsage()
    {
        var result = RunCommand("--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("persistence-compat", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is a usage error.</summary>
    [Fact]
    public void UnknownOptionIsUsageError()
    {
        using var consumer = ModelFixture.Prepare("small");

        var result = RunCommand("--root", consumer.Path, "--bogus");

        Assert.Equal(2, result.Status);
    }

    /// <summary>Runs the command with its own console output, so the case proves the exit code and the printed text together.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    static (int Status, string Stdout, string Stderr) RunCommand(params string[] args)
    {
        var result = TestCli.Run(["verify", "persistence-compat", .. args]);

        return (result.Status, result.Stdout, result.Stderr);
    }

    /// <summary>Writes a snapshot file for one release, so the check has a stored snapshot to read.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="release">The release folder name.</param>
    static void WriteSnapshot(string root, string release)
    {
        var folder = Path.Combine(root, ".litespec", "proof", "persistence-snapshots", release);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "expense.json"), """{"stored": true}""");
    }

    /// <summary>Names the persistence harness in the fixture project file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="command">The harness command.</param>
    static void AppendPersistence(string root, string command) =>
        File.AppendAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            "\nverify:\n  persistence:\n    command: '" + command + "'\n");

    /// <summary>Writes a check harness that logs its call and prints one snapshot line.</summary>
    /// <param name="root">The consumer root holding the harness script.</param>
    /// <param name="file">The snapshot path the printed line names.</param>
    /// <param name="status">The status the printed line states.</param>
    /// <param name="message">The reason the printed line states.</param>
    /// <returns>The harness command for the project file.</returns>
    static string WriteCheckHarness(string root, string file, string status, string message) => WriteHarness(
        root,
        "echo check %~2>> harness-calls.log\n"
            + "echo {\"file\": \"" + file + "\", \"status\": \"" + status + "\", \"message\": \"" + message + "\"}",
        "echo \"check $2\" >> harness-calls.log\n"
            + "echo '{\"file\": \"" + file + "\", \"status\": \"" + status + "\", \"message\": \"" + message + "\"}'");

    /// <summary>Writes a harness that logs its call and prints nothing.</summary>
    /// <param name="root">The consumer root holding the harness script.</param>
    /// <returns>The harness command for the project file.</returns>
    static string WriteQuietHarness(string root) => WriteHarness(
        root,
        "echo check %~2>> harness-calls.log",
        "echo \"check $2\" >> harness-calls.log");

    /// <summary>Writes a harness that logs its call, prints one ok line, and writes a snapshot file.</summary>
    /// <param name="root">The consumer root holding the harness script.</param>
    /// <param name="file">The snapshot path the printed line names.</param>
    /// <returns>The harness command for the project file.</returns>
    static string WriteRecordHarness(string root, string file) => WriteHarness(
        root,
        "echo record %~2>> harness-calls.log\n"
            + "echo {\"file\": \"" + file + "\", \"status\": \"ok\", \"message\": \"\"}\n"
            + "echo {\"stored\": true}> %~2/expense.json",
        "echo \"record $2\" >> harness-calls.log\n"
            + "echo '{\"file\": \"" + file + "\", \"status\": \"ok\", \"message\": \"\"}'\n"
            + "echo '{\"stored\": true}' > \"$2/expense.json\"");

    /// <summary>Writes a harness that logs its call and prints a line that is not a snapshot result.</summary>
    /// <param name="root">The consumer root holding the harness script.</param>
    /// <returns>The harness command for the project file.</returns>
    static string WriteBrokenHarness(string root) => WriteHarness(
        root,
        "echo check %~2>> harness-calls.log\n"
            + "echo this is not json",
        "echo \"check $2\" >> harness-calls.log\n"
            + "echo this is not json");

    /// <summary>Writes a fake harness script and returns the command naming it, quoted for the project file.</summary>
    /// <param name="root">The consumer root holding the harness script.</param>
    /// <param name="windowsBody">The batch script body, with one command per line.</param>
    /// <param name="unixBody">The shell script body, with one command per line.</param>
    /// <returns>The harness command for the project file.</returns>
    static string WriteHarness(string root, string windowsBody, string unixBody)
    {
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(root, "harness.cmd");
            File.WriteAllText(path, "@echo off\r\n" + windowsBody.Replace("\n", "\r\n", StringComparison.Ordinal));
            return "\"" + path + "\"";
        }

        var script = Path.Combine(root, "harness.sh");
        File.WriteAllText(script, unixBody + "\n");
        return "sh \"" + script + "\"";
    }

    /// <summary>Reads the harness call log the fake harness writes.</summary>
    /// <param name="root">The consumer root the harness runs in.</param>
    /// <returns>The logged harness calls.</returns>
    static string CallsLog(string root)
    {
        var log = Path.Combine(root, "harness-calls.log");
        Assert.True(File.Exists(log), "harness calls log");
        return File.ReadAllText(log);
    }

    /// <summary>Reads the findings of the JSON report the command writes.</summary>
    /// <param name="root">The consumer root the command writes under.</param>
    /// <returns>The reported findings.</returns>
    static JsonArray ReportFindings(string root)
    {
        var report = Path.Combine(root, ".litespec", "verify", "persistence-compat.json");
        Assert.True(File.Exists(report), "JSON report");
        return JsonNode.Parse(File.ReadAllText(report))!["findings"]!.AsArray();
    }
}
