namespace LiteSpec.Model.Readers.Settings;

/// <summary>One options class binding keyed settings: its section, properties and marked settings.</summary>
/// <param name="Class">The class name.</param>
/// <param name="File">The absolute path.</param>
/// <param name="Line">The 1-based line of the class name.</param>
/// <param name="SectionName">The <c>SectionName</c> constant value, or null when the class declares none.</param>
/// <param name="Properties">The instance property names.</param>
/// <param name="Settings">The <c>setting/</c> record ids its <c>implements</c> marks name.</param>
sealed record OptionsClass(
    string Class,
    string File,
    int Line,
    string? SectionName,
    IReadOnlyList<string> Properties,
    IReadOnlyList<string> Settings);
