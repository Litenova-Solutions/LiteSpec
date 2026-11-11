using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Scenarios;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the scenario reader takes each test's claims from its proves marks, in either style.</summary>
public sealed class DotnetScenarioMarkTests
{
    /// <summary>Test Claim Xml Links Its Scenario.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void TestClaimXmlLinksItsScenario()
    {
        using var temp = TestCli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                public Task BorrowingSucceeds() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(scenario.Claims));
    }

    /// <summary>Test Claim Attribute Links Its Scenario.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void TestClaimAttributeLinksItsScenario()
    {
        using var temp = TestCli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                [Proves("scenario/borrow-copy.borrowing-succeeds")]
                public Task BorrowingSucceeds() => Task.CompletedTask;
            }
            """);

        var scenario = Assert.Single(Read(temp.Path).Scenarios);

        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(scenario.Claims));
    }

    /// <summary>Test Claim Names A Rule.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void TestClaimNamesARule()
    {
        using var temp = TestCli.Temp("reader-marks");
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

    /// <summary>Test Claim Names A Proof Target.</summary>
    /// <param name="claim">The record identifier or provision identifier the case claims as its proof target.</param>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Theory]
    [InlineData("requirement/borrowing.borrow-latency")]
    [InlineData("decision/0002-store-loans-as-documents")]
    [InlineData("external-system/text-gateway")]
    [InlineData("integration-event/loan-borrowed")]
    [InlineData("litespec/backend/persistence#commit-each-command-pipeline-once")]
    // The InlineData values name each proof-target claim case.
    public void Test_Claim_Names_A_Proof_Target(string claim)
    {
        using var temp = TestCli.Temp("reader-marks");
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

    /// <summary>Test Without Claims Has No Scenario.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void TestWithoutClaimsHasNoScenario()
    {
        using var temp = TestCli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                public Task BorrowingSucceeds() => Task.CompletedTask;
            }
            """);

        Assert.Empty(Read(temp.Path).Scenarios);
    }

    /// <summary>Test Method Is Read As A Test Element.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void TestMethodIsReadAsATestElement()
    {
        using var temp = TestCli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyTests.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyTests
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                [Fact]
                public Task BorrowingSucceeds() => Task.CompletedTask;
            }
            """);

        var read = Read(temp.Path);
        var element = Assert.Single(read.Elements);

        Assert.Equal(CodeKind.Test, element.Kind);
        Assert.Equal("BorrowCopyTests", element.Class);
        Assert.Equal("BorrowingSucceeds", element.Member);
        Assert.True(element.InTests);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(element.Marks).RecordId);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(Assert.Single(read.Scenarios).Claims));
    }

    /// <summary>Proves Without Test Attribute Lands In Stray Marks.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ProvesWithoutTestAttributeLandsInStrayMarks()
    {
        using var temp = TestCli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "BorrowCopyScenarios.cs", """
            namespace Library.Acceptance.Tests;

            public sealed class BorrowCopyScenarios
            {
                /// <proves>scenario/borrow-copy.borrowing-succeeds</proves>
                public Task BorrowingSucceeds() => Task.CompletedTask;
            }
            """);

        var read = Read(temp.Path);

        Assert.Empty(read.Elements);
        var stray = Assert.Single(read.StrayMarks);

        Assert.Equal("BorrowCopyScenarios", stray.Class);
        Assert.Equal("BorrowingSucceeds", stray.Member);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(stray.Marks).RecordId);
        Assert.Equal("scenario/borrow-copy.borrowing-succeeds", Assert.Single(Assert.Single(read.Scenarios).Claims));
    }

    static TestRead Read(string root) => new DotnetScenarioReader().Read(ConsumerConfig.Load(root));

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"],
              "paths": {"testRoots": ["tests/Acceptance"]},
              "stackConfig": {"dotnet": {
                "modules": "src/App", "domain": "src/Domain",
                "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string file, string text)
    {
        var folder = Path.Combine(root, "tests", "Acceptance");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
