namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     One browser frontend the consumer declares.
/// </summary>
/// <param name="Name">The declared name.</param>
/// <param name="Path">The absolute path.</param>
/// <param name="Platform">The declared platform.</param>
/// <param name="RouteSuite">The absolute path of the route suite, or null when the frontend declares none.</param>
/// <param name="VisualGoldens">The absolute path of the visual goldens folder, or null when the frontend declares none.</param>
/// <param name="EntryPoint">The entry point id the frontend serves, or null to derive it from its screens.</param>
/// <param name="Client">The generated API client's contract and folder, or null when the frontend declares none.</param>
sealed record FrontendConfig(
    string Name,
    string Path,
    string Platform,
    string? RouteSuite = null,
    string? VisualGoldens = null,
    string? EntryPoint = null,
    FrontendClientConfig? Client = null)
{
    /// <summary>Gets the routes root the Svelte reader reads.</summary>
    public string RoutesRoot => System.IO.Path.Combine(Path, "src", "routes");

    /// <summary>Gets the use-case modules root the Svelte reader reads.</summary>
    public string ModulesRoot => System.IO.Path.Combine(Path, "src", "lib", "modules");

    /// <summary>Gets the frame folder holding the generated records file.</summary>
    public string FrameRoot => System.IO.Path.Combine(Path, "src", "lib", "app");
}
