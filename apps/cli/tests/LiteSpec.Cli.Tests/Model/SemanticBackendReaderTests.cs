using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the backend reader runs semantic with build output and syntax without, reporting its mode.</summary>
public sealed class SemanticBackendReaderTests
{
    /// <summary>Syntax Fallback Without Build Output.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SyntaxFallbackWithoutBuildOutput()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "Loan.cs", """
            namespace Library.App.Domain.Loans;

            public sealed class Loan
            {
            }
            """);

        var read = new DotnetBackendReader().ReadWithProblems(ConsumerConfig.Load(temp.Path));

        Assert.Null(read.Semantics);
    }

    /// <summary>Semantic With Project Assets.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SemanticWithProjectAssets()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "Loan.cs", """
            namespace Library.App.Domain.Loans;

            public sealed class Loan
            {
            }
            """);
        WriteAssets(temp.Path, Path.Combine("src", "App"));

        var read = new DotnetBackendReader().ReadWithProblems(ConsumerConfig.Load(temp.Path));

        Assert.NotNull(read.Semantics);
        Assert.Contains(read.Semantics.Evidence, evidence => evidence.EndsWith("project.assets.json", StringComparison.Ordinal));
    }

    /// <summary>Forced Mode Overrides Evidence.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ForcedModeOverridesEvidence()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        var trees = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);

        Assert.Null(BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, "syntax"));
        Assert.Null(BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, null));
        Assert.NotNull(BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, "semantic"));

        WriteAssets(temp.Path, Path.Combine("src", "App"));

        Assert.NotNull(BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, null));
        Assert.Null(BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, "syntax"));
    }

    /// <summary>Semantic Loads Framework References.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SemanticLoadsFrameworkReferences()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        var trees = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);

        var semantics = BackendSemantics.TryCreate(ConsumerConfig.Load(temp.Path), trees, "semantic");

        Assert.NotNull(semantics);
        Assert.Equal(2, semantics.FrameworkReferenceCount);
    }

    /// <summary>Semantic Resolves Using Static Constant.</summary>
    /// <proves>litespec/backend/application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void SemanticResolvesUsingStaticConstant()
    {
        using var temp = TestCli.Temp("reader");
        WriteLoanCodes(temp.Path);

        var syntax = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain(syntax.Literals, literal => literal.Declares && literal.Value == "lending.loan-not-available");

        WriteAssets(temp.Path, Path.Combine("src", "App"));

        var semantic = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Contains(semantic.Literals, literal => literal.Declares && literal.Value == "lending.loan-not-available");
    }

    /// <summary>Semantic Keeps Syntax Facts.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void SemanticKeepsSyntaxFacts()
    {
        using var temp = TestCli.Temp("reader");
        WriteLoanCodes(temp.Path);

        var syntax = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        WriteAssets(temp.Path, Path.Combine("src", "App"));

        var semantic = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Equal(ElementShapes(syntax), ElementShapes(semantic));
        Assert.Empty(syntax.Literals.Except(semantic.Literals));

        var added = semantic.Literals.Except(syntax.Literals).ToList();
        var only = Assert.Single(added);

        Assert.True(only.Declares);
        Assert.Equal("lending.loan-not-available", only.Value);
    }

    /// <summary>Check Reports Syntax Mode.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckReportsSyntaxMode()
    {
        using var fixture = ModelFixture.Prepare("small");
        var text = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Contains("Backend reader: syntax.", text.Output, StringComparison.Ordinal);

        var json = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal("syntax", JsonNode.Parse(json.Stdout)!["backendReader"]!.GetValue<string>());
    }

    /// <summary>Check Reports Semantic Mode With Assets And Keeps Every Finding.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckReportsSemanticModeWithAssets()
    {
        using var fixture = ModelFixture.Prepare("small");
        var syntax = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal("syntax", JsonNode.Parse(syntax.Stdout)!["backendReader"]!.GetValue<string>());

        var roots = SourceRoots.BackendRoots(ConsumerConfig.Load(fixture.Path));

        Assert.NotEmpty(roots);
        WriteAssets(fixture.Path, Path.GetRelativePath(fixture.Path, roots[0]));

        var text = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Contains("Backend reader: semantic (2 framework references, 0 package references).", text.Output, StringComparison.Ordinal);

        var semantic = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal("semantic", JsonNode.Parse(semantic.Stdout)!["backendReader"]!.GetValue<string>());
        Assert.Equal(FindingKeys(syntax.Stdout), FindingKeys(semantic.Stdout));
    }

    static List<string> FindingKeys(string json) =>
        JsonNode.Parse(json)!["findings"]!.AsArray()
            .Select(finding => finding!["code"]!.GetValue<string>() + "|" + finding!["message"]!.GetValue<string>() + "|" + finding!["path"]!.GetValue<string>() + "|" + finding!["line"]!.GetValue<int>())
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

    static List<string> ElementShapes(CodeFacts code) =>
        code.Elements.Select(element =>
            $"{element.Kind}|{element.Class}|{element.Member}|{element.File}|{element.Line}|{element.InTests}|{element.RuntimeValue}|" +
            string.Join(";", element.Marks.Select(mark => $"{mark.RecordId}|{mark.Style}|{mark.Verb}|{mark.File}|{mark.Line}")))
        .ToList();

    static void WriteLoanCodes(string root)
    {
        WriteProject(root);
        WriteSource(root, "App", "LoanRefusalCodes.cs", """
            namespace Library.App.Domain.Loans;

            public static class LoanRefusalCodes
            {
                public const string NotAvailable = "lending.loan-not-available";
            }
            """);
        WriteSource(root, "App", "LoanNotAvailableException.cs", """
            namespace Library.App.Domain.Loans;

            public sealed class LoanNotAvailableException(string code, string message) : Exception(message);
            """);
        WriteSource(root, "App", "BorrowCopyCommandHandler.cs", """
            using static Library.App.Domain.Loans.LoanRefusalCodes;

            namespace Library.App.Application.BorrowCopy;

            public sealed class BorrowCopyCommandHandler
            {
                public void Handle()
                {
                    throw new LoanNotAvailableException(NotAvailable, "No loan.");
                }
            }
            """);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App",
              "webapi": "src/WebApi"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }

    static void WriteAssets(string root, string project)
    {
        var folder = Path.Combine(root, project, "obj");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "project.assets.json"), """{"version":3,"targets":{},"packageFolders":[]}""");
    }
}
