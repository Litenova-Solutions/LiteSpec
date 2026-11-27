using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Adversarial;

/// <summary>
///     The replay bodies one object probe body runs as: one variant per top-level property with
///     its JSON type swapped, then one variant with a 10,000-character string body.
/// </summary>
/// <follows>litespec/core/backend-application#enforce-target-authorization-in-the-pipeline</follows>
/// <follows>litespec/core/concurrency#store-idempotency-state-atomically</follows>
/// <follows>litespec/core/backend-application#separate-input-validation-from-invariants</follows>
internal static class AdversarialVariants
{
    /// <summary>The length of the oversized string body the input matrix replays once per object body.</summary>
    public const int OversizedLength = 10000;

    /// <summary>Builds the replay variants of one probe body.</summary>
    /// <param name="body">The resolved probe body, or null when the probe sends none.</param>
    /// <returns>One label and body per variant, in replay order: the property swaps, then the oversized body.</returns>
    public static IReadOnlyList<(string Label, string BodyJson)> Build(JsonNode? body)
    {
        if (body is not JsonObject obj)
        {
            return [];
        }

        var variants = new List<(string Label, string BodyJson)>();

        foreach (var (name, value) in obj)
        {
            if (value is null || Swapped(value) is not { } swapped)
            {
                continue;
            }

            var replay = (JsonObject)obj.DeepClone();
            replay[name] = swapped;
            variants.Add((name, replay.ToJsonString()));
        }

        variants.Add(("10000-character body", JsonValue.Create(new string('x', OversizedLength)).ToJsonString()));

        return variants;
    }

    /// <summary>The swapped value one property replays with.</summary>
    /// <param name="value">The original property value.</param>
    /// <returns>The swapped value, or null when the value has no swapped form.</returns>
    private static JsonValue? Swapped(JsonNode value) => value.GetValueKind() switch
    {
        JsonValueKind.String => JsonValue.Create(12345),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => JsonValue.Create("x"),
        JsonValueKind.Object or JsonValueKind.Array => JsonValue.Create("x"),
        _ => null,
    };
}
