using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves each screen call keeps the operation that reached its use case.</summary>
public sealed class CallsLinkTests
{
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Screen_Reaching_Two_Use_Cases_Through_Two_Operations_Names_Each_Operation()
    {
        using var temp = Cli.Temp("calls-link");
        WriteProject(temp.Path);
        var config = ConsumerConfig.Load(temp.Path);
        var code = CodeWithEndpoints(
            ("ListLoans", "use-case/lending.list-loans"),
            ("BorrowCopy", "use-case/lending.borrow-copy"));
        var read = FrontendReadBuilder.Build(
            config, Frontend(temp.Path), FactsWithCalls("listLoans", "borrowCopy"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Equal(
            [
                new ScreenCall("borrowCopy", "use-case/lending.borrow-copy"),
                new ScreenCall("listLoans", "use-case/lending.list-loans"),
            ],
            screen.Calls);
        Assert.Equal(
            ["use-case/lending.borrow-copy", "use-case/lending.list-loans"],
            screen.UseCases);

        var model = new ProjectModel { Config = config, Code = code };
        model.Screens.Add(screen);
        ModelBuilder.ComputeCalls(model);

        Assert.Equal(2, model.Calls.Count);
        Assert.Contains(
            model.Calls,
            link => link.Screen == "web /home"
                && link.To == "use-case/lending.list-loans"
                && link.Operation == "listLoans");
        Assert.Contains(
            model.Calls,
            link => link.Screen == "web /home"
                && link.To == "use-case/lending.borrow-copy"
                && link.Operation == "borrowCopy");

        using var document = JsonDocument.Parse(model.ToJson());
        var screenJson = Assert.Single(
            document.RootElement.GetProperty("screens").EnumerateArray().ToList());
        var calls = screenJson.GetProperty("calls").EnumerateArray()
            .Select(entry => (
                Operation: entry.GetProperty("operation").GetString(),
                UseCase: entry.GetProperty("useCase").GetString()))
            .ToList();

        Assert.Contains(("listLoans", "use-case/lending.list-loans"), calls);
        Assert.Contains(("borrowCopy", "use-case/lending.borrow-copy"), calls);

        var links = document.RootElement
            .GetProperty("links").GetProperty("calls").EnumerateArray()
            .Select(entry => (
                To: entry.GetProperty("to").GetString(),
                Operation: entry.GetProperty("operation").GetString()))
            .ToList();

        Assert.Contains(("use-case/lending.list-loans", "listLoans"), links);
        Assert.Contains(("use-case/lending.borrow-copy", "borrowCopy"), links);
    }

    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Use_Case_Reached_Through_Two_Operations_Gets_One_Link_Per_Operation()
    {
        using var temp = Cli.Temp("calls-link");
        WriteProject(temp.Path);
        var config = ConsumerConfig.Load(temp.Path);
        var code = CodeWithEndpoints(
            ("ListLoans", "use-case/lending.list-loans"),
            ("ListOwnLoans", "use-case/lending.list-loans"));
        var read = FrontendReadBuilder.Build(
            config, Frontend(temp.Path), FactsWithCalls("listLoans", "listOwnLoans"), code);
        var screen = Assert.Single(read.Screens);
        var model = new ProjectModel { Config = config, Code = code };
        model.Screens.Add(screen);
        ModelBuilder.ComputeCalls(model);

        Assert.Equal(2, model.Calls.Count);
        Assert.All(model.Calls, link => Assert.Equal("use-case/lending.list-loans", link.To));
        Assert.Equal(
            ["listLoans", "listOwnLoans"],
            model.Calls.Select(link => link.Operation).Order(StringComparer.Ordinal).ToList());
    }

    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Use_Case_With_Unknown_Operation_Keeps_An_Empty_Operation()
    {
        using var temp = Cli.Temp("calls-link");
        WriteProject(temp.Path);
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Screens.Add(new ScreenInfo(
            "web", "/home", "apps/web/src/routes/home/+page.svelte",
            [new GeneratedOperation("listLoans", "", "")],
            ["use-case/lending.list-loans"], "screen/app.home"));

        ModelBuilder.ComputeCalls(model);

        var link = Assert.Single(model.Calls);
        Assert.Equal("web /home", link.Screen);
        Assert.Equal("use-case/lending.list-loans", link.To);
        Assert.Equal("", link.Operation);
    }

    static FrontendConfig Frontend(string root) =>
        ConsumerConfig.Load(root).Frontends.Single();

    static CodeModel CodeWithEndpoints(params (string Operation, string UseCase)[] endpoints)
    {
        var code = new CodeModel();

        foreach (var (operation, useCase) in endpoints)
        {
            code.Endpoints.Add(new EndpointOperation(operation, useCase, "", "", "Endpoint.cs", 1));
        }

        return code;
    }

    static FrontendFacts FactsWithCalls(params string[] operations) =>
        new(
            "web", Finding.TypeScriptReader, Qualified: true, Failure: null,
            Modules: [],
            Routes:
            [
                new RouteFact(
                    "src/routes/home/+page.svelte", 4, "/home",
                    ["screen/app.home"],
                    [.. operations.Select((operation, index) =>
                        new CallFact(operation, "src/routes/home/+page.svelte", 9 + index))]),
            ],
            Tests: [],
            Diagnostics: []);

    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["lending-team"],
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ]
              }
            }
            """);
    }
}
