using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the targeting pack resolution picks the pack matching the consumer and reports why none was found.</summary>
public sealed class TargetingPackTests
{
    /// <summary>Parses a target framework to its version.</summary>
    /// <param name="framework">The target framework.</param>
    /// <param name="major">The expected major version.</param>
    /// <param name="minor">The expected minor version.</param>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("net10.0", 10, 0)]
    [InlineData("net8.0", 8, 0)]
    [InlineData("net6.0", 6, 0)]
    [InlineData("net8.0-windows", 8, 0)]
    [InlineData("net10.0-android", 10, 0)]
    [InlineData("netcoreapp3.1", 3, 1)]
    [InlineData(".NETCoreApp,Version=v8.0", 8, 0)]
    public void ParsesFrameworkVersion(string framework, int major, int minor)
    {
        Assert.True(TargetingPack.TryParseFrameworkVersion(framework, out var version));
        Assert.Equal(new Version(major, minor), version);
    }

    /// <summary>Rejects a framework that names no .NET version.</summary>
    /// <param name="framework">The target framework.</param>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("net48")]
    [InlineData("net472")]
    [InlineData(".NETStandard,Version=v2.0")]
    [InlineData(".NETFramework,Version=v4.8")]
    [InlineData("")]
    [InlineData("net")]
    public void RejectsNonDotNetFramework(string framework) =>
        Assert.False(TargetingPack.TryParseFrameworkVersion(framework, out _));

    /// <summary>Parses a pack version folder name.</summary>
    /// <param name="folder">The pack version folder name.</param>
    /// <param name="expected">The expected version.</param>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("10.0.14", "10.0.14")]
    [InlineData("8.0.28", "8.0.28")]
    [InlineData("6.0.36", "6.0.36")]
    [InlineData("10.0.0-preview.1", "10.0.0")]
    public void ParsesPackVersion(string folder, string expected)
    {
        Assert.True(TargetingPack.TryParsePackVersion(folder, out var version));
        Assert.Equal(Version.Parse(expected), version);
    }

    /// <summary>Rejects a pack folder name that names no version.</summary>
    /// <param name="folder">The pack folder name.</param>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    public void RejectsNonPackVersion(string folder) =>
        Assert.False(TargetingPack.TryParsePackVersion(folder, out _));

    /// <summary>Reports no dotnet installation when no root was found.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReportsNoDotnetInstallation()
    {
        var (references, reason) = TargetingPack.Load(["net10.0"], []);

        Assert.Empty(references);
        Assert.Equal("no dotnet installation was found", reason);
    }

    /// <summary>Reports no targeting pack when the roots hold none.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReportsNoTargetingPack()
    {
        using var temp = TestCli.Temp("pack");

        var (references, reason) = TargetingPack.Load(["net10.0"], [temp.Path]);

        Assert.Empty(references);
        Assert.Equal("no targeting pack was found under the dotnet installation", reason);
    }

    /// <summary>Picks the highest patch of the matching framework.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void PicksHighestPatchOfMatchingFramework()
    {
        using var temp = TestCli.Temp("pack");
        WritePack(temp.Path, "8.0.14");
        WritePack(temp.Path, "8.0.28");

        var (references, reason) = TargetingPack.Load(["net8.0"], [temp.Path]);

        Assert.Empty(references);
        Assert.Contains("8.0.28", reason, StringComparison.Ordinal);
    }

    /// <summary>Picks the matching framework over the highest pack.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void PicksMatchingFrameworkOverHighestPack()
    {
        using var temp = TestCli.Temp("pack");
        WritePack(temp.Path, "8.0.28");
        WritePack(temp.Path, "10.0.14");

        var (references, reason) = TargetingPack.Load(["net8.0"], [temp.Path]);

        Assert.Empty(references);
        Assert.Contains("8.0.28", reason, StringComparison.Ordinal);
    }

    /// <summary>Picks the highest pack when no framework is named.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void PicksHighestPackWithoutFramework()
    {
        using var temp = TestCli.Temp("pack");
        WritePack(temp.Path, "8.0.28");
        WritePack(temp.Path, "10.0.14");

        var (references, reason) = TargetingPack.Load([], [temp.Path]);

        Assert.Empty(references);
        Assert.Contains("10.0.14", reason, StringComparison.Ordinal);
    }

    /// <summary>Reports no matching pack when the framework has none.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReportsNoMatchingPack()
    {
        using var temp = TestCli.Temp("pack");
        WritePack(temp.Path, "8.0.28");

        var (references, reason) = TargetingPack.Load(["net9.0"], [temp.Path]);

        Assert.Empty(references);
        Assert.Equal("no targeting pack for net9.0 was found", reason);
    }

    /// <summary>Reads the target frameworks the assets files name.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReadsTargetFrameworksFromAssets()
    {
        using var temp = TestCli.Temp("pack");
        WriteProject(temp.Path);
        WriteAssets(temp.Path, """{"version":3,"targets":{"net10.0":{}},"packageFolders":[]}""");

        Assert.Equal(["net10.0"], ProjectAssets.TargetFrameworks(ConsumerConfig.Load(temp.Path)));
    }

    /// <summary>Reads the framework before the target slash.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReadsFrameworkBeforeTargetSlash()
    {
        using var temp = TestCli.Temp("pack");
        WriteProject(temp.Path);
        WriteAssets(temp.Path, """{"version":3,"targets":{"net8.0/win-x64":{}},"packageFolders":[]}""");

        Assert.Equal(["net8.0"], ProjectAssets.TargetFrameworks(ConsumerConfig.Load(temp.Path)));
    }

    /// <summary>Reads no framework when the assets name none.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReadsNoFrameworkWithoutTargets()
    {
        using var temp = TestCli.Temp("pack");
        WriteProject(temp.Path);
        WriteAssets(temp.Path, """{"version":3,"targets":{},"packageFolders":[]}""");

        Assert.Empty(ProjectAssets.TargetFrameworks(ConsumerConfig.Load(temp.Path)));
    }

    static void WritePack(string root, string version) =>
        Directory.CreateDirectory(Path.Combine(root, "packs", "Microsoft.NETCore.App.Ref", version));

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App",
              "webapi": "src/WebApi"}}}
            """);

    static void WriteAssets(string root, string json)
    {
        var folder = Path.Combine(root, "src", "App", "obj");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "project.assets.json"), json);
    }
}
