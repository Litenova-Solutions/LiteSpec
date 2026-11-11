using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The record layers whose every record page carries an explanation file, from
///     <c>explanations.layers</c> in the project file. A project that names none keeps
///     explanation files optional, except the article types, which always require one.
/// </summary>
/// <param name="Layers">The required layers, in declaration order, each intent or behavior, the layers that hold record pages.</param>
/// <follows>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</follows>
public sealed record ExplanationClaims(IReadOnlyList<string> Layers)
{
    /// <summary>Gets the claim of a project file without <c>explanations</c>: no required layer.</summary>
    public static ExplanationClaims None { get; } = new([]);

    /// <summary>The layer names <c>explanations.layers</c> allows.</summary>
    private static readonly HashSet<string> Known = new(StringComparer.Ordinal) { "intent", "behavior" };

    /// <summary>
    ///     Reads the claim from the project file mapping. A file without <c>explanations</c>, or with
    ///     a value of the wrong shape, reads as <see cref="None" />; the project schema reports a value
    ///     of the wrong shape, so this reader never reports one. Unknown layer names are dropped here and
    ///     reported by the schema.
    /// </summary>
    /// <param name="root">The project file mapping.</param>
    /// <returns>The claim.</returns>
    public static ExplanationClaims Read(JsonText root)
    {
        if (!root.TryGetProperty("explanations", out var explanations) || explanations.Kind != JsonKind.Object)
        {
            return None;
        }

        if (!explanations.TryGetProperty("layers", out var layers) || layers.Kind != JsonKind.Array)
        {
            return None;
        }

        var found = new List<string>();

        foreach (var entry in layers.Items)
        {
            if (entry.Kind == JsonKind.String
                && entry.GetString() is { } name
                && Known.Contains(name)
                && !found.Contains(name))
            {
                found.Add(name);
            }
        }

        return new ExplanationClaims(found);
    }
}
