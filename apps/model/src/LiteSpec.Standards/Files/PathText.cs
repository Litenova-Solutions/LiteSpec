namespace LiteSpec.Standards;

/// <summary>
///     Computes where a file sits inside a root. A finding, a record link and a rule
///     target all show the path this type returns, so each one reads the same.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class PathText
{
    /// <summary>The path of a file or folder relative to a root, written with forward slashes.</summary>
    /// <param name="root">The folder the path is relative to.</param>
    /// <param name="file">The file or folder inside the root.</param>
    /// <returns>The relative path with forward slashes; <c>.</c> for the root itself.</returns>
    public static string Relative(string root, string file) => FileText.Slash(Path.GetRelativePath(root, file));
}
