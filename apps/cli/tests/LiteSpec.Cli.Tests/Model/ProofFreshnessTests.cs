using System.Text;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves a proof is fresh at HEAD or with only record paths changed, else stale with the reason.</summary>
public sealed class ProofFreshnessTests
{
    const string RunCommit = "1111111111111111111111111111111111111111";
    const string HeadCommit = "2222222222222222222222222222222222222222";

    /// <summary>A proof whose run commit is HEAD is fresh with nothing behind.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FreshAtHead()
    {
        using var temp = TestCli.Temp("freshness");
        var git = new FakeGit();
        git.Reply("rev-parse HEAD", Ok(HeadCommit + "\n"));
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(HeadCommit);

        Assert.Equal("fresh", status.Status);
        Assert.Equal(HeadCommit, status.Commit);
        Assert.Equal("2222222", status.LastPassedAt);
        Assert.Equal(0, status.CommitsBehind);
        Assert.Null(status.StaleReason);
        Assert.DoesNotContain(git.Calls, call => call.StartsWith("diff ", StringComparison.Ordinal));
    }

    /// <summary>A proof with only record and Markdown paths changed is fresh.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FreshWhenOnlyRecordPathsChanged()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, ".litespec/borrowing.yaml\0notes.md\0", "");
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(RunCommit);

        Assert.Equal("fresh", status.Status);
        Assert.Equal("1111111", status.LastPassedAt);
        Assert.Null(status.StaleReason);
    }

    /// <summary>A proof with a changed code file is stale with the commits behind.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void StaleWhenCodeChanged()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, "src/Lending/Loan.cs\0", "");
        git.Reply($"rev-list --count {RunCommit}..HEAD", Ok("3\n"));
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(RunCommit);

        Assert.Equal("stale", status.Status);
        Assert.Equal(RunCommit, status.Commit);
        Assert.Equal("1111111", status.LastPassedAt);
        Assert.Equal(3, status.CommitsBehind);
        Assert.Equal("files outside the record and documentation paths changed", status.StaleReason);
    }

    /// <summary>A proof whose run commit the repository does not know is stale.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void StaleWhenCommitUnknown()
    {
        using var temp = TestCli.Temp("freshness");
        var git = new FakeGit();
        git.Reply("rev-parse HEAD", Ok(HeadCommit + "\n"));
        git.Reply($"rev-parse --verify --quiet {RunCommit}^{{commit}}", Fail());
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(RunCommit);

        Assert.Equal("stale", status.Status);
        Assert.Equal(0, status.CommitsBehind);
        Assert.Equal("run commit is unknown to the repository", status.StaleReason);
    }

    /// <summary>A proof read where git is unavailable is stale.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void StaleWhenGitUnavailable()
    {
        using var temp = TestCli.Temp("freshness");
        var git = new FakeGit();
        git.Reply("rev-parse HEAD", Fail());
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(RunCommit);

        Assert.Equal("stale", status.Status);
        Assert.Equal(0, status.CommitsBehind);
        Assert.Equal("git is unavailable", status.StaleReason);
    }

    /// <summary>An untracked code file stales the proof like a tracked change.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UntrackedCodeFileStalesProof()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, "", "scratch/notes.cs\0");
        git.Reply($"rev-list --count {RunCommit}..HEAD", Ok("1\n"));
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf(RunCommit);

        Assert.Equal("stale", status.Status);
        Assert.Equal(1, status.CommitsBehind);
        Assert.Equal("files outside the record and documentation paths changed", status.StaleReason);
    }

    /// <summary>A Markdown file matches whatever letter case its extension uses.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void MarkdownMatchIsCaseInsensitive()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, "NOTES.MD\0", "");
        var freshness = Freshness(temp.Path, git);

        Assert.Equal("fresh", freshness.StatusOf(RunCommit).Status);
    }

    /// <summary>A text file that is no Markdown file stales the proof.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void NonMarkdownTextFileStalesProof()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, "notes.txt\0", "");
        git.Reply($"rev-list --count {RunCommit}..HEAD", Ok("2\n"));
        var freshness = Freshness(temp.Path, git);

        Assert.Equal("stale", freshness.StatusOf(RunCommit).Status);
    }

    /// <summary>A run that recorded no commit is stale.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void EmptyRunCommitReadsStale()
    {
        using var temp = TestCli.Temp("freshness");
        var git = new FakeGit();
        git.Reply("rev-parse HEAD", Ok(HeadCommit + "\n"));
        var freshness = Freshness(temp.Path, git);

        var status = freshness.StatusOf("");

        Assert.Equal("stale", status.Status);
        Assert.Equal("", status.Commit);
        Assert.Equal("", status.LastPassedAt);
        Assert.Equal("the run recorded no commit", status.StaleReason);
    }

    /// <summary>Changed paths are read once per distinct run commit.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ChangedPathsAreCachedPerCommit()
    {
        using var temp = TestCli.Temp("freshness");
        var git = AtHeadWithChanges(RunCommit, "src/Lending/Loan.cs\0", "");
        git.Reply($"rev-list --count {RunCommit}..HEAD", Ok("3\n"));
        var freshness = Freshness(temp.Path, git);

        Assert.Equal("stale", freshness.StatusOf(RunCommit).Status);
        Assert.Equal("stale", freshness.StatusOf(RunCommit).Status);
        Assert.Single(git.Calls, call => call.StartsWith("diff ", StringComparison.Ordinal));
    }

    static ProofFreshness Freshness(string root, FakeGit git) =>
        new(root, [Path.Combine(root, ".litespec")], git.Run);

    static FakeGit AtHeadWithChanges(string commit, string diff, string untracked)
    {
        var git = new FakeGit();
        git.Reply("rev-parse HEAD", Ok(HeadCommit + "\n"));
        git.Reply($"rev-parse --verify --quiet {commit}^{{commit}}", Ok(commit + "\n"));
        git.Reply("rev-parse --show-cdup", Ok("\n"));
        git.Reply($"diff {commit} --name-only -z", Ok(diff));
        git.Reply("ls-files --others --exclude-standard -z", Ok(untracked));

        return git;
    }

    static ProcessRunner.Result Ok(string stdout) =>
        new(0, Encoding.UTF8.GetBytes(stdout), "");

    static ProcessRunner.Result Fail() =>
        new(1, [], "git failed");

    /// <summary>Replies canned git output per command and records every call.</summary>
    sealed class FakeGit
    {
        /// <summary>Gets the commands run, each as its joined arguments.</summary>
        public List<string> Calls { get; } = [];

        /// <summary>The canned reply per joined command.</summary>
        readonly Dictionary<string, ProcessRunner.Result> replies = new(StringComparer.Ordinal);

        /// <summary>Replies to the command with the result.</summary>
        /// <param name="command">The joined command arguments.</param>
        /// <param name="result">The canned git result.</param>
        public void Reply(string command, ProcessRunner.Result result) => replies[command] = result;

        /// <summary>Runs the command against the canned replies.</summary>
        /// <param name="args">The git arguments.</param>
        /// <returns>The canned reply, or a failure for an unexpected command.</returns>
        public ProcessRunner.Result Run(IReadOnlyList<string> args)
        {
            var command = string.Join(" ", args);
            Calls.Add(command);

            return replies.TryGetValue(command, out var reply) ? reply : new ProcessRunner.Result(1, [], "unexpected git call");
        }
    }
}
