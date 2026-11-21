using System.Diagnostics;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check --since</c> blocks the findings the base lacks, and that <c>--staged</c> is rejected.</summary>
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

    /// <summary>Reads one finding's outcome and reason from the check JSON by code and subject.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <param name="code">The finding code to read.</param>
    /// <param name="subject">The finding subject to read.</param>
    /// <returns>The finding outcome and reason, each null when absent.</returns>
    static (string? Outcome, string? Reason) OutcomeOfSubject(string stdout, string code, string subject)
    {
        using var document = System.Text.Json.JsonDocument.Parse(stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == code
                && finding.GetProperty("subject").GetString() == subject)
            {
                var reason = finding.GetProperty("reason");

                return (
                    finding.GetProperty("outcome").GetString(),
                    reason.ValueKind == System.Text.Json.JsonValueKind.Null ? null : reason.GetString());
            }
        }

        throw new InvalidOperationException($"No finding carries '{code}' on '{subject}'.");
    }

    /// <summary>Reads the new since finding count from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <returns>The new finding count.</returns>
    static int NewFindingsOf(string stdout)
    {
        using var document = System.Text.Json.JsonDocument.Parse(stdout);

        return document.RootElement.GetProperty("newFindings").GetInt32();
    }

    /// <summary>Reads the base error from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <returns>The base error text.</returns>
    static string BaseErrorOf(string stdout)
    {
        using var document = System.Text.Json.JsonDocument.Parse(stdout);

        return document.RootElement.GetProperty("baseError").GetString()!;
    }

    /// <summary>Removes the permission block carrying the id from the lending permissions page.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="id">The permission id of the block to remove.</param>
    static void RemovePermission(string root, string id)
    {
        var file = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "permissions.yaml");
        var lines = File.ReadAllLines(file).ToList();
        var at = lines.FindIndex(line => line.Contains(id, StringComparison.Ordinal));

        if (at < 0)
        {
            throw new InvalidOperationException($"'{file}' holds no '{id}'.");
        }

        var start = at;

        while (start > 0 && !lines[start].StartsWith("- type:", StringComparison.Ordinal))
        {
            start--;
        }

        var end = at + 1;

        while (end < lines.Count && lines[end].StartsWith("  ", StringComparison.Ordinal))
        {
            end++;
        }

        lines.RemoveRange(start, end - start);
        File.WriteAllLines(file, lines);
    }

    /// <summary>A staged check is rejected as an unknown option.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void CheckStagedIsRejected()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--staged");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--staged'.", result.Output);
    }

    /// <summary>
    ///     A since check reads the same base when the root runs through a symbolic link, as a macOS
    ///     temporary folder does through /var: git names the repository by its resolved path, and the
    ///     base archive still meets the records under the root as given.
    /// </summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
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

    /// <summary>A since check blocks on new findings and reports the findings the base already holds as unchanged.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceBlocksOnlyNewFindings()
    {
        using var fixture = ModelFixture.Prepare("large");

        CommitAll(fixture.Path);
        ApplyOverlay(fixture.Path, "paging-undeclared");
        CommitWork(fixture.Path, "paging break");
        ApplyOverlay(fixture.Path, "write-undeclared");

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        Assert.Equal(1, since.Status);
        Assert.Contains("Since HEAD:", since.Output);
        Assert.Contains(" new findings.", since.Output);
        Assert.Contains("paging-undeclared", since.Output);
        Assert.Contains("write-undeclared", since.Output);
        Assert.Contains("[G unchanged]", since.Output);

        var json = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD", "--format", "json");
        var (pagingOutcome, pagingReason) = OutcomeOf(json.Stdout, "paging-undeclared");
        var (writeOutcome, _) = OutcomeOf(json.Stdout, "write-undeclared");

        Assert.Equal(1, json.Status);
        Assert.Contains("\"scope\"", json.Output);
        Assert.Contains("\"since\"", json.Output);
        Assert.True(NewFindingsOf(json.Stdout) >= 1);
        Assert.Equal("reported", pagingOutcome);
        Assert.Equal("unchanged", pagingReason);
        Assert.Equal("blocking", writeOutcome);

        var full = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (fullPaging, _) = OutcomeOf(full.Stdout, "paging-undeclared");

        Assert.Equal(1, full.Status);
        Assert.Equal("blocking", fullPaging);
    }

    /// <summary>A since check on a clean tree reports everything and exits 0.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceWithNothingChangedExits0()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        CommitAll(fixture.Path);

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        // When the trees differ, the message names what git sees as changed, so a
        // difference that appears on one operating system only can be read from the log.
        Assert.True(
            since.Output.Contains("Since HEAD: 0 new findings.", StringComparison.Ordinal),
            $"{since.Output}\ngit status after the check:\n{RunGit(fixture.Path, "status", "--porcelain", "--untracked-files=all")}");
        Assert.Equal(0, since.Status);
        Assert.Contains("write-undeclared", since.Output);
    }

    /// <summary>Files the tool writes under <c>.litespec/model</c> while it builds introduce no new since findings.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceIgnoresTheToolOutputFolder()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        CommitAll(fixture.Path);

        var cache = Path.Combine(fixture.Path, ".litespec/model", "frontend-facts");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "svelte-cached.json"), "{}");

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        Assert.Contains("Since HEAD: 0 new findings.", since.Output);
        Assert.Equal(0, since.Status);
    }

    /// <summary>A since check whose base cannot be built blocks every finding and says why.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceWithUnknownRevisionBlocksEveryFinding()
    {
        using var fixture = ModelFixture.Prepare("large", "write-undeclared");

        CommitAll(fixture.Path);

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "no-such-ref");

        Assert.Equal(1, since.Status);
        Assert.Contains("no-such-ref", since.Output);
        Assert.Contains("Every finding blocks.", since.Output);
        Assert.Contains("[S] [write-undeclared]", since.Output);

        var json = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "no-such-ref", "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "write-undeclared");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
        Assert.Contains("no-such-ref", BaseErrorOf(json.Stdout));
    }

    /// <summary>With no finding to block, an unreadable base still says why and exits 0.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceWithUnknownRevisionOnACleanTreeExits0()
    {
        using var fixture = ModelFixture.Prepare("large");

        CommitAll(fixture.Path);

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "no-such-ref");

        Assert.Equal(0, since.Status);
        Assert.Contains("no-such-ref", since.Output);
        Assert.Contains("Every finding blocks.", since.Output);
    }

    /// <summary>A since check blocks the use-case finding a deleted permission leaves two links away.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CheckSinceBlocksAUseCaseBrokenByAPermissionDelete()
    {
        using var fixture = ModelFixture.Prepare("large");

        CommitAll(fixture.Path);
        RemovePermission(fixture.Path, "permission/borrow-copy");

        var since = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD", "--format", "json");

        Assert.Equal(1, since.Status);
        var (outcome, reason) = OutcomeOfSubject(since.Stdout, "reference-unknown", "use-case/lending.borrow-copy");

        Assert.Equal("blocking", outcome);
        Assert.Null(reason);

        var text = TestCli.Run("model", "check", "--root", fixture.Path, "--since", "HEAD");

        Assert.Equal(1, text.Status);
        Assert.Contains("[reference-unknown]", text.Output);
    }

    /// <summary>Findings that differ only by path and line share one since key.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void SinceKeyIgnoresPathAndLine()
    {
        var before = new Finding("write-undeclared", "Use case 'use-case/lending.borrow-copy' takes repository 'ILoanStore', whose aggregate is outside its changes and reads.", "/base/file.cs", 12, "use-case/lending.borrow-copy");
        var after = new Finding("write-undeclared", "Use case 'use-case/lending.borrow-copy' takes repository 'ILoanStore', whose aggregate is outside its changes and reads.", "/head/file.cs", 40, "use-case/lending.borrow-copy");

        Assert.Equal(ModelCommand.SinceKey(before), ModelCommand.SinceKey(after));
    }

    /// <summary>Findings with different messages hold different since keys.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void SinceKeyDiffersByMessage()
    {
        var one = new Finding("write-undeclared", "Use case 'use-case/lending.borrow-copy' takes repository 'ILoanStore', whose aggregate is outside its changes and reads.", "/head/file.cs", 12, "use-case/lending.borrow-copy");
        var other = new Finding("write-undeclared", "Use case 'use-case/lending.borrow-copy' takes repository 'ICopyStore', whose aggregate is outside its changes and reads.", "/head/file.cs", 12, "use-case/lending.borrow-copy");

        Assert.NotEqual(ModelCommand.SinceKey(one), ModelCommand.SinceKey(other));
    }

    /// <summary>Findings with no subject share one since key when their code and message match.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void SinceKeyMatchesNullSubjects()
    {
        var before = new Finding("reader-missing", "Stack root 'modules' resolves nowhere.", "/base/project.yaml", 1, null);
        var after = new Finding("reader-missing", "Stack root 'modules' resolves nowhere.", "/head/project.yaml", 1, null);

        Assert.Equal(ModelCommand.SinceKey(before), ModelCommand.SinceKey(after));
    }
}
