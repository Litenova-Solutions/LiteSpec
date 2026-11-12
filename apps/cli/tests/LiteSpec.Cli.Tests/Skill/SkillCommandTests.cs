using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Skill;

/// <summary>Tests for <c>litespec skill</c>.</summary>
public sealed class SkillCommandTests
{
    /// <summary>Add installs the default agents with their own full copies.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add installs the default agents with their own full copies")]
    public void AddInstallsTheDefaultAgents()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run(
            ["skill", "add", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        foreach (var relative in new[] { ".agents/skills/litespec", ".claude/skills/litespec" })
        {
            var folder = SkillFixture.Join(consumer.Path, relative);
            Assert.Contains($"Wrote {folder}.", result.Stdout, StringComparison.Ordinal);
            Assert.Equal(SkillFixture.EntryTemplate.Replace(
                SkillFixture.PlaceholderLine,
                $"  litespec-release: {SkillFixture.FixtureVersion}\n",
                StringComparison.Ordinal), File.ReadAllText(Path.Combine(folder, "SKILL.md")));
            Assert.Equal("# Fixture Commands\n", File.ReadAllText(Path.Combine(folder, "references", "commands.md")));
            Assert.Equal("# Fixture Report\n", File.ReadAllText(Path.Combine(folder, "references", "report.md")));
        }

        Assert.False(Directory.Exists(SkillFixture.Join(consumer.Path, ".opencode/skills/litespec")));
    }

    /// <summary>Add installs only the selected agent.</summary>
    /// <param name="agent">The agent name.</param>
    /// <param name="expected">The expected forward-slash folder under the consumer root.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "add installs only the selected agent")]
    [InlineData("claude", ".claude/skills/litespec")]
    [InlineData("opencode", ".opencode/skills/litespec")]
    [InlineData("agents", ".agents/skills/litespec")]
    public void AddInstallsOnlyTheSelectedAgent(string agent, string expected)
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run(
            ["skill", "add", "--agent", agent, "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        var folder = SkillFixture.Join(consumer.Path, expected);
        Assert.Contains($"Wrote {folder}.", result.Stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(folder, "SKILL.md")));
        Assert.DoesNotContain("Updated ", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Add repeats --agent for several targets, in either option form.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add repeats --agent for several targets, in either option form")]
    public void AddRepeatsTheAgentOption()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run(
            ["skill", "add", "--agent", "claude", "--agent=opencode", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(SkillFixture.Join(consumer.Path, ".claude/skills/litespec/SKILL.md")));
        Assert.True(File.Exists(SkillFixture.Join(consumer.Path, ".opencode/skills/litespec/SKILL.md")));
        Assert.False(Directory.Exists(SkillFixture.Join(consumer.Path, ".agents/skills/litespec")));
    }

    /// <summary>Add replaces a copy it wrote with exactly the carried files.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add replaces a copy it wrote with exactly the carried files")]
    public void AddReplacesACopyItWrote()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var environment = new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path };
        Assert.Equal(0, TestCli.Run(["skill", "add", "--agent", "agents", "--root", consumer.Path], environment: environment).Status);
        var folder = SkillFixture.Join(consumer.Path, ".agents/skills/litespec");
        File.WriteAllText(Path.Combine(folder, "SKILL.md"), File.ReadAllText(Path.Combine(folder, "SKILL.md")) + "tampered\n");
        File.WriteAllText(Path.Combine(folder, "stale.md"), "stale\n");

        var result = TestCli.Run(["skill", "add", "--agent", "agents", "--root", consumer.Path], environment: environment);

        Assert.Equal(0, result.Status);
        Assert.Contains($"Updated {folder}.", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("tampered", File.ReadAllText(Path.Combine(folder, "SKILL.md")), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(folder, "stale.md")));
    }

    /// <summary>Add refuses a folder without its marker and writes nothing elsewhere.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add refuses a folder without its marker and writes nothing elsewhere")]
    public void AddRefusesAFolderWithoutItsMarker()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var folder = SkillFixture.Join(consumer.Path, ".agents/skills/litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "SKILL.md"), "---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n");

        var result = TestCli.Run(
            ["skill", "add", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(1, result.Status);
        Assert.Contains($"'{folder}' holds a skill this tool did not write; left alone.", result.Stderr, StringComparison.Ordinal);
        Assert.Equal("---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n", File.ReadAllText(Path.Combine(folder, "SKILL.md")));
        Assert.False(Directory.Exists(SkillFixture.Join(consumer.Path, ".claude/skills/litespec")));
    }

    /// <summary>Add at project scope needs the project file.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add at project scope needs the project file")]
    public void AddAtProjectScopeNeedsTheProjectFile()
    {
        using var root = TestCli.Temp("skill-noproject");
        Directory.Delete(Path.Combine(root.Path, ".litespec"));

        var result = TestCli.Run("skill", "add", "--root", root.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.yaml", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Add --global writes under the home folder and needs no project file.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "add --global writes under the home folder and needs no project file")]
    public void AddGlobalWritesUnderTheHomeFolder()
    {
        using var home = TestCli.Temp("skill-home");
        using var userHome = TestCli.Temp("skill-user");
        using var bare = TestCli.Temp("skill-bare");
        SkillFixture.FixtureHome(home.Path);
        Directory.Delete(Path.Combine(bare.Path, ".litespec"));

        var result = TestCli.Run(
            ["skill", "add", "--global"],
            bare.Path,
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path, ["LITESPEC_SKILL_HOME"] = userHome.Path });

        Assert.Equal(0, result.Status);
        foreach (var relative in new[] { ".agents/skills/litespec", ".claude/skills/litespec" })
        {
            var folder = SkillFixture.Join(userHome.Path, relative);
            Assert.Contains($"Wrote {folder}.", result.Stdout, StringComparison.Ordinal);
            var entry = File.ReadAllText(Path.Combine(folder, "SKILL.md"));
            Assert.DoesNotContain("litespec-release", entry, StringComparison.Ordinal);
            Assert.DoesNotContain("__LITESPEC_RELEASE_METADATA__", entry, StringComparison.Ordinal);
            Assert.Contains("generated-by: litespec", entry, StringComparison.Ordinal);
        }
    }

    /// <summary>Add --global installs each agent under its home folder.</summary>
    /// <param name="agent">The agent name.</param>
    /// <param name="expected">The expected forward-slash folder under the home folder.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "add --global installs each agent under its home folder")]
    [InlineData("claude", ".claude/skills/litespec")]
    [InlineData("opencode", ".config/opencode/skills/litespec")]
    [InlineData("agents", ".agents/skills/litespec")]
    public void AddGlobalInstallsEachAgent(string agent, string expected)
    {
        using var home = TestCli.Temp("skill-home");
        using var userHome = TestCli.Temp("skill-user");
        SkillFixture.FixtureHome(home.Path);

        var result = TestCli.Run(
            ["skill", "add", "--global", "--agent", agent],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path, ["LITESPEC_SKILL_HOME"] = userHome.Path });

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(SkillFixture.Join(userHome.Path, expected + "/SKILL.md")));
    }

    /// <summary>Remove deletes a copy this tool wrote.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "remove deletes a copy this tool wrote")]
    public void RemoveDeletesACopyItWrote()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var environment = new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path };
        Assert.Equal(0, TestCli.Run(["skill", "add", "--root", consumer.Path], environment: environment).Status);

        var result = TestCli.Run(["skill", "remove", "--root", consumer.Path], environment: environment);

        Assert.Equal(0, result.Status);
        foreach (var relative in new[] { ".agents/skills/litespec", ".claude/skills/litespec" })
        {
            var folder = SkillFixture.Join(consumer.Path, relative);
            Assert.Contains($"Removed {folder}.", result.Stdout, StringComparison.Ordinal);
            Assert.False(Directory.Exists(folder));
        }
    }

    /// <summary>Remove deletes only a folder carrying the marker.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "remove deletes only a folder carrying the marker")]
    public void RemoveDeletesOnlyAMarkedFolder()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);
        var folder = SkillFixture.Join(consumer.Path, ".agents/skills/litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "SKILL.md"), "---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n");

        var result = TestCli.Run(
            ["skill", "remove", "--agent", "agents", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(1, result.Status);
        Assert.Contains($"'{folder}' holds a skill this tool did not write; left alone.", result.Stderr, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(folder, "SKILL.md")));
    }

    /// <summary>Remove with no skill present reports nothing to remove and exits zero.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "remove with no skill present reports nothing to remove and exits zero")]
    public void RemoveWithNoSkillPresentExitsZero()
    {
        using var home = TestCli.Temp("skill-home");
        using var consumer = TestCli.Temp("skill-consumer");
        SkillFixture.FixtureHome(home.Path);
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run(
            ["skill", "remove", "--agent", "opencode", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Equal(0, result.Status);
        Assert.Contains("nothing to remove", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Remove --global deletes the copy under the home folder.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "remove --global deletes the copy under the home folder")]
    public void RemoveGlobalDeletesUnderTheHomeFolder()
    {
        using var home = TestCli.Temp("skill-home");
        using var userHome = TestCli.Temp("skill-user");
        SkillFixture.FixtureHome(home.Path);
        var environment = new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path, ["LITESPEC_SKILL_HOME"] = userHome.Path };
        Assert.Equal(0, TestCli.Run(["skill", "add", "--global", "--agent", "opencode"], environment: environment).Status);
        var folder = SkillFixture.Join(userHome.Path, ".config/opencode/skills/litespec");

        var result = TestCli.Run(["skill", "remove", "--global", "--agent", "opencode"], environment: environment);

        Assert.Equal(0, result.Status);
        Assert.Contains($"Removed {folder}.", result.Stdout, StringComparison.Ordinal);
        Assert.False(Directory.Exists(folder));
    }

    /// <summary>An unknown agent is a usage error.</summary>
    /// <param name="subcommand">The subcommand: add or remove.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "an unknown agent is a usage error")]
    [InlineData("add")]
    [InlineData("remove")]
    public void UnknownAgentIsAUsageError(string subcommand)
    {
        using var consumer = TestCli.Temp("skill-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run("skill", subcommand, "--agent", "bogus", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown agent 'bogus'; use agents, claude, or opencode.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>An empty agent value is a usage error.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "an empty agent value is a usage error")]
    public void EmptyAgentIsAUsageError()
    {
        using var consumer = TestCli.Temp("skill-consumer");
        Assert.Equal(0, TestCli.Run("init", "--preset", "records", "--size", "small", "--root", consumer.Path).Status);

        var result = TestCli.Run("skill", "add", "--agent=", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --agent needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>--global with --root is a usage error.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "--global with --root is a usage error")]
    public void GlobalWithRootIsAUsageError()
    {
        using var consumer = TestCli.Temp("skill-consumer");

        var result = TestCli.Run("skill", "add", "--global", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("--global and --root cannot be combined.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>An unknown subcommand is a usage error.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "an unknown subcommand is a usage error")]
    public void UnknownSubcommandIsAUsageError()
    {
        var result = TestCli.Run("skill", "update");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown command 'update'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Skill with no subcommand prints the usage text and exits 2.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "skill with no subcommand prints the usage text and exits 2")]
    public void SkillWithNoSubcommandPrintsUsage()
    {
        var result = TestCli.Run("skill");

        Assert.Equal(2, result.Status);
        Assert.Contains("Usage: litespec skill add", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Skill --help prints the usage text and exits zero.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "skill --help prints the usage text and exits zero")]
    public void SkillHelpPrintsUsage()
    {
        var result = TestCli.Run("skill", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec skill add", result.Stdout, StringComparison.Ordinal);
    }
}
