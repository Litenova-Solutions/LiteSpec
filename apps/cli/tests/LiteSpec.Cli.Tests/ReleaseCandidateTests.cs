using System.Text.Json.Nodes;
using LiteSpec.Cli.Init;
using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Tests;

/// <summary>
///     Proves the version a release candidate tool writes into a consumer: the package pins keep
///     the prerelease suffix, and the reviewed-release comparison uses the base version.
/// </summary>
public sealed class ReleaseCandidateTests
{
    /// <summary>The package version keeps the suffix the candidate was packed with.</summary>
    /// <param name="release">The base version the carried release names.</param>
    /// <param name="informational">The informational version the build was stamped with.</param>
    /// <param name="expected">The package version the build names.</param>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Theory(DisplayName = "package version keeps the candidate suffix")]
    [InlineData("6.0.0", "6.0.0-rc.1+3f2a9c", "6.0.0-rc.1")]
    [InlineData("6.0.0", "6.0.0-rc.0", "6.0.0-rc.0")]
    [InlineData("6.0.0", "6.0.0+3f2a9c", "6.0.0")]
    [InlineData("6.0.0", "6.0.0", "6.0.0")]
    [InlineData("6.0.0", "", "6.0.0")]
    [InlineData("9.9.9", "6.0.0-rc.2", "9.9.9-rc.2")]
    public void PackageVersionKeepsTheCandidateSuffix(string release, string informational, string expected) =>
        Assert.Equal(expected, ReleaseVersion.PackageOf(release, informational));

    /// <summary>The base version drops the candidate suffix.</summary>
    /// <param name="version">The version to reduce to its base.</param>
    /// <param name="expected">The base version.</param>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Theory(DisplayName = "base version drops the candidate suffix")]
    [InlineData("6.0.0-rc.1", "6.0.0")]
    [InlineData("6.0.0", "6.0.0")]
    public void BaseVersionDropsTheSuffix(string version, string expected) =>
        Assert.Equal(expected, ReleaseVersion.BaseOf(version));

    /// <summary>A reviewed release compares by base version, so a candidate of the installed release is not drift.</summary>
    /// <param name="reviewed">The version the project records as reviewed.</param>
    /// <param name="installed">The base version of the installed release.</param>
    /// <param name="same">Whether both name the same release.</param>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Theory(DisplayName = "reviewed release compares base versions")]
    [InlineData("6.0.0", "6.0.0", true)]
    [InlineData("6.0.0-rc.1", "6.0.0", true)]
    [InlineData("6.0.0-rc.1", "6.0.1", false)]
    [InlineData("5.2.0", "6.0.0", false)]
    [InlineData("5.2.0-rc.1", "6.0.0", false)]
    public void ReviewedReleaseComparesBaseVersions(string reviewed, string installed, bool same) =>
        Assert.Equal(same, ReleaseVersion.SameRelease(reviewed, installed));

    /// <summary>The tool manifest pins the package version, suffix included, so a restore finds the candidate.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact(DisplayName = "tool manifest pins the candidate package")]
    public void ToolManifestPinsTheCandidatePackage()
    {
        var manifest = JsonNode.Parse(InitCommand.ToolManifest("6.0.0-rc.1"))!;
        Assert.Equal("6.0.0-rc.1", (string?)manifest["tools"]!["LiteSpec.Cli"]!["version"]);
    }

    /// <summary>The agent protocol names the package version and links the release.</summary>
    /// <proves>litespec/core/maintaining#publish-a-litespec-release-from-a-tag</proves>
    [Fact(DisplayName = "agent protocol names the package and links the release")]
    public void AgentProtocolNamesThePackageAndLinksTheRelease()
    {
        var text = AgentProtocol.Fill(
            "follows __LITESPEC_PACKAGE_VERSION__; read https://litespec.dev/__LITESPEC_VERSION__/select/__SELECTION_KEY__/llms.txt",
            version: "6.0.0",
            packageVersion: "6.0.0-rc.1",
            size: "small",
            shape: "server",
            stacks: "dotnet",
            extensions: "none",
            layers: "intent and behavior: litespec",
            selectionKey: "server.dotnet",
            command: "dotnet litespec");

        Assert.Equal("follows 6.0.0-rc.1; read https://litespec.dev/6.0.0/select/server.dotnet/llms.txt", text);
    }
}
