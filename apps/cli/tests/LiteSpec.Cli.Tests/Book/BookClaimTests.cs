using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Proves the book follows the layers the project claims and shows the computed Intent coverage under every claim.</summary>
public sealed class BookClaimTests
{
    /// <summary>Under intent and behavior alone the book leaves out code, proof and level S findings, and carries no test run.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BookUnderIntentAndBehaviorLeavesOutCodeProofAndLevelSFindings()
    {
        using var temp = TestCli.Temp("book-claim");
        var model = ModelWithLevelSAndLevelGFindings();
        var outDir = Path.Combine(temp.Path, "out");
        BookTestBooks.Write(model, temp.Path, outDir, claims: new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: null));

        var data = Path.Combine(outDir, "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Equal("intent=litespec,behavior=litespec", ClaimsText(manifest));
        Assert.Null(manifest["testRun"]);
        Assert.NotNull(manifest["intentCoverage"]);

        var details = ReadRecords(data);
        var borrow = details["use-case/lending.borrow-copy"];
        Assert.Empty(borrow["impl"]!.AsArray());
        Assert.Empty(borrow["tests"]!.AsArray());
        Assert.False(BoolOf(Obj(borrow, "expect")["impl"]));
        Assert.False(BoolOf(Obj(borrow, "expect")["proof"]));
        var completion = Obj(borrow, "completion");
        Assert.Null(completion["hasCode"]);
        Assert.Null(completion["hasTests"]);
        Assert.Null(completion["scenarios"]);
        Assert.Empty(borrow["lacks"]!.AsArray());

        var scenario = details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"];
        Assert.Null(scenario["proof"]);
        Assert.Null(borrow["machine"]);

        var open = Arr(details["use-case/lending.return-copy"], "findings");
        Assert.Single(open);
        Assert.Equal("use-case-unimplemented", Str(open[0]!.AsObject(), "code"));

        var withS = details.Values
            .SelectMany(detail => Arr(detail, "findings").OfType<JsonObject>())
            .ToList();
        Assert.DoesNotContain(withS, finding => Str(finding, "tier") == "S" || Str(finding, "level") == "S");

        var outlineText = string.Join("\n", Directory.GetFiles(data, "outline-*.js").Select(File.ReadAllText));
        Assert.DoesNotContain("\"kind\":\"code\"", outlineText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"kind\":\"test\"", outlineText, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"finding\"", outlineText, StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(outDir, "index.html"));
        Assert.Contains("Intent coverage:", shell, StringComparison.Ordinal);
    }

    /// <summary>Under own code and proof the book adds code and proof status and the level S findings.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BookUnderOwnCodeAndProofAddsCodeProofAndLevelSFindings()
    {
        AssertCodeAndProofBook(
            new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: LayerPractice.Own),
            "intent=litespec,behavior=litespec,implementation=own,proof=own");
    }

    /// <summary>Under LiteSpec's code and proof the book adds code and proof status and the level S findings.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BookUnderLiteSpecCodeAndProofAddsCodeProofAndLevelSFindings()
    {
        AssertCodeAndProofBook(
            new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.LiteSpec, Proof: LayerPractice.LiteSpec),
            "intent=litespec,behavior=litespec,implementation=litespec,proof=litespec");
    }

    /// <summary>Writes the book under a code and proof claim and checks the claims, the proof and code data, and the level S finding it keeps.</summary>
    /// <param name="claims">The layer claims the book is written under.</param>
    /// <param name="expected">The claims the manifest must list, as layer=practice pairs.</param>
    static void AssertCodeAndProofBook(LayerClaims claims, string expected)
    {
        using var temp = TestCli.Temp("book-claim");
        var model = ModelWithLevelSAndLevelGFindings();
        var outDir = Path.Combine(temp.Path, "out");
        BookTestBooks.Write(model, temp.Path, outDir, claims: claims);

        var data = Path.Combine(outDir, "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Equal(expected, ClaimsText(manifest));
        Assert.NotNull(manifest["intentCoverage"]);

        var details = ReadRecords(data);
        var scenario = details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"];
        Assert.NotNull(scenario["proof"]);

        var findings = details.Values
            .SelectMany(detail => Arr(detail, "findings").OfType<JsonObject>())
            .ToList();
        Assert.Contains(findings, finding => Str(finding, "code") == "reader-missing");

        var outlineText = string.Join("\n", Directory.GetFiles(data, "outline-*.js").Select(File.ReadAllText));
        Assert.Contains("\"kind\":\"code\"", outlineText, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"test\"", outlineText, StringComparison.Ordinal);
    }

    /// <summary>The claims a manifest lists, as layer=practice pairs joined by commas.</summary>
    /// <param name="manifest">The manifest object.</param>
    /// <returns>The claims text.</returns>
    static string ClaimsText(JsonObject manifest) =>
        string.Join(",", manifest["claims"]!.AsArray()
            .Select(entry => $"{entry!["layer"]!.GetValue<string>()}={entry["practice"]!.GetValue<string>()}"));

    /// <summary>Code without proof: the book shows the code, and it reads no test, proof status or missing test for an unclaimed proof layer.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BookUnderCodeWithoutProofLeavesOutProof()
    {
        using var temp = TestCli.Temp("book-claim");
        var outDir = Path.Combine(temp.Path, "out");
        BookTestBooks.Write(ModelWithLevelSAndLevelGFindings(), temp.Path, outDir, claims: new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: null));

        var data = Path.Combine(outDir, "data");
        var details = ReadRecords(data);
        var scenario = details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"];
        Assert.Null(scenario["proof"]);
        var borrow = details["use-case/lending.borrow-copy"];
        Assert.Empty(borrow["tests"]!.AsArray());
        Assert.False(BoolOf(Obj(borrow, "expect")["proof"]));
        Assert.Null(Obj(borrow, "completion")["hasTests"]);
        Assert.DoesNotContain("not tested", string.Join(",", borrow["lacks"]!.AsArray().Select(node => node!.GetValue<string>())), StringComparison.Ordinal);

        var outlineText = string.Join("\n", Directory.GetFiles(data, "outline-*.js").Select(File.ReadAllText));
        Assert.Contains("\"kind\":\"code\"", outlineText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"kind\":\"test\"", outlineText, StringComparison.Ordinal);
    }

    /// <summary>Proof without code: the book shows the tests, and it reads no code or missing code for an unclaimed implementation layer.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void BookUnderProofWithoutCodeLeavesOutCode()
    {
        using var temp = TestCli.Temp("book-claim");
        var outDir = Path.Combine(temp.Path, "out");
        BookTestBooks.Write(ModelWithLevelSAndLevelGFindings(), temp.Path, outDir, claims: new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: LayerPractice.Own));

        var data = Path.Combine(outDir, "data");
        var details = ReadRecords(data);
        var borrow = details["use-case/lending.borrow-copy"];
        Assert.Empty(borrow["impl"]!.AsArray());
        Assert.False(BoolOf(Obj(borrow, "expect")["impl"]));
        Assert.Null(Obj(borrow, "completion")["hasCode"]);
        Assert.DoesNotContain("not coded", string.Join(",", borrow["lacks"]!.AsArray().Select(node => node!.GetValue<string>())), StringComparison.Ordinal);

        var outlineText = string.Join("\n", Directory.GetFiles(data, "outline-*.js").Select(File.ReadAllText));
        Assert.DoesNotContain("\"kind\":\"code\"", outlineText, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"test\"", outlineText, StringComparison.Ordinal);
    }

    /// <summary>The manifest Intent coverage counts each requirement and journey step and the share reached, under every claim.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void IntentCoverageCountsRequirementsAndJourneyStepsAndTheShareReached()
    {
        foreach (var claims in new[]
        {
            new LayerClaims(Intent: true, Behavior: false, Implementation: null, Proof: null),
            new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: null),
            new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: LayerPractice.Own),
            new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.LiteSpec, Proof: LayerPractice.LiteSpec),
        })
        {
            using var temp = TestCli.Temp("book-claim");
            var outDir = Path.Combine(temp.Path, "out");
            BookTestBooks.Write(CoverageModel(), temp.Path, outDir, claims: claims);

            var data = Path.Combine(outDir, "data");
            var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
            var coverage = Obj(manifest, "intentCoverage");
            Assert.Equal(3, coverage["reached"]!.GetValue<int>());
            Assert.Equal(5, coverage["total"]!.GetValue<int>());
            Assert.Equal(0.6, coverage["share"]!.GetValue<double>(), 3);
            Assert.Equal(1, Obj(coverage, "requirements")["reached"]!.GetValue<int>());
            Assert.Equal(2, Obj(coverage, "requirements")["total"]!.GetValue<int>());
            Assert.Equal(2, Obj(coverage, "steps")["reached"]!.GetValue<int>());
            Assert.Equal(3, Obj(coverage, "steps")["total"]!.GetValue<int>());

            var entries = coverage["entries"]!.AsArray().OfType<JsonObject>().ToList();
            Assert.Equal(5, entries.Count);
            var requirement = entries.Single(entry => Str(entry, "id") == "requirement/claims.receipt-kept");
            Assert.Equal("requirement", Str(requirement, "kind"));
            Assert.True(BoolOf(requirement["reached"]));
            Assert.Equal(
                ["rule/claims.receipt-kept-once", "use-case/claims.submit-claim"],
                Arr(requirement, "reachedBy").Select(node => node!.GetValue<string>()).ToList());
            var step = entries.Single(entry => Str(entry, "id") == "journey/submit-a-claim#s3");
            Assert.Equal("step", Str(step, "kind"));
            Assert.False(BoolOf(step["reached"]));
            Assert.Empty(Arr(step, "reachedBy"));

            var details = ReadRecords(data);
            var requirementDetail = Obj(details["requirement/claims.receipt-kept"], "intentCoverage");
            Assert.True(BoolOf(requirementDetail["reached"]));
            var journeyDetail = Obj(details["journey/submit-a-claim"], "intentCoverage");
            Assert.Equal(2, journeyDetail["reached"]!.GetValue<int>());
            Assert.Equal(3, journeyDetail["total"]!.GetValue<int>());
            Assert.Equal(3, Arr(journeyDetail, "steps").Count);
        }
    }

    /// <summary>The Intent coverage computation reads the fulfilling and serving links of a built model.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void IntentCoverageComputeReadsFulfillsAndServes()
    {
        var model = new ProjectModel();
        model.Records.Add(new Record
        {
            Type = RecordTypes.Requirement,
            Id = "requirement/claims.receipt-kept",
            Raw = Raw("{}"),
        });
        model.Records.Add(new Record
        {
            Type = RecordTypes.Journey,
            Id = "journey/submit-a-claim",
            Raw = Raw("""{"steps":[{"id":"s1"},{"id":"s2"}]}"""),
        });
        model.Records.Add(new Record
        {
            Type = RecordTypes.UseCase,
            Id = "use-case/claims.submit-claim",
            Raw = Raw("""{"fulfills":["requirement/claims.receipt-kept"],"serves":["journey/submit-a-claim#s1"]}"""),
        });
        model.Records.Add(new Record
        {
            Type = RecordTypes.Rule,
            Id = "rule/claims.receipt-kept-once",
            Raw = Raw("""{"fulfills":["requirement/claims.receipt-kept"]}"""),
        });

        var coverage = IntentCoverage.Compute(model);

        Assert.Equal(3, coverage.Total);
        Assert.Equal(2, coverage.Reached);
        Assert.Equal(1, coverage.RequirementsReached);
        Assert.Equal(1, coverage.RequirementsTotal);
        Assert.Equal(1, coverage.StepsReached);
        Assert.Equal(2, coverage.StepsTotal);
        Assert.Equal(2d / 3, coverage.Share, 3);
        var requirement = coverage.Entries.Single(entry => entry.Kind == "requirement");
        Assert.True(requirement.Reached);
        Assert.Equal(["rule/claims.receipt-kept-once", "use-case/claims.submit-claim"], requirement.ReachedBy.ToList());
        var unreached = coverage.Entries.Single(entry => entry.Id == "journey/submit-a-claim#s2");
        Assert.False(unreached.Reached);
        Assert.Empty(unreached.ReachedBy);
    }

    /// <summary>An empty model covers no Intent item and the share holds no division by zero.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void IntentCoverageOfNoIntentIsEmpty()
    {
        var coverage = IntentCoverage.Compute(new ProjectModel());
        Assert.Equal(0, coverage.Total);
        Assert.Equal(0, coverage.Reached);
        Assert.Equal(1, coverage.Share);
        Assert.Empty(coverage.Entries);
    }

    static JsonObject ModelWithLevelSAndLevelGFindings()
    {
        var model = BookTruthTests.ModelWithEmbeddedFindings();
        var findings = model["findings"]!.AsArray();
        findings.Add(new JsonObject
        {
            ["code"] = "reader-missing",
            ["tier"] = "S",
            ["message"] = "The reader read no code.",
            ["subject"] = "use-case/lending.borrow-copy",
            ["path"] = ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy.yaml",
            ["line"] = 3,
        });
        return model;
    }

    static JsonObject CoverageModel() => (JsonObject)JsonNode.Parse(
        """
        {
          "records": [
            {"type": "product", "id": "product/claimwick", "title": "Claimwick", "fields": {}, "links": {}},
            {"type": "requirement", "id": "requirement/claims.receipt-kept", "title": "Receipt kept", "fields": {"kind": "functional"}, "links": {}},
            {"type": "requirement", "id": "requirement/claims.paid-once", "title": "Paid once", "fields": {"kind": "functional"}, "links": {}},
            {"type": "journey", "id": "journey/submit-a-claim", "title": "Submit a claim", "fields": {"steps": [{"id": "s1"}, {"id": "s2"}, {"id": "s3"}]}, "links": {}},
            {"type": "use-case", "id": "use-case/claims.submit-claim", "title": "Submit claim", "fields": {"fulfills": ["requirement/claims.receipt-kept"], "serves": ["journey/submit-a-claim#s1", "journey/submit-a-claim#s2"]}, "links": {"fulfills": ["requirement/claims.receipt-kept"], "serves": ["journey/submit-a-claim#s1", "journey/submit-a-claim#s2"]}},
            {"type": "rule", "id": "rule/claims.receipt-kept-once", "title": "Receipt kept once", "fields": {"fulfills": ["requirement/claims.receipt-kept"]}, "links": {"fulfills": ["requirement/claims.receipt-kept"]}}
          ],
          "testRun": null
        }
        """)!;

    static JsonElement Raw(string json) => JsonDocument.Parse(json).RootElement.Clone();

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static Dictionary<string, JsonObject> ReadRecords(string dataDir)
    {
        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dataDir, "records-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            var shard = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in shard)
            {
                details[pair.Key] = pair.Value!.AsObject();
            }
        }

        return details;
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static bool BoolOf(JsonNode? node) =>
        node is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();
}
