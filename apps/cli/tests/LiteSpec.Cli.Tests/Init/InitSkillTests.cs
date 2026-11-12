using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Init;

/// <summary>Tests for <c>litespec init --skill</c> and the CLAUDE.md notice.</summary>
public sealed class InitSkillTests
{
    /// <summary>Init with --skill installs the skill at project scope, stamped with the release.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with --skill installs the skill at project scope")]
    public void InitSkillInstallsAtProjectScope()
    {
        using var consumer = TestCli.Temp("init-skill");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill", "agents");

        Assert.Equal(0, result.Status);
        var folder = Join(consumer.Path, ".agents/skills/litespec");
        Assert.Contains($"Wrote {folder}.", result.Stdout, StringComparison.Ordinal);
        var entry = File.ReadAllText(Path.Combine(folder, "SKILL.md"));
        Assert.Contains("generated-by: litespec", entry, StringComparison.Ordinal);
        Assert.Contains("litespec-release:", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_RELEASE_METADATA__", entry, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(folder, "references", "commands.md")));
        Assert.True(File.Exists(Path.Combine(folder, "references", "report.md")));
        Assert.False(Directory.Exists(Join(consumer.Path, ".claude/skills/litespec")));
        Assert.DoesNotContain("skill add", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init with --skill installs each comma separated agent.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with --skill installs each comma separated agent")]
    public void InitSkillInstallsCommaSeparatedAgents()
    {
        using var consumer = TestCli.Temp("init-skills");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill", "claude,opencode");

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Join(consumer.Path, ".claude/skills/litespec/SKILL.md")));
        Assert.True(File.Exists(Join(consumer.Path, ".opencode/skills/litespec/SKILL.md")));
        Assert.False(Directory.Exists(Join(consumer.Path, ".agents/skills/litespec")));
    }

    /// <summary>Init with an unknown skill agent is a usage error and writes no project.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with an unknown skill agent is a usage error")]
    public void InitSkillUnknownAgentIsUsageError()
    {
        using var consumer = TestCli.Temp("init-skill-unknown");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill", "bogus");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown agent 'bogus'; use agents, claude, or opencode.", result.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.yaml")));
    }

    /// <summary>Init with an empty --skill value is a usage error.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with an empty --skill value is a usage error")]
    public void InitSkillEmptyIsUsageError()
    {
        using var consumer = TestCli.Temp("init-skill-empty");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill=");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --skill needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Init with --skill and --skill-global installs under the home folder with no release stamp.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with --skill-global installs under the home folder")]
    public void InitSkillGlobalInstallsUnderTheHomeFolder()
    {
        using var consumer = TestCli.Temp("init-global-consumer");
        using var userHome = TestCli.Temp("init-global-home");

        var result = TestCli.Run(
            ["init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill", "agents", "--skill-global"],
            environment: new Dictionary<string, string> { ["LITESPEC_SKILL_HOME"] = userHome.Path });

        Assert.Equal(0, result.Status);
        var folder = Join(userHome.Path, ".agents/skills/litespec");
        Assert.Contains($"Wrote {folder}.", result.Stdout, StringComparison.Ordinal);
        var entry = File.ReadAllText(Path.Combine(folder, "SKILL.md"));
        Assert.Contains("generated-by: litespec", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("litespec-release", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_RELEASE_METADATA__", entry, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Join(consumer.Path, ".agents/skills/litespec")));
        Assert.True(File.Exists(Path.Combine(consumer.Path, ".litespec", "project.yaml")));
    }

    /// <summary>Init with --skill-global alone installs the default agents globally.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with --skill-global alone installs the default agents")]
    public void InitSkillGlobalWithoutSkillInstallsDefaults()
    {
        using var consumer = TestCli.Temp("init-global-defaults");
        using var userHome = TestCli.Temp("init-global-defaults-home");

        var result = TestCli.Run(
            ["init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill-global"],
            environment: new Dictionary<string, string> { ["LITESPEC_SKILL_HOME"] = userHome.Path });

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Join(userHome.Path, ".agents/skills/litespec/SKILL.md")));
        Assert.True(File.Exists(Join(userHome.Path, ".claude/skills/litespec/SKILL.md")));
        Assert.False(Directory.Exists(Join(userHome.Path, ".config/opencode/skills/litespec")));
    }

    /// <summary>Init without --skill prints the skill add command as its last line.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init without --skill prints the skill add command as its last line")]
    public void InitWithoutSkillPrintsSkillAddAsLastLine()
    {
        using var consumer = TestCli.Temp("init-hint");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Next step: dotnet litespec model check", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("dotnet litespec skill add", LastLine(result.Stdout), StringComparison.Ordinal);
    }

    /// <summary>Init without --skill prints the standalone skill add command for a standalone install.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init without --skill prints the standalone skill add command")]
    public void InitWithoutSkillPrintsStandaloneSkillAdd()
    {
        using var consumer = TestCli.Temp("init-hint-standalone");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--install", "standalone", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Next step: litespec model check", result.Stdout, StringComparison.Ordinal);
        Assert.Equal("To install the LiteSpec skill, run: litespec skill add", LastLine(result.Stdout));
    }

    /// <summary>Init with --skill refuses a folder without its marker and writes nothing elsewhere.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init with --skill refuses a folder without its marker")]
    public void InitSkillRefusesAFolderWithoutItsMarker()
    {
        using var consumer = TestCli.Temp("init-refused");
        var folder = Join(consumer.Path, ".agents/skills/litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "SKILL.md"), "---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path, "--skill", "agents,claude");

        Assert.Equal(1, result.Status);
        Assert.Contains($"'{folder}' holds a skill this tool did not write; left alone.", result.Stderr, StringComparison.Ordinal);
        Assert.Equal("---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n", File.ReadAllText(Path.Combine(folder, "SKILL.md")));
        Assert.False(Directory.Exists(Join(consumer.Path, ".claude/skills/litespec")));
    }

    /// <summary>Init notices a CLAUDE.md that does not import AGENTS.md, before the skill hint.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init notices a CLAUDE.md that does not import AGENTS.md")]
    public void InitNoticesClaudeWithoutImport()
    {
        using var consumer = TestCli.Temp("init-claude");
        var claude = Path.Combine(consumer.Path, "CLAUDE.md");
        File.WriteAllText(claude, "# Claude\n\nOur notes.\n");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"'{claude}' does not import AGENTS.md; add '@AGENTS.md' so Claude Code reads the agent protocol.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("skill add", LastLine(result.Stdout), StringComparison.Ordinal);
    }

    /// <summary>Init stays quiet when CLAUDE.md imports AGENTS.md.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init stays quiet when CLAUDE.md imports AGENTS.md")]
    public void InitStaysQuietWhenClaudeImports()
    {
        using var consumer = TestCli.Temp("init-claude-imports");
        File.WriteAllText(Path.Combine(consumer.Path, "CLAUDE.md"), "# Claude\n\n@AGENTS.md\n");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("does not import AGENTS.md", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Init stays quiet when the root holds no CLAUDE.md.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "init stays quiet when the root holds no CLAUDE.md")]
    public void InitStaysQuietWithoutClaude()
    {
        using var consumer = TestCli.Temp("init-no-claude");

        var result = TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("does not import AGENTS.md", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A consumer path from forward-slash segments.</summary>
    /// <param name="root">The consumer root or home folder.</param>
    /// <param name="relative">The forward-slash path under the root.</param>
    /// <returns>The full path.</returns>
    static string Join(string root, string relative) =>
        Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The last non-empty line of the output.</summary>
    /// <param name="output">The command output text.</param>
    /// <returns>The last line that holds text.</returns>
    static string LastLine(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Last(line => line.Length > 0);
}
