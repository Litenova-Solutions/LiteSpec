using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves the readers report what they skip as <c>reader-missing</c>, and that same-named types stay separate.</summary>
public sealed class ReaderProblemsTests
{
    /// <summary>An unparsable backend file reports one reader-missing finding naming the file.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void UnparsableBackendFileReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
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
    public void UnreadableBackendFileReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
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
    public void UnparsableScenarioFileReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
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
    public void UnparsableScreenFileReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
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

    /// <summary>A record file that does not parse reports record-invalid naming the file, the parser error and its line.</summary>
    /// <proves>litespec/core/model#state-the-common-fields-on-every-record</proves>
    [Fact]
    public void MalformedRecordFileReportsRecordInvalid()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ".litespec/behavior/broken-record.yaml", "type: use-case\n\tid: use-case/lending.broken\n");
        Write(temp.Path, ".litespec/README.md", "# Notes\n\nA plain readme.\n");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(
            model.Problems,
            found => found.Code == "record-invalid" && found.Path.EndsWith("broken-record.yaml", StringComparison.Ordinal));

        Assert.Contains("broken-record.yaml", problem.Message);
        Assert.Contains("could not be read", problem.Message);
        Assert.Contains("spaces, not tabs, for indentation at line 2", problem.Message);
        Assert.Equal(2, problem.Line);
        Assert.DoesNotContain(
            model.Problems,
            found => found.Path.EndsWith("README.md", StringComparison.Ordinal));
    }

    /// <summary>An explanation file with no record file beside it reports explanation-orphaned at line 1.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void OrphanedExplanationReportsExplanationOrphaned()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, ".litespec/behavior/stray-note.md", "# Stray note\n\nNo record file stands beside this file.\n");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "explanation-orphaned" && found.Path.EndsWith("stray-note.md", StringComparison.Ordinal));

        Assert.Equal(1, finding.Line);
        Assert.Contains("stray-note.md", finding.Message);
        Assert.Contains("stands beside no record file", finding.Message);
    }

    /// <summary>A declared stack root that does not exist reports reader-missing naming the declared root.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void MissingDeclaredStackRootReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"shape": "server", "stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "src/Missing.Modules"}}}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Equal("S", problem.Tier);
        Assert.Contains("modules", problem.Message);
        Assert.Contains("Missing.Modules", problem.Message);
    }

    /// <summary>A declared stack root wildcard that matches no folder reports reader-missing naming the pattern.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void WildcardStackRootWithNoMatchReportsReaderMissing()
    {
        using var temp = TestCli.Temp("reader-problems");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src"));
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"shape": "server", "stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "src/*.Modules"}}}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Contains("*.Modules", problem.Message);
    }

    /// <summary>A server project with no backend reader reports reader-missing; a client-only project stays silent.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ServerShapeWithoutBackendReaderReportsReaderMissing()
    {
        using var server = TestCli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(server.Path, ".litespec/project.yaml"),
            """{"shape": "server", "stacks": ["svelte"]}""");

        var model = ModelBuilder.Build(ConsumerConfig.Load(server.Path));
        var problem = Assert.Single(model.Problems, found => found.Code == "reader-missing");

        Assert.Contains("server", problem.Message);
        Assert.Contains("no backend reader", problem.Message);

        using var client = TestCli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(client.Path, ".litespec/project.yaml"),
            """{"shape": "client-only", "stacks": ["svelte"]}""");

        var clientModel = ModelBuilder.Build(ConsumerConfig.Load(client.Path));

        Assert.DoesNotContain(clientModel.Problems, found => found.Code == "reader-missing");
    }

    /// <summary>A LiteSpec manifest that cannot be parsed reports reader-missing naming the manifest.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ManifestReadFailureReportsReaderMissing()
    {
        using var consumer = TestCli.Temp("reader-problems");
        File.WriteAllText(
            Path.Combine(consumer.Path, ".litespec/project.yaml"),
            """{"shape": "client-only", "stacks": []}""");

        using var home = TestCli.Temp("reader-problems-home");
        File.WriteAllText(Path.Combine(home.Path, "litespec.manifest.yaml"), "[]\n");
        var schemas = Path.Combine(home.Path, "schemas");
        Directory.CreateDirectory(schemas);
        foreach (var schema in Directory.GetFiles(Path.Combine(TestCli.Repository, "schemas")))
        {
            File.Copy(schema, Path.Combine(schemas, Path.GetFileName(schema)));
        }

        var check = TestCli.Run(
            ["model", "check", "--root", consumer.Path],
            environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });

        Assert.Contains("reader-missing", check.Output);
        Assert.Contains("litespec.manifest.yaml", check.Output);
    }

    /// <summary>Two handlers with the same simple name in different namespaces keep their own kind and effects.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SameNamedHandlersInDifferentNamespacesKeepTheirOwnKindAndEffects()
    {
        var fixture = TestCli.Fixture("readers-same-name");

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
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "domain": "src/Domain",
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
