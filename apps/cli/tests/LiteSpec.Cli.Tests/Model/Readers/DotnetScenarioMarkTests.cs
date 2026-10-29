using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the scenario reader takes each test's claims from its proves marks, in either style.</summary>
public sealed class DotnetScenarioMarkTests
{
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Test_Claim_Xml_Links_Its_Scenario()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                public Task Borrowing_succeeds() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(scenario.Claims));
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Test_Claim_Attribute_Links_Its_Scenario()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                [Proves("scenario/borrow-copy.borrowing-succeeds")]
                public Task Borrowing_succeeds() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(scenario.Claims));
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Test_Claim_Names_A_Rule()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BarcodeTests.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BarcodeTests
            {
                /// <proves>rule/lending.copy-barcode-unique</proves>
                public Task Barcode_Is_Unique() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal("rule/lending.copy-barcode-unique", Assert.Single(scenario.Claims));
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("requirement/borrowing.borrow-latency")]
    [InlineData("decision/0002-store-loans-as-documents")]
    [InlineData("external-system/text-gateway")]
    [InlineData("integration-event/loan-borrowed")]
    [InlineData("litespec/backend/persistence#commit-each-command-pipeline-once")]
    public void Test_Claim_Names_A_Proof_Target(string claim)
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "LatencyTests.cs", $$"""
            namespace Library.Acceptance.Tests;

            public sealed class LatencyTests
            {
                /// <proves>{{claim}}</proves>
                public Task Borrowing_stays_fast() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal(claim, Assert.Single(scenario.Claims));
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Test_Without_Claims_Has_No_Scenario()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                public Task Borrowing_succeeds() => Task.CompletedTask;
            }
            """);

        Assert.Empty(Read(temp.Path).Scenarios);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Test_Method_Is_Read_As_A_Test_Element()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyTests.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyTests
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                [Fact]
                public Task Borrowing_succeeds() => Task.CompletedTask;
            }
            """);

        var read = Read(temp.Path);
        var element = Assert.Single(read.Elements);

        Assert.Equal(CodeKind.Test, element.Kind);
        Assert.Equal("BorrowCopyTests", element.Class);
        Assert.Equal("Borrowing_succeeds", element.Member);
        Assert.True(element.InTests);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(element.Marks).RecordId);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(Assert.Single(read.Scenarios).Claims));
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Proves_Without_Test_Attribute_Lands_In_Stray_Marks()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                public Task Borrowing_succeeds() => Task.CompletedTask;
            }
            """);

        var read = Read(temp.Path);

        Assert.Empty(read.Elements);
        var stray = Assert.Single(read.StrayMarks);

        Assert.Equal("BorrowCopyScenarios", stray.Class);
        Assert.Equal("Borrowing_succeeds", stray.Member);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(stray.Marks).RecordId);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(Assert.Single(read.Scenarios).Claims));
    }

    static TestRead Read(string root) => new DotnetScenarioReader().Read(ConsumerConfig.Load(root));

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"],
              "paths": {"testRoots": ["tests/Acceptance"]},
              "stackConfig": {"dotnet": {
                "application": "src/App", "domain": "src/Domain",
                "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string file, string text)
    {
        var folder = Path.Combine(root, "tests", "Acceptance");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
