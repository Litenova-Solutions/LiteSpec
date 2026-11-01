using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Layers;

/// <summary>
///     Finds the layer of a compilation: the <c>LiteSpecLayer</c> MSBuild property when the
///     project sets it, otherwise the assembly name suffix (<c>Shop.Domain</c> is Domain).
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class LayerResolver
{
    /// <summary>The analyzer config key the package's build props make visible.</summary>
    public const string PropertyKey = "build_property.LiteSpecLayer";

    /// <summary>Returns the layer.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <returns>The layer.</returns>
    public static Layer Resolve(Compilation compilation, AnalyzerOptions options)
    {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(PropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return Parse(declared.Trim());
        }

        return FromAssemblyName(compilation.AssemblyName ?? string.Empty);
    }

    /// <summary>Reads a declared layer name; an unknown name opts the project out.</summary>
    /// <param name="value">The declared layer name text, with or without dots.</param>
    /// <returns>The layer.</returns>
    public static Layer Parse(string value)
    {
        switch (value.Replace(".", string.Empty).ToUpperInvariant())
        {
            case "DOMAIN": return Layer.Domain;
            case "APPLICATIONABSTRACTIONS": return Layer.ApplicationAbstractions;
            case "APPLICATION": return Layer.Application;
            case "INFRASTRUCTURE": return Layer.Infrastructure;
            case "WEBAPI": return Layer.WebApi;
            case "WORKER": return Layer.Worker;
            case "TEST": return Layer.Test;
            case "TESTS": return Layer.Test;
            default: return Layer.None;
        }
    }

    /// <summary>Reads the layer from the last segments of an assembly name.</summary>
    /// <param name="assemblyName">The assembly name that names the application project, or null when it has none.</param>
    /// <returns>The layer named by the last assembly name segments, or None when none names one.</returns>
    public static Layer FromAssemblyName(string assemblyName)
    {
        if (EndsWithSegment(assemblyName, "Tests") || EndsWithSegment(assemblyName, "Test")) return Layer.Test;
        if (EndsWithSegment(assemblyName, "Application.Abstractions")) return Layer.ApplicationAbstractions;
        if (EndsWithSegment(assemblyName, "Domain")) return Layer.Domain;
        if (EndsWithSegment(assemblyName, "Application")) return Layer.Application;
        if (EndsWithSegment(assemblyName, "Infrastructure")) return Layer.Infrastructure;
        if (EndsWithSegment(assemblyName, "WebApi")) return Layer.WebApi;
        if (EndsWithSegment(assemblyName, "Worker")) return Layer.Worker;
        return Layer.None;
    }

    /// <summary>Returns true when the ends-with-segment condition holds; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="suffix">The final segment the name must end with.</param>
    /// <returns>True when the ends-with-segment condition holds; otherwise false.</returns>
    private static bool EndsWithSegment(string name, string suffix) =>
        name.Equals(suffix, StringComparison.Ordinal)
        || name.EndsWith("." + suffix, StringComparison.Ordinal);
}
