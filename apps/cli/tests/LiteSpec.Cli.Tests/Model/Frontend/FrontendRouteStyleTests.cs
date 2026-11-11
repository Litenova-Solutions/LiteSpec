using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Readers;
using LiteSpec.Cli.Model.Readers.Frontend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the frontend builder records the style each route mark was written in.</summary>
public sealed class FrontendRouteStyleTests
{
    /// <summary>TypeScript Route Marks Are Typed Whatever The Project Declares.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void TypeScriptRouteMarksAreTypedWhateverTheProjectDeclares()
    {
        using var temp = TestCli.Temp("route-style");
        WriteProject(temp.Path, "svelte", "attribute");
        var config = ConsumerConfig.Load(temp.Path);
        var code = new CodeModel();

        FrontendReadBuilder.Build(config, Frontend(temp.Path), FactsWithRoute("screen/app.expense-list", null, Finding.TypeScriptReader), code);

        var element = Assert.Single(code.Elements);
        Assert.Equal(CodeKind.FrontendRoute, element.Kind);
        Assert.Equal(MarkStyle.Typed, Assert.Single(element.Marks).Style);
    }

    /// <summary>Dotnet Route Marks Keep The Style The Reader Saw.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void DotnetRouteMarksKeepTheStyleTheReaderSaw()
    {
        using var temp = TestCli.Temp("route-style");
        WriteProject(temp.Path, "blazor", "xml");
        var config = ConsumerConfig.Load(temp.Path);
        var code = new CodeModel();

        FrontendReadBuilder.Build(
            config, Frontend(temp.Path),
            FactsWithRoute("screen/app.expense-list", [MarkStyle.Attribute]), code);

        var element = Assert.Single(code.Elements);
        Assert.Equal(MarkStyle.Attribute, Assert.Single(element.Marks).Style);
    }

    /// <summary>Dotnet Route Marks Without A Recorded Style Fall Back To The Declared Style.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void DotnetRouteMarksWithoutARecordedStyleFallBackToTheDeclaredStyle()
    {
        using var temp = TestCli.Temp("route-style");
        WriteProject(temp.Path, "blazor", "attribute");
        var config = ConsumerConfig.Load(temp.Path);
        var code = new CodeModel();

        FrontendReadBuilder.Build(config, Frontend(temp.Path), FactsWithRoute("screen/app.expense-list", null), code);

        var element = Assert.Single(code.Elements);
        Assert.Equal(MarkStyle.Attribute, Assert.Single(element.Marks).Style);
    }

    static FrontendFacts FactsWithRoute(string mark, IReadOnlyList<MarkStyle>? styles, string reader = Finding.RoslynReader) =>
        new(
            "web", reader, Qualified: true, Failure: null,
            Modules: [],
            Routes:
            [
                new RouteFact(
                    "Pages/ExpenseList.razor", 1, "/expenses",
                    [mark], [],
                    styles),
            ],
            Tests: [],
            Diagnostics: []);

    static FrontendConfig Frontend(string root) =>
        ConsumerConfig.Load(root).Frontends.Single();

    static void WriteProject(string root, string platform, string marks)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            $$"""
            {
              "owners": ["team"],
              "marks": "{{marks}}",
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "{{platform}}" }
                ], "paths": {
              }
            }
            """);
    }
}
