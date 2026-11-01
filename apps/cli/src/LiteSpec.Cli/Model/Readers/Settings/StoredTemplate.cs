namespace LiteSpec.Model.Readers.Settings;

/// <summary>One stored template: its key, its line, and the placeholders its texts use.</summary>
/// <param name="Key">The template key.</param>
/// <param name="Line">The 1-based line of the key.</param>
/// <param name="Placeholders">Every <c>{{Name}}</c> placeholder in the key's texts.</param>
sealed record StoredTemplate(string Key, int Line, IReadOnlySet<string> Placeholders);
