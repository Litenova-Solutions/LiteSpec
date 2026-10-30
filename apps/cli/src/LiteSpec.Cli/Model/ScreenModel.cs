namespace LiteSpec.Model;

/// <summary>One generated operation: its name, method and path.</summary>
sealed record GeneratedOperation(string Name, string Method, string Path);

/// <summary>One read route: its address, file, operations and use cases.</summary>
sealed record ScreenInfo(
    string Frontend,
    string Route,
    string File,
    IReadOnlyList<GeneratedOperation> Operations,
    IReadOnlyList<string> UseCases,
    string? ScreenId = null,
    string Reader = Finding.TypeScriptReader,
    int Line = 1);

/// <summary>One browser test and what it proves, reaches and asserts.</summary>
sealed record BrowserTestInfo(
    string Frontend,
    string Title,
    string File,
    int Line,
    IReadOnlyList<string> Proves,
    string? Journey,
    IReadOnlyList<string> Gotos,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string> Texts,
    string Reader = Finding.TypeScriptReader);

/// <summary>What the screen read found: route screens and file comments.</summary>
sealed record ScreenRead(
    IReadOnlyList<ScreenInfo> Screens,
    IReadOnlyList<CodeComment> Comments)
{
    /// <summary>The browser tests every frontend reader found.</summary>
    public IReadOnlyList<BrowserTestInfo> BrowserTests { get; init; } = [];

    /// <summary>Findings the readers themselves produced, such as <c>reader-unavailable</c>.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Whether the TypeScript reader's handshake matched, so it is qualified.</summary>
    public bool TypeScriptQualified { get; init; }
}
