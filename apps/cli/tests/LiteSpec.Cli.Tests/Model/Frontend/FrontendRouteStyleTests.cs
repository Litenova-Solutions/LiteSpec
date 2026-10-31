using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>Proves the frontend builder records the style each route mark was written in.</summary>
public sealed class FrontendRouteStyleTests
{
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void TypeScript_Route_Marks_Are_Typed_Whatever_The_Project_Declares()
    {
        using var temp = Cli.Temp("route-style");
        WriteProject(temp.Path, "svelte", "attribute");
        var config = ConsumerConfig.Load(temp.Path);
        var code = new CodeModel();

        FrontendReadBuilder.Build(config, Frontend(temp.Path), FactsWithRoute("screen/app.expense-list", null, Finding.TypeScriptReader), code);

        var element = Assert.Single(code.Elements);
        Assert.Equal(CodeKind.FrontendRoute, element.Kind);
        Assert.Equal(MarkStyle.Typed, Assert.Single(element.Marks).Style);
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Dotnet_Route_Marks_Keep_The_Style_The_Reader_Saw()
    {
        using var temp = Cli.Temp("route-style");
        WriteProject(temp.Path, "blazor", "xml");
        var config = ConsumerConfig.Load(temp.Path);
        var code = new CodeModel();

        FrontendReadBuilder.Build(
            config, Frontend(temp.Path),
            FactsWithRoute("screen/app.expense-list", [MarkStyle.Attribute]), code);

        var element = Assert.Single(code.Elements);
        Assert.Equal(MarkStyle.Attribute, Assert.Single(element.Marks).Style);
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Dotnet_Route_Marks_Without_A_Recorded_Style_Fall_Back_To_The_Declared_Style()
    {
        using var temp = Cli.Temp("route-style");
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
            Path.Combine(root, "litespec.project.json"),
            $$"""
            {
              "owners": ["team"],
              "marks": "{{marks}}",
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "{{platform}}" }
                ]
              }
            }
            """);
    }
}
