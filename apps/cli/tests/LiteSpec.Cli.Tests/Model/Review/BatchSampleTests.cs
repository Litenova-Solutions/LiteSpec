using System.Globalization;
using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Review;

/// <summary>Proves batch approval samples tier 3 from the semantic hash and writes the manifest.</summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
public sealed class BatchSampleTests
{
    static string LoanAggregate(string root) => ReviewPages.Page(
        root, "docs", "behavior", "modules", "lending", "loans", "README.md");

    static readonly string[] LoanRules =
    [
        "rule/loan.has-one-borrower",
        "rule/loan.borrowed-copy-never-leaves-unscanned",
        "rule/loan.extended-before-due",
        "rule/loan.returned-while-active",
    ];

    static void DraftLoanRules(string root)
    {
        foreach (var id in LoanRules)
        {
            ReviewPages.DraftEntry(LoanAggregate(root), id);
        }
    }

    /// <summary>The expected sample: the lowest semantic hashes, the share rounded up, at least one.</summary>
    static IReadOnlyList<string> ExpectedSample(string root, double share)
    {
        var (front, _) = ReviewPages.Read(LoanAggregate(root));

        return front["records"]!.AsArray()
            .OfType<JsonObject>()
            .Where(entry => LoanRules.Contains(entry["id"]!.GetValue<string>(), StringComparer.Ordinal))
            .Select(entry => (
                entry["id"]!.GetValue<string>(),
                Hash: ReviewPages.SemanticHash("rule", entry["id"]!.GetValue<string>(), entry)))
            .OrderBy(pair => ulong.Parse(pair.Hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ThenBy(pair => pair.Item1, StringComparer.Ordinal)
            .Take(Math.Max(1, (int)Math.Ceiling(LoanRules.Length * share)))
            .Select(pair => pair.Item1)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The sample draws the lowest semantic hashes at the project share.</summary>
    /// <proves>litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash</proves>
    [Fact]
    public void Sample_Draws_Lowest_Hashes_At_Share()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftLoanRules(fixture.Path);
        ReviewPages.SetSampleShare(fixture.Path, 0.5);

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-lending-rule.json")))!;

        Assert.Equal(0.5, manifest["sampleShare"]!.GetValue<double>());

        var sample = manifest["sample"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();

        Assert.Equal(2, sample.Count);
        Assert.Equal(ExpectedSample(fixture.Path, 0.5), sample);
    }

    /// <summary>The sample is deterministic across runs.</summary>
    /// <proves>litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash</proves>
    [Fact]
    public void Sample_Deterministic_Across_Runs()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftLoanRules(fixture.Path);

        var first = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--dry-run");
        var second = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--dry-run");

        Assert.Equal(0, first.Status);
        Assert.Equal(0, second.Status);

        var firstSample = first.Output.Split('\n').First(line => line.StartsWith("Sample (", StringComparison.Ordinal));
        var secondSample = second.Output.Split('\n').First(line => line.StartsWith("Sample (", StringComparison.Ordinal));

        Assert.Equal(firstSample, secondSample);
        Assert.Contains("Sample (1 of 4", firstSample);
    }

    /// <summary>The manifest records the sample share the project file names.</summary>
    /// <proves>litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash</proves>
    [Fact]
    public void SampleShare_Read_From_Project_File()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftLoanRules(fixture.Path);
        ReviewPages.SetSampleShare(fixture.Path, 1.0);

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(ReviewPages.Page(
            fixture.Path, "docs", "proof", "approvals", "2026-11-20-lending-rule.json")))!;

        Assert.Equal(1.0, manifest["sampleShare"]!.GetValue<double>());
        Assert.Equal(4, manifest["sample"]!.AsArray().Count);
    }

    /// <summary>A manifest name collision appends -2, then -3.</summary>
    /// <proves>litespec/core/release#write-a-manifest-for-every-batch-approval</proves>
    [Fact]
    public void Manifest_Collision_Appends_Suffix()
    {
        using var fixture = ModelFixture.Prepare("small");

        DraftLoanRules(fixture.Path);

        var folder = ReviewPages.Page(fixture.Path, "docs", "proof", "approvals");

        Directory.CreateDirectory(folder);

        var taken = Path.Combine(folder, "2026-11-20-lending-rule.json");

        File.WriteAllText(taken, "{}\n");

        var result = Cli.Run(
            "model", "approve", "--root", fixture.Path,
            "--batch", "lending/rule", "--by", "lending-team", "--on", "2026-11-20");

        Assert.Equal(0, result.Status);

        var suffixed = Path.Combine(folder, "2026-11-20-lending-rule-2.json");

        Assert.True(File.Exists(suffixed));
        Assert.Equal("{}\n", File.ReadAllText(taken));

        var manifest = (JsonObject)JsonNode.Parse(File.ReadAllText(suffixed))!;

        Assert.Equal("lending/rule", manifest["selector"]!.GetValue<string>());
        Assert.Equal(4, manifest["records"]!.AsArray().Count);
    }
}
