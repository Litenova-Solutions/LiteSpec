using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Model.Marks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves the builder extracts the new links, derives results in the written form and exports follows.</summary>
public sealed class LinkExportTests
{
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Feature_Advancing_A_Goal_Passes_Link_Types()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/shorter-queues", Path.Combine(temp.Path, "goal.md"), """{"statement": "Readers wait less.", "measure": "Median wait", "target": "Under two days", "dueBy": "2027-01-01"}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "feature.md"), """{"lifecycle": "shipped", "solves": ["problem/long-waits"], "advances": ["goal/shorter-queues"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "invalid-field" && finding.Message.Contains("advances", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Feature_Advancing_A_Problem_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/long-waits", Path.Combine(temp.Path, "problem.md"), """{"affects": ["actor/reader"], "statement": "Readers wait weeks."}"""),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "feature.md"), """{"lifecycle": "shipped", "solves": ["problem/long-waits"], "advances": ["problem/long-waits"]}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "invalid-field" && finding.Message.Contains("links advances to 'problem/long-waits', which is a problem", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/behavior#name-the-use-cases-that-read-each-setting</proves>
    [Fact]
    public void Setting_ReadBy_Is_Exported_As_A_Link()
    {
        using var temp = ModelFixture.Prepare("large");
        Write(temp.Path, "docs/operations/reading.md", SettingPage);

        using var document = Build(temp.Path);
        var setting = document.RootElement.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == "setting/lending.loan-days");

        Assert.Contains(
            setting.GetProperty("links").GetProperty("readBy").EnumerateArray().Select(link => link.GetString()),
            id => id == "use-case/lending.register-copy");
    }

    /// <proves>litespec/core/model#derive-generic-refusals</proves>
    [Fact]
    public void Derived_Results_Take_The_Written_Form()
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

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact]
    public void Follows_Marks_Are_Exported_Beside_Implements()
    {
        using var temp = MarksFixture.Prepare("follows");

        using var document = Build(temp.Path);
        var follows = document.RootElement.GetProperty("links").GetProperty("follows").EnumerateArray().ToList();

        Assert.Contains(follows, link =>
            link.GetProperty("to").GetString() == "decision/0002-store-loans-as-documents"
            && link.GetProperty("path").GetString()!.EndsWith(".cs", StringComparison.Ordinal)
            && !Path.IsPathRooted(link.GetProperty("path").GetString()!));
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact]
    public void A_Model_Without_Follows_Marks_Exports_An_Empty_Array()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());

        Assert.Equal(0, document.RootElement.GetProperty("links").GetProperty("follows").GetArrayLength());
    }

    static JsonDocument Build(string root)
    {
        var output = Path.Combine(root, ".model", "model.json");
        var result = Cli.Run("model", "build", "--root", root, "--out", output);
        Assert.Equal(0, result.Status);

        return JsonDocument.Parse(File.ReadAllText(output));
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string SettingPage = """
        ---
        {
          "type": "setting-set",
          "title": "Reading Settings",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-01-01",
          "records": [
            {
              "type": "setting",
              "id": "setting/lending.loan-days",
              "title": "Loan Days",
              "valueType": "integer",
              "default": 21,
              "range": "7..60",
              "unit": "days",
              "setBy": "founder",
              "readBy": ["use-case/lending.register-copy"]
            }
          ]
        }
        ---
        # Reading Settings
        """;
}
