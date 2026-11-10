using System.Diagnostics;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check --since</c> demotes the findings outside the changed scope, and that <c>--staged</c> is rejected.</summary>
public sealed class ModelCheckSinceTests
{
    /// <summary>Copies a broken overlay over the prepared fixture, like the shared fixture does.</summary>
    /// <param name="target">The prepared fixture folder to copy over.</param>
    /// <param name="overlayName">The overlay folder name under the fixtures.</param>
    static void ApplyOverlay(string target, string overlayName)
    {
        var overlay = TestCli.Fixture($"Model/broken/{overlayName}");

        foreach (var file in Directory.GetFiles(overlay, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(overlay, file);

            if (relative == "DELETES.txt")
            {
                continue;
            }

            var destination = Path.Combine(target, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    /// <summary>Runs git in the folder and returns its standard output, throwing when it fails.</summary>
    /// <param name="workdir">The folder to run git in.</param>
    /// <param name="args">The git arguments.</param>
    /// <returns>The git standard output.</returns>
    static string RunGit(string workdir, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"git {string.Join(' ', args)} did not finish in two minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr.Result}");
        }

        return stdout.Result;
    }

    /// <summary>Commits the whole fixture so the index starts clean.</summary>
    /// <param name="path">The fixture folder to commit.</param>
    static void CommitAll(string path)
    {
        RunGit(path, "-c", "init.defaultBranch=main", "init");
        RunGit(path, "add", "-A");
        RunGit(
            path, "-c", "user.name=Test", "-c", "user.email=test@example.com",
            "-c", "commit.gpgsign=false", "commit", "-m", "init");
    }

    /// <summary>Commits the working tree on top of the fixture commit.</summary>
    /// <param name="path">The fixture folder to commit.</param>
    /// <param name="message">The commit message.</param>
    static void CommitWork(string path, string message)
    {
        RunGit(path, "add", "-A");
        RunGit(
            path, "-c", "user.name=Test", "-c", "user.email=test@example.com",
            "-c", "commit.gpgsign=false", "commit", "-m", message);
    }

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

    /// <summary>A staged check is rejected as an unknown option.</summary>
    [Fact]
    public void CheckStagedIsRejected()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--staged");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--staged'.", result.Output);
    }

    /// <summary>
    ///     A since check reads the same scope when the root runs through a symbolic link, as a macOS
    ///     temporary folder does through /var: git names the repository by its resolved path, and the
    ///     changed files still meet the records under the root as given.
    /// </summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void CheckSinceReadsARootReachedThroughASymbolicLink()
    {
        using var fixture = ModelFixture.Prepare("large");
        using var links = TestCli.Temp("since-link");

        CommitAll(fixture.Path);
        ApplyOverlay(fixture.Path, "write-undeclared");

        var linked = Path.Combine(links.Path, "consumer");

        // A symbolic link on Windows needs a privilege a test run does not hold; a junction does not.
        if (OperatingSystem.IsWindows())
        {
            using var junction = Process.Start(new ProcessStartInfo("cmd.exe", ["/c", "mklink", "/J", linked, fixture.Path])
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            })!;
            junction.StandardOutput.ReadToEnd();
            junction.WaitForExit();
            Assert.Equal(0, junction.ExitCode);
        }
        else
        {
            Directory.CreateSymbolicLink(linked, fixture.Path);
        }

        var since = TestCli.Run("model", "check", "--root", linked, "--since", "HEAD");

        Assert.Equal(1, since.Status);
        Assert.Contains("write-undeclared", since.Output);
        Assert.DoesNotContain("[S unchanged] [write-undeclared]", since.Output);
    }

    /// <summary>A since check blocks on changed findings and reports the rest.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void CheckSinceBlocksOnlyChangedFindings()
    {
        using var fixture = ModelFixture.Prepare("large");

        CommitAll(fixture.Path);
        ApplyOverlay(fixture.Path, "paging-undeclared");
        CommitWork(fixture.Path, "paging break");
        ApplyOverlay(fixture.Path, "write-undeclared");

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        Assert.Equal(1, since.Status);
        Assert.Contains("Since HEAD:", since.Output);
        Assert.Contains("paging-undeclared", since.Output);
        Assert.Contains("write-undeclared", since.Output);
        Assert.Contains("[G unchanged]", since.Output);

        var json = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD", "--format", "json");
        var (pagingOutcome, pagingReason) = OutcomeOf(json.Stdout, "paging-undeclared");
        var (writeOutcome, _) = OutcomeOf(json.Stdout, "write-undeclared");

        Assert.Equal(1, json.Status);
        Assert.Contains("\"scope\"", json.Output);
        Assert.Contains("\"since\"", json.Output);
        Assert.Equal("reported", pagingOutcome);
        Assert.Equal("unchanged", pagingReason);
        Assert.Equal("blocking", writeOutcome);

        var full = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (fullPaging, _) = OutcomeOf(full.Stdout, "paging-undeclared");

        Assert.Equal(1, full.Status);
        Assert.Equal("blocking", fullPaging);
    }

    /// <summary>A since check on a clean tree reports everything and exits 0.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void CheckSinceWithNothingChangedExits0()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        CommitAll(fixture.Path);

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        // When the scope is not empty, the message names what git sees as changed, so a
        // difference that appears on one operating system only can be read from the log.
        Assert.True(
            since.Output.Contains("Since HEAD: 0 files, 0 records.", StringComparison.Ordinal),
            $"{since.Output}\ngit status after the check:\n{RunGit(fixture.Path, "status", "--porcelain", "--untracked-files=all")}");
        Assert.Equal(0, since.Status);
        Assert.Contains("write-undeclared", since.Output);
    }

    /// <summary>Files the tool writes under <c>.litespec/model</c> while it builds never widen the since scope.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void CheckSinceIgnoresTheToolOutputFolder()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        CommitAll(fixture.Path);

        var cache = Path.Combine(fixture.Path, ".litespec/model", "frontend-facts");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "svelte-cached.json"), "{}");

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        Assert.Contains("Since HEAD: 0 files, 0 records.", since.Output);
    }

    /// <summary>A since check with an unknown revision explains itself and exits 2.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void CheckSinceWithUnknownRevisionExits2()
    {
        using var fixture = ModelFixture.Prepare("large");

        CommitAll(fixture.Path);

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "no-such-ref");

        Assert.Equal(2, since.Status);
        Assert.Contains("no-such-ref", since.Output);
    }
}
