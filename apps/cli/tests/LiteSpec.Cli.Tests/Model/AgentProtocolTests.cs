using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model render</c> writes the agent-protocol region and <c>model check</c> reports a missing or drifted one.</summary>
public sealed class AgentProtocolTests
{
    /// <summary>Render creates the agent context with the region, and the check passes on it.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void RenderCreatesTheAgentContextAndTheCheckPasses()
    {
        using var fixture = ModelFixture.Prepare("small");
        var agents = Path.Combine(fixture.Path, "AGENTS.md");
        Assert.False(File.Exists(agents));

        var rendered = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, rendered.Status);
        Assert.Contains("AGENTS.md", rendered.Stdout, StringComparison.Ordinal);

        var text = File.ReadAllText(agents);
        Assert.Contains("<!-- generated:agent-protocol -->", text, StringComparison.Ordinal);
        Assert.Contains("<!-- /generated:agent-protocol -->", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_VERSION__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__SIZE__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__SHAPE__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__STACKS__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__EXTENSIONS__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__LAYERS__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_DOCS__", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__LITESPEC_COMMAND__", text, StringComparison.Ordinal);

        var checkedResult = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(checkedResult.Status == 0, checkedResult.Output);
        Assert.DoesNotContain("agent-protocol", checkedResult.Output, StringComparison.Ordinal);
    }

    /// <summary>Check blocks on a missing region, and render inserts it after the first heading.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void CheckReportsAMissingRegionAndRenderInsertsIt()
    {
        using var fixture = ModelFixture.Prepare("small");
        var agents = Path.Combine(fixture.Path, "AGENTS.md");
        File.WriteAllText(agents, "# Notes\n\nOur notes.\n");

        var missing = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.Equal(1, missing.Status);
        Assert.Contains("[agent-protocol-missing]", missing.Output, StringComparison.Ordinal);
        Assert.Contains("agent-protocol-missing (level G)", missing.Output, StringComparison.Ordinal);

        var rendered = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, rendered.Status);

        var text = File.ReadAllText(agents);
        Assert.Contains("# Notes\n", text, StringComparison.Ordinal);
        Assert.Contains("Our notes.\n", text, StringComparison.Ordinal);
        Assert.Contains("<!-- generated:agent-protocol -->", text, StringComparison.Ordinal);
        Assert.True(
            text.IndexOf("# Notes\n", StringComparison.Ordinal) < text.IndexOf("<!-- generated:agent-protocol -->", StringComparison.Ordinal)
            && text.IndexOf("<!-- /generated:agent-protocol -->", StringComparison.Ordinal) < text.IndexOf("Our notes.\n", StringComparison.Ordinal));

        var checkedResult = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(checkedResult.Status == 0, checkedResult.Output);
    }

    /// <summary>Check blocks on drift after a hand edit inside the region, and render rewrites it.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void CheckReportsDriftAndRenderRewritesIt()
    {
        using var fixture = ModelFixture.Prepare("small");
        var rendered = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, rendered.Status);

        var agents = Path.Combine(fixture.Path, "AGENTS.md");
        var text = File.ReadAllText(agents);
        File.WriteAllText(
            agents,
            text.Replace("<!-- generated:agent-protocol -->\n", "<!-- generated:agent-protocol -->\ndrift\n", StringComparison.Ordinal));

        var drifted = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.Equal(1, drifted.Status);
        Assert.Contains("[agent-protocol-drift]", drifted.Output, StringComparison.Ordinal);
        Assert.Contains("agent-protocol-drift (level G)", drifted.Output, StringComparison.Ordinal);

        var rewritten = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, rewritten.Status);
        Assert.DoesNotContain("drift\n", File.ReadAllText(agents), StringComparison.Ordinal);

        var checkedResult = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(checkedResult.Status == 0, checkedResult.Output);
    }

    /// <summary>A region rendered by another install of the release, with its own package version and docs folder, does not drift.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void RegionFromAnotherInstallDoesNotDrift()
    {
        using var fixture = ModelFixture.Prepare("small");
        var rendered = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, rendered.Status);

        var agents = Path.Combine(fixture.Path, "AGENTS.md");
        var lines = File.ReadAllText(agents).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith("This project follows LiteSpec ", StringComparison.Ordinal))
            {
                var colon = lines[index].IndexOf(':', StringComparison.Ordinal);
                lines[index] = "This project follows LiteSpec 7.0.0-dev.20990101000000" + lines[index][colon..];
            }

            if (lines[index].StartsWith("Before a task, read the LiteSpec pages of this release at ", StringComparison.Ordinal))
            {
                lines[index] = "Before a task, read the LiteSpec pages of this release at ~/.nuget/packages/litespec.cli.linux-x64/7.0.0-dev.20990101000000/tools/any/linux-x64/docs and open only the pages the task touches.";
            }
        }

        File.WriteAllText(agents, string.Join('\n', lines));

        var result = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("agent-protocol-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Render leaves a current region alone and reports no rewritten file for it.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void RenderLeavesACurrentRegionAlone()
    {
        using var fixture = ModelFixture.Prepare("small");
        var first = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, first.Status);
        Assert.Contains("AGENTS.md", first.Stdout, StringComparison.Ordinal);

        var second = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, second.Status);
        Assert.Contains("0 files rewritten.", second.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("AGENTS.md", second.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when the project holds no agent context at all.</summary>
    /// <proves>litespec/core/agent#keep-the-agent-protocol-current</proves>
    [Fact]
    public void CheckStaysCleanWithoutAnAgentContext()
    {
        using var fixture = ModelFixture.Prepare("small");
        Assert.False(File.Exists(Path.Combine(fixture.Path, "AGENTS.md")));

        var result = TestCli.Run("model", "check", "--root", fixture.Path);
        Assert.True(result.Status == 0, result.Output);
        Assert.DoesNotContain("agent-protocol", result.Output, StringComparison.Ordinal);
    }
}
