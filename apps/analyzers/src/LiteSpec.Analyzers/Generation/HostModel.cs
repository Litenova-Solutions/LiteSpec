using System;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// The compilation facts the generator reads beside the records and the marks: the project layer
/// and whether xUnit v3 is referenced. The layer follows the same rule the layer resolver uses:
/// the declared LiteSpecLayer property first, then the assembly name suffix.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class HostModel : IEquatable<HostModel>
{
    /// <summary>Creates a host model.</summary>
    /// <param name="layer">The project layer.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    public HostModel(Layer layer, bool xunitV3)
    {
        Layer = layer;
        XunitV3 = xunitV3;
    }

    /// <summary>The project layer.</summary>
    public Layer Layer { get; }

    /// <summary>Whether the compilation references xUnit v3.</summary>
    public bool XunitV3 { get; }

    /// <summary>Reads the declared layer property, or empty when the project sets none.</summary>
    /// <param name="options">The analyzer configuration options.</param>
    /// <returns>The declared layer, or empty.</returns>
    public static string DeclaredLayer(AnalyzerConfigOptionsProvider options)
    {
        if (options.GlobalOptions.TryGetValue(LayerResolver.PropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return declared.Trim();
        }

        return string.Empty;
    }

    /// <summary>Creates a host model from the declared layer, the assembly name and the xUnit probe.</summary>
    /// <param name="declared">The declared layer, or empty.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    /// <returns>The host model.</returns>
    public static HostModel Create(string declared, string assemblyName, bool xunitV3)
    {
        var layer = declared.Length == 0
            ? LayerResolver.FromAssemblyName(assemblyName)
            : LayerResolver.Parse(declared);

        return new HostModel(layer, xunitV3);
    }

    /// <inheritdoc />
    public bool Equals(HostModel? other) =>
        other is not null && Layer == other.Layer && XunitV3 == other.XunitV3;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HostModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Layer.GetHashCode();
            hash = hash * 31 + XunitV3.GetHashCode();

            return hash;
        }
    }
}
