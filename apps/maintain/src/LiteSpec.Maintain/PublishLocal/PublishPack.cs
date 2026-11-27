namespace LiteSpec.Maintain.PublishLocal;

/// <summary>One package the publish packs: the project it comes from and the extra settings the pack passes.</summary>
/// <param name="Project">The project the pack builds: the CLI tool project or the analyzers project.</param>
/// <param name="Extra">The extra settings passed to dotnet pack after the shared ones.</param>
internal sealed record PublishPack(PublishPackProject Project, IReadOnlyList<string> Extra);
