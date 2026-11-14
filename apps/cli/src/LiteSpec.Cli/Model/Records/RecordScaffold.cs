namespace LiteSpec.Cli.Model.Records;

/// <summary>The first-line marker that <c>litespec new</c> writes into a record it scaffolds from a template.</summary>
/// <follows>litespec/core/authoring#copy-each-template-from-the-example</follows>
internal static class RecordScaffold
{
    /// <summary>The text that opens the marker line, before the template file name.</summary>
    internal const string Prefix = "# Scaffolded by litespec new from templates/consumer/";

    /// <summary>Gets the marker line that a scaffold of one template file opens with.</summary>
    /// <param name="templateFile">The template file name under <c>templates/consumer/</c>.</param>
    /// <returns>The marker line, without a line break.</returns>
    internal static string LineFor(string templateFile) => $"{Prefix}{templateFile}; replace each value the template copied.";

    /// <summary>Reads the template file name from the marker line a scaffold opens with.</summary>
    /// <param name="text">The record file text.</param>
    /// <returns>The template file name, or null when the file is not a scaffold.</returns>
    internal static string? TemplateOf(string text)
    {
        var firstLine = text.Split('\n', 2)[0].TrimEnd('\r');
        if (!firstLine.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = firstLine[Prefix.Length..];
        var end = rest.IndexOf(';', StringComparison.Ordinal);
        return end > 0 ? rest[..end] : null;
    }
}
