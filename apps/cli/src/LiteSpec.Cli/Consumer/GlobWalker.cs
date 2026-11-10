using LiteSpec.Model;

namespace LiteSpec.Consumer;

/// <summary>
///     The page scan's file walk: every record file and Markdown file under the documentation root, in a stable order.
/// </summary>
/// <follows>litespec/core/model#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#use-the-declared-page-contract</follows>
static class GlobWalker
{
    /// <summary>The glob includes that collect the record files and the Markdown files.</summary>
    private static readonly string[] PageIncludes = ["**/*" + RecordFile.Extension, "**/*" + RecordFile.ExplanationExtension];

    /// <summary>The glob excludes that leave out generated output, hidden files and the top-level model and book folders.</summary>
    private static readonly string[] PageExcludes =
    [
        "**/node_modules/**/*",
        "**/.*",
        "**/.*/**/*",
        "model/**/*",
        "book/**/*",
    ];

    /// <summary>
    ///     Collects the record files and Markdown files under the documentation root in ordinal
    ///     path order, leaving out the files that are not pages: the glossary, which its own check
    ///     reads, and the project file and pattern catalog, which are YAML under the same root.
    /// </summary>
    /// <param name="docsRoot">The documentation root to walk.</param>
    /// <param name="notPages">The files under the root that are not record or Markdown pages.</param>
    /// <returns>Every record file and Markdown file under the root in ordinal path order.</returns>
    public static IReadOnlyList<string> PageFiles(string docsRoot, IReadOnlySet<string> notPages) =>
    [
        .. SourceTree.Match(docsRoot, PageIncludes, PageExcludes, StringComparison.Ordinal)
            .Where(file => !notPages.Contains(file)),
    ];
}
