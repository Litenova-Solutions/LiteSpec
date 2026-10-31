using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves the readers report what they skip as <c>reader-missing</c>, and that same-named types stay separate.</summary>
public sealed class ReaderProblemsTests
{
    /// <summary>An unparsable backend file reports one reader-missing finding naming the file.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Unparsable_Backend_File_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        WriteProject(temp.Path);
        WriteApp(temp.Path, "Broken.cs", "public sealed class BrokenHandler { public void Handle( { }");

        var read = new DotnetBackendReader().ReadWithProblems(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(read.Problems);

        Assert.Equal("reader-missing", problem.Code);
        Assert.Equal("S", problem.Tier);
        Assert.EndsWith("Broken.cs", problem.Path);
        Assert.Contains("Broken.cs", problem.Message);
        Assert.Contains("could not be parsed", problem.Message);
    }

    /// <summary>A backend file another process holds open reports one reader-missing finding naming the file.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Unreadable_Backend_File_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        WriteProject(temp.Path);
        var file = WriteApp(temp.Path, "HeldHandler.cs", """
            namespace Reader.App;

            public sealed class HeldHandler
            {
                public string Describe() => "held";
            }
            """);

        using var held = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
        var read = new DotnetBackendReader().ReadWithProblems(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(read.Problems);

        Assert.Equal("reader-missing", problem.Code);
        Assert.EndsWith("HeldHandler.cs", problem.Path);
        Assert.Contains("could not be read", problem.Message);
    }

    /// <summary>An unparsable scenario file reports one reader-missing finding naming the file.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Unparsable_Scenario_File_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"stacks": ["dotnet"], "paths": {"testRoots": ["src/Tests"]}}""");
        var folder = Path.Combine(temp.Path, "src", "Tests");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "BrokenScenarios.cs"), "public sealed class BrokenScenarios { public void Run( { }");

        var read = new DotnetScenarioReader().ReadWithProblems(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(read.Problems);

        Assert.Equal("reader-missing", problem.Code);
        Assert.EndsWith("BrokenScenarios.cs", problem.Path);
        Assert.Contains("could not be parsed", problem.Message);
    }

    /// <summary>An unparsable Android screen file reports one reader-missing finding naming the file.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Unparsable_Screen_File_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(
            Path.Combine(temp.Path, "BrokenActivity.cs"),
            "public sealed class BrokenActivity : Activity { public void Open( { }");
        var frontend = new FrontendConfig("mobile", temp.Path, "android");

        var read = DotnetScreenReader.ReadWithProblems(frontend);
        var problem = Assert.Single(read.Problems);

        Assert.Equal("reader-missing", problem.Code);
        Assert.EndsWith("BrokenActivity.cs", problem.Path);
        Assert.Contains("could not be parsed", problem.Message);
        Assert.Empty(read.Routes);
    }

    /// <summary>A record page whose frontmatter fence holds invalid JSON reports record-invalid naming the page and the parse error.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void Malformed_Frontmatter_Page_Reports_Record_Invalid()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/broken-frontmatter.md", """
            ---
            { "type": "use-case", "id": "use-case/lending.broken",
            ---

            # Broken

            A page whose frontmatter is not JSON.
            """);
        Write(temp.Path, "docs/plain-note.md", "# Plain note\n\nNo frontmatter fence.\n");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(
            model.Problems,
            found => found.Code == "record-invalid" && found.Path.EndsWith("broken-frontmatter.md", StringComparison.Ordinal));

        Assert.Contains("broken-frontmatter.md", problem.Message);
        Assert.Contains("frontmatter is not JSON", problem.Message);
        Assert.DoesNotContain(
            model.Problems,
            found => found.Path.EndsWith("plain-note.md", StringComparison.Ordinal));
    }

    /// <summary>A declared stack root that does not exist reports reader-missing naming the declared root.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Missing_Declared_Stack_Root_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"shape": "server", "stacks": ["dotnet"], "stackConfig": {"dotnet": {"application": "src/Missing.Application"}}}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Equal("S", problem.Tier);
        Assert.Contains("application", problem.Message);
        Assert.Contains("Missing.Application", problem.Message);
    }

    /// <summary>A declared stack root wildcard that matches no folder reports reader-missing naming the pattern.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Wildcard_Stack_Root_With_No_Match_Reports_Reader_Missing()
    {
        using var temp = Cli.Temp("reader-problems");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src"));
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"shape": "server", "stacks": ["dotnet"], "stackConfig": {"dotnet": {"application": "src/*.Application"}}}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Contains("*.Application", problem.Message);
    }

    /// <summary>A server project with no backend reader reports reader-missing; a client-only project stays silent.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Server_Shape_Without_Backend_Reader_Reports_Reader_Missing()
    {
        using var server = Cli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(server.Path, "litespec.project.json"),
            """{"shape": "server", "stacks": ["svelte"]}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(server.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Contains("server", problem.Message);
        Assert.Contains("no backend reader", problem.Message);

        using var client = Cli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(client.Path, "litespec.project.json"),
            """{"shape": "client-only", "stacks": ["svelte"]}""");

        var clientModel = ModelBuilder.Build(ConsumerConfig.Load(client.Path));

        Assert.DoesNotContain(clientModel.Problems, found => found.Code == "reader-missing");
    }

    /// <summary>A LiteSpec manifest that cannot be parsed reports reader-missing naming the manifest.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Manifest_Read_Failure_Reports_Reader_Missing()
    {
        using var consumer = Cli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(consumer.Path, "litespec.project.json"),
            """{"shape": "client-only", "stacks": []}""");

        using var home = Cli.Temp("reader-problems-home");
        File.WriteAllText(Path.Combine(home.Path, "litespec.manifest.json"), "{ not json");

        var check = Cli.Run(
            ["model", "check", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Contains("reader-missing", check.Output);
        Assert.Contains("litespec.manifest.json", check.Output);
    }

    /// <summary>Two handlers with the same simple name in different namespaces keep their own kind and effects.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Same_Named_Handlers_In_Different_Namespaces_Keep_Their_Own_Kind_And_Effects()
    {
        var fixture = Cli.Fixture("readers-same-name");

        var read = new DotnetBackendReader().ReadWithProblems(ConsumerConfig.Load(fixture));

        Assert.Empty(read.Problems);

        var alpha = Assert.Single(read.Code.Handlers, handler => handler.UseCaseId == "use-case/alpha.get-order");
        var beta = Assert.Single(read.Code.Handlers, handler => handler.UseCaseId == "use-case/beta.place-order");

        Assert.Equal("OrderHandler", alpha.Class);
        Assert.Equal("OrderHandler", beta.Class);
        Assert.Equal("query", alpha.Kind);
        Assert.Equal("command", beta.Kind);

        var alphaDispatch = Assert.Single(alpha.Dispatches!);
        var betaDispatch = Assert.Single(beta.Dispatches!);

        Assert.Equal("runs", alphaDispatch.Verb);
        Assert.Equal("ChargeCardCommand", alphaDispatch.Target);
        Assert.Equal("runs", betaDispatch.Verb);
        Assert.Equal("RefundCardCommand", betaDispatch.Target);

        Assert.Equal(
            2,
            read.Code.Elements.Count(element => element.Class == "OrderHandler" && element.Kind == CodeKind.Handler));
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static string WriteApp(string root, string file, string text)
    {
        var folder = Path.Combine(root, "src", "App");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, file);
        File.WriteAllText(path, text);

        return path;
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
