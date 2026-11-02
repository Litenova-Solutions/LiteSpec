using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves transitions mark the states they enter, and the states that start a lifecycle.</summary>
public sealed class ReachabilityTests
{
    /// <summary>Entered states reach, and so does the state the lifecycle starts from.</summary>
    /// <proves>litespec/core/domain-model#connect-every-state-through-transitions</proves>
    [Fact]
    public void Transitions_Mark_Entered_And_Starting_States_Reachable()
    {
        using var temp = ModelFixture.Prepare("small");
        PatchRecord(temp.Path, Aggregate, front =>
        {
            front["transitions"] = new JsonArray(
                new JsonObject
                {
                    ["from"] = "Active",
                    ["to"] = "Overdue",
                    ["by"] = "use-case/lending.extend-loan",
                },
                new JsonObject
                {
                    ["from"] = "Overdue",
                    ["to"] = "Returned",
                    ["by"] = "use-case/lending.return-copy",
                },
                new JsonObject
                {
                    ["from"] = "Active",
                    ["to"] = "Returned",
                    ["by"] = "use-case/lending.return-copy",
                });
        });

        using var document = Build(temp.Path);
        var states = document.RootElement.GetProperty("reachableStates").GetProperty("aggregate/lending.loan");

        Assert.Equal(
            ["Active", "Overdue", "Returned"],
            states.EnumerateArray().Select(state => state.GetString()).ToList());
    }

    /// <summary>An aggregate without transitions reaches no state.</summary>
    /// <proves>litespec/core/domain-model#connect-every-state-through-transitions</proves>
    [Fact]
    public void Aggregate_Without_Transitions_Reaches_No_State()
    {
        using var temp = ModelFixture.Prepare("small");
        PatchRecord(temp.Path, Aggregate, front => front.Remove("transitions"));

        using var document = Build(temp.Path);
        var states = document.RootElement.GetProperty("reachableStates").GetProperty("aggregate/lending.loan");

        Assert.Empty(states.EnumerateArray());
    }

    const string Aggregate = ".litespec/behavior/modules/lending/loans/aggregate.yaml";

    static void PatchRecord(string root, string relative, Action<JsonObject> mutate)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        var node = JsonNode.Parse(YamlText.Parse(File.ReadAllText(path)).ToJson())!.AsObject();

        mutate(node);
        File.WriteAllText(path, RecordFile.Write(node));
    }

    static JsonDocument Build(string root)
    {
        var output = Path.Combine(root, ".litespec/model", "model.json");
        var result = Cli.Run("model", "build", "--root", root, "--out", output);
        Assert.Equal(0, result.Status);

        return JsonDocument.Parse(File.ReadAllText(output));
    }
}
