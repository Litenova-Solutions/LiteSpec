using LiteSpec.Maintain.PublishLocal;
using LiteSpec.Maintain.Tests.Support;
using PublishLocalEdits = LiteSpec.Maintain.PublishLocal.PublishLocal;

namespace LiteSpec.Maintain.Tests.PublishLocal;

/// <summary>Tests for <c>litespec-maintain publish-local --fast</c>: the smaller pack set, the unchanged pinning, and the refused options.</summary>
public sealed class PublishLocalFastTests
{
    /// <summary>Fast packs the framework-dependent tool and the analyzers.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FastPacksTheFrameworkDependentToolAndTheAnalyzers()
    {
        var packs = PublishLocalEdits.Packs(true, "win-x64");

        Assert.Equal(3, packs.Count);
        Assert.Equal(PublishPackProject.Analyzers, packs[0].Project);
        Assert.Empty(packs[0].Extra);
        Assert.Equal(PublishPackProject.Cli, packs[1].Project);
        // The top-level package of a fast run lists only the framework-dependent build, so a consumer never asks for the native one.
        Assert.Equal(["-p:TopLevelToolPackage=true", "-p:ToolPackageRuntimeIdentifiers=any"], packs[1].Extra);
        Assert.Equal(PublishPackProject.Cli, packs[2].Project);
        Assert.Equal(["--runtime", "any", "-p:PublishAot=false"], packs[2].Extra);
    }

    /// <summary>A full run's top-level package keeps every runtime the project lists.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FullRunKeepsEveryRuntimeInTheTopLevelPackage()
    {
        Assert.Equal(["-p:TopLevelToolPackage=true"], PublishLocalEdits.Packs(false, "win-x64")[1].Extra);
    }

    /// <summary>Fast skips the native pack of the machine that a full run keeps.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FastSkipsTheNativePackOfTheMachine()
    {
        Assert.DoesNotContain(PublishLocalEdits.Packs(true, "win-x64"), pack => pack.Extra.Contains("win-x64"));

        var full = PublishLocalEdits.Packs(false, "win-x64");

        Assert.Equal(4, full.Count);
        var native = Assert.Single(full, pack => pack.Extra.Contains("win-x64"));
        Assert.Equal(PublishPackProject.Cli, native.Project);
    }

    /// <summary>Fast pins the consumer exactly as a full run does.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FastPinsTheConsumerExactlyAsAFullRun()
    {
        const string version = "7.0.0-dev.42";

        var (toolsText, toolsChanged) = PublishLocalEdits.SetToolVersion(null, version);

        Assert.True(toolsChanged);
        Assert.Equal(PublishLocalEdits.ToolManifest(version), toolsText);

        const string props = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="LiteSpec.Analyzers" Version="7.0.0" />
              </ItemGroup>
            </Project>

            """;

        var (propsNew, changed) = PublishLocalEdits.SetAnalyzerVersion(props, version);

        Assert.Equal(1, changed);
        Assert.Contains("<PackageVersion Include=\"LiteSpec.Analyzers\" Version=\"7.0.0-dev.42\" />", propsNew, StringComparison.Ordinal);
    }

    /// <summary>Fast is a known option: help with fast prints the usage and exits zero.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FastIsAccepted()
    {
        var result = Cli.Run("publish-local", "--fast", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("--fast", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is refused with a usage error.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void UnknownOptionIsRefused()
    {
        var result = Cli.Run("publish-local", "--consumer", "some-consumer", "--bogus");

        Assert.Equal(2, result.Status);
        Assert.Contains("--bogus", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Fast with a CI build is refused: the CI build packs nothing to skip.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void FastWithFromCiIsRefused()
    {
        var result = Cli.Run("publish-local", "--consumer", "some-consumer", "--from-ci", "42", "--fast");

        Assert.Equal(2, result.Status);
        Assert.Contains("--fast", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("--from-ci", result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown option", result.Stderr, StringComparison.Ordinal);
    }
}
