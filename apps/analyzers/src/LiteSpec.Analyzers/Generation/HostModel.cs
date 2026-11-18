using System;
using LiteSpec.Model;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// The compilation facts the generator reads beside the records and the marks: the project layer,
/// whether the compilation is a module project, and whether xUnit v3 is referenced.
/// The layer follows the assembly name suffix: a module project names no layer.
/// The assembly ending .SharedKernel is the shared kernel and generates nothing.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class HostModel : IEquatable<HostModel>
{
    /// <summary>Initializes a new instance of the <see cref="HostModel"/> class.Creates a host model.</summary>
    /// <param name="layer">The project layer.</param>
    /// <param name="moduleProject">Whether the compilation is a module project.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    /// <param name="moduleHost">Whether the compilation is a host beside a module project.</param>
    public HostModel(ArchitectureLayer layer, bool moduleProject, bool xunitV3, bool moduleHost = false)
    {
        ModuleHost = moduleHost;
        Layer = layer;
        ModuleProject = moduleProject;
        XunitV3 = xunitV3;
    }

    /// <summary>Gets the project layer.</summary>
    public ArchitectureLayer Layer { get; }

    /// <summary>Gets a value indicating whether the compilation is a module project.</summary>
    public bool ModuleProject { get; }

    /// <summary>
    ///     Gets a value indicating whether the compilation is a host beside a module
    ///     project: its layer comes from its assembly name, and the module project generates the
    ///     values it uses.
    /// </summary>
    public bool ModuleHost { get; }

    /// <summary>Gets a value indicating whether the compilation references xUnit v3.</summary>
    public bool XunitV3 { get; }

    /// <summary>Creates a host model from the assembly name and the xUnit probe.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="xunitV3">Whether the compilation references xUnit v3.</param>
    /// <returns>The host model.</returns>
    public static HostModel Create(string assemblyName, bool xunitV3)
    {
        var layer = ArchitectureLayerResolver.FromAssemblyName(assemblyName);

        // A module project names no layer and generates the registry, the permissions and
        // the roles. The assembly ending .SharedKernel reads layer None with no module
        // project and no host, so it generates nothing.
        var moduleProject = layer == ArchitectureLayer.None
            && !IsSharedKernelAssembly(assemblyName);

        var moduleHost = layer != ArchitectureLayer.None;

        return new HostModel(layer, moduleProject, xunitV3, moduleHost);
    }

    /// <summary>Returns true when the assembly is the shared kernel; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly name ends with the SharedKernel segment; otherwise false.</returns>
    private static bool IsSharedKernelAssembly(string assemblyName) =>
        assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
        || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal);

    /// <inheritdoc />
    public bool Equals(HostModel? other) =>
        other is not null && Layer == other.Layer
        && ModuleProject == other.ModuleProject && XunitV3 == other.XunitV3 && ModuleHost == other.ModuleHost;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HostModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Layer.GetHashCode();
            hash = hash * 31 + ModuleProject.GetHashCode();
            hash = hash * 31 + XunitV3.GetHashCode();
            hash = hash * 31 + ModuleHost.GetHashCode();

            return hash;
        }
    }
}
