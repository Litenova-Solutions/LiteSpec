using System.Text.Json.Nodes;
using Xunit;
using LiteSpec.Cli.Audit.Semantic;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Audit.Semantic;

/// <summary>Tests for <c>litespec audit semantic</c>: the changed-file mapping, the review packets, and the stub markers.</summary>
public sealed class SemanticAuditTests
{
    /// <summary>The use case the fixture handler implements.</summary>
    const string UseCaseId = "use-case/lending.register-copy";

    /// <summary>The handler path as git names it, with forward slashes.</summary>
    const string HandlerRelative = "apps/api/src/Modules/Library.Lending/Application/Copies/RegisterCopy/RegisterCopyCommandHandler.cs";

    /// <summary>A changed handler holding a TODO reports code-unimplemented and writes the use-case packet and report.</summary>
    [Fact(DisplayName = "a changed handler holding a TODO reports code-unimplemented and writes the use-case packet and report")]
    public void ChangedHandlerWithTodoReportsStubAndWritesPacket()
    {
        using var temp = ModelFixture.Prepare("small");
        var handler = Path.Combine(temp.Path, HandlerRelative.Replace('/', Path.DirectorySeparatorChar));
        File.AppendAllText(handler, "// TODO: register the shelf copy.\n");
        var line = File.ReadAllLines(handler).Length;
        var since = "HEAD~1";

        var (status, stdout, _) = RunAudit(temp.Path, since, HandlerRelative + "\0", "");

        Assert.Equal(1, status);
        Assert.Contains($"{HandlerRelative}:{line} [S] [code-unimplemented]", stdout, StringComparison.Ordinal);

        var packet = Path.Combine(temp.Path, ".litespec", "audit", "semantic", "lending.register-copy.md");
        Assert.True(File.Exists(packet), "use-case packet");
        var packetText = File.ReadAllText(packet);
        Assert.Contains(UseCaseId, packetText, StringComparison.Ordinal);
        Assert.Contains(HandlerRelative, packetText, StringComparison.Ordinal);

        var report = ReadReport(Path.Combine(temp.Path, ".litespec", "audit", "semantic.json"));
        Assert.Equal(since, report["since"]!.GetValue<string>());
        Assert.Equal([UseCaseId], report["useCases"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal([".litespec/audit/semantic/lending.register-copy.md"], report["packets"]!.AsArray().Select(node => node!.GetValue<string>()));
        var findings = report["findings"]!.AsArray();
        var finding = Assert.Single(findings)!.AsObject();
        Assert.Equal("code-unimplemented", finding["code"]!.GetValue<string>());
        Assert.Equal(HandlerRelative, finding["path"]!.GetValue<string>());
        Assert.Equal(line, finding["line"]!.GetValue<int>());
        Assert.Equal(UseCaseId, finding["subject"]!.GetValue<string>());
    }

    /// <summary>A changed line holding a stub marker reports code-unimplemented.</summary>
    /// <param name="marker">The stub marker the appended line holds.</param>
    [Theory(DisplayName = "a changed line holding a stub marker reports code-unimplemented")]
    [InlineData("// TODO: register the shelf copy.")]
    [InlineData("// throw new NotImplementedException();")]
    [InlineData("// throw new Error(\"not implemented\");")]
    public void ChangedLineHoldingMarkerReportsStub(string marker)
    {
        using var temp = ModelFixture.Prepare("small");
        var handler = Path.Combine(temp.Path, HandlerRelative.Replace('/', Path.DirectorySeparatorChar));
        File.AppendAllText(handler, marker + "\n");

        var (status, stdout, _) = RunAudit(temp.Path, "HEAD~1", HandlerRelative + "\0", "");

        Assert.Equal(1, status);
        Assert.Contains("[S] [code-unimplemented]", stdout, StringComparison.Ordinal);
        Assert.Contains(HandlerRelative, stdout, StringComparison.Ordinal);
    }

    /// <summary>A changed handler without a marker writes the packet with no findings.</summary>
    [Fact(DisplayName = "a changed handler without a marker writes the packet with no findings")]
    public void ChangedHandlerWithoutMarkerWritesPacketWithoutFindings()
    {
        using var temp = ModelFixture.Prepare("small");

        var (status, stdout, _) = RunAudit(temp.Path, "HEAD~1", HandlerRelative + "\0", "");

        Assert.Equal(0, status);
        Assert.DoesNotContain("code-unimplemented", stdout);
        Assert.True(File.Exists(Path.Combine(temp.Path, ".litespec", "audit", "semantic", "lending.register-copy.md")), "use-case packet");

        var report = ReadReport(Path.Combine(temp.Path, ".litespec", "audit", "semantic.json"));
        Assert.Empty(report["findings"]!.AsArray());
        Assert.Equal([UseCaseId], report["useCases"]!.AsArray().Select(node => node!.GetValue<string>()));
    }

    /// <summary>A TODO in a test file reports nothing and writes no packet.</summary>
    [Fact(DisplayName = "a TODO in a test file reports nothing and writes no packet")]
    public void MarkerInTestFileReportsNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        var testFile = Path.Combine(temp.Path, "apps", "api", "tests", "Library.Acceptance.Tests", "ExtraCheck.cs");
        File.WriteAllText(testFile, "// TODO: cover the late return.\n");
        var relative = "apps/api/tests/Library.Acceptance.Tests/ExtraCheck.cs";

        var (status, _, _) = RunAudit(temp.Path, "HEAD~1", "", relative + "\0");

        Assert.Equal(0, status);

        var report = ReadReport(Path.Combine(temp.Path, ".litespec", "audit", "semantic.json"));
        Assert.Empty(report["findings"]!.AsArray());
        Assert.Empty(report["useCases"]!.AsArray());
        Assert.False(Directory.Exists(Path.Combine(temp.Path, ".litespec", "audit", "semantic")), "no packet folder");
    }

    /// <summary>A stub in a file no use case maps reports the finding with no packet.</summary>
    [Fact(DisplayName = "a stub in a file no use case maps reports the finding with no packet")]
    public void StubInUnmappedFileReportsFindingWithoutPacket()
    {
        using var temp = ModelFixture.Prepare("small");
        var script = Path.Combine(temp.Path, "ops", "notes.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(script)!);
        File.WriteAllText(script, "// TODO: script the copy count.\n");
        var relative = "ops/notes.cs";

        var (status, stdout, _) = RunAudit(temp.Path, "HEAD~1", "", relative + "\0");

        Assert.Equal(1, status);
        Assert.Contains($"{relative}:1 [S] [code-unimplemented]", stdout, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, ".litespec", "audit", "semantic")), "no packet folder");

        var report = ReadReport(Path.Combine(temp.Path, ".litespec", "audit", "semantic.json"));
        var finding = Assert.Single(report["findings"]!.AsArray())!.AsObject();
        Assert.Null(finding["subject"]);
    }

    /// <summary>No changed files writes an empty report with no packets.</summary>
    [Fact(DisplayName = "no changed files writes an empty report with no packets")]
    public void EmptyChangeSetWritesEmptyReport()
    {
        using var temp = ModelFixture.Prepare("small");

        var (status, _, _) = RunAudit(temp.Path, "HEAD~1", "", "");

        Assert.Equal(0, status);

        var report = ReadReport(Path.Combine(temp.Path, ".litespec", "audit", "semantic.json"));
        Assert.Empty(report["useCases"]!.AsArray());
        Assert.Empty(report["packets"]!.AsArray());
        Assert.Empty(report["findings"]!.AsArray());
        Assert.False(Directory.Exists(Path.Combine(temp.Path, ".litespec", "audit", "semantic")), "no packet folder");
    }

    /// <summary>A revision git cannot resolve reports unreadable changes.</summary>
    [Fact(DisplayName = "a revision git cannot resolve reports unreadable changes")]
    public void UnknownRevisionReportsUnreadableChanges()
    {
        using var temp = ModelFixture.Prepare("small");
        var since = "no-such-revision";
        SemanticGit.Runner = (_, args) =>
            args is ["rev-parse", "--show-cdup"]
                ? (0, "", "")
                : (1, "", $"unknown revision '{since}'");

        try
        {
            var config = ConsumerConfig.Load(temp.Path);
            var error = new StringWriter();

            var status = SemanticCommand.Execute(config, since, new StringWriter(), error);

            Assert.Equal(2, status);
            Assert.Contains(since, error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            SemanticGit.Runner = null;
        }
    }

    /// <summary>A missing --since is a usage error.</summary>
    [Fact(DisplayName = "a missing --since is a usage error")]
    public void MissingSinceIsAUsageError()
    {
        using var temp = ModelFixture.Prepare("small");

        Assert.Equal(2, SemanticCommand.Run(["--root", temp.Path]));
    }

    /// <summary>--help prints the usage text and exits zero.</summary>
    [Fact(DisplayName = "--help prints the usage text and exits zero")]
    public void HelpPrintsUsage()
    {
        Assert.Equal(0, SemanticCommand.Run(["--help"]));
        Assert.Contains("--since", SemanticCommand.Usage, StringComparison.Ordinal);
    }

    /// <summary>Reads a JSON object file.</summary>
    /// <param name="path">The file path to read and parse.</param>
    /// <returns>The parsed JSON object.</returns>
    static JsonObject ReadReport(string path) => (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;

    /// <summary>Runs the audit with canned git output, restoring the real runner after.</summary>
    /// <param name="root">The consumer root to audit.</param>
    /// <param name="since">The revision the fake git answers for.</param>
    /// <param name="tracked">The NUL-separated tracked paths the fake diff answers.</param>
    /// <param name="untracked">The NUL-separated untracked paths the fake ls-files answers.</param>
    /// <returns>The exit code with the two output streams.</returns>
    static (int Status, string Stdout, string Stderr) RunAudit(string root, string since, string tracked, string untracked)
    {
        SemanticGit.Runner = (_, args) =>
        {
            if (args is ["rev-parse", "--show-cdup"])
            {
                return (0, "", "");
            }

            if (args is ["rev-parse", "--verify", "--quiet", _])
            {
                return (0, "abc123\n", "");
            }

            if (args is ["diff", _, "--name-only", "-z"])
            {
                return (0, tracked, "");
            }

            if (args is ["ls-files", "--others", "--exclude-standard", "-z"])
            {
                return (0, untracked, "");
            }

            return (1, "", "unexpected git call");
        };

        try
        {
            var config = ConsumerConfig.Load(root);
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            var status = SemanticCommand.Execute(config, since, stdout, stderr);

            return (status, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            SemanticGit.Runner = null;
        }
    }
}
