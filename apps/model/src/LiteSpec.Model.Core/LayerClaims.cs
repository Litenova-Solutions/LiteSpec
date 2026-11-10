using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The record layers a project claims in <c>layers</c>, and the practice each claimed
///     Implementation or Proof layer follows. A provision or finding code applies when its layer
///     is <see cref="RecordLayer.Any" /> or a claimed layer and, for Implementation and Proof, when
///     it ties the layer to the records or the layer follows LiteSpec's practice.
/// </summary>
/// <param name="Intent">Whether the Intent layer is claimed.</param>
/// <param name="Behavior">Whether the Behavior layer is claimed.</param>
/// <param name="Implementation">The Implementation practice, or null when the layer is not claimed.</param>
/// <param name="Proof">The Proof practice, or null when the layer is not claimed.</param>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
public sealed record LayerClaims(bool Intent, bool Behavior, LayerPractice? Implementation, LayerPractice? Proof)
{
    /// <summary>Gets every layer claimed with LiteSpec's practice, which a project file without <c>layers</c> reads as.</summary>
    public static LayerClaims All { get; } = new(true, true, LayerPractice.LiteSpec, LayerPractice.LiteSpec);

    /// <summary>Whether a provision or finding code of a layer applies to this claim.</summary>
    /// <param name="layer">The layer it governs.</param>
    /// <param name="trace">Whether it ties the layer to the records rather than stating LiteSpec's practice for it.</param>
    /// <returns>True when it applies.</returns>
    public bool Covers(RecordLayer layer, bool trace) => layer switch
    {
        RecordLayer.Any => true,
        RecordLayer.Intent => Intent,
        RecordLayer.Behavior => Behavior,
        RecordLayer.Implementation => Implementation is { } practice && (trace || practice == LayerPractice.LiteSpec),
        RecordLayer.Proof => Proof is { } practice && (trace || practice == LayerPractice.LiteSpec),
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Not a record layer."),
    };

    /// <summary>Whether a finding code applies to this claim with the selected extensions.</summary>
    /// <param name="code">The finding code.</param>
    /// <param name="extensions">The extension ids the project selects.</param>
    /// <returns>True when the code applies.</returns>
    public bool Covers(FindingCode code, IReadOnlyCollection<string> extensions) =>
        Covers(code.Layer, code.Trace) && (code.Extension is null || Contains(extensions, code.Extension));

    /// <summary>Whether a finding code applies to this claim with the selected extensions; an unknown code always applies.</summary>
    /// <param name="code">The finding code text.</param>
    /// <param name="extensions">The extension ids the project selects.</param>
    /// <returns>True when the code applies.</returns>
    public bool Covers(string code, IReadOnlyCollection<string> extensions) =>
        !FindingCodes.IsKnown(code) || Covers(FindingCodes.Of(code), extensions);

    /// <summary>Gets the claim as the project file writes it, in layer order, for machine output.</summary>
    /// <returns>Each claimed layer with its practice.</returns>
    public IReadOnlyList<KeyValuePair<string, string>> Entries()
    {
        var entries = new List<KeyValuePair<string, string>>();
        if (Intent)
        {
            entries.Add(new("intent", "litespec"));
        }

        if (Behavior)
        {
            entries.Add(new("behavior", "litespec"));
        }

        if (Implementation is { } implementation)
        {
            entries.Add(new("implementation", NameOf(implementation)));
        }

        if (Proof is { } proof)
        {
            entries.Add(new("proof", NameOf(proof)));
        }

        return entries;
    }

    /// <summary>
    ///     Reads the claim from the project file mapping. A file without <c>layers</c> reads as
    ///     <see cref="All" />; the project schema reports a value of the wrong shape, so this
    ///     reader never reports one and reads an unknown practice as LiteSpec's.
    /// </summary>
    /// <param name="root">The project file mapping.</param>
    /// <returns>The claim.</returns>
    public static LayerClaims Read(JsonText root)
    {
        if (!root.TryGetProperty("layers", out var layers) || layers.Kind != JsonKind.Object)
        {
            return All;
        }

        return new LayerClaims(
            layers.TryGetProperty("intent", out _),
            layers.TryGetProperty("behavior", out _),
            PracticeOf(layers, "implementation"),
            PracticeOf(layers, "proof"));
    }

    /// <summary>The name the project file writes for a practice.</summary>
    /// <param name="practice">The practice.</param>
    /// <returns><c>litespec</c> or <c>own</c>.</returns>
    public static string NameOf(LayerPractice practice) => practice == LayerPractice.Own ? "own" : "litespec";

    /// <summary>The name the project file writes for a layer.</summary>
    /// <param name="layer">The layer.</param>
    /// <returns><c>any</c>, <c>intent</c>, <c>behavior</c>, <c>implementation</c> or <c>proof</c>.</returns>
    public static string NameOf(RecordLayer layer) => layer switch
    {
        RecordLayer.Any => "any",
        RecordLayer.Intent => "intent",
        RecordLayer.Behavior => "behavior",
        RecordLayer.Implementation => "implementation",
        RecordLayer.Proof => "proof",
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Not a record layer."),
    };

    /// <summary>Reads a layer from its name.</summary>
    /// <param name="name">The layer name.</param>
    /// <param name="layer">The layer, or <see cref="RecordLayer.Any" /> when the name is none.</param>
    /// <returns>True when the name is a layer.</returns>
    public static bool TryParse(string? name, out RecordLayer layer)
    {
        layer = name switch
        {
            "intent" => RecordLayer.Intent,
            "behavior" => RecordLayer.Behavior,
            "implementation" => RecordLayer.Implementation,
            "proof" => RecordLayer.Proof,
            _ => RecordLayer.Any,
        };
        return name is "any" or "intent" or "behavior" or "implementation" or "proof";
    }

    /// <summary>The practice a layer claims, or null when the layer is not claimed.</summary>
    /// <param name="layers">The layers mapping.</param>
    /// <param name="layer">The layer name.</param>
    /// <returns>The practice, or null.</returns>
    private static LayerPractice? PracticeOf(JsonText layers, string layer) =>
        !layers.TryGetProperty(layer, out var value) ? null
        : value.GetString() == "own" ? LayerPractice.Own
        : LayerPractice.LiteSpec;

    /// <summary>Whether a list holds a text, by ordinal comparison.</summary>
    /// <param name="values">The list.</param>
    /// <param name="value">The text.</param>
    /// <returns>True when the list holds the text.</returns>
    private static bool Contains(IReadOnlyCollection<string> values, string value)
    {
        foreach (var item in values)
        {
            if (string.Equals(item, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
