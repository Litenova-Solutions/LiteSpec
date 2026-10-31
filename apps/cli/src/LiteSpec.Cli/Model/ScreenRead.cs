namespace LiteSpec.Model;

/// <summary>What the screen read found: route screens and file comments.</summary>
sealed record ScreenRead(
    IReadOnlyList<ScreenInfo> Screens,
    IReadOnlyList<CodeComment> Comments)
{
    /// <summary>Gets the browser tests every frontend reader found.</summary>
    public IReadOnlyList<BrowserTestInfo> BrowserTests { get; init; } = [];

    /// <summary>Gets findings the readers themselves produced, such as <c>reader-missing</c>.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Gets a value indicating whether whether the TypeScript reader's handshake matched, so it is qualified.</summary>
    public bool TypeScriptQualified { get; init; }
}
