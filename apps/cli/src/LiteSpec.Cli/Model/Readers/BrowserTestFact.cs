namespace LiteSpec.Model.Readers;

/// <summary>One browser test a frontend reader found.</summary>
/// <param name="File">The test file, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the test.</param>
/// <param name="Title">The test title.</param>
/// <param name="Proves">Every proof target its <c>proves</c> annotations name.</param>
/// <param name="Journey">The journey its <c>journey</c> annotation names, or null.</param>
/// <param name="Gotos">Every <c>page.goto</c> target in the test body.</param>
/// <param name="Codes">Every refusal code its assertions hold.</param>
/// <param name="Texts">Every other asserted text.</param>
/// <follows>litespec/frontend/testing#mark-the-scenario-a-browser-test-proves</follows>
sealed record BrowserTestFact(
    string File,
    int Line,
    string Title,
    IReadOnlyList<string> Proves,
    string? Journey,
    IReadOnlyList<string> Gotos,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string> Texts);
