using LiteSpec.Model;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>Proves the TypeScript reader records each use-case module's <c>litespec</c> constant.</summary>
public sealed class FrontendModuleReaderTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Reader_Records_The_Litespec_Constant_As_A_Frontend_Mark()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var modules = FrontendMarksFixture.Build(fixture.Path).Code.Elements
            .Where(element => element.Kind == CodeKind.FrontendModule)
            .ToList();

        Assert.Equal(3, modules.Count);

        var borrow = Assert.Single(modules, element => element.Class == "lending/loans/borrow-copy");
        var mark = Assert.Single(borrow.Marks);

        Assert.Equal("use-case/lending.borrow-copy", mark.RecordId);
        Assert.Equal(MarkVerb.Implements, mark.Verb);
        Assert.Equal(MarkStyle.Typed, mark.Style);
        Assert.Equal(borrow.File, mark.File);
        Assert.EndsWith("litespec.ts", mark.File);
        Assert.Equal(3, mark.Line);
        Assert.Equal(3, borrow.Line);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Reader_Reads_React_Modules_Beside_Svelte_Modules()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var modules = FrontendMarksFixture.Build(fixture.Path).Code.Elements
            .Where(element => element.Kind == CodeKind.FrontendModule)
            .ToList();

        Assert.Contains(modules, element =>
            element.Class == "lending/loans/list-loans"
            && element.File.Contains(Path.Combine("apps", "console")));
        Assert.Contains(modules, element =>
            element.Class == "lending/loans/list-loans"
            && element.File.Contains(Path.Combine("apps", "web")));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Reader_Records_An_Unmarked_Module_With_No_Marks()
    {
        using var fixture = FrontendMarksFixture.Prepare("missing");
        var modules = FrontendMarksFixture.Build(fixture.Path).Code.Elements
            .Where(element => element.Kind == CodeKind.FrontendModule)
            .ToList();

        Assert.Equal(2, modules.Count);

        var borrow = Assert.Single(modules, element => element.Class == "lending/loans/borrow-copy");

        Assert.Empty(borrow.Marks);
        Assert.Equal(1, borrow.Line);
        Assert.True(Directory.Exists(borrow.File));
        Assert.Equal("borrow-copy", Path.GetFileName(borrow.File));
    }
}
