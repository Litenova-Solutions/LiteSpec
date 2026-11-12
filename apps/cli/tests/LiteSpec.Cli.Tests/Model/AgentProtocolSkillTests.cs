using System.Text.Json.Nodes;
using LiteSpec.Cli.Skill;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>model render</c> rewrites marked skill copies, <c>model check</c> reports drift and an unread CLAUDE.md, and the carried skill frontmatter parses as YAML.</summary>
public sealed class AgentProtocolSkillTests
{
    /// <summary>Check reports skill-drift after a hand edit, and render rewrites the copy.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check reports skill-drift and render rewrites the copy")]
    public void CheckReportsSkillDriftAndRenderRewritesIt()
    {
        using var fixture = ModelFixture.Prepare("small");
        Assert.Equal(0, TestCli.Run("skill", "add", "--agent", "agents", "--root", fixture.Path).Status);
        var entry = Join(fixture.Path, ".agents/skills/litespec/SKILL.md");
        File.WriteAllText(entry, File.ReadAllText(entry) + "drift\n");

        var drifted = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, drifted.Status);
        Assert.Contains("[skill-drift]", drifted.Output, StringComparison.Ordinal);

        var rewritten = TestCli.Run("model", "render", "--root", fixture.Path);

        Assert.Equal(0, rewritten.Status);
        Assert.Contains(".agents/skills/litespec/SKILL.md", rewritten.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("drift\n", File.ReadAllText(entry), StringComparison.Ordinal);

        var checkedResult = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(checkedResult.Status == 0, checkedResult.Output);
        Assert.DoesNotContain("[skill-drift]", checkedResult.Output, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when the project holds no skill copy.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check stays clean without a skill copy")]
    public void CheckStaysCleanWithoutASkillCopy()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("skill-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check ignores a skill copy without the marker, and render leaves it alone.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check ignores a skill copy without the marker")]
    public void CheckIgnoresAForeignSkillCopy()
    {
        using var fixture = ModelFixture.Prepare("small");
        Assert.Equal(0, TestCli.Run("model", "render", "--root", fixture.Path).Status);
        var folder = Join(fixture.Path, ".agents/skills/litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "SKILL.md"), "---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("skill-drift", result.Output, StringComparison.Ordinal);

        var rendered = TestCli.Run("model", "render", "--root", fixture.Path);

        Assert.Equal(0, rendered.Status);
        Assert.Equal("---\nname: litespec\ndescription: Mine.\n---\n\n# Mine\n", File.ReadAllText(Path.Combine(folder, "SKILL.md")));
        Assert.DoesNotContain("skills/litespec", rendered.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Check ignores a drifted global copy outside the project.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check ignores a drifted global copy")]
    public void CheckIgnoresGlobalCopies()
    {
        using var fixture = ModelFixture.Prepare("small");
        using var userHome = TestCli.Temp("skill-global-check");
        var environment = new Dictionary<string, string> { ["LITESPEC_SKILL_HOME"] = userHome.Path };
        Assert.Equal(0, TestCli.Run(["skill", "add", "--global", "--agent", "agents"], environment: environment).Status);
        var entry = Join(userHome.Path, ".agents/skills/litespec/SKILL.md");
        File.WriteAllText(entry, File.ReadAllText(entry) + "drift\n");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("skill-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Render leaves a current skill copy alone and reports no rewritten file for it.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "render leaves a current skill copy alone")]
    public void RenderLeavesACurrentSkillCopyAlone()
    {
        using var fixture = ModelFixture.Prepare("small");
        Assert.Equal(0, TestCli.Run("skill", "add", "--agent", "agents", "--root", fixture.Path).Status);
        Assert.Equal(0, TestCli.Run("model", "render", "--root", fixture.Path).Status);

        var second = TestCli.Run("model", "render", "--root", fixture.Path);

        Assert.Equal(0, second.Status);
        Assert.Contains("0 files rewritten.", second.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("skills/litespec", second.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Check reports an extra file in a marked copy as drift, and render removes it.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check reports an extra file as skill-drift")]
    public void CheckReportsAnExtraFileAsDrift()
    {
        using var fixture = ModelFixture.Prepare("small");
        Assert.Equal(0, TestCli.Run("skill", "add", "--agent", "claude", "--root", fixture.Path).Status);
        var folder = Join(fixture.Path, ".claude/skills/litespec");
        File.WriteAllText(Path.Combine(folder, "stale.md"), "stale\n");

        var drifted = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, drifted.Status);
        Assert.Contains("[skill-drift]", drifted.Output, StringComparison.Ordinal);

        Assert.Equal(0, TestCli.Run("model", "render", "--root", fixture.Path).Status);
        Assert.False(File.Exists(Path.Combine(folder, "stale.md")));
    }

    /// <summary>Check reports agent-protocol-unreached as advisory when CLAUDE.md does not import AGENTS.md.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check reports agent-protocol-unreached as advisory")]
    public void CheckReportsUnreadAsAdvisory()
    {
        using var fixture = ModelFixture.Prepare("small");
        File.WriteAllText(Path.Combine(fixture.Path, "CLAUDE.md"), "# Claude\n\nOur notes.\n");

        var listed = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Equal(0, listed.Status);
        Assert.Contains("[agent-protocol-unreached]", listed.Output, StringComparison.Ordinal);

        var summarized = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(0, summarized.Status);
        Assert.DoesNotContain("[agent-protocol-unreached]", summarized.Output, StringComparison.Ordinal);
        Assert.Contains("agent-protocol-unreached (level A)", summarized.Output, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when CLAUDE.md imports AGENTS.md.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check stays clean when CLAUDE.md imports AGENTS.md")]
    public void CheckStaysCleanWhenClaudeImports()
    {
        using var fixture = ModelFixture.Prepare("small");
        File.WriteAllText(Path.Combine(fixture.Path, "CLAUDE.md"), "# Claude\n\n@AGENTS.md\n");

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("agent-protocol-unreached", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when the project holds no CLAUDE.md.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "check stays clean without a CLAUDE.md")]
    public void CheckStaysCleanWithoutClaude()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("agent-protocol-unreached", result.Output, StringComparison.Ordinal);
    }

    /// <summary>The carried skill renders at both scopes and each entry frontmatter parses as YAML.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact(DisplayName = "the carried skill renders at both scopes and each frontmatter parses as YAML")]
    public void CarriedSkillFrontmatterParsesAsYaml()
    {
        var templates = Path.Combine(TestCli.Repository, "templates");
        var project = SkillFiles.Render(templates, "9.9.9");
        var global = SkillFiles.Render(templates, release: null);

        Assert.Equal(new[] { "SKILL.md", "references/commands.md", "references/report.md" }, project.Select(file => file.RelativePath).ToArray());
        Assert.Equal(new[] { "SKILL.md", "references/commands.md", "references/report.md" }, global.Select(file => file.RelativePath).ToArray());

        var projectEntry = project[0].Text;
        var globalEntry = global[0].Text;
        Assert.DoesNotContain("__LITESPEC_RELEASE_METADATA__", projectEntry, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_RELEASE_METADATA__", globalEntry, StringComparison.Ordinal);

        var projectFront = Assert.IsType<JsonObject>(RecordText.Parse(Frontmatter(projectEntry), yaml: true));
        var globalFront = Assert.IsType<JsonObject>(RecordText.Parse(Frontmatter(globalEntry), yaml: true));

        foreach (var front in new[] { projectFront, globalFront })
        {
            Assert.Equal("litespec", Assert.IsAssignableFrom<JsonValue>(front["name"]).GetValue<string>());
            var description = Assert.IsAssignableFrom<JsonValue>(front["description"]).GetValue<string>();
            Assert.True(description.Length is > 0 and < 1024, $"The description runs {description.Length} characters.");
            Assert.Contains(".litespec/project.yaml", description, StringComparison.Ordinal);
            Assert.Contains("code", description, StringComparison.Ordinal);
            Assert.Contains("records", description, StringComparison.Ordinal);
            Assert.Contains("provision", description, StringComparison.Ordinal);
            Assert.Contains("completion report", description, StringComparison.Ordinal);
            var metadata = Assert.IsType<JsonObject>(front["metadata"]);
            Assert.Equal("litespec", Assert.IsAssignableFrom<JsonValue>(metadata["generated-by"]).GetValue<string>());
        }

        var projectMetadata = Assert.IsType<JsonObject>(projectFront["metadata"]);
        var globalMetadata = Assert.IsType<JsonObject>(globalFront["metadata"]);
        Assert.Equal("9.9.9", Assert.IsAssignableFrom<JsonValue>(projectMetadata["litespec-release"]).GetValue<string>());
        Assert.Null(globalMetadata["litespec-release"]);
        Assert.True(BodyLines(projectEntry) < 200, "The project body stays under 200 lines.");
        Assert.True(BodyLines(globalEntry) < 200, "The global body stays under 200 lines.");
    }

    /// <summary>A consumer path from forward-slash segments.</summary>
    /// <param name="root">The consumer root or home folder.</param>
    /// <param name="relative">The forward-slash path under the root.</param>
    /// <returns>The full path.</returns>
    static string Join(string root, string relative) =>
        Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The YAML frontmatter between the leading fences of the entry text.</summary>
    /// <param name="text">The entry file text.</param>
    /// <returns>The frontmatter without its fences.</returns>
    static string Frontmatter(string text)
    {
        var lines = text.Split('\n');
        Assert.True(lines.Length >= 2 && lines[0].Trim() == "---", "The entry opens with a frontmatter fence.");

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "---")
            {
                return string.Join('\n', lines[1..index]);
            }
        }

        throw new InvalidOperationException("The entry holds no closing frontmatter fence.");
    }

    /// <summary>The Markdown body line count after the closing frontmatter fence.</summary>
    /// <param name="text">The entry file text.</param>
    /// <returns>The body line count.</returns>
    static int BodyLines(string text)
    {
        var lines = text.Split('\n');
        var fences = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "---")
            {
                fences += 1;

                if (fences == 2)
                {
                    return lines.Length - index - 1;
                }
            }
        }

        throw new InvalidOperationException("The entry holds no closing frontmatter fence.");
    }
}
