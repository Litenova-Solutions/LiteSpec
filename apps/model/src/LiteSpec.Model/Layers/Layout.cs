namespace LiteSpec.Model;

/// <summary>
///     The backend layout a consumer builds: the module tree in one application
///     project, or the five projects of the layer-projects extension.
/// </summary>
public enum Layout
{
    /// <summary>The module layout: every module, at every layer, in one application project with separate hosts.</summary>
    Modules,
    /// <summary>The layer-projects layout: one project per layer, as the layer-projects extension states.</summary>
    LayerProjects,
}
