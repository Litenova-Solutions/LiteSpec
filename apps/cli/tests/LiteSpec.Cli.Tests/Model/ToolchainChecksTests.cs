using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>model check</c> reports toolchain-drift when the pinned tool, the referenced analyzers, and the running CLI name different builds.</summary>
public sealed class ToolchainChecksTests
{
    /// <summary>Check stays clean when the project pins no tool and references no analyzers.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckStaysCleanWithoutToolchainFiles()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        // The exit code follows every other finding on the fixture; this check's silence is the absence of its code.
        Assert.DoesNotContain("toolchain-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when the tool manifest pins the running build.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckStaysCleanWhenTheToolManifestPinsTheRunningBuild()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteToolManifest(fixture.Path, RunningVersion());

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.DoesNotContain("toolchain-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check reports drift as blocking when the tool manifest pins another build.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckReportsDriftWhenTheToolManifestPinsAnotherBuild()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteToolManifest(fixture.Path, "0.0.0");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[toolchain-drift]", result.Output, StringComparison.Ordinal);
        Assert.Contains("LiteSpec.Cli 0.0.0", result.Output, StringComparison.Ordinal);
        Assert.Contains(RunningVersion(), result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check stays clean when the central package file references the running build.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckStaysCleanWhenTheAnalyzersReferenceTheRunningBuild()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCentralPackages(fixture.Path, RunningVersion());

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.DoesNotContain("toolchain-drift", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check reports drift as blocking when the central package file references another build.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckReportsDriftWhenTheAnalyzersReferenceAnotherBuild()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteCentralPackages(fixture.Path, "0.0.0");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[toolchain-drift]", result.Output, StringComparison.Ordinal);
        Assert.Contains("LiteSpec.Analyzers 0.0.0", result.Output, StringComparison.Ordinal);
        Assert.Contains(RunningVersion(), result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check reports drift when the pinned tool and the referenced analyzers differ.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckReportsDriftWhenTheToolAndTheAnalyzersDiffer()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteToolManifest(fixture.Path, RunningVersion());
        WriteCentralPackages(fixture.Path, "0.0.0");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[toolchain-drift]", result.Output, StringComparison.Ordinal);
        Assert.Contains("LiteSpec.Cli " + RunningVersion(), result.Output, StringComparison.Ordinal);
        Assert.Contains("LiteSpec.Analyzers 0.0.0", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Check reads the analyzer version from a direct project reference.</summary>
    /// <proves>litespec/core/release#run-one-litespec-build</proves>
    [Fact]
    public void CheckReadsTheAnalyzerVersionFromADirectProjectReference()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteProject(fixture.Path, RunningVersion());

        var clean = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.DoesNotContain("toolchain-drift", clean.Output, StringComparison.Ordinal);

        WriteProject(fixture.Path, "0.0.0");

        var drifted = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, drifted.Status);
        Assert.Contains("[toolchain-drift]", drifted.Output, StringComparison.Ordinal);
    }

    /// <summary>The version the running CLI reports for itself.</summary>
    /// <returns>The trimmed <c>litespec --version</c> output.</returns>
    static string RunningVersion() => TestCli.Run("--version").Stdout.Trim();

    /// <summary>Writes a tool manifest pinning the LiteSpec.Cli tool at a version.</summary>
    /// <param name="root">The fixture folder.</param>
    /// <param name="version">The version to pin.</param>
    static void WriteToolManifest(string root, string version)
    {
        var file = Path.Combine(root, ".config", "dotnet-tools.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, $$"""
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                "LiteSpec.Cli": {
                  "version": "{{version}}",
                  "commands": ["litespec"]
                }
              }
            }
            """ + "\n");
    }

    /// <summary>Writes a central package file referencing the analyzers at a version.</summary>
    /// <param name="root">The fixture folder.</param>
    /// <param name="version">The version to reference.</param>
    static void WriteCentralPackages(string root, string version)
    {
        File.WriteAllText(Path.Combine(root, "Directory.Packages.props"), $"""
            <Project>
              <ItemGroup>
                <PackageVersion Include="LiteSpec.Analyzers" Version="{version}" />
              </ItemGroup>
            </Project>
            """ + "\n");
    }

    /// <summary>Writes a project referencing the analyzers directly at a version.</summary>
    /// <param name="root">The fixture folder.</param>
    /// <param name="version">The version to reference.</param>
    static void WriteProject(string root, string version)
    {
        File.WriteAllText(Path.Combine(root, "App.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="LiteSpec.Analyzers" Version="{version}" />
              </ItemGroup>
            </Project>
            """ + "\n");
    }
}
