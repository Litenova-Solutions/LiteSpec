using LiteSpec.Model;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// The compilation facts the generator reads beside the records and the marks: the project layer,
/// the backend layout, whether the compilation is the modules project, and
/// whether xUnit v3 is referenced. The layer follows the same rule the layer resolver uses:
/// the declared LiteSpecLayer property first, then the assembly name suffix.
/// The assembly ending .SharedKernel is the shared kernel and generates nothing.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class HostModel : IEquatable<HostModel>
{
    /// <summary>Initializes a new instance of the <see cref="HostModel"/> class.Creates a host model.</summary>
    /// <param name="layer">The project layer.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="applicationProject">Whether the compilation is the modules project.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    /// <param name="moduleHost">Whether the compilation is a host beside the modules project.</param>
    public HostModel(ArchitectureLayer layer, BackendLayout layout, bool applicationProject, bool xunitV3, bool moduleHost = false)
    {
        ModuleHost = moduleHost;
        Layer = layer;
        Layout = layout;
        ApplicationProject = applicationProject;
        XunitV3 = xunitV3;
    }

    /// <summary>Gets the project layer.</summary>
    public ArchitectureLayer Layer { get; }

    /// <summary>Gets the backend layout the consumer builds.</summary>
    public BackendLayout Layout { get; }

    /// <summary>Gets a value indicating whether the compilation is the modules project.</summary>
    public bool ApplicationProject { get; }

    /// <summary>
    ///     Gets a value indicating whether the compilation is a host beside the modules
    ///     project: its layer comes from its assembly name, and the modules project generates the
    ///     values it uses.
    /// </summary>
    public bool ModuleHost { get; }

    /// <summary>Gets a value indicating whether the compilation references xUnit v3.</summary>
    public bool XunitV3 { get; }

    /// <summary>Reads the declared layer property, or empty when the project sets none.</summary>
    /// <param name="options">The analyzer configuration options.</param>
    /// <returns>The declared layer, or empty.</returns>
    public static string DeclaredLayer(AnalyzerConfigOptionsProvider options)
    {
        if (options.GlobalOptions.TryGetValue(ArchitectureLayerResolver.PropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return declared.Trim();
        }

        return string.Empty;
    }

    /// <summary>Reads the declared layout property, or the module layout when the project sets none.</summary>
    /// <param name="options">The analyzer configuration options.</param>
    /// <returns>The declared layout.</returns>
    public static BackendLayout DeclaredLayout(AnalyzerConfigOptionsProvider options)
    {
        if (options.GlobalOptions.TryGetValue(ArchitectureLayerResolver.LayoutPropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return ArchitectureLayerResolver.ParseLayout(declared);
        }

        return BackendLayout.Modules;
    }

    /// <summary>Creates a host model from the declared layer, the layout, the assembly name and the xUnit probe.</summary>
    /// <param name="declared">The declared layer, or empty.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    /// <returns>The host model.</returns>
    public static HostModel Create(string declared, BackendLayout layout, string assemblyName, bool xunitV3)
    {
        var layer = declared.Length == 0
            ? ArchitectureLayerResolver.FromAssemblyName(assemblyName)
            : ArchitectureLayerResolver.Parse(declared);

        // The assembly ending .Modules is the modules project and generates the registry,
        // the permissions and the roles. The assembly ending .SharedKernel reads layer
        // None with no application project and no host, so it generates nothing.
        var applicationProject = layout == BackendLayout.Modules && declared.Length == 0 && layer == ArchitectureLayer.None
            && !IsSharedKernelAssembly(assemblyName);

        var moduleHost = layout == BackendLayout.Modules && declared.Length == 0 && layer != ArchitectureLayer.None;

        return new HostModel(layer, layout, applicationProject, xunitV3, moduleHost);
    }

    /// <summary>Returns true when the assembly is the shared kernel; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly name ends with the SharedKernel segment; otherwise false.</returns>
    private static bool IsSharedKernelAssembly(string assemblyName) =>
        assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
        || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal);

    /// <inheritdoc />
    public bool Equals(HostModel? other) =>
        other is not null && Layer == other.Layer && Layout == other.Layout
        && ApplicationProject == other.ApplicationProject && XunitV3 == other.XunitV3 && ModuleHost == other.ModuleHost;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HostModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Layer.GetHashCode();
            hash = hash * 31 + Layout.GetHashCode();
            hash = hash * 31 + ApplicationProject.GetHashCode();
            hash = hash * 31 + XunitV3.GetHashCode();
            hash = hash * 31 + ModuleHost.GetHashCode();

            return hash;
        }
    }
}
