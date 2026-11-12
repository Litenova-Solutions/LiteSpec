using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers;
using LiteSpec.Cli.Model.Readers.Dotnet.Scenarios;
using LiteSpec.Cli.Model.Readers.JUnit;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves a JUnit case proves the Behavior scenarios its litespec.proves properties name.</summary>
public sealed class JUnitProvesTests
{
    const string Borrow = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string Return = "scenario/return-copy.returning-a-borrowed-copy-succeeds";
    const string Unknown = "scenario/borrow-copy.borrowing-an-unknown-copy-succeeds";

    const string Failure = "      <failure message=\"The loan stayed active.\" />\n";
    const string Error = "      <error message=\"The loan stayed active.\" />\n";
    const string Skip = "      <skipped />\n";
    const string Method = "Library.Acceptance.Tests.Borrow.BorrowScenarios.Borrowing_an_available_copy_succeeds";

    /// <summary>A passing case with a proves property proves the scenario as passed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingCaseWithAProvesPropertyProvesTheScenario()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow)));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Passed", run.OutcomeByScenario[Borrow]);
    }

    /// <summary>One property with comma-separated ids proves each scenario.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void OnePropertyWithCommaSeparatedIdsProvesEachScenario()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves($"{Borrow}, {Return}")));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Passed", run.OutcomeByScenario[Borrow]);
        Assert.Equal("Passed", run.OutcomeByScenario[Return]);
    }

    /// <summary>One property per scenario proves each scenario.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void OnePropertyPerScenarioProvesEachScenario()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow, Return)));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Passed", run.OutcomeByScenario[Borrow]);
        Assert.Equal("Passed", run.OutcomeByScenario[Return]);
    }

    /// <summary>A failing case proves as failed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FailingCaseProvesAsFailed()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Failure));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Failed", run.OutcomeByScenario[Borrow]);
    }

    /// <summary>A case with an error proves as failed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ErrorCaseProvesAsFailed()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Error));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Failed", run.OutcomeByScenario[Borrow]);
    }

    /// <summary>A skipped case proves as skipped.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void SkippedCaseProvesAsSkipped()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Skip));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Skipped", run.OutcomeByScenario[Borrow]);
    }

    /// <summary>A case without a proves property proves nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CaseWithoutAProvesPropertyProvesNothing()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", ""));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Empty(run.OutcomeByScenario);
    }

    /// <summary>Other properties and blank ids prove nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void OtherPropertiesAndBlankIdsProveNothing()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(
            temp.Path,
            "smoke.xml",
            Case(
                "test_borrow.TestBorrow",
                "test_borrowing_an_available_copy_succeeds",
                "      <properties>\n"
                + "        <property name=\"owner\" value=\"lending\" />\n"
                + "        <property name=\"litespec.proves\" value=\" , \" />\n"
                + "      </properties>\n"));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Empty(run.OutcomeByScenario);
    }

    /// <summary>A failure wins when two files prove one scenario.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FailureWinsWhenTwoFilesProveOneScenario()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "first.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow)));
        WriteJUnit(temp.Path, "second.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Failure));

        var run = new JUnitRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Failed", run.OutcomeByScenario[Borrow]);
        Assert.Contains(Borrow, run.Flaky);
    }

    /// <summary>A combined run merges scenario outcomes across files.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CombinedRunMergesScenarioOutcomesAcrossFiles()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteJUnit(temp.Path, "first.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow, Return)));
        WriteJUnit(temp.Path, "second.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Failure));

        var run = new CombinedTestRunReader(new JUnitRunReader(), new JUnitRunReader())
            .Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Failed", run.OutcomeByScenario[Borrow]);
        Assert.Equal("Passed", run.OutcomeByScenario[Return]);
        Assert.Contains(Borrow, run.Flaky);
    }

    /// <summary>A combined run keeps scenario outcomes beside other formats.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CombinedRunKeepsScenarioOutcomesBesideOtherFormats()
    {
        using var temp = TestCli.Temp("reader-junit-proves");
        WriteProject(temp.Path);
        WriteTrx(temp.Path, "results.trx", Method, "Passed");
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow)));

        var run = new CombinedTestRunReader(new TrxTestRunReader(), new JUnitRunReader())
            .Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Passed", run.OutcomeByMethod[Method]);
        Assert.Equal("Passed", run.OutcomeByScenario[Borrow]);
    }

    /// <summary>A passing JUnit property confirms the scenario it names.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingJUnitPropertyConfirmsTheScenarioItNames()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/borrow-copy-junit-proof-scenario.yaml", ScenarioJson(Borrow, "Borrowing an available copy succeeds", "result/borrow-copy.borrowed"));
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow)));

        using var document = Build(temp.Path);
        var link = LinkOf(document, Borrow);

        Assert.True(link.HasValue);

        var found = link!.Value;

        Assert.True(found.GetProperty("confirmed").GetBoolean());
        Assert.True(found.GetProperty("passing").GetBoolean());
    }

    /// <summary>A failing JUnit property leaves the scenario not passing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FailingJUnitPropertyLeavesTheScenarioNotPassing()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/borrow-copy-junit-proof-scenario.yaml", ScenarioJson(Borrow, "Borrowing an available copy succeeds", "result/borrow-copy.borrowed"));
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_available_copy_succeeds", Proves(Borrow) + Failure));

        using var document = Build(temp.Path);
        var link = LinkOf(document, Borrow);

        Assert.True(link.HasValue);

        var found = link!.Value;

        Assert.True(found.GetProperty("confirmed").GetBoolean());
        Assert.False(found.GetProperty("passing").GetBoolean());
    }

    /// <summary>An unknown scenario id in a JUnit property proves nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UnknownScenarioIdInAJUnitPropertyProvesNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteJUnit(temp.Path, "smoke.xml", Case("test_borrow.TestBorrow", "test_borrowing_an_unknown_copy_succeeds", Proves(Unknown)));

        using var document = Build(temp.Path);

        Assert.False(LinkOf(document, Unknown).HasValue);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), """{ "owners": ["team"] }""");

    static void WriteTrx(string root, string file, string testName, string outcome)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, file),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<TestRun name=\"smoke run\" xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n"
            + "  <Results>\n"
            + $"    <UnitTestResult testName=\"{testName}\" outcome=\"{outcome}\" />\n"
            + "  </Results>\n"
            + "</TestRun>\n");
    }

    static void WriteJUnit(string root, string file, string cases)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, file),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<testsuites>\n"
            + "  <testsuite name=\"smoke\">\n"
            + cases
            + "  </testsuite>\n"
            + "</testsuites>\n");
    }

    static string Case(string classname, string name, string body) =>
        $"    <testcase classname=\"{classname}\" name=\"{name}\">\n{body}    </testcase>\n";

    static string Proves(params string[] values) =>
        "      <properties>\n"
        + string.Concat(values.Select(value => $"        <property name=\"litespec.proves\" value=\"{value}\" />\n"))
        + "      </properties>\n";

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement? LinkOf(JsonDocument document, string claim)
    {
        foreach (var link in document.RootElement.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link;
            }
        }

        return null;
    }

    static string ScenarioJson(string id, string title, string result) => $$"""
        {
          "type": "scenario",
          "id": "{{id}}",
          "title": "{{title}}",
          "given": "The library holds the copy.",
          "when": "The reader acts on it.",
          "result": "{{result}}",
          "then": "The library answers."
        }
        """;
}
