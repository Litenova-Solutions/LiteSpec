using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers;
using LiteSpec.Cli.Model.Readers.Frontend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves a full-page navigation to an endpoint route reaches its use case like a generated-client call.</summary>
public sealed class NavigationTests
{
    /// <summary>Navigation To An Endpoint Route Reaches Its Use Case.</summary>
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void NavigationToAnEndpointRouteReachesItsUseCase()
    {
        using var temp = TestCli.Temp("navigation");
        WriteProject(temp.Path);
        var code = CodeWithEndpoint("StartSignIn", "use-case/lending.borrow-copy", "GET", "/auth/login", "StartSignInEndpoint.cs");
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithNavigation("/auth/login?next=/home"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Equal(["use-case/lending.borrow-copy"], screen.UseCases);
        Assert.Contains(screen.Operations, operation => operation.Name == "StartSignIn" && operation.Method == "GET" && operation.Path == "/auth/login");
        Assert.Contains(screen.Calls, call => call.Operation == "StartSignIn" && call.UseCase == "use-case/lending.borrow-copy");
        Assert.Empty(read.Findings);
    }

    /// <summary>Navigation To An Unknown Path Reaches No Use Case.</summary>
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void NavigationToAnUnknownPathReachesNoUseCase()
    {
        using var temp = TestCli.Temp("navigation");
        WriteProject(temp.Path);
        var code = CodeWithEndpoint("StartSignIn", "use-case/lending.borrow-copy", "GET", "/auth/login", "StartSignInEndpoint.cs");
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithNavigation("/elsewhere"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Empty(screen.UseCases);
        Assert.Empty(screen.Operations);
        Assert.Empty(screen.Calls);
        Assert.Empty(read.Findings);
    }

    /// <summary>Navigation Two Endpoints Map To Different Use Cases Is A Diagnostic.</summary>
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void NavigationTwoEndpointsMapToDifferentUseCasesIsADiagnostic()
    {
        using var temp = TestCli.Temp("navigation");
        WriteProject(temp.Path);
        var code = new CodeFacts();
        code.Endpoints.Add(new EndpointOperation("", "use-case/lending.borrow-copy", "GET", "/auth/login", "FirstEndpoint.cs", 1));
        code.Endpoints.Add(new EndpointOperation("", "use-case/lending.list-loans", "GET", "/auth/login", "SecondEndpoint.cs", 1));
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithNavigation("/auth/login"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Empty(screen.UseCases);
        var finding = Assert.Single(read.Findings);
        Assert.Equal("behavior-unrecorded", finding.Code);
        Assert.Contains("/auth/login", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Reader Returns The Navigations Its Response Names.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void ReaderReturnsTheNavigationsItsResponseNames()
    {
        using var temp = TestCli.Temp("navigation");
        WriteProject(temp.Path);

        var facts = NodeFrontendReader.ParseResponse(
            ConsumerConfig.Load(temp.Path).Frontends,
            """
            {"protocol": 1, "reader": "1.1.0", "typescript": "5.9.3", "svelte": "5.38.2", "frontends": [{
              "name": "web",
              "modules": [],
              "routes": [{ "file": "src/routes/sign-in/+page.svelte", "line": 4, "address": "/sign-in", "marks": ["screen/app.sign-in"], "calls": [],
                "navigations": [{ "path": "/auth/login", "file": "src/routes/sign-in/+page.svelte", "line": 9 }] }],
              "tests": [],
              "diagnostics": []
            }]}
            """);
        var route = Assert.Single(Assert.Single(facts).Routes);
        var navigation = Assert.Single(route.Navigations ?? []);

        Assert.Equal("/auth/login", navigation.Path);
        Assert.Equal(9, navigation.Line);
    }

    /// <summary>Reader Without Navigations Leaves Empty Navigations.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void ReaderWithoutNavigationsLeavesEmptyNavigations()
    {
        using var temp = TestCli.Temp("navigation");
        WriteProject(temp.Path);

        var facts = NodeFrontendReader.ParseResponse(
            ConsumerConfig.Load(temp.Path).Frontends,
            """
            {"protocol": 1, "reader": "1.0.0", "typescript": "5.9.3", "svelte": "5.38.2", "frontends": [{
              "name": "web",
              "modules": [],
              "routes": [{ "file": "src/routes/sign-in/+page.svelte", "line": 4, "address": "/sign-in", "marks": ["screen/app.sign-in"], "calls": [] }],
              "tests": [],
              "diagnostics": []
            }]}
            """);
        var route = Assert.Single(Assert.Single(facts).Routes);

        Assert.Empty(route.Navigations ?? []);
    }

    static FrontendConfig Frontend(string root) =>
        ConsumerConfig.Load(root).Frontends.Single();

    static CodeFacts CodeWithEndpoint(string operation, string useCase, string method, string route, string file)
    {
        var code = new CodeFacts();
        code.Endpoints.Add(new EndpointOperation(operation, useCase, "", "", file, 1));
        code.Endpoints.Add(new EndpointOperation("", useCase, method, route, file, 1));

        return code;
    }

    static FrontendFacts FactsWithNavigation(string path) =>
        new(
            "web", Finding.TypeScriptReader, Qualified: true, Failure: null,
            Modules: [],
            Routes:
            [
                new RouteFact(
                    "src/routes/sign-in/+page.svelte", 4, "/sign-in",
                    ["screen/app.sign-in"],
                    [],
                    Navigations: [new NavigationFact(path, "src/routes/sign-in/+page.svelte", 9)]),
            ],
            Tests: [],
            Diagnostics: []);

    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ], "paths": {
              }
            }
            """);
    }
}
