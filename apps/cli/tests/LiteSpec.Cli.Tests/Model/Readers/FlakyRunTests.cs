using LiteSpec.Model;
using LiteSpec.Model.Readers;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Model.Readers.JUnit;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the test run readers record a test that both passed and failed as flaky.</summary>
public sealed class FlakyRunTests
{
    const string Method = "Library.Acceptance.Tests.Flaky.FlakyScenarios.Borrowing_an_available_copy_succeeds";

    /// <summary>Same Test Passed And Failed Across Trx Files Is Flaky.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Same_Test_Passed_And_Failed_Across_Trx_Files_Is_Flaky()
    {
        using var temp = Cli.Temp("reader-flaky");
        WriteProject(temp.Path);
        WriteTrx(temp.Path, "first.trx", Method, "Passed");
        WriteTrx(temp.Path, "second.trx", Method, "Failed");

        var run = new TrxTestRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Contains(Method, run.Flaky);
        Assert.Equal("Failed", run.OutcomeByMethod[Method]);
    }

    /// <summary>Same Test Passed And Failed Across Readers Is Flaky.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Same_Test_Passed_And_Failed_Across_Readers_Is_Flaky()
    {
        using var temp = Cli.Temp("reader-flaky");
        WriteProject(temp.Path);
        WriteTrx(temp.Path, "results.trx", Method, "Passed");
        WriteJUnit(temp.Path);

        var run = new CombinedTestRunReader(new TrxTestRunReader(), new JUnitRunReader())
            .Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Contains(Method, run.Flaky);
        Assert.Equal("Failed", run.OutcomeByMethod[Method]);
    }

    /// <summary>Passed Test Skipped Elsewhere Is Not Flaky.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passed_Test_Skipped_Elsewhere_Is_Not_Flaky()
    {
        using var temp = Cli.Temp("reader-flaky");
        WriteProject(temp.Path);
        WriteTrx(temp.Path, "first.trx", Method, "Passed");
        WriteTrx(temp.Path, "second.trx", Method, "Skipped");

        var run = new TrxTestRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.DoesNotContain(Method, run.Flaky);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(Path.Combine(root, "litespec.project.json"), """{ "owners": ["team"] }""");

    static void WriteTrx(string root, string file, string testName, string outcome)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, file),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<TestRun name=\"flaky run\" xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n"
            + "  <Results>\n"
            + $"    <UnitTestResult testName=\"{testName}\" outcome=\"{outcome}\" />\n"
            + "  </Results>\n"
            + "</TestRun>\n");
    }

    static void WriteJUnit(string root)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "results.xml"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<testsuites>\n"
            + "  <testsuite name=\"flaky\">\n"
            + "    <testcase classname=\"Library.Acceptance.Tests.Flaky.FlakyScenarios\" name=\"Borrowing_an_available_copy_succeeds\">\n"
            + "      <failure />\n"
            + "    </testcase>\n"
            + "  </testsuite>\n"
            + "</testsuites>\n");
    }
}
