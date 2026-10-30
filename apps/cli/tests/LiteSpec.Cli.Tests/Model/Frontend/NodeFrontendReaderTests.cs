using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Frontend;

/// <summary>Proves the Node frontend reader: the protocol round trip, its failures, and the call mapping.</summary>
public sealed class NodeFrontendReaderTests
{
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Reader_Returns_The_Facts_Its_Response_Names()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var bundle = WriteFakeReader(temp.Path, CannedResponse);

        if (NodeFrontendReader.FindNode() is not { } node)
        {
            return;
        }

        var facts = NodeFrontendReader.Read(ConsumerConfig.Load(temp.Path), bundle, node);
        var web = Assert.Single(facts);

        Assert.True(web.Qualified);
        Assert.Null(web.Failure);
        var route = Assert.Single(web.Routes);
        Assert.Equal("/expenses", route.Address);
        Assert.Equal(["screen/app.expense-list"], route.Marks);
        Assert.Equal("ListExpenses", Assert.Single(route.Calls).Operation);
        Assert.Equal(["use-case/spending.list-expenses"], Assert.Single(web.Modules).Marks);
        var test = Assert.Single(web.Tests);
        Assert.Equal(["scenario/list-expenses.listing-succeeds"], test.Proves);
        Assert.Equal(["/expenses"], test.Gotos);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Reader_With_No_Bundle_Leaves_Empty_Facts_With_The_Cause()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);

        var facts = NodeFrontendReader.Read(ConsumerConfig.Load(temp.Path), null, "node");
        var web = Assert.Single(facts);

        Assert.False(web.Qualified);
        Assert.Contains("litespec-fe-reader.mjs", web.Failure);
        Assert.Empty(web.Routes);
        Assert.Empty(web.Modules);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Reader_Exiting_Non_Zero_Names_The_Exit_Code()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var bundle = WriteFakeReader(
            temp.Path,
            "process.stderr.write('boom: parse failed\\n'); process.exit(3);");

        if (NodeFrontendReader.FindNode() is not { } node)
        {
            return;
        }

        var facts = NodeFrontendReader.Read(ConsumerConfig.Load(temp.Path), bundle, node);
        var web = Assert.Single(facts);

        Assert.Contains("code 3", web.Failure);
        Assert.Contains("boom", web.Failure);
        Assert.Empty(web.Routes);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Reader_With_A_Mismatched_Protocol_Fails_Every_Frontend()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);

        var facts = NodeFrontendReader.ParseResponse(
            ConsumerConfig.Load(temp.Path).Frontends,
            """{"protocol": 2, "reader": "9.9.9", "typescript": "5.9.3", "frontends": []}""");
        var web = Assert.Single(facts);

        Assert.False(web.Qualified);
        Assert.Contains("protocol 2", web.Failure);
    }

    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void Reader_With_Unparseable_Output_Fails_Every_Frontend()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);

        var facts = NodeFrontendReader.ParseResponse(
            ConsumerConfig.Load(temp.Path).Frontends, "not json");
        var web = Assert.Single(facts);

        Assert.Contains("not a JSON response", web.Failure);
        Assert.Empty(web.Tests);
    }

    /// <proves>litespec/core/behavior#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Calls_Map_A_Client_Export_Through_A_PascalCase_WithName()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var code = CodeWithEndpoints(("ListExpenses", "use-case/spending.list-expenses"));
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithCall("listExpenses"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Equal(["use-case/spending.list-expenses"], screen.UseCases);
        Assert.Empty(read.Findings);
    }

    /// <proves>litespec/core/behavior#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Calls_Map_Through_The_Endpoints_WithName_Operation()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var code = CodeWithEndpoints(("ListExpenses", "use-case/spending.list-expenses"));
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithCall("ListExpenses"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Equal(["use-case/spending.list-expenses"], screen.UseCases);
        Assert.Empty(read.Findings);
    }

    /// <proves>litespec/core/behavior#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Call_Two_Endpoints_Map_To_Different_Use_Cases_Is_A_Diagnostic()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var code = CodeWithEndpoints(
            ("ListExpenses", "use-case/spending.list-expenses"),
            ("ListExpenses", "use-case/spending.list-other"));
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path), Frontend(temp.Path), FactsWithCall("ListExpenses"), code);
        var screen = Assert.Single(read.Screens);

        Assert.Empty(screen.UseCases);
        var finding = Assert.Single(read.Findings);
        Assert.Equal("behavior-unrecorded", finding.Code);
        Assert.Contains("different use cases", finding.Message);
    }

    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Failed_Facts_Become_One_Reader_Unavailable_Finding()
    {
        using var temp = Cli.Temp("node-reader");
        WriteProject(temp.Path);
        var read = FrontendReadBuilder.Build(
            ConsumerConfig.Load(temp.Path),
            Frontend(temp.Path),
            FrontendFacts.Failed("web", Finding.TypeScriptReader, "Node is absent from PATH."),
            new CodeModel());

        var finding = Assert.Single(read.Findings);
        Assert.Equal("reader-unavailable", finding.Code);
        Assert.Equal(Finding.TypeScriptReader, finding.Reader);
        Assert.False(finding.ReaderIsQualified);
        Assert.Empty(read.Screens);
    }

    const string CannedResponse = """
        let input = '';
        process.stdin.on('data', chunk => { input += chunk; });
        process.stdin.on('end', () => {
          const request = JSON.parse(input);
          if (request.protocol !== 1 || request.frontends[0].name !== 'web') {
            process.stderr.write('bad request\n');
            process.exit(2);
          }
          process.stdout.write(JSON.stringify({
            protocol: 1, reader: '1.0.0', typescript: '5.9.3', svelte: '5.38.2',
            frontends: [{
              name: 'web',
              modules: [{ folder: 'src/lib/modules/spending/expenses/list-expenses', file: 'src/lib/modules/spending/expenses/list-expenses/litespec.ts', line: 3, marks: ['use-case/spending.list-expenses'] }],
              routes: [{ file: 'src/routes/expenses/+page.ts', line: 4, address: '/expenses', marks: ['screen/app.expense-list'], calls: [{ operation: 'ListExpenses', file: 'src/routes/expenses/+page.svelte', line: 9 }] }],
              tests: [{ file: 'tests/e2e/expenses.spec.ts', line: 7, title: 'lists the expenses', proves: ['scenario/list-expenses.listing-succeeds'], journey: null, gotos: ['/expenses'], codes: [], texts: ['Expenses'] }],
              diagnostics: []
            }]
          }));
        });
        """;

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

    static FrontendFacts FactsWithCall(string operation) =>
        new(
            "web", Finding.TypeScriptReader, Qualified: true, Failure: null,
            Modules: [],
            Routes:
            [
                new RouteFact(
                    "src/routes/expenses/+page.ts", 4, "/expenses",
                    ["screen/app.expense-list"],
                    [new CallFact(operation, "src/routes/expenses/+page.svelte", 9)]),
            ],
            Tests: [],
            Diagnostics: []);

    static string WriteFakeReader(string root, string script)
    {
        var path = Path.Combine(root, "fake-reader.mjs");
        File.WriteAllText(path, script);

        return path;
    }

    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["team"],
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ]
              }
            }
            """);
    }
}
