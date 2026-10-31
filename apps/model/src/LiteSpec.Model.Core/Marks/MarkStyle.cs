namespace LiteSpec.Model;

/// <summary>
///     The mark style of one language: C# uses <see cref="Xml" /> or
///     <see cref="Attribute" /> as the project file declares, TypeScript uses
///     <see cref="Typed" />, and operations files use <see cref="Comment" />.
/// </summary>
public enum MarkStyle
{
    /// <summary>
    ///     A documentation tag: <c>&lt;implements&gt;</c> or <c>&lt;proves&gt;</c>.
    ///     This is the default when the project file names no style.
    /// </summary>
    Xml = 0,

    /// <summary>
    ///     A generated attribute: <c>Implements</c> or <c>Proves</c>.
    /// </summary>
    Attribute = 1,

    /// <summary>
    ///     A typed constant: the <c>litespec</c> constant a frontend use-case
    ///     module exports, typed against the generated union of identifiers.
    /// </summary>
    Typed = 2,

    /// <summary>
    ///     A hash comment: the <c>&lt;follows&gt;</c> tag in the
    ///     <c>#</c> comments of an operations file.
    /// </summary>
    Comment = 3,
}
