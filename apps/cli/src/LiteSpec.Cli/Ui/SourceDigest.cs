using System.Text;
using System.Text.RegularExpressions;

namespace LiteSpec.Cli.Ui;

/// <summary>
///     The digest a source lock records for one copied primitive: the SHA-256 of its
///     source with uniform line endings and no trailing spaces. The lock check recomputes
///     it to compare, and <c>litespec model render</c> writes it, so both read one function.
/// </summary>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class SourceDigest
{
    /// <summary>A line ending.</summary>
    static readonly Regex Newline = new("\\r\\n?", RegexOptions.Compiled);

    /// <summary>Spaces at a line end.</summary>
    static readonly Regex TrailingSpaces = new("[ \\t]+$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    ///     The digest of one source file, or null when the file cannot be read.
    /// </summary>
    /// <param name="file">The source file.</param>
    /// <returns>The digest in the form <c>sha256:&lt;hex&gt;</c>, or null.</returns>
    public static string? Of(string file)
    {
        try
        {
            return "sha256:" + Convert.ToHexStringLower(Sha256.Hash(Encoding.UTF8.GetBytes(Normalize(File.ReadAllText(file)))));
        }
        catch (Exception cause) when (cause is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    ///     The digest of one component: the digest of its one path, or, for a component copied
    ///     as several files, the SHA-256 of their digests joined by newlines in the recorded order,
    ///     so a change to any of its files changes the component's digest.
    /// </summary>
    /// <param name="fileDigests">The digest of each of the component's files, in recorded order.</param>
    /// <returns>The component digest in the form <c>sha256:&lt;hex&gt;</c>.</returns>
    public static string OfComponent(IReadOnlyList<string> fileDigests) =>
        fileDigests.Count == 1
            ? fileDigests[0]
            : "sha256:" + Convert.ToHexStringLower(Sha256.Hash(Encoding.UTF8.GetBytes(string.Join('\n', fileDigests))));

    /// <summary>
    ///     Source text with uniform line endings and no trailing spaces.
    /// </summary>
    /// <param name="contents">The source text.</param>
    /// <returns>The normalized source.</returns>
    static string Normalize(string contents) =>
        TrailingSpaces.Replace(Newline.Replace(contents, "\n"), "").TrimEnd() + "\n";
}
