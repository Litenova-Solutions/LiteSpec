using System.Globalization;
using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Tables;

/// <summary>Proves the test run writes its date, preferring result timestamps.</summary>
public sealed class TestRunDateTests
{
    /// <summary>A TRX run writes its finish day.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void TrxRunWritesItsFinishDay()
    {
        using var temp = ModelFixture.Prepare("small");
        CopyResults(temp.Path, "trx", "results.trx", "results.trx");

        using var document = Build(temp.Path);

        Assert.Equal("2026-01-01", Run(document).GetProperty("date").GetString());
    }

    /// <summary>A JUnit run writes its suite timestamp day.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void JUnitRunWritesItsSuiteTimestampDay()
    {
        using var temp = ModelFixture.Prepare("small");
        var file = CopyResults(temp.Path, "junit", "results.xml", "results.xml");
        var text = File.ReadAllText(file);
        File.WriteAllText(file, text.Replace("<testsuite name=", "<testsuite timestamp=\"2026-10-20T12:00:00\" name="));

        using var document = Build(temp.Path);

        Assert.Equal("2026-10-20", Run(document).GetProperty("date").GetString());
    }

    /// <summary>A JUnit run without a timestamp writes its file day.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void JUnitRunWithoutATimestampWritesTheFileDay()
    {
        using var temp = ModelFixture.Prepare("small");
        var file = CopyResults(temp.Path, "junit", "results.xml", "results.xml");

        using var document = Build(temp.Path);
        var expected = File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        Assert.Equal(expected, Run(document).GetProperty("date").GetString());
    }

    /// <summary>A combined run writes the latest day across formats.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CombinedRunWritesTheLatestDay()
    {
        using var temp = ModelFixture.Prepare("small");
        CopyResults(temp.Path, "trx", "results.trx", "results.trx");
        var junit = CopyResults(temp.Path, "junit", "results.xml", "results.xml");
        var text = File.ReadAllText(junit);
        File.WriteAllText(junit, text.Replace("<testsuite name=", "<testsuite timestamp=\"2026-10-20T12:00:00\" name="));

        using var document = Build(temp.Path);
        var run = Run(document);

        Assert.Equal(4, run.GetProperty("total").GetInt32());
        Assert.Equal("2026-10-20", run.GetProperty("date").GetString());
    }

    static string CopyResults(string root, string fixture, string name, string target)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, target);
        File.Copy(TestCli.Fixture($"Model/{fixture}/{name}"), destination);

        return destination;
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement Run(JsonDocument document) =>
        document.RootElement.GetProperty("testRun");
}
