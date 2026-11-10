using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Model.Marks;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves the builder extracts the new links, derives results in the written form and exports follows.</summary>
public sealed class LinkExportTests
{
    /// <summary>Feature Advancing A Goal Passes Link Types.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void FeatureAdvancingAGoalPassesLinkTypes()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/shorter-queues", Path.Combine(temp.Path, "goal.yaml"), """{"statement": "Readers wait less.", "measure": "Median wait", "target": "Under two days"}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "feature.yaml"), """{"solves": ["problem/long-waits"], "advances": ["goal/shorter-queues"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "field-invalid" && finding.Message.Contains("advances", StringComparison.Ordinal));
    }

    /// <summary>Feature Advancing A Problem Reports Invalid Field.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void FeatureAdvancingAProblemReportsInvalidField()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/long-waits", Path.Combine(temp.Path, "problem.yaml"), """{"affects": ["actor/reader"], "statement": "Readers wait weeks."}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "feature.yaml"), """{"solves": ["problem/long-waits"], "advances": ["problem/long-waits"]}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "field-invalid" && finding.Message.Contains("links advances to 'problem/long-waits', which is a problem", StringComparison.Ordinal));
    }

    /// <summary>Use Case Reads Setting Is Exported As A Link.</summary>
    /// <proves>litespec/core/use-cases#name-the-settings-each-use-case-reads</proves>
    [Fact]
    public void UseCaseReadsSettingIsExportedAsALink()
    {
        using var temp = ModelFixture.Prepare("large");
        TestRecords.Write(temp.Path, ".litespec/operations/reading.yaml", SettingJson);
        PatchRecord(
            temp.Path,
            ".litespec/behavior/modules/lending/loans/borrow-copy.yaml",
            front => front["reads"] = new JsonArray("setting/lending.loan-days"));

        using var document = Build(temp.Path);
        var records = document.RootElement.GetProperty("records").EnumerateArray().ToList();
        var useCase = records
            .Single(record => record.GetProperty("id").GetString() == "use-case/lending.borrow-copy");
        var setting = records
            .Single(record => record.GetProperty("id").GetString() == "setting/lending.loan-days");

        Assert.Contains(
            useCase.GetProperty("links").GetProperty("reads").EnumerateArray().Select(link => link.GetString()),
            id => id == "setting/lending.loan-days");
        Assert.False(setting.GetProperty("links").TryGetProperty("readBy", out _));
    }

    /// <summary>Derived Results Take The Written Form.</summary>
    /// <proves>litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them</proves>
    [Fact]
    public void DerivedResultsTakeTheWrittenForm()
    {
        using var temp = ModelFixture.Prepare("large");

        using var document = Build(temp.Path);
        var derived = document.RootElement.GetProperty("records").EnumerateArray()
            .Select(record => record.GetProperty("id").GetString() ?? "")
            .Where(id => id.EndsWith(".forbidden", StringComparison.Ordinal) || id.EndsWith(".invalid-input", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(derived);
        Assert.All(derived, id => Assert.Equal(2, id["result/".Length..].Split('.').Length));
    }

    /// <summary>Follows Marks Are Exported Beside Implements.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact]
    public void FollowsMarksAreExportedBesideImplements()
    {
        using var temp = MarksFixture.Prepare("follows");

        using var document = Build(temp.Path);
        var follows = document.RootElement.GetProperty("links").GetProperty("follows").EnumerateArray().ToList();

        Assert.Contains(follows, link =>
            link.GetProperty("to").GetString() == "decision/0002-store-loans-as-documents"
            && link.GetProperty("path").GetString()!.EndsWith(".cs", StringComparison.Ordinal)
            && !Path.IsPathRooted(link.GetProperty("path").GetString()!));
    }

    /// <summary>A Model Without Follows Marks Exports An Empty Array.</summary>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact]
    public void AModelWithoutFollowsMarksExportsAnEmptyArray()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());

        Assert.Equal(0, document.RootElement.GetProperty("links").GetProperty("follows").GetArrayLength());
    }

    /// <summary>Wave5 Objects Export Their Ids As Links.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Wave5ObjectsExportTheirIdsAsLinks()
    {
        Assert.Equal(
            ["use-case/lending.release-hold"],
            ModelBuilder.TargetsOf(Element("""{"mode": "use-case", "useCase": "use-case/lending.release-hold"}""")));
        Assert.Equal(
            ["use-case/lending.release-hold"],
            ModelBuilder.TargetsOf(Element("""{"name": "place", "enforcedBy": {"mode": "use-case", "useCase": "use-case/lending.release-hold"}}""")));
        Assert.Equal(
            ["aggregate/lending.loan"],
            ModelBuilder.TargetsOf(Element("""{"mode": "advisory", "authority": "aggregate/lending.loan"}""")));
        Assert.Equal(
            ["external-system/payout-bank#void"],
            ModelBuilder.TargetsOf(Element("""{"mode": "call", "operation": "external-system/payout-bank#void"}""")));
        Assert.Equal(
            ["integration-event/payout-voided"],
            ModelBuilder.TargetsOf(Element("""{"mode": "event", "event": "integration-event/payout-voided"}""")));
        Assert.Empty(ModelBuilder.TargetsOf(Element("""{"mode": "none", "reason": "money has moved"}""")));
        Assert.Empty(ModelBuilder.TargetsOf(Element("""{"mode": "never", "reason": "the hold never releases"}""")));
        Assert.Empty(ModelBuilder.TargetsOf(Element("""{"mode": "impossible", "reason": "the processor keeps no erasure call"}""")));
    }

    /// <summary>Wave6 Effects Triggers And Steps Export Their Ids As Links.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Wave6EffectsTriggersAndStepsExportTheirIdsAsLinks()
    {
        Assert.Equal(
            ["use-case/lending.release-hold"],
            ModelBuilder.TargetsOf(Element("""{"runs": "use-case/lending.release-hold"}""")));
        Assert.Equal(
            ["integration-event/hold-released"],
            ModelBuilder.TargetsOf(Element("""{"emits": "integration-event/hold-released"}""")));
        Assert.Equal(
            ["event/loan.loan-borrowed"],
            ModelBuilder.TargetsOf(Element("""{"event": "event/loan.loan-borrowed"}""")));
        Assert.Equal(
            ["use-case/lending.release-hold"],
            ModelBuilder.TargetsOf(Element("""{"id": "release", "runs": "use-case/lending.release-hold", "guard": "the hold is expired"}""")));
        Assert.Empty(ModelBuilder.TargetsOf(Element("""{"schedule": {"every": "PT1M"}}""")));
        Assert.Empty(ModelBuilder.TargetsOf(Element("""{"manual": true}""")));
    }

    static JsonElement Element(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static void PatchRecord(string root, string relative, Action<JsonObject> mutate)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        var node = JsonNode.Parse(YamlText.Parse(File.ReadAllText(path)).ToJson())!.AsObject();

        mutate(node);
        File.WriteAllText(path, RecordFile.Write(node));
    }

    const string SettingJson = """
        {
          "type": "setting-set",
          "title": "Reading Settings",
          "records": [
            {
              "type": "setting",
              "id": "setting/lending.loan-days",
              "title": "Loan Days",
              "valueType": "integer",
              "default": 21,
              "range": "7..60",
              "unit": "days",
              "setBy": "product"
            }
          ]
        }
        """;
}
