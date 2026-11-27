using LiteSpec.Cli.Agent;
using LiteSpec.Cli.Tests.Model;

namespace LiteSpec.Cli.Tests.Agent;

/// <summary>Proves <c>litespec agent sync</c> writes the thin agent context and the path-scoped rule files.</summary>
public sealed class AgentSyncTests
{
    /// <summary>The console gate: command runs write to the process console, so only one runs at once.</summary>
    static readonly object ConsoleGate = new();

    /// <summary>Sync writes the rule files, the agent context under 100 lines and the hooks, with no rule for an unselected stack.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SyncWritesRuleFilesAgentsFileAndHooks()
    {
        using var temp = ModelFixture.Prepare("small");

        var (status, _, _) = RunSync("sync", "--root", temp.Path);
        Assert.Equal(0, status);

        Assert.True(File.Exists(Path.Combine(temp.Path, "AGENTS.md")), "AGENTS.md");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".claude", "rules", "dotnet.md")), "claude dotnet rules");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".cursor", "rules", "dotnet.mdc")), "cursor dotnet rules");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".github", "instructions", "dotnet.instructions.md")), "github dotnet instructions");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".claude", "rules", "spec-writing.md")), "spec writing rules");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".claude", "rules", "ui-routes.md")), "route rules");
        Assert.True(File.Exists(Path.Combine(temp.Path, ".claude", "settings.json")), "claude settings");

        var agents = File.ReadAllText(Path.Combine(temp.Path, "AGENTS.md"));
        Assert.True(agents.Split('\n').Length < 100, "AGENTS.md stays under 100 lines");
        Assert.Contains("<!-- generated:agent-sync -->", agents, StringComparison.Ordinal);
        Assert.Contains(".claude/rules/", agents, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(temp.Path, ".claude", "rules", "svelte.md")), "no svelte rules");
        Assert.False(File.Exists(Path.Combine(temp.Path, ".claude", "rules", "android.md")), "no android rules");

        foreach (var file in RuleFiles(temp.Path))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("litespec/packs/android#", text, StringComparison.Ordinal);
            Assert.DoesNotContain("litespec/packs/svelte#", text, StringComparison.Ordinal);
        }

        var dotnet = File.ReadAllText(Path.Combine(temp.Path, ".claude", "rules", "dotnet.md"));
        Assert.Contains("paths:", dotnet, StringComparison.Ordinal);
        Assert.Contains("litespec/", dotnet, StringComparison.Ordinal);

        foreach (var line in dotnet.Split('\n').Where(line => line.StartsWith("- ", StringComparison.Ordinal)))
        {
            Assert.Contains("litespec/", line, StringComparison.Ordinal);
        }

        var settings = File.ReadAllText(Path.Combine(temp.Path, ".claude", "settings.json"));
        Assert.Contains("litespec hook post-edit", settings, StringComparison.Ordinal);
        Assert.Contains("litespec hook pre-stop", settings, StringComparison.Ordinal);

        var (check, _, _) = RunSync("sync", "--root", temp.Path, "--check");
        Assert.Equal(0, check);
    }

    /// <summary>Sync --check exits 1 listing the drifted files, and a resync clears it.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SyncCheckReportsDriftAndResyncClearsIt()
    {
        using var temp = ModelFixture.Prepare("small");

        var (drifted, driftedOut, _) = RunSync("sync", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted);
        Assert.Contains("AGENTS.md", driftedOut, StringComparison.Ordinal);
        Assert.Contains("files would change.", driftedOut, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(temp.Path, "AGENTS.md")));

        Assert.Equal(0, RunSync("sync", "--root", temp.Path).Status);
        Assert.True(File.Exists(Path.Combine(temp.Path, "AGENTS.md")));

        Assert.Equal(0, RunSync("sync", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Sync keeps the text outside the marked region and inserts the region after the heading.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SyncKeepsTextOutsideTheMarkedRegion()
    {
        using var temp = ModelFixture.Prepare("small");
        File.WriteAllText(Path.Combine(temp.Path, "AGENTS.md"), "# Notes\n\nOur notes.\n");

        Assert.Equal(0, RunSync("sync", "--root", temp.Path).Status);

        var text = File.ReadAllText(Path.Combine(temp.Path, "AGENTS.md"));
        Assert.Contains("# Notes\n", text, StringComparison.Ordinal);
        Assert.Contains("Our notes.\n", text, StringComparison.Ordinal);
        Assert.Contains("<!-- generated:agent-sync -->", text, StringComparison.Ordinal);
    }

    /// <summary>Sync merges the hooks into existing settings without dropping their entries.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SyncMergesHooksIntoExistingSettings()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, ".claude");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "settings.json"),
            """{"hooks": {"PostToolUse": [{"matcher": "Edit|Write", "hooks": [{"type": "command", "command": "echo kept"}]}]}}""");

        Assert.Equal(0, RunSync("sync", "--root", temp.Path).Status);

        var settings = File.ReadAllText(Path.Combine(folder, "settings.json"));
        Assert.Contains("echo kept", settings, StringComparison.Ordinal);
        Assert.Contains("litespec hook post-edit", settings, StringComparison.Ordinal);
        Assert.Contains("litespec hook pre-stop", settings, StringComparison.Ordinal);

        Assert.Equal(0, RunSync("sync", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Sync --help prints the usage text, and an unknown subcommand or option is a usage error.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SyncHelpAndUsageErrors()
    {
        using var temp = ModelFixture.Prepare("small");

        var help = RunSync("sync", "--help");
        Assert.Equal(0, help.Status);
        Assert.Contains("litespec agent sync", help.Stdout, StringComparison.Ordinal);

        Assert.Equal(2, RunSync("bogus", "--root", temp.Path).Status);
        Assert.Equal(2, RunSync("sync", "--root", temp.Path, "--bogus").Status);
    }

    /// <summary>Every rule file the sync wrote: the area, cursor, instruction and shared rule files.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <returns>The rule file paths.</returns>
    static IReadOnlyList<string> RuleFiles(string root) =>
        Directory.GetFiles(Path.Combine(root, ".claude"), "*.md", SearchOption.AllDirectories)
            .Concat(Directory.Exists(Path.Combine(root, ".cursor")) ? Directory.GetFiles(Path.Combine(root, ".cursor"), "*.mdc", SearchOption.AllDirectories) : [])
            .Concat(Directory.Exists(Path.Combine(root, ".github")) ? Directory.GetFiles(Path.Combine(root, ".github"), "*.md", SearchOption.AllDirectories) : [])
            .ToList();

    /// <summary>Runs agent sync with the process console redirected, so the case reads the exit code and the output together.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    static (int Status, string Stdout, string Stderr) RunSync(params string[] args)
    {
        lock (ConsoleGate)
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var previousOut = Console.Out;
            var previousError = Console.Error;
            Console.SetOut(stdout);
            Console.SetError(stderr);

            try
            {
                var status = AgentSyncCommand.Run(args);
                stdout.Flush();
                stderr.Flush();
                return (status, stdout.ToString(), stderr.ToString());
            }
            finally
            {
                Console.SetOut(previousOut);
                Console.SetError(previousError);
            }
        }
    }
}
