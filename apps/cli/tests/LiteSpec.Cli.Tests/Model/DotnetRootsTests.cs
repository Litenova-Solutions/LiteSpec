using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the dotnet root lookup finds installations in order and skips what is missing.</summary>
public sealed class DotnetRootsTests
{
    /// <summary>Prefers the environment roots in order.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void PrefersEnvironmentRootsInOrder()
    {
        using var temp = TestCli.Temp("roots");
        var first = Directory.CreateDirectory(Path.Combine(temp.Path, "first")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(temp.Path, "second")).FullName;
        var third = Directory.CreateDirectory(Path.Combine(temp.Path, "third")).FullName;

        var roots = DotnetRoots.Candidates(first, second, null, [third]);

        Assert.Equal([first, second, third], roots);
    }

    /// <summary>Skips roots that do not exist.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SkipsMissingRoots()
    {
        using var temp = TestCli.Temp("roots");
        var missing = Path.Combine(temp.Path, "missing");

        var roots = DotnetRoots.Candidates(missing, null, null, [missing]);

        Assert.Empty(roots);
    }

    /// <summary>Lists each root once.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ListsEachRootOnce()
    {
        using var temp = TestCli.Temp("roots");
        var only = Directory.CreateDirectory(Path.Combine(temp.Path, "only")).FullName;

        var roots = DotnetRoots.Candidates(only, only, null, [only]);

        Assert.Equal([only], roots);
    }

    /// <summary>Finds the dotnet root on PATH.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void FindsDotnetRootOnPath()
    {
        using var temp = TestCli.Temp("roots");
        var bin = Directory.CreateDirectory(Path.Combine(temp.Path, "bin")).FullName;
        File.WriteAllText(Path.Combine(bin, "dotnet"), "");

        var roots = DotnetRoots.RootsFromPath(bin);

        Assert.Equal([bin], roots);
    }

    /// <summary>Finds no root when PATH names no dotnet.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void FindsNoRootWithoutDotnet()
    {
        using var temp = TestCli.Temp("roots");

        Assert.Empty(DotnetRoots.RootsFromPath(temp.Path));
        Assert.Empty(DotnetRoots.RootsFromPath(null));
    }

    /// <summary>Leaves a path without a link unchanged.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void LeavesPathWithoutLinkUnchanged()
    {
        using var temp = TestCli.Temp("roots");
        var file = Path.Combine(temp.Path, "dotnet");
        File.WriteAllText(file, "");

        Assert.Equal(file, DotnetRoots.ResolveLinks(file));
    }

    /// <summary>Names standard locations for the platform.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void NamesStandardLocations() =>
        Assert.NotEmpty(DotnetRoots.StandardLocations());
}
