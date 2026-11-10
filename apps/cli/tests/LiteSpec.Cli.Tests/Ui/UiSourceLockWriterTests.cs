using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Ui;

/// <summary>Proves <c>litespec model render</c> writes the UI source lock beside the shared package.</summary>
public sealed class UiSourceLockWriterTests
{
    /// <summary>Render rewrites only the digest of a primitive whose source changed.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderRewritesOnlyTheStaleDigest()
    {
        using var fixture = UiFixture.Create();
        Assert.Equal(0, Render(fixture).Status);
        var before = File.ReadAllText(fixture.LockFile);
        var oldDigest = DigestNamed(fixture.LockFile, "button");
        var skeletonDigest = DigestNamed(fixture.LockFile, "skeleton");

        File.AppendAllText(fixture.ButtonFile, "export const extra = 1;\n");
        UiFixture.AssertProblem(fixture.Run(), "no longer matches the recorded digest");

        var result = Render(fixture);
        Assert.Equal(0, result.Status);
        Assert.Contains("ui-source-lock.yaml", result.Stdout);

        var newDigest = DigestOf(File.ReadAllText(fixture.ButtonFile));
        Assert.NotEqual(oldDigest, newDigest);
        Assert.Equal(newDigest, DigestNamed(fixture.LockFile, "button"));
        Assert.Equal(skeletonDigest, DigestNamed(fixture.LockFile, "skeleton"));
        Assert.Equal(before.Replace(oldDigest, newDigest, StringComparison.Ordinal), File.ReadAllText(fixture.LockFile));
        Assert.DoesNotContain(UiFixture.Problems(fixture.Run()), problem => problem.Contains("ui-sourcelock-invalid", StringComparison.Ordinal));
    }

    /// <summary>A component copied as two files records one digest over both, so a change to its second file is reported and render records it.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void AComponentDigestCoversEveryPath()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(lockObject =>
        {
            var button = ((JsonArray)lockObject["components"]!).OfType<JsonObject>().Single(item => (string?)item["name"] == "button");
            ((JsonArray)button["paths"]!).Add("src/components/skeleton.tsx");
        });
        Assert.Equal(0, Render(fixture).Status);
        Assert.DoesNotContain(UiFixture.Problems(fixture.Run()), problem => problem.Contains("ui-sourcelock-invalid", StringComparison.Ordinal));
        var recorded = DigestNamed(fixture.LockFile, "button");

        File.AppendAllText(Path.Join(fixture.PackageRoot, "src/components/skeleton.tsx"), "export const extra = 1;\n");
        UiFixture.AssertProblem(fixture.Run(), "no longer matches the recorded digest");

        Assert.Equal(0, Render(fixture).Status);
        Assert.NotEqual(recorded, DigestNamed(fixture.LockFile, "button"));
        Assert.DoesNotContain(UiFixture.Problems(fixture.Run()), problem => problem.Contains("ui-sourcelock-invalid", StringComparison.Ordinal));
    }

    /// <summary>Render leaves a lock that is already current byte for byte as it was.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderLeavesACurrentLockUntouched()
    {
        using var fixture = UiFixture.Create();
        Assert.Equal(0, Render(fixture).Status);
        var before = File.ReadAllText(fixture.LockFile);

        var result = Render(fixture);

        Assert.Equal(0, result.Status);
        Assert.Equal(before, File.ReadAllText(fixture.LockFile));
        Assert.DoesNotContain("ui-source-lock.yaml", result.Stdout);
    }

    /// <summary>Render writes no lock for a project that declares no shared package.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderWritesNoLockWithoutASharedPackage()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateProject(project =>
        {
            project.Remove("ui");
        });
        File.AppendAllText(fixture.ButtonFile, "export const extra = 1;\n");
        var before = File.ReadAllText(fixture.LockFile);

        var result = Render(fixture);

        Assert.Equal(0, result.Status);
        Assert.Equal(before, File.ReadAllText(fixture.LockFile));
        Assert.DoesNotContain("ui-source-lock.yaml", result.Stdout);
    }

    /// <summary>Render does not create a lock the team has not started, so a missing lock stays missing.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderDoesNotCreateAMissingLock()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.LockFile);

        var result = Render(fixture);

        Assert.Equal(0, result.Status);
        Assert.False(File.Exists(fixture.LockFile));
    }

    /// <summary>Render restores the preset and the CLI pin from the manifest baseline, so a drifted lock passes the check.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderRestoresThePresetAndCliFromTheManifest()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(sourceLock =>
        {
            sourceLock["cli"] = "0.0.1";
            ((JsonObject)sourceLock["preset"]!)["radius"] = "large";
        });
        UiFixture.AssertProblem(fixture.Run(), "preset.radius does not match the manifest baseline");

        Assert.Equal(0, Render(fixture).Status);

        var written = (JsonObject)UiFixture.ReadJson(fixture.LockFile)!;
        Assert.Equal("4.21.5", written["cli"]!.GetValue<string>());
        Assert.Equal("default", written["preset"]!["radius"]!.GetValue<string>());
        Assert.DoesNotContain(UiFixture.Problems(fixture.Run()), problem => problem.Contains("ui-sourcelock-invalid", StringComparison.Ordinal));
    }

    /// <summary>Render takes the Svelte baseline and the shadcn-svelte CLI pin for a Svelte lock.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void RenderWritesTheSveltePinForASvelteLock()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        fixture.MutateLock(sourceLock => sourceLock["cli"] = "0.0.1");

        Assert.Equal(0, Render(fixture).Status);

        var written = (JsonObject)UiFixture.ReadJson(fixture.LockFile)!;
        Assert.Equal("shadcn-svelte", written["generator"]!.GetValue<string>());
        Assert.Equal("1.7.0", written["cli"]!.GetValue<string>());
        Assert.Equal("bits-ui", written["preset"]!["base"]!.GetValue<string>());
        Assert.DoesNotContain(UiFixture.Problems(fixture.Run()), problem => problem.Contains("ui-sourcelock-invalid", StringComparison.Ordinal));
    }

    /// <summary>Renders the fixture with its own LiteSpec release as the home.</summary>
    /// <param name="fixture">The fixture consumer.</param>
    /// <returns>The exit code and the output streams of the run.</returns>
    static CliResult Render(UiFixture fixture) => TestCli.Run(
        ["model", "render", "--root", fixture.Root],
        null,
        new Dictionary<string, string> { ["LITESPEC_HOME"] = fixture.HomeDir });

    /// <summary>The digest the lock records for one named component.</summary>
    /// <param name="lockFile">The source lock file.</param>
    /// <param name="name">The component name.</param>
    /// <returns>The recorded digest.</returns>
    static string DigestNamed(string lockFile, string name)
    {
        var written = (JsonObject)UiFixture.ReadJson(lockFile)!;
        foreach (var component in (JsonArray)written["components"]!)
        {
            if (component!["name"]!.GetValue<string>() == name)
            {
                return component["digest"]!.GetValue<string>();
            }
        }

        throw new InvalidOperationException($"No component named {name}.");
    }

    /// <summary>The digest of one source text, computed independently of the CLI.</summary>
    /// <param name="source">The source text.</param>
    /// <returns>The digest in the form <c>sha256:&lt;hex&gt;</c>.</returns>
    static string DigestOf(string source)
    {
        var normalized = Regex.Replace(
            source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal),
            @"[ \t]+$",
            string.Empty,
            RegexOptions.Multiline).TrimEnd() + "\n";
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }
}
