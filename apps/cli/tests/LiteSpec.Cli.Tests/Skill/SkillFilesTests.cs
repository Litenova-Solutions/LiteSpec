using LiteSpec.Cli.Skill;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Skill;

/// <summary>Tests for the skill file generator.</summary>
public sealed class SkillFilesTests
{
    /// <summary>Render returns every file of the copy with its forward-slash path and text.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render returns every file of the copy with its forward-slash path and text")]
    public void RenderListsEveryFileWithItsText()
    {
        using var home = TestCli.Temp("skill-render");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));

        var files = SkillFiles.Render(Path.Combine(home.Path, "templates"), "9.9.9");

        Assert.Equal(3, files.Count);
        Assert.Equal("SKILL.md", files[0].RelativePath);
        Assert.Equal("references/commands.md", files[1].RelativePath);
        Assert.Equal("references/report.md", files[2].RelativePath);
        Assert.Equal("# Fixture Commands\n", files[1].Text);
        Assert.Equal("# Fixture Report\n", files[2].Text);
    }

    /// <summary>Render stamps the project copy with the carried release.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render stamps the project copy with the carried release")]
    public void RenderStampsTheProjectCopyWithTheRelease()
    {
        using var home = TestCli.Temp("skill-stamp");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));

        var files = SkillFiles.Render(Path.Combine(home.Path, "templates"), "9.9.9");

        Assert.Equal(
            """
            ---
            name: litespec
            description: Fixture skill for the skill tests.
            metadata:
              generated-by: litespec
              litespec-release: 9.9.9
            ---

            # Fixture Skill
            """ + "\n",
            files[0].Text);
    }

    /// <summary>Render removes the placeholder line for a global copy, which names no release.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render removes the placeholder line for a global copy, which names no release")]
    public void RenderRemovesThePlaceholderLineAtGlobalScope()
    {
        using var home = TestCli.Temp("skill-global");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));

        var files = SkillFiles.Render(Path.Combine(home.Path, "templates"), release: null);

        Assert.Equal(
            """
            ---
            name: litespec
            description: Fixture skill for the skill tests.
            metadata:
              generated-by: litespec
            ---

            # Fixture Skill
            """ + "\n",
            files[0].Text);
    }

    /// <summary>Render refuses a release that holds no readable skill template.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render refuses a release that holds no readable skill template")]
    public void RenderRefusesAMissingTemplate()
    {
        using var home = TestCli.Temp("skill-missing");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));
        File.Delete(SkillFixture.Join(home.Path, "templates/skill/litespec/references/report.md"));

        var exception = Assert.Throws<IOException>(() => SkillFiles.Render(Path.Combine(home.Path, "templates"), "9.9.9"));

        Assert.Equal("The LiteSpec release holds no readable templates/skill/litespec/references/report.md.", exception.Message);
    }

    /// <summary>Render refuses an entry template with no placeholder line.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render refuses an entry template with no placeholder line")]
    public void RenderRefusesAnEntryWithoutThePlaceholder()
    {
        using var home = TestCli.Temp("skill-placeholder");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));
        File.WriteAllText(
            SkillFixture.Join(home.Path, "templates/skill/litespec/SKILL.md"),
            "---\nname: litespec\nmetadata:\n  generated-by: litespec\n---\n\n# Fixture Skill\n");

        var exception = Assert.Throws<IOException>(() => SkillFiles.Render(Path.Combine(home.Path, "templates"), "9.9.9"));

        Assert.Equal("The LiteSpec release holds no __LITESPEC_RELEASE_METADATA__ line in templates/skill/litespec/SKILL.md.", exception.Message);
    }

    /// <summary>Each agent maps to its skill folder at project scope.</summary>
    /// <param name="agent">The agent name.</param>
    /// <param name="expected">The expected forward-slash folder under the consumer root.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "each agent maps to its skill folder at project scope")]
    [InlineData("claude", ".claude/skills/litespec")]
    [InlineData("opencode", ".opencode/skills/litespec")]
    [InlineData("agents", ".agents/skills/litespec")]
    public void ProjectFolderMapsEachAgent(string agent, string expected)
    {
        using var consumer = TestCli.Temp("skill-project");

        Assert.Equal(SkillFixture.Join(consumer.Path, expected), SkillFiles.ProjectFolder(consumer.Path, agent));
    }

    /// <summary>Each agent maps to its skill folder at global scope.</summary>
    /// <param name="agent">The agent name.</param>
    /// <param name="expected">The expected forward-slash folder under the home folder.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "each agent maps to its skill folder at global scope")]
    [InlineData("claude", ".claude/skills/litespec")]
    [InlineData("opencode", ".config/opencode/skills/litespec")]
    [InlineData("agents", ".agents/skills/litespec")]
    public void GlobalFolderMapsEachAgent(string agent, string expected)
    {
        using var userHome = TestCli.Temp("skill-home");

        Assert.Equal(SkillFixture.Join(userHome.Path, expected), SkillFiles.GlobalFolder(userHome.Path, agent));
    }

    /// <summary>A rendered copy carries the marker.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "a rendered copy carries the marker")]
    public void RenderedCopyCarriesTheMarker()
    {
        using var home = TestCli.Temp("skill-marker");
        using var consumer = TestCli.Temp("skill-marked");
        SkillFixture.FixtureTemplates(Path.Combine(home.Path, "templates"));
        var project = SkillFiles.Render(Path.Combine(home.Path, "templates"), "9.9.9");
        var global = SkillFiles.Render(Path.Combine(home.Path, "templates"), release: null);
        var skill = Path.Combine(consumer.Path, "skill");
        Directory.CreateDirectory(skill);

        File.WriteAllText(Path.Combine(skill, "SKILL.md"), project[0].Text);
        Assert.True(SkillFiles.CarriesMarker(skill));

        File.WriteAllText(Path.Combine(skill, "SKILL.md"), global[0].Text);
        Assert.True(SkillFiles.CarriesMarker(skill));
    }

    /// <summary>A skill this tool did not write carries no marker.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "a skill this tool did not write carries no marker")]
    public void ForeignSkillCarriesNoMarker()
    {
        using var consumer = TestCli.Temp("skill-foreign");
        var skill = Path.Combine(consumer.Path, "skill");
        Directory.CreateDirectory(skill);
        File.WriteAllText(Path.Combine(skill, "SKILL.md"), "---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n");

        Assert.False(SkillFiles.CarriesMarker(skill));
    }

    /// <summary>A folder with no entry file carries no marker.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "a folder with no entry file carries no marker")]
    public void MissingEntryCarriesNoMarker()
    {
        using var consumer = TestCli.Temp("skill-noentry");
        var skill = Path.Combine(consumer.Path, "skill");
        Directory.CreateDirectory(skill);

        Assert.False(SkillFiles.CarriesMarker(skill));
        Assert.False(SkillFiles.CarriesMarker(Path.Combine(consumer.Path, "absent")));
    }

    /// <summary>An entry file with no readable frontmatter carries no marker.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "an entry file with no readable frontmatter carries no marker")]
    public void UnreadableFrontmatterCarriesNoMarker()
    {
        using var consumer = TestCli.Temp("skill-frontmatter");
        var skill = Path.Combine(consumer.Path, "skill");
        Directory.CreateDirectory(skill);

        File.WriteAllText(Path.Combine(skill, "SKILL.md"), "# No Frontmatter\n");
        Assert.False(SkillFiles.CarriesMarker(skill));

        File.WriteAllText(Path.Combine(skill, "SKILL.md"), "---\nname: litespec\n");
        Assert.False(SkillFiles.CarriesMarker(skill));

        File.WriteAllText(Path.Combine(skill, "SKILL.md"), "---\nmetadata: [unclosed\n---\n\n# Broken\n");
        Assert.False(SkillFiles.CarriesMarker(skill));
    }

    /// <summary>The home folder falls back to the user profile when the override is unset.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "the home folder falls back to the user profile when the override is unset")]
    public void HomeFolderFallsBackToTheUserProfile()
    {
        if (Environment.GetEnvironmentVariable(SkillFiles.HomeVariable) is { Length: > 0 })
        {
            return;
        }

        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), SkillFiles.HomeFolder());
    }

    /// <summary>Only the three agent names select an agent.</summary>
    /// <param name="name">The candidate agent name.</param>
    /// <param name="expected">Whether the name selects an agent.</param>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Theory(DisplayName = "only the three agent names select an agent")]
    [InlineData("claude", true)]
    [InlineData("opencode", true)]
    [InlineData("agents", true)]
    [InlineData("codex", false)]
    [InlineData("Claude", false)]
    [InlineData("", false)]
    public void OnlyAgentNamesSelectAnAgent(string name, bool expected)
    {
        Assert.Equal(expected, SkillFiles.IsAgent(name));
    }
}
