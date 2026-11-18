namespace LiteSpec.Cli.Model.Readers;

/// <summary>One full-page navigation a route takes: the literal path it sends the browser to.</summary>
/// <param name="Path">The literal path as written, with any query kept for the matcher to strip.</param>
/// <param name="File">The file holding the navigation, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the navigation.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record NavigationFact(string Path, string File, int Line);
