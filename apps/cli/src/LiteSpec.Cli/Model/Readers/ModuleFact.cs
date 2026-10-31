namespace LiteSpec.Model.Readers;

/// <summary>One use-case module folder a frontend reader found.</summary>
/// <param name="Folder">The folder, relative to the frontend root with forward slashes.</param>
/// <param name="File">The file holding its mark, relative to the frontend root.</param>
/// <param name="Line">The 1-based line of the mark.</param>
/// <param name="Marks">Every use-case id its <c>litespec</c> constants name.</param>
/// <follows>litespec/frontend/web/structure#mark-each-use-case-module-with-its-use-case</follows>
sealed record ModuleFact(
    string Folder,
    string File,
    int Line,
    IReadOnlyList<string> Marks);
